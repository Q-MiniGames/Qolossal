using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Builds Assets/Audio/Resources/SfxLibrary.asset from Assets/Audio/SFX/sfx_manifest.json (written by
// Tools/Audio/build_sfx_library.py) and the Qolossal mixer, Assets/Audio/Qolossal.mixer:
//   Master > Music, Effects (Player, Combat, Enemy, World, Pickup, Town, Story), Ambience, UI.
// The effect, ambience and UI groups sit `headroom` dB up, so a quiet take's gain can lift it; the
// music group stays at 0 dB (its tracks are mixed already).
// A rebuild keeps every audition decision (take states, notes, loop choices) and, for a sound that
// already exists, its tuning; mapping facts (bus, event, hook, status, files, gains) are refreshed.
public static class SfxLibraryBuilder
{
    public const string LibraryPath = "Assets/Audio/Resources/SfxLibrary.asset";
    public const string ManifestPath = "Assets/Audio/SFX/sfx_manifest.json";
    public const string MixerPath = "Assets/Audio/Qolossal.mixer";

#pragma warning disable 0649
    [Serializable] class Manifest { public float headroom_db; public SceneRow[] scenes; public string[] low_voice_speakers; public Row[] entries; }
    [Serializable] class SceneRow { public string prefix, ambience, surface; }
    [Serializable] class Row
    {
        public string id, bus, @event, hook, status, prompt;
        public bool loop, positional;
        public float requested_duration_s, volume, pitch_jitter, cooldown;
        public int max_voices;
        public string[] supersedes;
        public TakeRow[] takes;
    }
    [Serializable] class TakeRow { public string source, loop; public float gain_db; public string[] flags; }
#pragma warning restore 0649

    [MenuItem("Qolossal/Audio/Rebuild SFX Library")]
    public static void BuildMenu() => Build(false);

    [MenuItem("Qolossal/Audio/Rebuild SFX Library (reset tuning from manifest)")]
    public static void BuildResetMenu()
    {
        if (EditorUtility.DisplayDialog("Reset SFX tuning", "Replace every sound's volume, pitch, cooldown, voices and position settings with the manifest's? Audition decisions are kept.", "Reset", "Cancel"))
            Build(true);
    }

    /// <summary>Batch entry point: -executeMethod SfxLibraryBuilder.BuildBatch [-sfxResetTuning]</summary>
    public static void BuildBatch() { Build(Environment.GetCommandLineArgs().Contains("-sfxResetTuning")); AssetDatabase.SaveAssets(); }

    public static SfxLibrary Build(bool resetTuning)
    {
        var text = File.Exists(ManifestPath) ? File.ReadAllText(ManifestPath) : null;
        if (text == null) { Debug.LogError("[Sfx] manifest missing: " + ManifestPath); return null; }
        var m = JsonUtility.FromJson<Manifest>(text);

        Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));
        var lib = AssetDatabase.LoadAssetAtPath<SfxLibrary>(LibraryPath);
        if (lib == null) { lib = ScriptableObject.CreateInstance<SfxLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }

        EnsureMixer(lib, m.headroom_db);

        var old = lib.entries.Where(e => e != null && !string.IsNullOrEmpty(e.id)).ToDictionary(e => e.id);
        var fresh = new List<SfxEntry>();
        foreach (var r in m.entries)
        {
            bool existed = old.TryGetValue(r.id, out var e);
            if (!existed) e = new SfxEntry { id = r.id };
            e.bus = (SfxBus)Enum.Parse(typeof(SfxBus), r.bus);
            e.loop = r.loop; e.gameEvent = r.@event; e.hook = r.hook; e.status = r.status; e.prompt = r.prompt;
            e.requestedSeconds = r.requested_duration_s;
            e.supersedes = r.supersedes ?? new string[0];
            if (!existed || resetTuning)
            {
                e.volume = r.volume; e.pitchJitter = r.pitch_jitter; e.cooldown = r.cooldown;
                e.maxVoices = Mathf.Max(1, r.max_voices); e.positional = r.positional;
            }
            var takes = new SfxTake[r.takes.Length];
            for (int i = 0; i < takes.Length; i++)
            {
                var t = r.takes[i];
                var keep = e.takes != null ? e.takes.FirstOrDefault(x => x != null && x.sourcePath == t.source) : null;
                takes[i] = keep ?? new SfxTake();
                takes[i].sourcePath = t.source; takes[i].loopPath = t.loop ?? "";
                takes[i].gainDb = t.gain_db;
                takes[i].flags = t.flags != null ? string.Join("; ", t.flags) : "";
            }
            e.takes = takes;
            fresh.Add(e);
        }
        lib.entries = fresh;
        if (lib.scenes == null || lib.scenes.Count == 0)
            lib.scenes = m.scenes.Select(s => new SfxSceneRule { scenePrefix = s.prefix, ambience = s.ambience, surface = s.surface }).ToList();
        if (m.low_voice_speakers != null && m.low_voice_speakers.Length > 0 && (lib.lowVoiceSpeakers == null || lib.lowVoiceSpeakers.Length == 0))
            lib.lowVoiceSpeakers = m.low_voice_speakers;

        int missing = 0;
        foreach (var e in lib.entries) missing += RefreshClips(lib, e);
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        int inUse = lib.entries.Sum(e => e.clips.Length);
        Debug.Log($"[Sfx] library built: {lib.entries.Count} sounds, {inUse} takes in use, {missing} clips missing, mixer {(lib.mixer != null ? "ok" : "none")} (headroom {lib.headroomDb} dB)");
        return lib;
    }

    /// <summary>The takes in use: the Favorites if any, otherwise every take not Rejected. Returns
    /// how many clips couldn't be loaded.</summary>
    public static int RefreshClips(SfxLibrary lib, SfxEntry e)
    {
        bool anyFavorite = e.takes.Any(t => t.state == SfxTakeState.Favorite);
        var clips = new List<AudioClip>(); var gains = new List<float>(); int missing = 0;
        foreach (var t in e.takes)
        {
            if (anyFavorite ? t.state != SfxTakeState.Favorite : t.state == SfxTakeState.Rejected) continue;
            string path = e.loop && t.useLoopDerivative && !string.IsNullOrEmpty(t.loopPath) ? t.loopPath : t.sourcePath;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { missing++; continue; }
            clips.Add(clip);
            gains.Add(Mathf.Min(1f, Mathf.Pow(10f, (t.gainDb - lib.headroomDb) / 20f)));
        }
        e.clips = clips.ToArray(); e.gains = gains.ToArray();
        return missing;
    }

    // ---------------------------------------------------------------- mixer

    static readonly string[] EffectBuses = { "Player", "Combat", "Enemy", "World", "Pickup", "Town", "Story" };

    static void EnsureMixer(SfxLibrary lib, float headroom)
    {
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            try { mixer = CreateMixer(headroom); }
            catch (Exception ex) { Debug.LogWarning("[Sfx] couldn't create the mixer (" + ex.GetBaseException().Message + "); sounds play unrouted without headroom"); }
        }
        lib.mixer = mixer;
        if (mixer == null) { lib.busGroups = new AudioMixerGroup[0]; lib.musicGroup = null; lib.headroomDb = 0f; return; }
        AudioMixerGroup G(string path) => mixer.FindMatchingGroups(path).FirstOrDefault(g => g.name == path.Split('/').Last());
        var groups = new AudioMixerGroup[Enum.GetValues(typeof(SfxBus)).Length];
        foreach (SfxBus b in Enum.GetValues(typeof(SfxBus)))
            groups[(int)b] = b == SfxBus.Ambience ? G("Master/Ambience") : b == SfxBus.UI ? G("Master/UI") : G("Master/Effects/" + b);
        lib.busGroups = groups;
        lib.musicGroup = G("Master/Music");
        lib.headroomDb = groups.All(g => g != null) ? headroom : 0f;
    }

    // Unity has no public API for authoring mixers; this uses the editor's own AudioMixerController.
    static AudioMixer CreateMixer(float headroom)
    {
        var editor = typeof(Editor).Assembly;
        Type ctlType = editor.GetType("UnityEditor.Audio.AudioMixerController"), groupType = editor.GetType("UnityEditor.Audio.AudioMixerGroupController");
        const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        var ctl = ctlType.GetMethod("CreateMixerControllerAtPath", Any).Invoke(null, new object[] { MixerPath });
        var master = ctlType.GetProperty("masterGroup", Any).GetValue(ctl);
        var snapshot = ctlType.GetProperty("TargetSnapshot", Any)?.GetValue(ctl) ?? ctlType.GetProperty("startSnapshot", Any).GetValue(ctl);
        var newGroup = ctlType.GetMethod("CreateNewGroup", Any);
        var addChild = ctlType.GetMethod("AddChildToParent", Any);
        var setVolume = groupType.GetMethod("SetValueForVolume", Any);
        object Add(string name, object parent, float db)
        {
            var g = newGroup.Invoke(ctl, new object[] { name, false });
            addChild.Invoke(ctl, new[] { g, parent });
            if (db != 0f) setVolume.Invoke(g, new[] { ctl, snapshot, (object)db });
            return g;
        }
        Add("Music", master, 0f);
        var effects = Add("Effects", master, headroom);
        foreach (var b in EffectBuses) Add(b, effects, 0f);
        Add("Ambience", master, headroom);
        Add("UI", master, headroom);
        ctlType.GetMethod("OnSubAssetChanged", Any)?.Invoke(ctl, null);
        EditorUtility.SetDirty((UnityEngine.Object)ctl);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(MixerPath);
        return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Sound effects: one persistent player for the whole game (the music keeps its own, MraMusicPlayer).
// Gameplay code calls Sfx.Play("Sound_ID") at the moment something happens; the library
// (Resources/SfxLibrary) says which takes, how loud, how often and how many at once.
//   - Variation: a random take in use, never the same one twice running, with a little pitch spread.
//   - Spam: a per-sound cooldown and voice limit; a full pool steals the oldest, least important voice.
//   - Position: a positional sound pans gently by where it is on screen and fades out past the edges.
//   - Pause: everything pauses with AudioListener.pause, except the UI bus.
// Story sounds (the reveal and the ending) play only through PlayStory, from their own scenes.
public static class Sfx
{
    public static SfxLibrary Library => SfxPlayer.Library;

    /// <summary>True if the library has takes in use for this sound.</summary>
    public static bool Has(string id) => Library != null && Library.Find(id) is SfxEntry e && e.clips != null && e.clips.Length > 0;

    /// <summary>Plays a sound where something happened in the world (positional sounds pan and fade by it).</summary>
    public static AudioSource Play(string id, Vector2 at, float volume = 1f, float pitch = 1f) =>
        SfxPlayer.Ensure() is SfxPlayer p ? p.Start(id, at, true, volume, pitch, false) : null;

    /// <summary>Plays a sound centred (Qori's own sounds, pickups, UI).</summary>
    public static AudioSource Play(string id, float volume = 1f, float pitch = 1f) =>
        SfxPlayer.Ensure() is SfxPlayer p ? p.Start(id, default, false, volume, pitch, false) : null;

    /// <summary>A story moment's sound (Story bus): only the reveal and the ending call this.</summary>
    public static AudioSource PlayStory(string id) =>
        SfxPlayer.Ensure() is SfxPlayer p ? p.Start(id, default, false, 1f, 1f, true) : null;

    /// <summary>Stops every voice of this sound now playing (a story moment cut short).</summary>
    public static void Stop(string id) { if (SfxPlayer.Instance != null) SfxPlayer.Instance.StopVoices(id); }

    /// <summary>Plays after a short delay in real time (e.g. a thread snagging just after it flies).</summary>
    public static void PlayAfter(string id, float seconds, float volume = 1f)
    {
        var p = SfxPlayer.Ensure(); if (p != null) p.StartCoroutine(p.After(id, seconds, volume));
    }

    /// <summary>The mixer's Music group (the soundtrack's players route there), if the library has a mixer.</summary>
    public static UnityEngine.Audio.AudioMixerGroup MusicGroup => SfxPlayer.Ensure() != null ? Library.musicGroup : null;

    /// <summary>Qori's footstep surface in this scene (the library's scene rules): Moss, Stone, Wood or Water.</summary>
    public static string Surface => SfxPlayer.SceneSurface;

    /// <summary>The ambience now playing (tests read it).</summary>
    public static string Ambience => SfxPlayer.AmbienceId;

    // Diagnostics, read by the play test and the audition window.
    public static int Count(string id) => SfxPlayer.Counts.TryGetValue(id, out int n) ? n : 0;
    public static int Refused(string id) => SfxPlayer.Refusals.TryGetValue(id, out int n) ? n : 0;
    public static IReadOnlyList<string> Log => SfxPlayer.History;
    public static int ActiveVoices(string id) => SfxPlayer.Instance != null ? SfxPlayer.Instance.Voices(id) : 0;
    /// <summary>Seconds (unscaled) since this sound last started; infinity if never.</summary>
    public static float SinceLast(string id) => SfxPlayer.Instance != null ? SfxPlayer.Instance.Since(id) : float.PositiveInfinity;
}

// Player volume settings (saved in PlayerPrefs, separate from the game save). Master is the
// listener's volume; Music scales the soundtrack; Effects and Ambience scale this player's buses.
public static class SfxSettings
{
    static readonly string[] Keys = { "audio.master", "audio.music", "audio.effects", "audio.ambience" };
    static readonly float[] Values = { -1f, -1f, -1f, -1f };

    public static float Master { get => Get(0); set { Set(0, value); AudioListener.volume = Master; } }
    public static float Music { get => Get(1); set => Set(1, value); }
    public static float Effects { get => Get(2); set => Set(2, value); }
    public static float Ambience { get => Get(3); set => Set(3, value); }

    static float Get(int i)
    {
        if (Values[i] < 0f) { try { Values[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(Keys[i], 1f)); } catch { Values[i] = 1f; } }
        return Values[i];
    }
    static void Set(int i, float v) { Values[i] = Mathf.Clamp01(v); try { PlayerPrefs.SetFloat(Keys[i], Values[i]); PlayerPrefs.Save(); } catch { } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { for (int i = 0; i < Values.Length; i++) Values[i] = -1f; }

    /// <summary>Steps a setting down by a fifth, wrapping from silent back to full (a menu button).</summary>
    public static float Step(float v) => v <= .01f ? 1f : Mathf.Max(0f, Mathf.Round((v - .2f) * 10f) / 10f);
    public static string Label(string name, float v) => $"{name}: {Mathf.RoundToInt(v * 100f)}%";
}

public sealed class SfxPlayer : MonoBehaviour
{
    const int PoolSize = 24;
    const float AmbienceFade = 2.5f, SupersedeWindow = .5f;

    public static SfxPlayer Instance { get; private set; }
    public static SfxLibrary Library { get; private set; }
    public static string SceneSurface { get; private set; } = "Moss";
    public static string AmbienceId { get; private set; } = "";
    internal static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
    internal static readonly Dictionary<string, int> Refusals = new Dictionary<string, int>();
    internal static readonly List<string> History = new List<string>();
    static bool searched;

    sealed class Voice { public AudioSource source; public string id; public float started; public SfxBus bus; }
    readonly List<Voice> voices = new List<Voice>();
    readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
    readonly Dictionary<string, float> blockedUntil = new Dictionary<string, float>();
    readonly Dictionary<string, int> lastTake = new Dictionary<string, int>();
    AudioSource ambA, ambB; float ambAGain, ambBGain, ambALevel, ambBLevel, ambDuck = 1f; string ambClipId = "";
    GameObject lastSelected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        Instance = null; Library = null; searched = false; SceneSurface = "Moss"; AmbienceId = "";
        Counts.Clear(); Refusals.Clear(); History.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        AudioListener.volume = SfxSettings.Master;
        var p = Ensure();
        if (p != null) p.SceneChanged(SceneManager.GetActiveScene());
    }

    public static SfxPlayer Ensure()
    {
        if (Instance != null) return Instance;
        if (!Application.isPlaying) return null;
        if (Library == null && !searched) { searched = true; Library = Resources.Load<SfxLibrary>("SfxLibrary"); }
        if (Library == null) return null;
        Instance = new GameObject("Sound Effects").AddComponent<SfxPlayer>();
        DontDestroyOnLoad(Instance.gameObject);
        return Instance;
    }

    void Awake()
    {
        for (int i = 0; i < PoolSize; i++) voices.Add(new Voice { source = NewSource() });
        ambA = NewSource(); ambB = NewSource();
        foreach (var s in new[] { ambA, ambB }) { s.loop = true; s.priority = 32; s.outputAudioMixerGroup = Library.Group(SfxBus.Ambience); }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; if (Instance == this) Instance = null; }

    AudioSource NewSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false; s.spatialBlend = 0f; s.dopplerLevel = 0f; s.volume = 0f;
        return s;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) SceneChanged(scene); }

    void SceneChanged(Scene scene)
    {
        var rule = Library.RuleFor(scene.name);
        SceneSurface = rule != null && !string.IsNullOrEmpty(rule.surface) ? rule.surface : "Moss";
        SetAmbience(rule != null ? rule.ambience : "");
    }

    public static float BusVolume(SfxBus bus) => bus == SfxBus.Ambience ? SfxSettings.Ambience : SfxSettings.Effects;

    // How audible a positional sound at `at` is from the camera (1 on screen, fading to 0 at 1.6x
    // the half-view past the edge), and its pan (gentle: at most half way to a side).
    public static float Reach(Vector2 at, out float pan)
    {
        pan = 0f;
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return 1f;
        float halfH = cam.orthographicSize, halfW = halfH * Mathf.Max(.1f, cam.aspect);
        Vector2 d = at - (Vector2)cam.transform.position;
        pan = Mathf.Clamp(d.x / halfW, -1f, 1f) * .5f;
        float far = Mathf.Max(Mathf.Abs(d.x) / halfW, Mathf.Abs(d.y) / halfH);
        return far <= 1f ? 1f : Mathf.Clamp01(1f - (far - 1f) / .6f);
    }

    static int Rank(SfxBus bus) => bus == SfxBus.Story ? 4 : bus == SfxBus.UI ? 3 : bus == SfxBus.Player || bus == SfxBus.Combat ? 2 : 1;

    public void StopVoices(string id) { foreach (var v in voices) if (v.id == id && v.source.isPlaying) v.source.Stop(); }

    public float Since(string id) => lastPlayed.TryGetValue(id, out float t) ? Time.unscaledTime - t : float.PositiveInfinity;

    public int Voices(string id) { int n = 0; foreach (var v in voices) if (v.id == id && v.source.isPlaying) n++; return n; }

    static void Note(Dictionary<string, int> d, string id) => d[id] = (d.TryGetValue(id, out int n) ? n : 0) + 1;

    internal AudioSource Start(string id, Vector2 at, bool placed, float volume, float pitch, bool story)
    {
        var e = Library.Find(id);
        if (e == null || e.clips == null || e.clips.Length == 0) { Note(Refusals, id ?? "(null)"); return null; }
        if (e.bus == SfxBus.Story && !story) { Debug.LogWarning("[Sfx] story sound " + id + " requested outside its story event"); Note(Refusals, id); return null; }
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(id, out float last) && now - last < e.cooldown) { Note(Refusals, id); return null; }
        if (blockedUntil.TryGetValue(id, out float until) && now < until) { Note(Refusals, id); return null; }
        float pan = 0f, reach = placed && e.positional ? Reach(at, out pan) : 1f;
        if (reach < .02f) { Note(Refusals, id); return null; }

        // A voice: the oldest copy of this sound when it's at its limit; otherwise a free one, or
        // the oldest of the least important sounds playing.
        Voice pick = null; int same = 0;
        foreach (var v in voices) if (v.id == id && v.source.isPlaying) { same++; if (pick == null || v.started < pick.started) pick = v; }
        if (same < e.maxVoices)
        {
            pick = null;
            foreach (var v in voices) if (!v.source.isPlaying) { pick = v; break; }
            if (pick == null)
                foreach (var v in voices)
                    if (Rank(v.bus) <= Rank(e.bus) && (pick == null || Rank(v.bus) < Rank(pick.bus) || Rank(v.bus) == Rank(pick.bus) && v.started < pick.started)) pick = v;
            if (pick == null) { Note(Refusals, id); return null; }
        }

        int take = 0;
        if (e.clips.Length > 1)
        {
            int prev = lastTake.TryGetValue(id, out int p) ? p : -1;
            take = Random.Range(0, e.clips.Length - (prev >= 0 ? 1 : 0));
            if (prev >= 0 && take >= prev) take++;
        }
        lastTake[id] = take;
        float gain = e.gains != null && take < e.gains.Length ? e.gains[take] : 1f;

        var s = pick.source;
        s.Stop();
        s.clip = e.clips[take]; s.loop = false;
        s.volume = Mathf.Clamp01(e.volume * gain * volume * reach * BusVolume(e.bus));
        s.pitch = pitch * (1f + Random.Range(-e.pitchJitter, e.pitchJitter));
        s.panStereo = pan;
        s.outputAudioMixerGroup = Library.Group(e.bus);
        s.ignoreListenerPause = e.bus == SfxBus.UI;
        s.priority = e.bus == SfxBus.Story ? 0 : e.bus == SfxBus.UI ? 16 : Rank(e.bus) == 2 ? 64 : 128;
        s.Play();
        pick.id = id; pick.started = now; pick.bus = e.bus;
        lastPlayed[id] = now;

        if (e.supersedes != null)
            foreach (var other in e.supersedes)
            {
                blockedUntil[other] = now + SupersedeWindow;
                foreach (var v in voices) if (v.id == other && v.source.isPlaying && now - v.started < SupersedeWindow) v.source.Stop();
            }
        Note(Counts, id);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (History.Count >= 512) History.RemoveAt(0);
        History.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00} {1} {2}", now, id, SceneManager.GetActiveScene().name));
#endif
        return s;
    }

    internal IEnumerator After(string id, float seconds, float volume)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Start(id, default, false, volume, 1f, false);
    }

    // ---------------------------------------------------------------- ambience

    /// <summary>Crossfades to a scene's ambience bed (empty: fades to silence). The same one carries on.</summary>
    public void SetAmbience(string id)
    {
        id = id ?? "";
        if (id == AmbienceId && (id.Length == 0 || ambA.isPlaying)) return;
        AmbienceId = id;
        (ambA, ambB) = (ambB, ambA);
        ambBGain = ambAGain; ambBLevel = ambALevel; ambAGain = 0f; ambALevel = 0f;
        ambA.Stop(); ambClipId = "";
        var e = Library.Find(id);
        if (e == null || e.clips == null || e.clips.Length == 0) return;
        int take = Random.Range(0, e.clips.Length);
        ambA.clip = e.clips[take];
        ambA.volume = 0f;
        ambA.time = Random.Range(0f, ambA.clip.length * .9f);   // a fresh place in the bed each visit
        ambA.Play();
        ambClipId = id;
        ambALevel = e.volume * (e.gains != null && take < e.gains.Length ? e.gains[take] : 1f);
    }

    // For tests: the ambience crossfade's two gains, and the incoming bed's source.
    public float AmbienceInGain => ambAGain;
    public float AmbienceOutGain => ambBGain;
    public AudioSource AmbienceSource => ambA;

    void Update()
    {
        // Ambience: crossfade, and step back under the listening tree and the story moments.
        float duck = ListeningSpot.IsSitting ? .45f : MraReveal.IsPlaying || MraEnding.IsPlaying ? .3f : 1f;
        ambDuck = Mathf.MoveTowards(ambDuck, duck, Time.unscaledDeltaTime / 1.5f);
        ambAGain = Mathf.MoveTowards(ambAGain, ambClipId.Length > 0 ? 1f : 0f, Time.unscaledDeltaTime / AmbienceFade);
        ambBGain = Mathf.MoveTowards(ambBGain, 0f, Time.unscaledDeltaTime / AmbienceFade);
        if (ambBGain <= 0f && ambB.isPlaying) ambB.Stop();
        float scale = ambDuck * SfxSettings.Ambience;
        ambA.volume = ambALevel * ambAGain * scale;
        ambB.volume = ambBLevel * ambBGain * scale;

        // UI: moving the selection in any uGUI menu ticks.
        var es = EventSystem.current;
        var selected = es != null ? es.currentSelectedGameObject : null;
        if (selected != lastSelected)
        {
            if (selected != null && lastSelected != null && selected.activeInHierarchy && lastSelected.activeInHierarchy) Start("UI_Move", default, false, 1f, 1f, false);
            lastSelected = selected;
        }
    }
}

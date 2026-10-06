using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// The sound-effect library (Tools/Audio/AUDIO_INTEGRATION_HANDOFF.md): every ElevenLabs sound by its
// Sound ID, its four source takes, and how the game plays it. The Sfx Audition window edits it;
// Qolossal > Audio > Rebuild SFX Library refreshes it from Assets/Audio/SFX/sfx_manifest.json while
// keeping every audition decision. At runtime Sfx plays only `clips`: the takes in use.
public enum SfxBus { Player, Combat, Enemy, World, Pickup, Town, UI, Story, Ambience }

// An audition decision. Unheard (no decision yet) and Candidate takes are both in provisional use
// until a Favorite is chosen; Rejected takes are never played (and stay out of builds), but are kept
// in the project. Whether a take has actually been listened to is SfxTake.heard.
public enum SfxTakeState { Unheard, Candidate, Favorite, Rejected }

[Serializable]
public sealed class SfxTake
{
    [Tooltip("The untouched source WAV (never edited).")] public string sourcePath = "";
    [Tooltip("A seamless-loop derivative made from the source, when its seam needed one; empty: none.")] public string loopPath = "";
    public SfxTakeState state;
    [Tooltip("Loudness match to the bus target, from the source's measured level (dB, at most 0).")] public float gainDb;
    [Tooltip("Off: play the source even where a loop derivative exists.")] public bool useLoopDerivative = true;
    [Tooltip("Played at least once in the audition window.")] public bool heard;
    [Tooltip("The audition notes field: free text.")] public string note = "";
    [Tooltip("Facts from the source audit, e.g. clipping.")] public string flags = "";
}

[Serializable]
public sealed class SfxEntry
{
    public string id = "";
    public SfxBus bus;
    public bool loop;
    [Tooltip("What makes it play.")] public string gameEvent = "";
    [Tooltip("The code that plays it.")] public string hook = "";
    [Tooltip("wired / no hook / future, from the mapping manifest.")] public string status = "";
    [TextArea(2, 4), Tooltip("The ElevenLabs prompt it was generated from.")] public string prompt = "";
    public float requestedSeconds;
    [Range(0f, 1f)] public float volume = .8f;
    [Range(0f, .3f), Tooltip("Random pitch spread, as a fraction (.05 = ±5%).")] public float pitchJitter = .03f;
    [Min(0f), Tooltip("Seconds before the same sound may start again (unscaled time).")] public float cooldown = .05f;
    [Min(1), Tooltip("Most copies of this sound audible at once.")] public int maxVoices = 2;
    [Tooltip("Pans and fades by where it happens on screen; off: centred, always full level.")] public bool positional;
    [Tooltip("Starting this one stops a just-started copy of these (and blocks them briefly), e.g. a Waymark lighting over the plain checkpoint touch.")]
    public string[] supersedes = new string[0];
    public SfxTake[] takes = new SfxTake[0];

    // Runtime: the takes in use (Favorites if there are any, otherwise every take not Rejected),
    // rebuilt by the editor from the take states; the loop derivative replaces a looping source.
    public AudioClip[] clips = new AudioClip[0];
    public float[] gains = new float[0];
}

[Serializable]
public sealed class SfxSceneRule
{
    [Tooltip("Matches scenes whose name starts with this; the longest match wins.")] public string scenePrefix = "";
    [Tooltip("The scene's ambience (an Ambience bus Sound ID); empty: silence.")] public string ambience = "";
    [Tooltip("Qori's footsteps there: Moss, Stone, Wood or Water.")] public string surface = "Moss";
}

[CreateAssetMenu(menuName = "Qolossal/Audio/SFX Library")]
public sealed class SfxLibrary : ScriptableObject
{
    public List<SfxEntry> entries = new List<SfxEntry>();
    public List<SfxSceneRule> scenes = new List<SfxSceneRule>();
    [Tooltip("Speakers whose dialogue uses the low blip (an old voice).")] public string[] lowVoiceSpeakers = { "Grandfather Tallow" };
    public AudioMixer mixer;
    [Tooltip("Mixer groups, indexed by SfxBus; any may be empty.")] public AudioMixerGroup[] busGroups = new AudioMixerGroup[0];
    public AudioMixerGroup musicGroup;
    [Tooltip("The SFX mixer groups' boost (dB), which lets per-take gains lift quiet takes; 0 without a mixer.")] public float headroomDb;

    Dictionary<string, SfxEntry> byId;

    public SfxEntry Find(string id)
    {
        if (byId == null || byId.Count != entries.Count)
        {
            byId = new Dictionary<string, SfxEntry>(StringComparer.Ordinal);
            foreach (var e in entries) if (e != null && !string.IsNullOrEmpty(e.id)) byId[e.id] = e;
        }
        return id != null && byId.TryGetValue(id, out var entry) ? entry : null;
    }

    public AudioMixerGroup Group(SfxBus bus) => busGroups != null && (int)bus < busGroups.Length ? busGroups[(int)bus] : null;

    public SfxSceneRule RuleFor(string scene)
    {
        SfxSceneRule best = null;
        foreach (var r in scenes)
            if (r != null && scene != null && scene.StartsWith(r.scenePrefix, StringComparison.Ordinal) && (best == null || r.scenePrefix.Length > best.scenePrefix.Length))
                best = r;
        return best;
    }

    void OnValidate() => byId = null;
}

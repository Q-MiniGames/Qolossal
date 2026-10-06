using System.Collections.Generic;
using UnityEngine;

// A looping sound attached to something in the world (a knot's hum, a moth's wings, Qori's open
// Glidecap). It fades in while `Playing` and out when not, pans and fades with the camera like a
// positional one-shot, and only the nearest copies of a sound (its library voice limit) are heard:
// a field of hovering moths is two moths, not twelve. Each starts at a random point in its loop so
// neighbours never beat in phase. Silent (and costs nothing) without a library or the sound.
public sealed class SfxEmitter : MonoBehaviour
{
    public string id;
    [Tooltip("Whether it should sound now; it fades to and from this.")] public bool Playing = true;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(.01f)] public float fadeSeconds = .35f;
    [Tooltip("Off: centred (Qori's own loops).")] public bool positional = true;

    static readonly List<SfxEmitter> all = new List<SfxEmitter>();
    static readonly Dictionary<string, List<SfxEmitter>> byId = new Dictionary<string, List<SfxEmitter>>();
    static readonly System.Comparison<SfxEmitter> Louder = (a, b) => (b.Playing ? b.reach : 0f).CompareTo(a.Playing ? a.reach : 0f);
    static int rankedFrame = -1;
    AudioSource source; SfxEntry entry; float gain, level, reach, pan; bool allowed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { all.Clear(); byId.Clear(); rankedFrame = -1; }

    /// <summary>Adds (or finds) an emitter for `id` on `host`.</summary>
    public static SfxEmitter Attach(GameObject host, string id, bool playing = true, bool positional = true, float volume = 1f)
    {
        foreach (var e in host.GetComponents<SfxEmitter>()) if (e.id == id) return e;
        var em = host.AddComponent<SfxEmitter>();
        em.id = id; em.Playing = playing; em.positional = positional; em.volume = volume;
        return em;
    }

    public bool IsAudible => source != null && source.isPlaying && source.volume > .001f;

    void OnEnable() { all.Add(this); }
    void OnDisable() { all.Remove(this); if (source != null) source.Stop(); gain = 0f; }

    void Start()
    {
        if (SfxPlayer.Ensure() == null) return;
        entry = Sfx.Library.Find(id);
        if (entry == null || entry.clips == null || entry.clips.Length == 0) { entry = null; return; }
        int take = Random.Range(0, entry.clips.Length);
        level = entry.volume * (entry.gains != null && take < entry.gains.Length ? entry.gains[take] : 1f);
        source = gameObject.AddComponent<AudioSource>();
        source.clip = entry.clips[take]; source.loop = true; source.playOnAwake = false;
        source.spatialBlend = 0f; source.dopplerLevel = 0f; source.volume = 0f; source.priority = 160;
        source.pitch = 1f + Random.Range(-entry.pitchJitter, entry.pitchJitter);
        source.outputAudioMixerGroup = Sfx.Library.Group(entry.bus);
    }

    // Once a frame: each sound's emitters by how audible they are; the loudest maxVoices may sound.
    static void Rank()
    {
        if (rankedFrame == Time.frameCount) return;
        rankedFrame = Time.frameCount;
        foreach (var list in byId.Values) list.Clear();   // no allocation once warm
        foreach (var e in all)
        {
            if (e.entry == null) continue;
            e.reach = e.positional ? SfxPlayer.Reach(e.transform.position, out e.pan) : 1f;
            if (!byId.TryGetValue(e.id, out var list)) byId[e.id] = list = new List<SfxEmitter>();
            list.Add(e);
        }
        foreach (var list in byId.Values)
        {
            if (list.Count > 1) list.Sort(Louder);
            for (int i = 0; i < list.Count; i++) list[i].allowed = i < list[i].entry.maxVoices;
        }
    }

    void Update()
    {
        if (entry == null || source == null) return;
        Rank();
        bool on = Playing && allowed && reach > .02f;
        gain = Mathf.MoveTowards(gain, on ? 1f : 0f, Time.unscaledDeltaTime / fadeSeconds);
        if (gain > 0f && !source.isPlaying)
        {
            source.time = Random.Range(0f, source.clip.length * .95f);
            source.Play();
        }
        else if (gain <= 0f && source.isPlaying) source.Stop();
        source.volume = level * volume * gain * reach * SfxPlayer.BusVolume(entry.bus);
        source.panStereo = pan;
    }
}

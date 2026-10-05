using UnityEngine;

// A scene's music cue: the track that plays while Qori is here. One persistent player crossfades
// between cues as scenes change (a cave or house that shares its region's track just carries on),
// loops each track with a crossfade over its fade-out, and ducks while the listening tree plays.
public sealed class MraMusic : MonoBehaviour
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = .7f;

    void Start() => MraMusicPlayer.Play(clip, volume);
}

public sealed class MraMusicPlayer : MonoBehaviour
{
    const float SceneFade = 2.5f, LoopFade = 6f;
    static MraMusicPlayer instance;
    AudioSource a, b;   // a: the current track, b: the one fading out
    float level = .7f, fadeIn = 1f, fadeOut;
    float aGain, bGain;

    /// <summary>The track now playing (tests read it).</summary>
    public static AudioClip Current => instance != null && instance.a != null ? instance.a.clip : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => instance = null;

    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (instance == null)
        {
            instance = new GameObject("Music Player").AddComponent<MraMusicPlayer>();
            DontDestroyOnLoad(instance.gameObject);
            instance.a = instance.Source(); instance.b = instance.Source();
        }
        instance.Switch(clip, volume);
    }

    AudioSource Source()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = false; s.spatialBlend = 0f; s.volume = 0f; s.priority = 0;
        return s;
    }

    void Switch(AudioClip clip, float volume)
    {
        level = volume;
        if (a.clip == clip && a.isPlaying) return;   // the same track carries on (a cave in its region)
        Crossfade(clip, SceneFade);
    }

    // The current track becomes the outgoing one; the new one starts from its beginning.
    void Crossfade(AudioClip clip, float seconds)
    {
        (a, b) = (b, a);
        bGain = aGain;
        a.clip = clip; a.time = 0f; a.Play();
        aGain = 0f; fadeIn = 1f / Mathf.Max(.01f, seconds); fadeOut = fadeIn;
    }

    void Update()
    {
        // Loop: as the track nears its end (its own fade-out), it starts again under it.
        if (a.clip != null && a.isPlaying && a.time > a.clip.length - LoopFade) Crossfade(a.clip, LoopFade);
        aGain = Mathf.MoveTowards(aGain, 1f, fadeIn * Time.unscaledDeltaTime);
        bGain = Mathf.MoveTowards(bGain, 0f, fadeOut * Time.unscaledDeltaTime);
        if (bGain <= 0f && b.isPlaying) b.Stop();
        float duck = ListeningSpot.IsSitting ? 0f : 1f;
        duckGain = Mathf.MoveTowards(duckGain, duck, Time.unscaledDeltaTime / 1.5f);
        a.volume = level * aGain * duckGain; b.volume = level * bGain * duckGain;
    }
    float duckGain = 1f;
}

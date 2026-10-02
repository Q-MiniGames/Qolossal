using UnityEngine;

// Freeze frames: the whole game stops for a moment on a big hit, so it lands. The world's time
// slows almost to a stop (not to 0: several scripts read a time scale of 0 as "paused"), on the
// unscaled clock, then resumes. Requests don't add up: a longer one extends the freeze, a shorter
// one inside it is absorbed, and a short cooldown after each freeze keeps a flurry of hits from
// stuttering. The pause menu pauses the freeze too: its countdown waits while the game is paused.
// Durations live here, in one place, for tuning. Players can turn it off (pause menu).
[DefaultExecutionOrder(-900)]
public sealed class HitStop : MonoBehaviour
{
    // The moments that freeze the game, in seconds (Hollow Knight-like: short and sharp).
    public const float QoriHurt = .12f, HeavyHit = .07f, EnemyKilled = .08f, Stagger = .1f, GuardianExposed = .15f, GuardianKilled = .2f;
    const float FrozenScale = .0001f, Cooldown = .08f;
    const string PrefKey = "Qolossal.HitStop";

    static HitStop runner;
    static float endsAt = -1f, cooldownUntil = -1f, restoreScale = 1f, pausedAt = -1f;
    static float Now => Time.realtimeSinceStartup;   // real time, unaffected by the freeze or frame pacing

    public static bool IsFrozen => endsAt > 0f;
    public static int Count { get; private set; }

    public static bool Enabled
    {
        get { try { return PlayerPrefs.GetInt(PrefKey, 1) == 1; } catch { return true; } }
        set { try { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); } catch { } if (!value) Release(); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { runner = null; endsAt = -1f; pausedAt = -1f; cooldownUntil = -1f; restoreScale = 1f; Count = 0; }

    // Freezes the game for `seconds` (real time). Ignored while paused, during the stir
    // cinematics and open windows, and when the player has turned it off.
    public static void Freeze(float seconds)
    {
        if (seconds <= 0f || !Enabled || !Application.isPlaying) return;
        if (GamePauseMenu.IsPaused || StirSequence.IsPlaying || ModalUi.IsOpen || AreaTransition.IsTransitioning) return;
        if (endsAt <= 0f)
        {
            if (Now < cooldownUntil || Time.timeScale <= 0f) return;
            restoreScale = Time.timeScale;
            Time.timeScale = FrozenScale;
            Count++;
            endsAt = Now + seconds;
        }
        else endsAt = Mathf.Max(endsAt, Now + seconds);
        if (runner == null) { runner = new GameObject("Hit Stop").AddComponent<HitStop>(); DontDestroyOnLoad(runner.gameObject); }
    }

    // Ends a freeze at once (turning the option off, leaving a scene).
    static void Release()
    {
        if (endsAt <= 0f) return;
        endsAt = -1f; pausedAt = -1f;
        if (!GamePauseMenu.IsPaused && Time.timeScale == FrozenScale) Time.timeScale = restoreScale;
    }

    void Update()
    {
        if (endsAt <= 0f) return;
        // Paused: the freeze waits, and the time spent paused is added back on.
        if (GamePauseMenu.IsPaused) { if (pausedAt < 0f) pausedAt = Now; return; }
        if (pausedAt >= 0f) { endsAt += Now - pausedAt; pausedAt = -1f; }
        if (Now < endsAt) return;
        endsAt = -1f;
        cooldownUntil = Now + Cooldown;
        if (Time.timeScale == FrozenScale) Time.timeScale = restoreScale;
    }

    void OnDestroy()
    {
        if (runner != this) return;
        Release(); runner = null;
    }
}

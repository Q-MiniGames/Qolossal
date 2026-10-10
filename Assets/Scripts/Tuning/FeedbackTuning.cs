using UnityEngine;

// How hits and big moments feel, in one asset (Resources/Tuning/FeedbackTuning): the whole-game
// freezes (HitStop) and the camera shake. Read when each moment happens, so edits in the Inspector
// during Play mode apply at once and are kept afterwards. Per-weapon hit feedback lives in WeaponTuning.
// The defaults are the values the game shipped with before the asset existed.
[CreateAssetMenu(menuName = "Qolossal/Tuning/Feedback", fileName = "FeedbackTuning")]
public sealed class FeedbackTuning : ScriptableObject
{
    [Header("Hit-stop: whole-game freezes (seconds, real time)")]
    [Range(0f, .5f)] public float qoriHurt = .12f;
    [Tooltip("A mace hit that lands.")] [Range(0f, .5f)] public float heavyHit = .07f;
    [Range(0f, .5f)] public float enemyKilled = .08f;
    [Tooltip("A shield or shell knocked out of its guard.")] [Range(0f, .5f)] public float stagger = .1f;
    [Range(0f, .5f)] public float guardianExposed = .15f;
    [Range(0f, .5f)] public float guardianKilled = .2f;
    [Tooltip("Rest after a freeze before another can start, so a flurry of hits doesn't stutter.")] [Range(0f, .5f)] public float hitStopCooldown = .08f;

    [Header("Camera shake (strength = peak offset in world units of one shake alone)")]
    [Range(0f, .3f)] public float qoriHurtShake = .1f;
    [Range(0f, .3f)] public float guardianSlamShake = .12f;
    [Range(0f, .3f)] public float guardianKilledShake = .2f;
    [Tooltip("The largest offset stacked shakes can reach (world units).")] [Min(.01f)] public float shakeMaxOffset = .3f;
    [Tooltip("How fast shake fades (trauma per second; trauma 1 is the maximum).")] [Min(.01f)] public float shakeDecay = 1.6f;
    [Tooltip("How quickly the shake wanders (noise steps per second).")] [Min(.1f)] public float shakeFrequency = 22f;

    static FeedbackTuning current;
    // The asset in Resources/Tuning, or the defaults if it's missing.
    public static FeedbackTuning Current
    {
        get
        {
            if (current != null) return current;
            current = Resources.Load<FeedbackTuning>("Tuning/FeedbackTuning");
            if (current == null) { current = CreateInstance<FeedbackTuning>(); current.hideFlags = HideFlags.DontSave; }
            return current;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => current = null;
}

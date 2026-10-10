using UnityEngine;

// Adds a small presentation offset after CameraFollow, restoring it before the next follow update.
// Trauma-based: each impulse adds trauma (0..1), which decays steadily; the offset is trauma squared
// times MaxOffset, on smooth noise, so small hits stay subtle and big ones (a guardian's slam) build up.
// A strength keeps its old meaning: the peak offset in world units of one impulse on its own.
// Players can scale or turn it off (pause menu > Options > Screen shake).
[DefaultExecutionOrder(1000),DisallowMultipleComponent]
public sealed class CombatCameraShake : MonoBehaviour
{
    // The shake's shape lives in FeedbackTuning (max offset, decay, frequency).
    static float MaxOffset => FeedbackTuning.Current.shakeMaxOffset;
    static float Decay => FeedbackTuning.Current.shakeDecay;
    static float Frequency => FeedbackTuning.Current.shakeFrequency;
    const string PrefKey = "Qolossal.ScreenShake";

    PlayerCombatFeedback feedback;Vector3 applied;float trauma,seed;

    // 0 (off) to 1 (full), saved with the player's settings.
    static float scale=-1f;
    public static float Scale
    {
        get { if (scale < 0f) { try { scale = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKey, 1f)); } catch { scale = 1f; } } return scale; }
        set { scale = Mathf.Clamp01(value); try { PlayerPrefs.SetFloat(PrefKey, scale); PlayerPrefs.Save(); } catch { } }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => scale = -1f;

    public void Initialize(PlayerCombatFeedback source)
    {
        if(feedback!=null)feedback.OnCameraImpulse-=Impulse;feedback=source;if(feedback!=null)feedback.OnCameraImpulse+=Impulse;
    }
    // A shake from something other than Qori's own blows (a guardian's slam, Qori hurt).
    public static void Kick(float strength){var shake=FindFirstObjectByType<CombatCameraShake>();if(shake!=null)shake.Impulse(default,strength);}

    void Awake() => seed = Random.value * 100f;

    // strength = the peak offset (world units) this impulse would give alone: trauma² x MaxOffset.
    void Impulse(Vector2 point,float strength)
    {
        if (strength <= 0f) return;
        trauma = Mathf.Min(1f, trauma + Mathf.Sqrt(Mathf.Clamp(strength, 0f, MaxOffset) / MaxOffset));
    }

    void Update(){transform.position-=applied;applied=Vector3.zero;if(Time.timeScale<=0)trauma=0;}

    void LateUpdate()
    {
        if (trauma <= 0f) return;
        trauma = Mathf.Max(0f, trauma - Decay * Time.unscaledDeltaTime);
        float amount = trauma * trauma * MaxOffset * Scale;
        if (amount <= 0f) return;
        float t = Time.unscaledTime * Frequency;
        applied = new Vector3(Mathf.PerlinNoise(seed, t) * 2f - 1f, Mathf.PerlinNoise(seed + 37f, t) * 2f - 1f, 0f) * amount;
        transform.position += applied;
    }

    void OnDisable(){transform.position-=applied;applied=Vector3.zero;trauma=0;if(feedback!=null)feedback.OnCameraImpulse-=Impulse;}
}

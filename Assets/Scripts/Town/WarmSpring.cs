using UnityEngine;

// A warm spring that pulses slowly (one of the fair clues: every spring pulses in the same rhythm,
// like a heartbeat). After a quake it beats faster. Pulses the glow's size and brightness.
[DisallowMultipleComponent]
public sealed class WarmSpring : MonoBehaviour
{
    public Transform glow;
    public SpriteRenderer glowSprite;
    public BodyShape glowShape;
    [Tooltip("Seconds per beat while calm, and after a quake.")] public float calmPeriod = 3.2f, stirredPeriod = 2.1f;

    Vector3 baseScale; float phase;

    void Awake() { if (glow != null) baseScale = glow.localScale; SfxEmitter.Attach(gameObject, "Spring_Pulse"); }

    void Update()
    {
        float period = TownState.Quakes > 0 ? stirredPeriod : calmPeriod;
        phase += Time.deltaTime / period;
        // A double beat, like a heart: a strong pulse, then a softer one.
        float t = Mathf.Repeat(phase, 1f);
        float beat = Mathf.Exp(-Mathf.Pow((t - .1f) * 14f, 2f)) + .55f * Mathf.Exp(-Mathf.Pow((t - .3f) * 14f, 2f));
        if (glow != null) glow.localScale = baseScale * (1f + .18f * beat);
        if (glowShape != null) glowShape.SetAlpha(.35f + .65f * beat);
        if (glowSprite != null) { var c = glowSprite.color; c.a = .35f + .65f * beat; glowSprite.color = c; }
    }
}

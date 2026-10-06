using UnityEngine;

// A platform that travels back and forth between its start and `offset`, easing at each end
// and pausing briefly, carrying Qori while he stands on it.
[DisallowMultipleComponent, DefaultExecutionOrder(-50)]
public sealed class MovingPlatform : MonoBehaviour
{
    public Vector2 offset = new Vector2(6f, 0f);
    [Min(.5f)] public float travelSeconds = 3.5f;
    [Min(0f)] public float pauseSeconds = .6f;
    public Collider2D solid;

    Rigidbody2D body;
    Vector2 start;
    float clock;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        start = body.position;
        SfxEmitter.Attach(gameObject, "MovingPlatform_Loop");
    }

    // Moves by velocity so riders (PlayerMovement adds this body's velocity while standing on it)
    // move in the same physics step, with no interpolation mismatch.
    void FixedUpdate()
    {
        clock += Time.fixedDeltaTime;
        float leg = travelSeconds + pauseSeconds, cycle = Mathf.Repeat(clock, 2f * leg);
        float t = cycle < leg ? Mathf.Clamp01(cycle / travelSeconds) : 1f - Mathf.Clamp01((cycle - leg) / travelSeconds);
        Vector2 next = start + offset * (t * t * (3f - 2f * t));
        body.linearVelocity = (next - body.position) / Time.fixedDeltaTime;
    }
}

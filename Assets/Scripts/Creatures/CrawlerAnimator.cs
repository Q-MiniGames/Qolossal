using System.Collections.Generic;
using UnityEngine;

// Drives the Bramble Crawler cutout rig (built by CrawlerRigBuilder) from its GroundCreature:
// a four-beat walk with two-bone IK legs, a body bob, and a telegraph before a charge (head
// lowers, jaw opens, back thorns bristle). All IK runs in the Facing transform's local space,
// so flipping to face left never changes the maths.
[DisallowMultipleComponent]
public sealed class CrawlerAnimator : MonoBehaviour
{
    [SerializeField] Transform facing, body, head, jaw, thorns;
    [SerializeField] Transform[] uppers = new Transform[4], lowers = new Transform[4];   // near hind, near front, far hind, far front
    [SerializeField] float[] upperLengths = new float[4], lowerLengths = new float[4];
    [SerializeField] SpriteRenderer[] renderers;

    [Header("Gait")]
    [Tooltip("Foot travel per step (world units). Longer strides mean fewer, slower steps at the same speed.")]
    [SerializeField] float stride = .38f, lift = .07f, strideSpeedScale = 1f;
    [Tooltip("Stride multiplier while charging (bounding steps instead of faster ones).")]
    [SerializeField] float chargeStride = 1.8f;
    [SerializeField] float bob = .015f;
    [Header("Charge telegraph")]
    [SerializeField] float headDip = -9f, jawOpen = -22f, thornBristle = 1.18f;

    static readonly float[] Offsets = { 0f, .5f, .25f, .75f };
    GroundCreature creature;
    Rigidbody2D rb;
    Vector2[] footRest = new Vector2[4], hipRest = new Vector2[4];
    float[] bendSign = new float[4];
    float[] upperRest = new float[4], lowerRest = new float[4];
    Vector3 bodyRest, thornScaleRest;
    float headRest, jawRest, thornRotRest;
    Color[] baseColors;
    float phase, blend, facingSign = 1f;

    public void Configure(Transform facingPivot, Dictionary<string, Transform> bones, Dictionary<string, float> lengths, SpriteRenderer[] parts)
    {
        facing = facingPivot; renderers = parts;
        body = bones["Body"]; head = bones["Head"]; jaw = bones["Jaw"]; thorns = bones["BackThorns"];
        string[] legs = { "HindNear", "FrontNear", "HindFar", "FrontFar" };
        for (int i = 0; i < 4; i++)
        {
            uppers[i] = bones[legs[i] + "Upper"]; lowers[i] = bones[legs[i] + "Lower"];
            upperLengths[i] = lengths[legs[i] + "Upper"]; lowerLengths[i] = lengths[legs[i] + "Lower"];
        }
    }

    void Awake()
    {
        creature = GetComponentInParent<GroundCreature>();
        rb = GetComponentInParent<Rigidbody2D>();
        if (facing == null || body == null) { enabled = false; return; }
        if (baseColors != null) return;   // rest pose already captured
        bodyRest = body.localPosition;
        headRest = head.localEulerAngles.z; jawRest = jaw.localEulerAngles.z;
        thornRotRest = thorns.localEulerAngles.z; thornScaleRest = thorns.localScale;
        for (int i = 0; i < 4; i++)
        {
            upperRest[i] = uppers[i].localEulerAngles.z; lowerRest[i] = lowers[i].localEulerAngles.z;
            hipRest[i] = facing.InverseTransformPoint(uppers[i].position);
            Vector2 knee = facing.InverseTransformPoint(lowers[i].position);
            footRest[i] = facing.InverseTransformPoint(lowers[i].TransformPoint(new Vector3(lowerLengths[i], 0f, 0f)));
            bendSign[i] = Mathf.Sign(Cross(footRest[i] - hipRest[i], knee - hipRest[i]));
        }
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    void LateUpdate()
    {
        if (creature == null || rb == null) return;
        float dt = Time.deltaTime;
        Vector2 velocity = rb.linearVelocity;
        if (Mathf.Abs(velocity.x) > .05f) facingSign = Mathf.Sign(velocity.x);
        else if (creature.IsTelegraphing) facingSign = creature.Direction;
        facing.localScale = new Vector3(facingSign, 1f, 1f);

        bool walking = creature.IsAlive && Mathf.Abs(velocity.x) > .05f;
        blend = Mathf.MoveTowards(blend, walking ? 1f : 0f, dt * 8f);
        float strideLength = stride * (creature.IsCharging ? chargeStride : 1f);
        if (walking) phase = Mathf.Repeat(phase + dt * Mathf.Abs(velocity.x) * strideSpeedScale / (strideLength / .75f), 1f);
        Color tint = !creature.IsAlive ? new Color(.55f, .55f, .55f, 1f) : creature.IsFlashing ? new Color(1.6f, 1.6f, 1.6f, 1f) : Color.white;
        Pose(phase, blend, creature.Telegraph01, creature.IsCharging, tint);
    }

    // Poses the rig for a gait phase (0-1), walk blend, telegraph amount and charge flag.
    // Public so editor tools can render poses without play mode.
    public void Pose(float gaitPhase, float walkBlend, float tele, bool charging, Color tint)
    {
        float strideLength = stride * (charging ? chargeStride : 1f);
        body.localPosition = bodyRest + new Vector3(0f, bob * Mathf.Sin(gaitPhase * Mathf.PI * 4f) * walkBlend - .04f * tele, 0f);
        float bodyAngle = body.localEulerAngles.z;
        for (int i = 0; i < 4; i++)
        {
            float cycle = Mathf.Repeat(gaitPhase + Offsets[i], 1f);
            float stepX, stepY;
            if (cycle < .75f) { stepX = Mathf.Lerp(strideLength * .5f, -strideLength * .5f, cycle / .75f); stepY = 0f; }
            else
            {
                float t = (cycle - .75f) / .25f;
                stepX = Mathf.Lerp(-strideLength * .5f, strideLength * .5f, t * t * (3f - 2f * t));
                stepY = Mathf.Sin(t * Mathf.PI) * lift;
            }
            Vector2 hip = facing.InverseTransformPoint(uppers[i].position);
            Solve(i, hip, footRest[i] + new Vector2(stepX, stepY) * walkBlend, bodyAngle);
        }
        float bristle = Mathf.Max(tele, charging ? 1f : 0f);
        head.localEulerAngles = new Vector3(0f, 0f, headRest + headDip * tele + 1.5f * Mathf.Sin(gaitPhase * Mathf.PI * 4f + 1f) * walkBlend);
        jaw.localEulerAngles = new Vector3(0f, 0f, jawRest + jawOpen * bristle);
        thorns.localScale = new Vector3(thornScaleRest.x, thornScaleRest.y * Mathf.Lerp(1f, thornBristle, bristle), 1f);
        thorns.localEulerAngles = new Vector3(0f, 0f, thornRotRest + 3f * bristle);
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i] * tint;
    }

    public void CaptureRest() => Awake();

    // Two-bone IK in Facing space, keeping each knee on the side it was painted.
    void Solve(int i, Vector2 hip, Vector2 target, float parentAngle)
    {
        float l1 = upperLengths[i], l2 = lowerLengths[i];
        Vector2 d = target - hip;
        float dist = Mathf.Clamp(d.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, l1 + l2 - 1e-3f);
        float baseAngle = Mathf.Atan2(d.y, d.x);
        float offset = Mathf.Acos(Mathf.Clamp((l1 * l1 + dist * dist - l2 * l2) / (2f * l1 * dist), -1f, 1f));
        float a1 = (baseAngle + bendSign[i] * offset) * Mathf.Rad2Deg;
        Vector2 knee = hip + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * l1;
        Vector2 foot = hip + d.normalized * dist;
        float a2 = Mathf.Atan2(foot.y - knee.y, foot.x - knee.x) * Mathf.Rad2Deg;
        uppers[i].localEulerAngles = new Vector3(0f, 0f, a1 - parentAngle);
        lowers[i].localEulerAngles = new Vector3(0f, 0f, a2 - a1);
    }

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}

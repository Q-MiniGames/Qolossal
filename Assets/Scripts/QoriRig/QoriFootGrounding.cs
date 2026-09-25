using UnityEngine;

// Plants Qori's feet on uneven ground after the Animator has posed him. The clips are authored
// for flat ground at the collider's bottom edge; on a slope each foot is moved by the terrain
// height under it, the hips drop to let the downhill foot reach, and each leg is re-solved with
// two-bone IK. All maths runs in the rig Root's local space, which is never mirrored or sheared.
public sealed class QoriFootGrounding
{
    const float MaxAdjust = .5f;      // world units a foot may move from its authored height
    const float SoleOffset = .756f;   // ankle to sole, Root-local units (rig px / 100)
    const float BlendSeconds = .08f;

    readonly Transform root, body;
    readonly Leg[] legs;
    readonly PlayerMovement movement;
    readonly Collider2D playerCollider;
    readonly int groundMask;
    float weight;
    Vector3 lastBodyWrite, lastBodyShift;   // undo our hip drop if the Animator didn't rewrite Body

    sealed class Leg { public Transform thigh, shin, foot; }

    public QoriFootGrounding(Transform root, PlayerMovement movement)
    {
        this.root = root;
        this.movement = movement;
        playerCollider = movement.GetComponent<Collider2D>();
        groundMask = LayerMask.GetMask("Ground");
        body = Find(root, "Body");
        legs = new[] { MakeLeg("Near"), MakeLeg("Far") };
    }

    public bool Valid => body != null && legs[0] != null && legs[1] != null && playerCollider != null;

    Leg MakeLeg(string side)
    {
        Transform thigh = Find(root, "Thigh" + side), shin = Find(root, "Shin" + side), foot = Find(root, "Foot" + side);
        return thigh != null && shin != null && foot != null ? new Leg { thigh = thigh, shin = shin, foot = foot } : null;
    }

    static Transform Find(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
        return null;
    }

    // enabled: grounded and on his feet (not hanging, climbing or wall sliding).
    public void Apply(bool enabled, float dt)
    {
        if (!Valid) return;
        if (lastBodyShift != Vector3.zero && body.localPosition == lastBodyWrite) body.localPosition -= lastBodyShift;
        lastBodyShift = Vector3.zero;
        weight = Mathf.MoveTowards(weight, enabled ? 1f : 0f, dt / BlendSeconds);
        if (weight <= 0f) return;

        float flatY = playerCollider.bounds.min.y;
        var offsets = new float[2];
        for (int i = 0; i < 2; i++)
        {
            Vector3 sole = root.TransformPoint(root.InverseTransformPoint(legs[i].foot.position) + Vector3.down * SoleOffset);
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(sole.x, flatY + MaxAdjust + .05f), Vector2.down, MaxAdjust * 2f + .1f, groundMask);
            offsets[i] = hit ? Mathf.Clamp(hit.point.y - flatY, -MaxAdjust, MaxAdjust) * weight : 0f;
        }
        if (Mathf.Abs(offsets[0]) < .002f && Mathf.Abs(offsets[1]) < .002f) return;

        // Drop the hips so the lower foot can reach; the other foot bends its knee more.
        float drop = Mathf.Max(0f, -Mathf.Min(offsets[0], offsets[1]));
        Vector3 dropLocal = root.InverseTransformVector(Vector3.down * drop);
        lastBodyShift = body.parent.InverseTransformVector(root.TransformVector(dropLocal));
        body.localPosition += lastBodyShift;
        lastBodyWrite = body.localPosition;
        for (int i = 0; i < 2; i++)
        {
            Vector3 targetWorld = legs[i].foot.position + Vector3.up * (offsets[i] + drop);
            Solve(legs[i], root.InverseTransformPoint(targetWorld));
        }
    }

    // Two-bone IK in Root space, keeping the knee on the side it was authored on and the foot's
    // Root-space angle unchanged.
    void Solve(Leg leg, Vector2 target)
    {
        Vector2 hip = root.InverseTransformPoint(leg.thigh.position);
        Vector2 knee = root.InverseTransformPoint(leg.shin.position);
        Vector2 ankle = root.InverseTransformPoint(leg.foot.position);
        float l1 = (knee - hip).magnitude, l2 = (ankle - knee).magnitude;
        if (l1 < 1e-4f || l2 < 1e-4f) return;
        Vector2 toTarget = target - hip;
        float distance = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, l1 + l2 - 1e-3f);
        float bendSide = Mathf.Sign(Cross(ankle - hip, knee - hip));
        if (bendSide == 0f) bendSide = 1f;
        float baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
        float offset = Mathf.Acos(Mathf.Clamp((l1 * l1 + distance * distance - l2 * l2) / (2f * l1 * distance), -1f, 1f));
        float thighAngle = baseAngle + bendSide * offset;
        Vector2 newKnee = hip + new Vector2(Mathf.Cos(thighAngle), Mathf.Sin(thighAngle)) * l1;
        Vector2 newAnkle = hip + toTarget.normalized * distance;

        float thighDelta = Vector2.SignedAngle(knee - hip, newKnee - hip);
        float shinDelta = Vector2.SignedAngle(ankle - knee, newAnkle - newKnee) - thighDelta;
        Rotate(leg.thigh, thighDelta);
        Rotate(leg.shin, shinDelta);
        Rotate(leg.foot, -(thighDelta + shinDelta));
    }

    // The rig is a flat chain of z rotations under Root with positive scale, so a local z delta
    // equals the same delta in Root space.
    static void Rotate(Transform bone, float degrees) => bone.localRotation *= Quaternion.Euler(0f, 0f, degrees);

    static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}

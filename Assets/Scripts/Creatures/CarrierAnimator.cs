using UnityEngine;

// Seed Carrier (E-02) visuals on the harmless FlyingCreature: flapping leaf wings, a bobbing body,
// the seed pod swinging below, and the grapple-anchor highlight / hit flash on the whole rig.
[DisallowMultipleComponent]
public sealed class CarrierAnimator : MonoBehaviour
{
    CreatureRig rig; ThreadAnchor anchor; Rigidbody2D body;
    float flap, facing = 1f, lastX;

    void Awake()
    {
        rig = GetComponent<CreatureRig>();
        anchor = GetComponentInParent<ThreadAnchor>();
        body = GetComponentInParent<Rigidbody2D>();
        lastX = transform.position.x;
    }

    void LateUpdate()
    {
        float dx = transform.position.x - lastX; lastX = transform.position.x;
        if (Mathf.Abs(dx) > .0005f) facing = Mathf.Sign(dx);
        rig.Face(facing);
        flap += Time.deltaTime * 11f;
        float wing = Mathf.Sin(flap) * 30f;
        rig.Pose("WingFront", wing);
        rig.Pose("WingBack", -wing * .8f);
        rig.Pose("Body", Mathf.Sin(flap * .5f) * 3f, new Vector2(0f, Mathf.Sin(flap) * .015f));
        rig.Pose("SeedPod", Mathf.Sin(flap * .35f) * 9f);
        rig.Pose("Head", Mathf.Sin(flap * .5f + 1.2f) * 2.5f);
        Color tint = anchor != null && anchor.IsHitFlashing ? new Color(1.6f, 1.6f, 1.6f, 1f)
            : anchor != null && anchor.IsHighlighted ? new Color(1.3f, 1.3f, 1.2f, 1f) : Color.white;
        rig.Tint(tint);
    }
}

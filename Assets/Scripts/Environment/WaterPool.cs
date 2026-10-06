using UnityEngine;

// Shallow flowing water that Qori wades through: a body and a surface strip drawn in front of
// him, and ripples at his feet while he moves in it. It doesn't slow or hurt him.
[DisallowMultipleComponent]
public sealed class WaterPool : MonoBehaviour
{
    public Sprite ripple;
    [Tooltip("World y of the water's surface.")] public float surfaceY;

    PlayerMovement wader; float nextRipple;

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;
        if (player == null) return;
        wader = player;
        if (player.LastLandingSpeed > 3f || !player.IsGrounded) { Ripple(player, 1.1f); Sfx.Play("Qori_Step_Water"); }   // splashing in
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() == wader) wader = null;
    }

    void Update()
    {
        if (wader == null || Time.time < nextRipple || Mathf.Abs(wader.ObservedVelocity.x) < 1f) return;
        Ripple(wader, .7f);
    }

    void Ripple(PlayerMovement player, float size)
    {
        nextRipple = Time.time + .22f;
        if (ripple != null) Fx.Pop(ripple, new Vector2(player.transform.position.x, surfaceY), size, .45f, 26);
    }
}

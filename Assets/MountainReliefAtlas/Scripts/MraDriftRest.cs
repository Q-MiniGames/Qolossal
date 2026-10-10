using UnityEngine;

// A safe stone in a floating-platform level (drift level): once Qori stands on it, a fall into the
// drift below brings him back here instead of to the level's start.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraDriftRest : MonoBehaviour
{
    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerStay2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori != null && qori.IsGrounded) qori.SetRespawn(transform.position);
    }
}

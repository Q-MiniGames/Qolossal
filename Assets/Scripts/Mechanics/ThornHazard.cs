using UnityEngine;

// A strip of thorns: touching it costs Qori one heart and bounces him away from it.
[DisallowMultipleComponent]
public sealed class ThornHazard : MonoBehaviour
{
    [Tooltip("Direction Qori is bounced (floor thorns: up; wall thorns: away from the wall).")]
    public Vector2 bounce = new Vector2(0f, 9f);

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) return;
        var health = other.attachedRigidbody.GetComponent<PlayerHealth>();
        if (health != null) health.TakeDamage(transform.position, bounce);
    }
}

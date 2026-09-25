using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class Hazard : MonoBehaviour
{
    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.attachedRigidbody == null) return;
        PlayerMovement player = other.attachedRigidbody.GetComponent<PlayerMovement>();
        if (player != null && player.isActiveAndEnabled)
            player.Respawn();
    }
}

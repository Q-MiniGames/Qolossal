using UnityEngine;

// A sheltered pocket in a wind room: reaching it makes it where a gust returns Qori to (his
// respawn point for this visit; the room's checkpoint is unchanged).
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgShelter : MonoBehaviour
{
    [Tooltip("Where Qori's feet go when a gust sends him back.")] public Vector2 feet;
    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other)
    {
        var q = MraState.QoriOf(other); if (q == null) return;
        var box = q.GetComponent<BoxCollider2D>();
        q.SetRespawn(feet + new Vector2(0f, q.transform.position.y - box.bounds.min.y + .02f));
    }
}

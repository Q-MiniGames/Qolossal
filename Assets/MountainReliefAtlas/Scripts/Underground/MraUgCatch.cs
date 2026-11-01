using UnityEngine;

// A soft catch under a glide crossing: landing on it puts Qori straight back on the launch (no
// heart lost, nothing else reset). It is a strip lying on the floor, so the floor under a failed
// glide is never a walkable way to the far side.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgCatch : MonoBehaviour
{
    [Tooltip("Where Qori's feet go: the launch.")] public Vector2 launchFeet;
    float caughtAt = -10f;
    public int Catches { get; private set; }
    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerStay2D(Collider2D other)
    {
        var q = MraState.QoriOf(other); if (q == null || Time.time - caughtAt < .5f || AreaTransition.IsTransitioning) return;
        caughtAt = Time.time; Catches++;
        var box = q.GetComponent<BoxCollider2D>();
        q.PlaceAt(launchFeet + new Vector2(0f, q.transform.position.y - box.bounds.min.y + .02f), 1f, false);
        Sfx.Play("Qori_Respawn");
        TownHud.Toast("Caught. Back to the launch.", 2f);
    }
}

using UnityEngine;

// A heart sector's arrival floor: saves the sector's visit flag once Qori is standing on it after
// a completed arrival. Seeing a sector through the grilles, or the overview, never counts.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraUgVisit : MonoBehaviour
{
    public string flag;
    PlayerMovement inside;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) { var q = MraState.QoriOf(other); if (q != null) inside = q; }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) inside = null; }

    void Update()
    {
        if (inside == null || AreaTransition.IsTransitioning || !inside.IsGrounded || GameSave.HasFlag(flag)) return;
        GameSave.SetFlag(flag);
    }
}

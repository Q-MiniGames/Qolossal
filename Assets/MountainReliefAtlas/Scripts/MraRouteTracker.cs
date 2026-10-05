using Mra;
using UnityEngine;

// Follows Qori along the region's route for the Chart: which edge he's on and where that lies on
// the relief map, updated as he moves. The edges he has walked are saved (mra:<edge>:seen), so the
// Chart draws only the paths he knows (or a page bought from Scribble).
[DisallowMultipleComponent]
public sealed class MraRouteTracker : MonoBehaviour
{
    public static MraRouteTracker Current { get; private set; }
    public MraRoute.Fix Fix { get; private set; }
    public bool HasFix { get; private set; }
    Region region; Transform qori; float nextAt;
    const float SeenWithin = 4f;

    void Awake() => Current = this;
    void OnDestroy() { if (Current == this) Current = null; }

    void Start()
    {
        region = MraSession.Current != null ? MraSession.Current.Region : null;
        var player = FindAnyObjectByType<PlayerMovement>();
        if (player != null) qori = player.transform;
    }

    void Update()
    {
        if (region == null || qori == null || MraSession.Current == null || MraSession.Current.InChamber) return;
        // Every frame for the marker (cheap: about a hundred segments per region).
        Fix = MraRoute.Locate(region, qori.position, HasFix ? Fix.edge : -1);
        HasFix = Fix.edge >= 0;
        if (!HasFix || Time.time < nextAt) return;
        nextAt = Time.time + .25f;
        if (Fix.distance < SeenWithin) GameSave.SetFlag(MraState.EdgeFlag(region.edges[Fix.edge].id));
    }
}

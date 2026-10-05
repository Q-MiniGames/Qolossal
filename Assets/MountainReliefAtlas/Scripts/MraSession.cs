using Mra;
using UnityEngine;

// One per Mountain Relief Atlas scene (a region or a side chamber). Before anything else wakes it
// binds the town to the saved game (Amber, residents, purchases and shells persist), records the
// visit, gives the pause menu the regional Chart, and keeps the post-reveal descent closed until
// the saved reveal flag exists, however the scene was reached (a direct load included).
[DefaultExecutionOrder(-2000), DisallowMultipleComponent]
public sealed class MraSession : MonoBehaviour
{
    public string regionId;
    [Tooltip("In a side chamber's scene: its chamber id.")] public string chamberId;
    [Tooltip("Where a premature visit to the descent is sent instead: the Summit's reveal ledge.")]
    public string redirectScene = "MRAtlas_MR07_Summit", redirectArrival = "mra:MR07:cp:10";

    public static MraSession Current { get; private set; }
    public Region Region { get; private set; }
    public Chamber Chamber { get; private set; }
    public bool InChamber => Chamber != null;
    public bool Blocked { get; private set; }

    void Awake()
    {
        Current = this;
        var world = World.Load();
        Region = world?.RegionById(regionId);
        Chamber = string.IsNullOrEmpty(chamberId) ? null : world?.ChamberById(chamberId);
        TownState.BindToSave();
        TownHud.Ensure().alwaysShowAmber = regionId == "MR04" && !InChamber;
        Blocked = Region != null && Region.PostReveal && !MraState.Revealed;
        if (!Blocked && Region != null && !InChamber)
        {
            GameSave.SetFlag(MraState.VisitedFlag(regionId));
            TownState.Set("visited:" + regionId);   // Scribble sells pages for visited places
        }
        ChartScreen.OpenReplacement = MraChart.Open;
        MraChart.Ensure();
    }

    void Start()
    {
        if (!Blocked) return;
        // The descent exists only after the reveal: send Qori back to the Summit's ledge.
        TownHud.Toast("The way down isn't open yet.");
        var player = FindAnyObjectByType<PlayerMovement>();
        if (player == null) return;
        if (AreaTransition.CanTravelTo(redirectScene)) AreaTransition.Travel(redirectScene, redirectArrival, player);
        else player.enabled = false;
    }

    void OnDestroy()
    {
        if (Current != this) return;
        Current = null;
        ChartScreen.OpenReplacement = null;
    }
}

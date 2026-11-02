using UnityEngine;

// A side chamber's mouth in a region. Coming close discovers it (the Chart shows it from then on);
// Up goes in. A chamber meant for a relic Qori hasn't found says what it needs, only once he has
// looked into it. The route never depends on a chamber.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraChamberDoor : MonoBehaviour
{
    public string chamberId, chamberName, chamberScene;
    public string arrivalId = "entry";
    [Tooltip("The relic id it's meant for (e.g. glidecap); empty for none.")] public string prerequisite;
    [Tooltip("The prompt's height over the mouth's foot: just above the entrance art.")] public float promptHeight = 2.9f;
    [Tooltip("Opt-in cave threshold: walking inward past this point enters without an Up prompt.")]
    public bool walkThroughEntrance;
    [Tooltip("World X direction into this cave: +1 right, -1 left.")]
    public float walkThroughDirection = 1f;
    bool near; PlayerMovement qori;

    public string DiscoveredFlag => "mra:" + chamberId + ":discovered";
    // A Qvale home (its own interior scene) rather than a cave.
    bool IsHouse => Mra.World.Load()?.ChamberById(chamberId) is Mra.Chamber c && c.IsHouse;
    public bool Ready => string.IsNullOrEmpty(prerequisite) || GameSave.HasRelic(prerequisite);

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var q = MraState.QoriOf(other); if (q == null) return;
        near = true; qori = q;
        if (GameSave.SetFlag(DiscoveredFlag)) { TownHud.Toast("Found: " + chamberName); Sfx.Play("Discovery"); }
    }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    void Update()
    {
        if (!near || MraState.Busy || GamePauseMenu.BlocksGameplayInput) return;
        bool walkingIn = walkThroughEntrance && Ready && qori != null && qori.HasGroundContact &&
            (qori.transform.position.x-transform.position.x)*walkThroughDirection >= .05f &&
            qori.ObservedVelocity.x*walkThroughDirection > .1f;
        if (walkingIn || TownInput.Up()) Enter();
    }

    // Up calls this; so can tests. False while the chamber is out of reach.
    public bool Enter()
    {
        if (AreaTransition.IsTransitioning) return false;
        if (!Ready) { TownHud.Toast($"{chamberName}: you'll need the {MraState.Describe("relic:" + prerequisite)} in there."); return false; }
        if (qori == null) qori = FindAnyObjectByType<PlayerMovement>();
        Sfx.Play(IsHouse ? "Door_Enter" : "Cave_Enter");
        if (!AreaTransition.CanTravelTo(chamberScene)) return false;
        AreaTransition.Travel(chamberScene, arrivalId, qori, "", GetComponent<CaveThresholdPresentation>());
        return true;
    }

    void OnGUI()
    {
        if (near && !MraState.Busy && !(walkThroughEntrance && Ready))
            MraState.Prompt(transform.position + Vector3.up * promptHeight, (Ready ? "▲ Enter " : "▲ Look into ") + chamberName);
    }
}

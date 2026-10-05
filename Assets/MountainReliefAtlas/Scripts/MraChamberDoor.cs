using UnityEngine;

// A side chamber's mouth in a region. Coming close discovers it (the Chart shows it from then on);
// Up goes in. A chamber meant for a relic Qori hasn't found says what it needs, only once he has
// looked into it. The route never depends on a chamber.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraChamberDoor : MonoBehaviour
{
    public string chamberId, chamberName, chamberScene;
    [Tooltip("The relic id it's meant for (e.g. glidecap); empty for none.")] public string prerequisite;
    [Tooltip("The prompt's height over the mouth's foot: just above the entrance art.")] public float promptHeight = 2.9f;
    bool near; PlayerMovement qori;

    public string DiscoveredFlag => "mra:" + chamberId + ":discovered";
    public bool Ready => string.IsNullOrEmpty(prerequisite) || GameSave.HasRelic(prerequisite);

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var q = MraState.QoriOf(other); if (q == null) return;
        near = true; qori = q;
        if (GameSave.SetFlag(DiscoveredFlag)) TownHud.Toast("Found: " + chamberName);
    }
    void OnTriggerExit2D(Collider2D other) { if (MraState.IsQori(other)) near = false; }

    void Update()
    {
        if (near && !MraState.Busy && !GamePauseMenu.BlocksGameplayInput && TownInput.Up()) Enter();
    }

    // Up calls this; so can tests. False while the chamber is out of reach.
    public bool Enter()
    {
        if (AreaTransition.IsTransitioning) return false;
        if (!Ready) { TownHud.Toast($"{chamberName}: you'll need the {MraState.Describe("relic:" + prerequisite)} in there."); return false; }
        if (qori == null) qori = FindAnyObjectByType<PlayerMovement>();
        AreaTransition.Travel(chamberScene, "entry", qori);
        return true;
    }

    void OnGUI()
    {
        if (near && !MraState.Busy) MraState.Prompt(transform.position + Vector3.up * promptHeight, (Ready ? "▲ Enter " : "▲ Look into ") + chamberName);
    }
}

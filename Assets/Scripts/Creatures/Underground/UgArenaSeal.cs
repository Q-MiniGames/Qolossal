using UnityEngine;

// A door of a boss's room: it shuts once the fight is on and Qori is clear of it, and opens when the fight ends.
[DisallowMultipleComponent]
public sealed class UgArenaSeal : MonoBehaviour
{
    public static bool ForceClosed;   // for the fight area check: measures the arena as it stands in a fight
    public BoxCollider2D solid;
    public SpriteRenderer art;
    UndergroundGuardian boss;
    PlayerMovement qori;
    bool closed;

    void Awake()
    {
        var room = GetComponentInParent<UndergroundRoom>();
        if (room != null) boss = room.GetComponentInChildren<UndergroundGuardian>(true);
        Set(false);
    }

    public void Apply() => Set(ForceClosed);

    void Update()
    {
        if (ForceClosed) return;
        if (qori == null) qori = FindAnyObjectByType<PlayerMovement>();
        bool fight = boss != null && boss.gameObject.activeInHierarchy && boss.IsAlive && !boss.Asleep;
        if (fight == closed) return;
        if (fight && qori != null)
        {
            // Never shut on him: wait until he has stepped in.
            var clear = solid.bounds; clear.Expand(new Vector3(2.4f, 2.4f, 100f));
            if (clear.Contains(qori.transform.position)) return;
        }
        Set(fight);
    }

    void Set(bool shut)
    {
        closed = shut;
        if (solid != null) solid.enabled = shut;
        if (art != null) art.enabled = shut;
    }
}

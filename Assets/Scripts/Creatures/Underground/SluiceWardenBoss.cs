using UnityEngine;

// G-03, the Sluice Warden (A08): a great stone crustacean. Phase one sweeps a claw low along the floor (jump it).
// Phase two alternates the sweep with a slam that floods the arena floor for a few seconds (get on the ledges).
// Phase three does both at once. Its valve, on top of its shell, opens after each attack.
public sealed class SluiceWardenBoss : UndergroundGuardian
{
    [SerializeField] float floodSeconds = 2.6f, floodHeight = .9f;
    [Tooltip("The floor the flood covers, from the room's left edge (A08: everything between the door steps).")]
    [SerializeField] float floodFrom = 10f, floodTo = 46f;
    protected override string DesignId => "G-03";
    protected override Vector2 WeakOffset => new Vector2(-1f, 4.6f);
    protected override float WeakRadius => 1.5f;
    protected override Vector2 BodySize => new Vector2(9f, 5f);
    protected override int NextAttack(int n) => Phase == 1 ? 0 : Phase == 2 ? n % 2 : 2;
    protected override string WarnSegment(int a) => "Warn";
    protected override void Awake() { base.Awake(); warnTime = 1.1f; }   // the longest wind-up: its sweep covers so much floor
    protected override void OnStrike(int a)
    {
        if (a == 0) return;
        float x0 = Room.min.x + floodFrom, x1 = Room.min.x + floodTo;
        UgRisingWater.Flood(new Rect(x0, Room.min.y, x1 - x0, floodHeight), floodSeconds / Speed);
    }
    protected override bool StrikeZone(int a, out Vector2 centre, out Vector2 size)
    {
        // Low enough for a plain jump to clear it: Qori's feet stay above 1.2 u for about .55 s.
        size = new Vector2(6f, 1.2f);
        centre = Feet + new Vector2(facing * (BodySize.x * .5f + 3f), .6f);
        return a != 1;   // the flood alone has no sweep
    }
}

using UnityEngine;

// G-05, the Tempest Owl (D08): a storm owl. Phase one beats a great gust that throws Qori away (it doesn't hurt).
// Phase two alternates the gust with a dive: it climbs, marks where Qori stands, and drops on the spot. Phase three
// picks at random and each gust throws harder. The eye on its chest opens after each attack.
public sealed class TempestOwlBoss : UndergroundGuardian
{
    [SerializeField] float gustReach = 18f, gustSpeed = 13f, diveClimb = 9f, diveSpeed = 17f;
    Vector2 diveTarget, home; bool landed;
    protected override string DesignId => "G-05";
    protected override Vector2 WeakOffset => new Vector2(.8f, 2.5f);
    protected override Vector2 BodySize => new Vector2(4.5f, 4.5f);
    protected override string WarnSegment(int a) => a == 1 ? "DiveWarn" : "Warn";
    protected override int NextAttack(int n) => Phase == 1 ? 0 : Phase == 2 ? n % 2 : Random.value < .5f ? 0 : 1;

    protected override void Awake() { base.Awake(); home = transform.position; strikeTime = 1f; }   // long enough for the dive to land

    void Update()
    {
        // The climb during the dive's warning: it rises, keeping its eye on Qori.
        if (Current == State.Warn && Attack == 1)
        {
            float k = Mathf.Clamp01((Time.time - since) / (warnTime / Speed));
            var p = Player;
            if (p != null) { Face(); diveTarget = new Vector2(Mathf.Clamp(p.transform.position.x, Room.min.x + 3f, Room.max.x - 3f), home.y); }
            body.MovePosition(new Vector2(Feet.x, home.y + diveClimb * Mathf.SmoothStep(0f, 1f, k)));
        }
    }

    protected override void OnStrike(int a)
    {
        landed = false;
        if (a == 1) return;
        var p = Player;
        if (p != null && (p.transform.position.x - Feet.x) * facing > 0f && Mathf.Abs(p.transform.position.x - Feet.x) < gustReach && Mathf.Abs(p.transform.position.y - Feet.y) < 10f)
            p.ApplyKnockback(new Vector2(facing * gustSpeed * (Phase >= 3 ? 1.3f : 1f), 3f));
        if (Fx.Library != null) for (int i = 0; i < 4; i++) Fx.Leaves(Feet + new Vector2(facing * (2f + i * 2.5f), 1.5f + i * .3f), 3, 1f);
    }

    protected override void OnStrikeTick(int a, float t)
    {
        if (a != 1 || landed) return;
        Vector2 next = Vector2.MoveTowards(Feet, diveTarget, diveSpeed * Speed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if ((next - diveTarget).sqrMagnitude < .01f)
        {
            landed = true;
            Sfx.Play("Guardian_ArmSlam", next); CombatCameraShake.Kick(.16f);
            if (Fx.Library != null) Fx.Play(Fx.Library.dustLand, next, 14f, 2.6f, 30);
        }
    }

    // The dive hurts until it lands; the gust never does.
    protected override bool StrikeZone(int a, out Vector2 centre, out Vector2 size)
    {
        centre = Feet + new Vector2(0f, 2f); size = new Vector2(3.5f, 3.5f);
        return a == 1 && !landed;
    }

}

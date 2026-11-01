using UnityEngine;

// E-20, the Wilted Husk: a dormant flower. It wakes as Qori passes, lunges once, and collapses open: two hits finish it. Shut, it turns blows.
// Untouched, it gathers itself and goes dormant again.
public sealed class WiltedHuskEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-20";
    protected override Vector2 BodySize => new Vector2(1.6f, 1.5f);
    protected override int Hits => 2;
    protected override bool Blocks(CombatDamage hit) => phase == Phase.Idle;   // hard shut until it wakes
    public override bool HurtsOnContact => IsAlive && phase == Phase.Attack;
    protected override bool Rooted => true;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle: Show("Dormant"); if (Near(3.8f, 2.5f)) { Face(); Enter(Phase.Warn); } break;
            case Phase.Warn: Show("Warn", .6f); if (T > .6f) Enter(Phase.Attack); break;
            case Phase.Attack:
                Show("Attack", .35f);
                if (!WallAhead(facing, 1.2f) && !EdgeAhead(facing, 1f)) MoveTo(Pos + new Vector2(facing * 7f * Time.fixedDeltaTime, 0f));
                if (T > .35f) Enter(Phase.Recover);
                break;
            case Phase.Recover: Show("Vulnerable", 1.6f); if (T > 2.4f) Enter(Phase.Idle); break;
        }
    }
    protected override bool StrikeZone(out Vector2 c, out Vector2 s) { c = Pos + new Vector2(facing * 1.4f, .8f); s = new Vector2(2f, 1.4f); return phase == Phase.Attack; }
}

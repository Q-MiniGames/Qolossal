using UnityEngine;

// E-15, the Bramble Creeper: crawls its patrol under a thorny shell that turns ordinary hits. Sees Qori, curls into a
// ball and rolls at him until it hits a wall or runs out, then lies belly up: strike it then.
public sealed class BrambleCreeperEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-15";
    protected override Vector2 BodySize => new Vector2(2f, 1.2f);
    protected override int Hits => 4;
    float dir = 1f;
    protected override bool Blocks(CombatDamage hit) => !WeaponKinds.IsHeavy(hit) && phase != Phase.Recover;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle: Enter(Phase.Walk); break;
            case Phase.Walk:
                Show("Walk");
                if (Near(7f, 1.6f)) { Face(); Enter(Phase.Warn); break; }
                if (WallAhead(dir, 1.2f) || EdgeAhead(dir, 1.1f) || (Pos.x - Home.x) * dir > 4f) dir = -dir;
                facing = dir;
                MoveTo(Pos + new Vector2(dir * .9f * Time.fixedDeltaTime, 0f));
                break;
            case Phase.Warn: Show("Warn", .65f); if (T > .65f) Enter(Phase.Attack); break;
            case Phase.Attack:
                segment = "Warn"; segmentU = 1f;   // holds the curled ball while it rolls
                if (T > 1.4f || WallAhead(facing, 1.2f) || EdgeAhead(facing, 1f)) { Enter(Phase.Recover); break; }
                MoveTo(Pos + new Vector2(facing * 8f * Time.fixedDeltaTime, 0f));
                break;
            case Phase.Recover: Show("Vulnerable", 1.4f); if (T > 1.4f) { Home = Pos; Enter(Phase.Walk); } break;
        }
    }
    protected override bool StrikeZone(out Vector2 c, out Vector2 s) { c = Pos + Vector2.up * .6f; s = new Vector2(1.6f, 1.2f); return phase == Phase.Attack; }
}

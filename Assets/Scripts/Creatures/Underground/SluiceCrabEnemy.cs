using UnityEngine;

// E-11, the Sluice Crab: scuttles along its floor, claws up as a shield when faced (hits from the front glance off);
// close in and it snaps low, then sags with its soft underside showing. Jump over and strike from behind, or after a snap.
public sealed class SluiceCrabEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-11";
    protected override Vector2 BodySize => new Vector2(2.2f, 1f);
    float dir = 1f, turning;
    protected override bool Blocks(CombatDamage hit)
    {
        var p = Player;
        return !WeaponKinds.IsHeavy(hit) && phase != Phase.Recover && p != null && (p.transform.position.x - Pos.x) * facing > 0f && p.transform.position.y < Pos.y + 1.6f;
    }
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle: Enter(Phase.Walk); break;
            case Phase.Walk:
                if (Near(3.2f, 1.6f) && (Player.transform.position.x - Pos.x) * facing > 0f) { Enter(Phase.Warn); break; }
                if (Near(6f, 2f))
                {
                    // Slow to turn, so a jump over it finds its back.
                    bool behind = (Player.transform.position.x - Pos.x) * facing < 0f;
                    if (!behind) turning = 0f; else if ((turning += Time.fixedDeltaTime) > .7f) { Face(); turning = 0f; }
                    Show("Guard"); break;
                }
                turning = 0f;
                Show("Walk");
                if (WallAhead(dir, 1.3f) || EdgeAhead(dir, 1.2f) || (Pos.x - Home.x) * dir > 4f) dir = -dir;
                facing = dir;
                MoveTo(Pos + new Vector2(dir * 1.4f * Time.fixedDeltaTime, 0f));
                break;
            case Phase.Warn: Show("Warn", .6f); if (T > .6f) Enter(Phase.Attack); break;
            case Phase.Attack: Show("Attack", .3f); if (T > .3f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Recover", 1.2f); if (T > 1.2f) Enter(Phase.Walk); break;
        }
    }
    protected override bool StrikeZone(out Vector2 c, out Vector2 s) { c = Pos + new Vector2(facing * 1.8f, .5f); s = new Vector2(2f, 1f); return phase == Phase.Attack; }
}

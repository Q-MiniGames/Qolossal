using UnityEngine;

// E-13, the Root Snapper: a rooted flytrap. Come close and its jaws gape, then it lunges and snaps; it droops after,
// open to a strike or a Bloomfall bounce on its closed head.
public sealed class RootSnapperEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-13";
    protected override Vector2 BodySize => new Vector2(1.2f, 1.7f);
    protected override int Hits => 4;
    protected override bool Rooted => true;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle: Show("Idle"); if (Near(3.6f, 3f)) { Face(); Enter(Phase.Warn); } break;
            case Phase.Warn: Show("Warn", .65f); if (T > .65f) Enter(Phase.Attack); break;
            case Phase.Attack: Show("Attack", .35f); if (T > .35f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Recover", 1.3f); if (T > 1.3f) Enter(Phase.Idle); break;
        }
    }
    protected override bool StrikeZone(out Vector2 c, out Vector2 s) { c = Pos + new Vector2(facing * 2.2f, 1.4f); s = new Vector2(2.6f, 1.4f); return phase == Phase.Attack; }
}

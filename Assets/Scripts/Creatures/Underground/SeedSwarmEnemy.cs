using UnityEngine;

// E-14, the Seed Swarm: a slow cloud that follows Qori within reach of its home. It gathers, then stings in a pulse.
// Three hits scatter it for good. The drifting cloud itself doesn't hurt.
public sealed class SeedSwarmEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-14";
    protected override Vector2 BodySize => new Vector2(1.6f, 1.6f);
    protected override bool Flyer => true;
    protected override int Hits => 3;
    public override bool HurtsOnContact => false;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        var p = Player;
        switch (phase)
        {
            case Phase.Idle:
                Show("Idle");
                Vector2 goal = p != null && InRoom(p) && Vector2.Distance(p.transform.position, Home) < 9f ? (Vector2)p.transform.position + Vector2.up * .6f : Home;
                Face();
                MoveTo(Vector2.MoveTowards(Pos, goal, 1.5f * Time.fixedDeltaTime));
                if (p != null && InRoom(p) && Vector2.Distance(p.transform.position, Pos) < 2.8f) Enter(Phase.Warn);
                break;
            case Phase.Warn: Show("Warn", .7f); if (T > .7f) Enter(Phase.Attack); break;
            case Phase.Attack: Show("Attack", .35f); if (T > .35f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Idle"); if (T > 1.2f) Enter(Phase.Idle); break;
        }
    }
    protected override bool StrikeZone(out Vector2 c, out Vector2 s) { c = Pos; s = new Vector2(3.6f, 3.6f); return phase == Phase.Attack; }
}

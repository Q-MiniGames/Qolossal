using UnityEngine;

// E-16, the Kite Ray: rides a slow loop round its home on the wind (touching it hurts). When Qori comes near it tilts,
// then swoops through where he was and climbs back to its loop. Strike it as it turns.
public sealed class KiteRayEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-16";
    protected override Vector2 BodySize => new Vector2(1.8f, 1f);
    protected override bool Flyer => true;
    protected override int Hits => 3;
    Vector2 swoop;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle:
                Show("Idle");
                float a = Time.time * .8f;
                Vector2 goal = Home + new Vector2(Mathf.Cos(a) * 3f, Mathf.Sin(a));
                facing = goal.x >= Pos.x ? 1f : -1f;
                MoveTo(Vector2.MoveTowards(Pos, goal, 4f * Time.fixedDeltaTime));
                if (Near(7f, 6f)) { Face(); Enter(Phase.Warn); }
                break;
            case Phase.Warn:
                Show("Warn", .6f);
                if (Player != null) swoop = (Vector2)Player.transform.position + new Vector2(facing * 2f, .6f);
                if (T > .6f) Enter(Phase.Attack);
                break;
            case Phase.Attack:
                Show("Attack", .7f);
                MoveTo(Vector2.MoveTowards(Pos, swoop, 11f * Time.fixedDeltaTime));
                if ((Pos - swoop).sqrMagnitude < .05f || T > 1.2f) Enter(Phase.Recover);
                break;
            case Phase.Recover:
                Show("Idle");
                facing = Home.x >= Pos.x ? 1f : -1f;
                MoveTo(Vector2.MoveTowards(Pos, Home, 5f * Time.fixedDeltaTime));
                if ((Pos - Home).sqrMagnitude < .1f || T > 2.5f) Enter(Phase.Idle);
                break;
        }
    }
}

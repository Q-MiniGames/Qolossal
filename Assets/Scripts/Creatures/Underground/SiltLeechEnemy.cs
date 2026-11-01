using UnityEngine;

// E-10, the Silt Leech: hangs from a ledge or ceiling, drops when Qori passes under, crawls a moment, then climbs back.
public sealed class SiltLeechEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-10";
    protected override Vector2 BodySize => new Vector2(.9f, 1.4f);
    protected override bool Flyer => true;
    protected override int Hits => 2;
    public override bool HurtsOnContact => IsAlive && phase == Phase.Attack;
    protected override bool Rooted => true;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle:
                Show("Idle");
                if (Near(1.8f, 14f) && Player.transform.position.y < Pos.y) Enter(Phase.Warn);
                break;
            case Phase.Warn: Show("Warn", .6f); if (T > .6f) Enter(Phase.Attack); break;
            case Phase.Attack:
                Show("Attack", .6f);
                if (Physics2D.Raycast(Pos, Vector2.down, .8f, GroundLayer.Mask)) Enter(Phase.Recover);
                else MoveTo(Pos + Vector2.down * 12f * Time.fixedDeltaTime);
                break;
            case Phase.Recover:
                Show("Walk");
                if (T > 1.6f) { MoveTo(Vector2.MoveTowards(Pos, Home, 4f * Time.fixedDeltaTime)); if ((Pos - Home).sqrMagnitude < .02f) Enter(Phase.Idle); }
                break;
        }
    }
}

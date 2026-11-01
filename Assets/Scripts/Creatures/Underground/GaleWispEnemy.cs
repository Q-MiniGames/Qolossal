using UnityEngine;

// E-17, the Gale Wisp: hovers, inhales to pull Qori in (no damage; the Wind Leaf dash breaks free), then exhales a short
// push and droops: strike it then. Neither the wind nor touching it hurts.
public sealed class GaleWispEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-17";
    protected override Vector2 BodySize => new Vector2(1.4f, 1.4f);
    protected override bool Flyer => true;
    protected override int Hits => 3;
    public override bool HurtsOnContact => false;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        MoveTo(Home + Vector2.up * Mathf.Sin(Time.time * 1.3f) * .2f);
        var p = Player;
        switch (phase)
        {
            case Phase.Idle: Show("Idle"); if (Near(9f, 5f)) { Face(); Enter(Phase.Warn); } break;
            case Phase.Warn:
                Show("Warn", 1.3f);
                // A drag on him, not a hold: he can still walk, swing, jump and dash. Running beats it.
                if (p != null && T > .15f && !p.IsDashing && Near(9f, 5f)) p.AddPull(new Vector2(Mathf.Sign(Pos.x - p.transform.position.x) * 4.5f, 0f));
                if (T > 1.3f)
                {
                    Enter(Phase.Attack);
                    Sfx.Play("GustMoth_Blast", Pos, .7f);
                    if (p != null && Near(5f, 3f)) p.ApplyKnockback(new Vector2(facing * 12f, 3f));
                }
                break;
            case Phase.Attack: Show("Attack", .4f); if (T > .4f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Recover", 1.5f); if (T > 1.5f) Enter(Phase.Idle); break;
        }
    }
}

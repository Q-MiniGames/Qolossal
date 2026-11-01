using UnityEngine;

// E-12, the Pump Bloater: floats, swells, then blasts a water jet that pushes Qori back (it doesn't hurt) and needs to
// refill: strike it then. Touching its body costs a heart.
public sealed class PumpBloaterEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-12";
    protected override Vector2 BodySize => new Vector2(1.5f, 1.5f);
    protected override bool Flyer => true;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        MoveTo(Home + Vector2.up * Mathf.Sin(Time.time * 1.6f) * .25f);
        switch (phase)
        {
            case Phase.Idle: Show("Idle"); if (Near(8f, 4f)) { Face(); Enter(Phase.Warn); } break;
            case Phase.Warn: Show("Warn", .7f); if (T > .7f) { Enter(Phase.Attack); Jet(); } break;
            case Phase.Attack: Show("Attack", .35f); if (T > .35f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Recover", 1.6f); if (T > 1.6f) Enter(Phase.Idle); break;
        }
    }
    void Jet()
    {
        var p = Player;
        Sfx.Play("GustMoth_Blast", Pos, .7f);
        if (p != null && (p.transform.position.x - Pos.x) * facing > 0f && Near(9f, 2.5f)) p.ApplyKnockback(new Vector2(facing * 11f, 2f));
    }
}

using UnityEngine;

// E-18, the Hail Sentry: perched, it rears its head and fires an ice shard along the wind, then shakes itself. Glide
// under the arc, or answer with a Leaf Bolt.
public sealed class HailSentryEnemy : UndergroundEnemy
{
    protected override string DesignId => "E-18";
    protected override Vector2 BodySize => new Vector2(1.2f, 1.7f);
    protected override int Hits => 4;
    protected override bool Rooted => true;
    void FixedUpdate()
    {
        if (!IsAlive) return;
        switch (phase)
        {
            case Phase.Idle: Show("Idle"); if (T > 1f && Near(13f, 8f)) { Face(); Enter(Phase.Warn); } break;
            case Phase.Warn: Show("Warn", .7f); if (T > .7f) { Enter(Phase.Attack); Fire(); } break;
            case Phase.Attack: Show("Attack", .3f); if (T > .3f) Enter(Phase.Recover); break;
            case Phase.Recover: Show("Recover", .9f); if (T > .9f) Enter(Phase.Idle); break;
        }
    }
    void Fire()
    {
        var p = Player; if (p == null) return;
        Vector2 from = Pos + new Vector2(facing * .8f, 1.3f);
        Vector2 aim = ((Vector2)p.transform.position + Vector2.up * .5f - from).normalized;
        SpitterSeed.Launch(UgArenaArt.Square, from, aim * 8f);
    }
}

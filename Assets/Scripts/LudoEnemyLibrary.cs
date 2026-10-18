using UnityEngine;

// The Ludo.ai frame sets for each enemy kind (Resources/LudoFrames/LudoEnemyFrames, written by QoriWalkTestBuilder),
// and how to dress a live enemy with them (LudoFramesInstaller).
public sealed class LudoEnemyLibrary : MonoBehaviour
{
    [System.Serializable] public sealed class Entry
    {
        public EnemyFramesOnCreature.Kind kind;
        public EnemyFramesOnCreature.Set[] sets = new EnemyFramesOnCreature.Set[0];
        public float worldWidth = 1.8f, figureWidthPx = 400f;
    }
    public Entry[] enemies = new Entry[0];
    [Header("Bramble Crawler (GroundCreature)")]
    public Sprite[] crawlerWalk = new Sprite[0], crawlerWarn = new Sprite[0], crawlerCharge = new Sprite[0], crawlerHurt = new Sprite[0], crawlerDeath = new Sprite[0];
    public float crawlerFigureWidthPx = 772f;

    static bool KindOf(EnemyBase e, out EnemyFramesOnCreature.Kind kind)
    {
        kind = default;
        switch (e)
        {
            case ThornwingEnemy _: kind = EnemyFramesOnCreature.Kind.Thornwing; return true;
            case SpitterEnemy _: kind = EnemyFramesOnCreature.Kind.PodSpitter; return true;
            case ShellbackEnemy _: kind = EnemyFramesOnCreature.Kind.Shellback; return true;
            case NewtEnemy _: kind = EnemyFramesOnCreature.Kind.Newt; return true;
            case GrubEnemy _: kind = EnemyFramesOnCreature.Kind.Grub; return true;
            case GustMothEnemy _: kind = EnemyFramesOnCreature.Kind.GustMoth; return true;
            case SentinelEnemy _: kind = EnemyFramesOnCreature.Kind.Sentinel; return true;
            default: return false;   // the guardians keep their own art
        }
    }

    public void Dress(EnemyBase e)
    {
        if (!KindOf(e, out var kind)) return;
        foreach (var entry in enemies)
        {
            if (entry.kind != kind || entry.sets.Length == 0) continue;
            var f = e.gameObject.AddComponent<EnemyFramesOnCreature>();
            f.kind = kind; f.sets = entry.sets; f.worldWidth = entry.worldWidth; f.figureWidthPx = entry.figureWidthPx;
            return;
        }
    }

    public void DressCrawler(GameObject go)
    {
        if (crawlerWalk.Length == 0) return;
        var f = go.AddComponent<CrawlerFramesOnCreature>();
        f.walk = crawlerWalk; f.warn = crawlerWarn; f.charge = crawlerCharge; f.hurt = crawlerHurt; f.death = crawlerDeath;
        f.figureWidthPx = crawlerFigureWidthPx;
    }
}

using UnityEngine;

// An underground creature drawn with its Ludo.ai frames (export_underground.py) on top of the stand-in enemy whose
// behaviour it borrows until its own is written: a Thornwing's hover/warning/dive, a Pod Spitter's swell and spit,
// a Shellback's walk and crack, a Sentinel's guard/draw/thrust, a Gust Moth's raise and beat, a Grub's rise and bite,
// the Knucklebramble's slam cycle and opened knot. Each picks the creature's matching segment (Idle, Walk, Warn,
// Attack, Recover, Guard, Vulnerable, Open/Exposed/Close, Roar); a hit plays Hurt; death leaves a body that plays Death.
// The frames face right; the sprite pivot is the canvas pivot (feet, or a flyer's body centre).
[DefaultExecutionOrder(200)]
public sealed class UndergroundCreatureFrames : MonoBehaviour
{
    [Tooltip("The design ID, e.g. E-11 or G-03 (UndergroundCreatureLibrary).")] public string designId = "";
    public float loopFps = 12f, hurtSeconds = .25f, deathSeconds = 1.4f;

    UndergroundCreatureLibrary.Entry entry;
    EnemyBase enemy;
    SpriteRenderer view;
    Transform qori;
    float hurtAt = -9f, facing = -1f, crackedSeen = -1f;
    bool wasFlashing, dead;

    public static bool Covers(EnemyBase e) => e != null && e.GetComponent<UndergroundCreatureFrames>() != null;

    void Start()
    {
        enemy = GetComponentInChildren<EnemyBase>();
        entry = UndergroundCreatureLibrary.Find(designId);
        if (entry == null || enemy == null) { enabled = false; return; }
        var art = new GameObject(designId + " frames");
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 8;
        art.transform.localScale = Vector3.one * entry.Scale;
        if (enemy.HitFeel != null) { enemy.HitFeel.AddRenderer(view); enemy.HitFeel.SetVisual(art.transform); }
        var player = FindAnyObjectByType<PlayerMovement>(); if (player != null) qori = player.transform;
    }

    Sprite[] Get(string n) => entry.Get(n);
    static Sprite At(Sprite[] f, float u) => f[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * f.Length), 0, f.Length - 1)];
    static Sprite Loop(Sprite[] f, float t, float fps) => f[(int)Mathf.Repeat(t * fps, f.Length)];
    Sprite[] First(params string[] names) { foreach (var n in names) { var f = Get(n); if (f != null && f.Length > 0) return f; } return null; }

    void LateUpdate()
    {
        if (enemy == null || view == null) return;
        foreach (var r in enemy.GetComponentsInChildren<Renderer>()) if (r.enabled && !KeepVisible(r)) r.enabled = false;   // the stand-in's own art stays hidden
        if (!enemy.IsAlive)
        {
            if (!dead) { dead = true; CrawlerCorpse.Leave(view, First("Death", "Hurt"), deathSeconds, facing, entry.pivot == "center"); view = null; }
            return;
        }
        float t = Time.time;
        bool flashing = enemy.IsFlashing;
        if (flashing && !wasFlashing) hurtAt = t;
        wasFlashing = flashing;

        Sprite s = Pick(t);
        var hurt = Get("Hurt");
        if (t - hurtAt < hurtSeconds && hurt != null && hurt.Length > 0) s = At(hurt, (t - hurtAt) / hurtSeconds);
        view.enabled = s != null;
        if (s != null) view.sprite = s;
        view.flipX = facing < 0f;
        view.color = flashing ? new Color(1f, .8f, .8f) : Color.white;
        view.transform.position = enemy.transform.position;
    }

    // The Knucklebramble's arms are its hazards: they stay drawn so a slam can always be seen.
    bool KeepVisible(Renderer r) => enemy is KnucklebrambleGuardian && r.GetComponentInParent<GuardianArmHazard>() != null;

    Sprite Pick(float t)
    {
        var idle = First("Idle", "Walk");
        switch (enemy)
        {
            case ThornwingEnemy w:
                facing = w.AnimFacing;
                switch (w.AnimPhase)
                {
                    case "Telegraph": return At(First("Warn", "Idle"), w.AnimTime / .55f);
                    case "Dive": return Loop(First("Attack", "Idle"), w.AnimTime, 10f);
                    default: return Loop(idle, t, loopFps);
                }
            case SpitterEnemy p:
                facing = p.AnimFacing;
                if (p.Swell01 >= 0f) return At(First("Warn", "Idle"), p.Swell01);
                if (t - p.LastSpitTime < .9f)
                {
                    float u = (t - p.LastSpitTime) / .9f; var atk = First("Attack"); var rec = Get("Recover");
                    return rec != null && rec.Length > 0 && u > .5f ? At(rec, (u - .5f) * 2f) : At(atk, u * 2f);
                }
                return Loop(idle, t, loopFps);
            case ShellbackEnemy sb:
                facing = sb.AnimFacing;
                if (sb.CrackedAt >= 0f && t - sb.CrackedAt < 1.4f) return At(First("Vulnerable", "Warn", "Walk"), (t - sb.CrackedAt) / 1.4f);
                return Loop(First("Walk", "Idle"), t, 9f * Mathf.Clamp(sb.WalkSpeed / .8f, .3f, 2f));
            case SentinelEnemy sn:
                facing = sn.Facing;
                switch (sn.AnimPhase)
                {
                    case "Telegraph": return At(First("Warn"), sn.AnimTime / Mathf.Max(.01f, sn.TelegraphTime));
                    case "Bash": return At(First("Attack"), sn.AnimTime / Mathf.Max(.01f, sn.ThrustTime));
                    case "Recover": return At(First("Recover", "Attack"), sn.AnimTime / .7f);
                    case "Guard": { var g = First("Guard", "Walk"); return g[(int)Mathf.PingPong(t * 6f, g.Length - 1)]; }
                    default: return Loop(First("Walk", "Idle"), t, loopFps * Mathf.Clamp(sn.WalkSpeed / .7f, .3f, 2f));
                }
            case GustMothEnemy m:
                facing = m.AnimFacing;
                switch (m.AnimPhase)
                {
                    case "Telegraph": return At(First("Warn"), m.AnimTime / Mathf.Max(.01f, m.TelegraphTime));
                    case "Beat": return At(First("Attack"), m.AnimTime / Mathf.Max(.01f, m.BeatTime));
                    default: return Loop(idle, t, loopFps);
                }
            case GrubEnemy g:
                facing = g.AnimFacing;
                switch (g.AnimPhase)
                {
                    case "Rise": return At(First("Warn"), g.AnimTime / Mathf.Max(.01f, g.RiseTime));
                    case "Up":
                    {
                        float u = g.AnimTime / Mathf.Max(.01f, g.UpTime);
                        return u < .5f ? At(First("Attack"), u * 2f) : At(First("Vulnerable", "Attack"), (u - .5f) * 2f);
                    }
                    case "Sink": return At(First("Warn"), 1f - g.AnimTime / Mathf.Max(.01f, g.SinkTime));
                    default: return First("Dormant", "Idle")[0];   // dormant, still visible
                }
            case KnucklebrambleGuardian k:
                facing = qori != null && qori.position.x < k.transform.position.x ? -1f : 1f;
                switch (k.Current)
                {
                    case KnucklebrambleGuardian.State.Telegraph: return Loop(First("Warn"), t, loopFps);
                    case KnucklebrambleGuardian.State.Slam:
                    case KnucklebrambleGuardian.State.Hold: return Loop(First("Attack"), t, loopFps);
                    case KnucklebrambleGuardian.State.Recover: return Loop(First("Recover", "Idle"), t, loopFps);
                    case KnucklebrambleGuardian.State.Opening: return Loop(First("Open", "Exposed"), t, loopFps);
                    case KnucklebrambleGuardian.State.Exposed: return Loop(First("Exposed"), t, 8f);
                    case KnucklebrambleGuardian.State.Closing: return Loop(First("Close", "Exposed"), t, loopFps);
                    default: return Loop(idle, t, 8f);
                }
        }
        return Loop(idle, t, loopFps);
    }

    void OnDestroy() { if (view != null) Destroy(view.gameObject); }
}

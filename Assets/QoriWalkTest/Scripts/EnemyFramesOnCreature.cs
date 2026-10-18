using UnityEngine;

// Test-only: a Thornwing, Pod Spitter, Shellback or Ripple Newt (the real enemy, its behaviour unchanged) drawn with
// the Ludo.ai frames instead of its cutout rig (export_enemies.py). Each kind picks its frames from what the enemy is
// doing (its AnimPhase/Swell01/CrackedAt readouts): hovering, warning, diving; swaying, swelling, spitting; walking,
// cracking; surfacing, leaping, looking round, springing back. A hit plays the flinch; death leaves a body behind
// that plays the collapse (the flyer drops to the ground first), since the enemy itself vanishes at once.
// The frames are drawn facing right; the sprite pivot is the canvas pivot (feet, or the flyer's body centre).
[DefaultExecutionOrder(200)]
public sealed class EnemyFramesOnCreature : MonoBehaviour
{
    public enum Kind { Thornwing, PodSpitter, Shellback, Newt, Grub, GustMoth, Sentinel }
    [System.Serializable] public sealed class Set { public string name; public Sprite[] frames = new Sprite[0]; }

    public Kind kind;
    public Set[] sets = new Set[0];
    [Tooltip("The reference frame's width in world units, and in frame pixels.")]
    public float worldWidth = 1.8f, figureWidthPx = 400f;
    public float loopFps = 14f, hurtSeconds = .25f, deathSeconds = 1.2f;

    EnemyBase enemy;
    SpriteRenderer view;
    float hurtAt = -9f, facing = -1f;
    bool wasFlashing, dead;

    Sprite[] Get(string n) { foreach (var s in sets) if (s.name == n) return s.frames; return null; }
    static Sprite At(Sprite[] f, float u) => f[Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(u) * f.Length), 0, f.Length - 1)];
    static Sprite Loop(Sprite[] f, float t, float fps) => f[(int)Mathf.Repeat(t * fps, f.Length)];

    void Start()
    {
        enemy = GetComponent<EnemyBase>();
        var art = new GameObject(kind + " frames (test)");
        view = art.AddComponent<SpriteRenderer>(); view.sortingOrder = 8;
        art.transform.localScale = Vector3.one * (worldWidth / (figureWidthPx / 100f));
    }

    void LateUpdate()
    {
        if (enemy == null || view == null) return;
        foreach (var r in GetComponentsInChildren<Renderer>()) if (r.enabled) r.enabled = false;   // the rig stays hidden
        if (!enemy.IsAlive)
        {
            if (!dead) { dead = true; CrawlerCorpse.Leave(view, Get("Death"), kind == Kind.GustMoth ? 2.4f : deathSeconds, facing, kind == Kind.Thornwing || kind == Kind.GustMoth); view = null; }
            return;
        }
        float t = Time.time;
        bool flashing = enemy.IsFlashing;
        if (flashing && !wasFlashing) hurtAt = t;
        wasFlashing = flashing;

        Sprite s = Pick(t, out bool hidden);
        var hurt = Get("Hurt");
        if (!hidden && t - hurtAt < hurtSeconds && hurt != null && hurt.Length > 0) s = At(hurt, (t - hurtAt) / hurtSeconds);
        view.enabled = !hidden && s != null;
        if (s != null) view.sprite = s;
        view.flipX = facing < 0f;
        view.color = flashing ? new Color(1f, .8f, .8f) : Color.white;
        view.transform.position = transform.position;
        view.transform.rotation = Quaternion.identity;
    }

    Sprite Pick(float t, out bool hidden)
    {
        hidden = false;
        switch (kind)
        {
            case Kind.Thornwing:
            {
                var w = (ThornwingEnemy)enemy; facing = w.AnimFacing;
                switch (w.AnimPhase)
                {
                    case "Telegraph": return At(Get("Warn"), w.AnimTime / .55f);
                    case "Dive":
                        // tilted along the dive
                        var d = w.DiveDirection; float tilt = Mathf.Atan2(d.y, Mathf.Abs(d.x)) * Mathf.Rad2Deg;
                        view.transform.rotation = Quaternion.Euler(0, 0, facing < 0f ? -tilt : tilt);
                        return Loop(Get("Dive"), w.AnimTime, 10f);
                    default: return Loop(Get("Hover"), t, 20f);
                }
            }
            case Kind.PodSpitter:
            {
                var p = (SpitterEnemy)enemy; facing = p.AnimFacing;
                if (p.Swell01 >= 0f) return At(Get("Swell"), p.Swell01);
                if (t - p.LastSpitTime < .6f) return At(Get("Spit"), (t - p.LastSpitTime) / .6f);
                return Loop(Get("Idle"), t, 12f);
            }
            case Kind.Shellback:
            {
                var sb = (ShellbackEnemy)enemy; facing = sb.AnimFacing;
                var crack = Get("Crack");
                if (sb.CrackedAt >= 0f)
                {
                    float since = t - sb.CrackedAt;
                    if (since < .9f) return At(crack, since / .9f);
                    // cracked: walking on with the glow showing
                    var cracked = Get("CrackedWalk");
                    if (cracked != null && cracked.Length > 0) return Loop(cracked, t, 9f * Mathf.Clamp(sb.WalkSpeed / .8f, .2f, 2f));
                    return crack[crack.Length - 1];
                }
                return Loop(Get("Walk"), t, 9f * Mathf.Clamp(sb.WalkSpeed / .8f, .2f, 2f));
            }
            case Kind.Sentinel:
            {
                var sn = (SentinelEnemy)enemy; facing = sn.Facing;
                var thrust = Get("Thrust");
                switch (sn.AnimPhase)
                {
                    case "Telegraph": return At(Get("Draw"), sn.AnimTime / Mathf.Max(.01f, sn.TelegraphTime));
                    case "Bash": return At(thrust, sn.AnimTime / Mathf.Max(.01f, sn.ThrustTime));
                    case "Recover":   // the spear held out, then drawn back in (the thrust played backward)
                    {
                        float u = sn.AnimTime / .7f;
                        return u < .4f ? thrust[thrust.Length - 1] : At(thrust, 1f - (u - .4f) / .6f);
                    }
                    case "Guard":
                    {
                        var g = Get("Guard"); return g[(int)Mathf.PingPong(t * 6f, g.Length - 1)];
                    }
                    default: return Loop(Get("Walk"), t, 14f * Mathf.Clamp(sn.WalkSpeed / .7f, .2f, 2f));
                }
            }
            case Kind.GustMoth:
            {
                var m = (GustMothEnemy)enemy; facing = m.AnimFacing;
                switch (m.AnimPhase)
                {
                    case "Telegraph": return At(Get("Raise"), m.AnimTime / Mathf.Max(.01f, m.TelegraphTime));
                    case "Beat": return At(Get("Beat"), m.AnimTime / Mathf.Max(.01f, m.BeatTime));
                    default: return Loop(Get("Hover"), t, 14f);
                }
            }
            case Kind.Grub:
            {
                var g = (GrubEnemy)enemy; facing = g.AnimFacing;
                switch (g.AnimPhase)
                {
                    case "Rise": return At(Get("Erupt"), g.AnimTime / Mathf.Max(.01f, g.RiseTime) * .6f);   // the rise is quick; the rest of the erupt plays as it settles
                    case "Up":
                    {
                        var erupt = Get("Erupt"); float settle = .25f;
                        if (g.AnimTime < settle) return At(erupt, .6f + .4f * g.AnimTime / settle);
                        return At(Get("Up"), (g.AnimTime - settle) / Mathf.Max(.01f, g.UpTime - settle));
                    }
                    case "Sink": return At(Get("Sink"), g.AnimTime / Mathf.Max(.01f, g.SinkTime));
                    default: hidden = true; return null;   // under the soil
                }
            }
            case Kind.Newt:
            {
                var n = (NewtEnemy)enemy; facing = n.AnimFacing;
                float k = n.AnimTime / Mathf.Max(.01f, n.LeapTime);
                switch (n.AnimPhase)
                {
                    case "Submerged": hidden = true; return null;
                    case "Telegraph": return At(Get("Crouch"), n.AnimTime / .5f);
                    case "Leap": return At(Get("Leap"), k);
                    case "Return": return At(Get("Spring"), .45f + .55f * k);   // already springing; the airborne half
                    default:   // Landed: settle, then look round
                        return n.AnimTime < .4f ? At(Get("Land"), n.AnimTime / .4f) : Loop(Get("Look"), n.AnimTime - .4f, 10f);
                }
            }
        }
        return null;
    }

    void OnDestroy() { if (view != null) Destroy(view.gameObject); }
}

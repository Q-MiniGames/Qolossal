#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// F9 in the editor and development builds (never in a release): draws every live combat box over the game, with each
// enemy's state. Green: Qori's body. Yellow: an enemy's body while it doesn't hurt. Orange: an enemy's body while it
// hurts on touch. Red: a strike zone that is live. White: Qori's attack while its hit is live, grey before and after.
public sealed class CombatDebugOverlay : MonoBehaviour
{
    public static bool Showing { get; set; }
    static readonly Color Body = new Color(.3f, 1f, .4f), Quiet = new Color(1f, .9f, .2f), Touch = new Color(1f, .55f, .1f),
        Strike = new Color(1f, .15f, .15f), Live = Color.white, Idle = new Color(.6f, .6f, .6f);
    readonly List<LineRenderer> pool = new List<LineRenderer>();
    readonly List<(Vector2 at, string text)> labels = new List<(Vector2, string)>();
    readonly List<Vector2> points = new List<Vector2>();
    readonly Vector3[] loop = new Vector3[33];
    Material material;
    PlayerCombat combat;
    float nextFind;
    EnemyBase[] enemies = new EnemyBase[0];
    int used;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("Combat debug overlay"); DontDestroyOnLoad(go);
        go.AddComponent<CombatDebugOverlay>();
    }

    void LateUpdate()
    {
        var key = Keyboard.current;
        if (key != null && key.f9Key.wasPressedThisFrame) Showing = !Showing;
        if (key != null && key.f10Key.wasPressedThisFrame)   // the danger zones, until Settings has them: off, big attacks, all
        {
            AttackZoneTelegraph.Mode = (AttackZoneTelegraph.Level)(((int)AttackZoneTelegraph.Mode + 1) % 3);
            Debug.Log("Danger zones: " + AttackZoneTelegraph.Mode);
        }
        used = 0; labels.Clear();
        if (Showing) Draw();
        for (int i = used; i < pool.Count; i++) pool[i].enabled = false;
    }

    void Draw()
    {
        if (Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + .5f;
            combat = FindAnyObjectByType<PlayerCombat>();
            enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        }
        if (combat != null)
        {
            foreach (var c in combat.GetComponents<Collider2D>()) Outline(c, Body);
            if (combat.CurrentAttack != null) Attack(combat);
        }
        foreach (var e in enemies)
        {
            if (e == null) continue;
            foreach (var c in e.GetComponentsInChildren<Collider2D>())
            {
                if (!c.enabled) continue;
                var hazard = c.GetComponentInParent<IContactHazard>();
                bool hurts = hazard != null && hazard.HurtsOnContact;
                bool strike = hazard is Component h && h.gameObject != e.gameObject;
                Outline(c, strike ? (hurts ? Strike : Quiet) : hurts ? Touch : Quiet);
            }
            string state = e is UndergroundEnemy u ? u.State : e is UndergroundGuardian g ? g.Current.ToString() : "";
            labels.Add((e.transform.position, e.name + (state.Length > 0 ? ": " + state : "") + " (" + Mathf.RoundToInt(e.HealthFraction * 100f) + "%)"));
        }
    }

    void Attack(PlayerCombat who)
    {
        var a = who.CurrentAttack;
        Color color = who.Phase == AttackPhase.Active ? Live : Idle;
        Vector2 offset = a.hitboxOffset; offset.x *= who.AttackDirection;
        Vector2 centre = (Vector2)who.transform.position + offset;
        switch (a.hitShape)
        {
            case CombatHitShape.Circle: Circle(centre, a.hitboxSize.x * .5f, color); break;
            case CombatHitShape.Box:
                Vector2 half = a.hitboxSize * .5f;
                Poly(color, centre - half, centre + new Vector2(half.x, -half.y), centre + half, centre + new Vector2(-half.x, half.y));
                break;
            default:
                if (who.Phase != AttackPhase.Active) break;
                Vector2 tip = who.LastHitLineB, start = Vector2.Lerp(who.LastHitLineA, tip, a.bladeStart);
                Vector2 side = (tip - start).normalized; side = new Vector2(-side.y, side.x) * a.bladeRadius;
                Poly(color, start + side, tip + side, tip - side, start - side);
                break;
        }
        labels.Add((who.transform.position + Vector3.up * 2f, a.attackId + " " + who.Phase));
    }

    void Outline(Collider2D c, Color color)
    {
        switch (c)
        {
            case BoxCollider2D b:
                Vector2 h = b.size * .5f, o = b.offset;
                Poly(color, b.transform.TransformPoint(o - h), b.transform.TransformPoint(o + new Vector2(h.x, -h.y)),
                    b.transform.TransformPoint(o + h), b.transform.TransformPoint(o + new Vector2(-h.x, h.y)));
                break;
            case CircleCollider2D r:
                Circle(r.bounds.center, r.bounds.extents.x, color);
                break;
            default:
                var e = c.bounds;
                Poly(color, e.min, new Vector2(e.max.x, e.min.y), e.max, new Vector2(e.min.x, e.max.y));
                break;
        }
    }

    void Circle(Vector2 centre, float radius, Color color)
    {
        points.Clear();
        for (int i = 0; i < 32; i++) points.Add(centre + new Vector2(Mathf.Cos(i * Mathf.PI / 16f), Mathf.Sin(i * Mathf.PI / 16f)) * radius);
        Line(color);
    }

    void Poly(Color color, params Vector2[] corners)
    {
        points.Clear(); points.AddRange(corners);
        Line(color);
    }

    void Line(Color color)
    {
        if (used == pool.Count)
        {
            if (material == null) material = new Material(Shader.Find("Sprites/Default"));
            var go = new GameObject("Overlay line"); go.transform.SetParent(transform, false);
            var made = go.AddComponent<LineRenderer>();
            made.sharedMaterial = material; made.useWorldSpace = true; made.loop = true; made.startWidth = made.endWidth = .025f;
            made.sortingOrder = 5000;
            pool.Add(made);
        }
        var line = pool[used++];
        line.enabled = true; line.startColor = line.endColor = color;
        line.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++) loop[i] = points[i];
        line.SetPositions(loop);
    }

    void OnGUI()
    {
        if (!Showing || Camera.main == null) return;
        foreach (var (at, text) in labels)
        {
            Vector3 p = Camera.main.WorldToScreenPoint(at);
            if (p.z < 0f) continue;
            GUI.Label(new Rect(p.x + 8f, Screen.height - p.y, 340f, 24f), text);
        }
    }
}
#endif

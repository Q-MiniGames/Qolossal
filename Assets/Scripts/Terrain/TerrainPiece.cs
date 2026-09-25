using System.Collections.Generic;
using UnityEngine;

// A single kit sprite with a collider fitted to its painted walk surface. The transform
// sits on the walk line: at the slope's lower end, or at the platform's left end.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class TerrainPiece : MonoBehaviour
{
    public enum Kind { Slope30, OneWay, FloatingSmall, FloatingMedium, FloatingLarge }

    public TerrainKit kit;
    public Kind kind;
    public int sortingOrder = -35;
    [Tooltip("Depth of a floating platform's solid collider below its walk line.")] public float floatingDepth = .5f;

    private GameObject art;
    private bool dirty;

    public Sprite Sprite => kit == null ? null : kind switch
    {
        Kind.Slope30 => kit.slope30,
        Kind.OneWay => kit.platformOneWay,
        Kind.FloatingSmall => kit.floatingSmall,
        Kind.FloatingMedium => kit.floatingMedium,
        _ => kit.floatingLarge
    };

    // Walk line (px from top) and the left/right ends of the usable surface (px from left).
    private Vector3 Line => kind switch
    {
        Kind.OneWay => new Vector3(kit.oneWaySpan.x, kit.oneWaySpan.y, kit.oneWayWalkLine),
        Kind.FloatingSmall => kit.floatingSmallLine,
        Kind.FloatingMedium => kit.floatingMediumLine,
        _ => kit.floatingLargeLine
    };

    // Width of the walkable surface, for laying out a level.
    public float SurfaceWidth => Sprite == null ? 0f : kind == Kind.Slope30
        ? (kit.slopeSurface[kit.slopeSurface.Length - 1].x - kit.slopeSurface[0].x) / Sprite.pixelsPerUnit
        : (Line.y - Line.x) / Sprite.pixelsPerUnit;

    public float SlopeRise => kind != Kind.Slope30 || Sprite == null ? 0f
        : (kit.slopeSurface[0].y - kit.slopeSurface[kit.slopeSurface.Length - 2].y) / Sprite.pixelsPerUnit;

    // World-space distance from the slope's lower end to where it reaches its full height, so a
    // block overlapping the crest can start its collider there instead of forming a lip.
    public float SlopeTopStart
    {
        get
        {
            if (kind != Kind.Slope30 || Sprite == null) return 0f;
            Vector2[] p = kit.slopeSurface;
            float top = p[p.Length - 2].y;
            for (int i = 1; i < p.Length - 1; i++)
                if (p[i].y <= top + .5f) return (p[i].x - p[0].x) / Sprite.pixelsPerUnit;
            return (p[p.Length - 2].x - p[0].x) / Sprite.pixelsPerUnit;
        }
    }

    private void OnEnable() => Rebuild();
    private void OnDisable() { if (art != null) Remove(art); art = null; }
    private void OnValidate() => dirty = true;
    private void Update() { if (dirty) { dirty = false; Rebuild(); } }

    public void Rebuild()
    {
        if (art != null) Remove(art);
        for (int i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name == "Art (generated)") Remove(transform.GetChild(i).gameObject);
        Sprite sprite = Sprite;
        if (sprite == null) return;

        Vector2 anchor = kind == Kind.Slope30 ? kit.slopeSurface[0] : new Vector2(Line.x, Line.z);
        art = new GameObject("Art (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        art.transform.SetParent(transform, false);
        var renderer = art.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        art.transform.localPosition = -TerrainKit.Local(sprite, anchor);

        float ppu = sprite.pixelsPerUnit;
        Vector2 ToLocal(Vector2 px) => new Vector2((px.x - anchor.x) / ppu, (anchor.y - px.y) / ppu);
        if (kind == Kind.Slope30)
        {
            var polygon = Ensure<PolygonCollider2D>();
            var points = new List<Vector2>();
            foreach (Vector2 px in kit.slopeSurface) points.Add(ToLocal(px));
            polygon.SetPath(0, points);
            return;
        }

        float length = (Line.y - Line.x) / ppu;
        if (kind == Kind.OneWay)
        {
            var edge = Ensure<EdgeCollider2D>();
            edge.points = new[] { Vector2.zero, new Vector2(length, 0f) };
            edge.usedByEffector = true;
            var effector = Ensure<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
            return;
        }

        var box = Ensure<BoxCollider2D>();
        box.size = new Vector2(length, floatingDepth);
        box.offset = new Vector2(length * .5f, -floatingDepth * .5f);
    }

    private T Ensure<T>() where T : Component => TryGetComponent(out T found) ? found : gameObject.AddComponent<T>();

    private static void Remove(Object item)
    {
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}

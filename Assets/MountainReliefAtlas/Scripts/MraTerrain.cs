using System.Collections.Generic;
using UnityEngine;

// A piece of ground under an authored walk line (the route data's surface points, left to right),
// filled down to `bottom`. Collision is a polygon on exactly those points: nothing is traced from a
// painting. The look comes from accepted terrain art, unchanged: the region family's Fill tiled in
// world space under the line, and its Top strip (moss lip, walk line `walkLinePx` from the strip's
// top edge) laid along every tread and gentle slope. Steep risers show the Fill, like cut stone
// steps. Every riser and open piece end gets a band of the family's cliff-face art (a painted
// rock wall; legacy Wall_Side strips put their ragged painted edge on the riser line), tops get a
// small moss lip over each wall, and the fill darkens with depth so big rock masses read as mass,
// not wallpaper. Only the points and sprites are saved; the meshes are rebuilt when enabled.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class MraTerrain : MonoBehaviour
{
    public Vector2[] surface = new Vector2[0];
    public float bottom = -8f;
    public Sprite fill, top, face;
    [Tooltip("A floating slab's underside: a stone band (a kit's Ceiling_Under, flipped) along the bottom edge, so it never shows a raw cut.")]
    public Sprite under;
    [Tooltip("Tints the underside band (to sit with the cave rock it hangs from).")] public Color underTint = Color.white;
    [Tooltip("Top strip: the walk line, px from its top edge.")] public float walkLinePx = 96f;
    [Tooltip("Slopes steeper than this (rise over run) get no top strip.")] public float maxStripSlope = 1.2f;
    public bool solid = true, leftFace, rightFace;
    [Tooltip("Draw the face art on every riser and on the piece's open ends.")] public bool riserFaces = true;
    [Tooltip("Face art with a ragged painted edge (legacy Wall_Side): the u of that edge; 0 for an opaque face (Cliff_Face).")]
    [Range(0f, 1f)] public float faceEdgeU;
    [Tooltip("How far the face band reaches into the rock behind a riser (u).")] [Min(.2f)] public float faceDepth = 1.3f;
    [Tooltip("How far a top strip's moss lip overhangs a wall (u).")] [Min(0f)] public float lipOverhang = .22f;
    [Tooltip("The fill darkens over this depth below the walk line (u); 0 for flat fill.")] [Min(0f)] public float shadeDepth = 7f;
    [Range(.5f, 1f)] public float shadeFloor = .8f;
    [Tooltip("Opaque face art only: an irregular rock contour on the open side of every riser and end (visual; collision stays on the line).")]
    public bool roughContour = true;
    [Tooltip("How far the contour reaches out past the wall line at most (u).")] [Range(0f, .6f)] public float contourReach = .3f;
    [Header("Painted cliff kit (Batch 13): replaces the riser bands when set")]
    [Tooltip("Open face of a wall whose rock lies to the right (L) / left (R): 512x1024, collision line 64 px from the open edge, tiles vertically.")]
    public Sprite cliffSideL, cliffSideR;
    [Tooltip("Outer top corner: 512x512, walk line 96 px from the top, its bottom row joins the side's top row.")]
    public Sprite cornerTopL, cornerTopR;
    [Tooltip("Talus foot: 512x384, the lower walk line 342 px from the top.")]
    public Sprite cornerFootL, cornerFootR;
    public const int CliffSideOrder = -35, CliffCornerOrder = -34, CliffFootOrder = -33;
    public bool HasCliffKit => cliffSideL != null && cliffSideR != null && cornerTopL != null && cornerTopR != null && cornerFootL != null && cornerFootR != null;
    [Tooltip("Off for a wall that must not be climbed with Climbing Moss (a relic gate's face, a lintel).")] public bool clingable = true;
    public Color tint = Color.white;
    public int sortingOffset;

    public const int FillOrder = -40, FaceOrder = -38, TopOrder = -36;
    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    readonly List<Object> owned = new List<Object>();

    void OnEnable() => Rebuild();
    void OnDisable() => Clear();

    void Clear()
    {
        foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
        owned.Clear();
    }

    public void Rebuild()
    {
        Clear();
        if (surface == null || surface.Length < 2) return;
        if (fill != null) Part("Fill", fill, FillOrder, FillMesh());
        if (top != null) Part("Top", top, TopOrder, TopMesh());
        if (face != null && leftFace) Part("Face (left)", face, FaceOrder, FaceMesh(surface[0], -1f));
        if (face != null && rightFace) Part("Face (right)", face, FaceOrder, FaceMesh(surface[surface.Length - 1], 1f));
        if (HasCliffKit) CliffParts();
        else if (face != null && riserFaces) Part("Riser faces", face, FaceOrder, RiserMesh());
        if (under != null) Part("Underside", under, FaceOrder, UnderMesh());

        TryGetComponent(out PolygonCollider2D collider);
        if (solid)
        {
            gameObject.layer = LayerMask.NameToLayer("Ground");
            if (collider == null) collider = gameObject.AddComponent<PolygonCollider2D>();
            var path = new List<Vector2>(surface) { new Vector2(surface[surface.Length - 1].x, bottom), new Vector2(surface[0].x, bottom) };
            collider.pathCount = 1; collider.SetPath(0, path.ToArray());
            if (!TryGetComponent(out WallSurface wall)) wall = gameObject.AddComponent<WallSurface>();
            wall.allowsWallCling = clingable;
        }
        else if (collider != null) collider.enabled = false;
    }

    void Part(string label, Sprite sprite, int order, Mesh mesh)
    {
        var obj = new GameObject(label + " (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        obj.transform.SetParent(transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var view = obj.AddComponent<MeshRenderer>();
        view.sharedMaterial = TerrainMaterial.Shared; view.sortingOrder = order + sortingOffset;
        var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, sprite.texture); view.SetPropertyBlock(block);
        owned.Add(obj); owned.Add(mesh);
    }

    Vector2 WorldSize(Sprite s) => new Vector2(s.texture.width, s.texture.height) / s.pixelsPerUnit;
    Vector3 Origin => transform.position;

    // Columns under each segment, down to the bottom, with world-space texture coordinates. Each
    // column darkens from the walk line to `shadeFloor` over `shadeDepth`, then stays there.
    Mesh FillMesh()
    {
        Vector2 size = WorldSize(fill);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); var colors = new List<Color>();
        Color deep = new Color(tint.r * shadeFloor, tint.g * shadeFloor, tint.b * shadeFloor, tint.a);
        void Vertex(Vector2 p, Color c) { v.Add(p); Vector2 w = p + (Vector2)Origin; uv.Add(new Vector2(w.x / size.x, w.y / size.y)); colors.Add(c); }
        // At a wall, the higher column shades from the lower ground's height down, so the two
        // columns meet with the same shade (no seam under the wall); above that it stays light.
        float Ref(int i, int beside)
        {
            Vector2 p = surface[i];
            if (beside >= 0 && beside < surface.Length && Mathf.Abs(surface[beside].x - p.x) < 1e-4f) return Mathf.Min(p.y, surface[beside].y);
            return p.y;
        }
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (Mathf.Abs(b.x - a.x) < 1e-4f) continue;   // a riser: its neighbours' columns meet under it
            int n = v.Count;
            float ra = Ref(i, i - 1), rb = Ref(i + 1, i + 2);
            float da = Mathf.Max(bottom, ra - shadeDepth), db = Mathf.Max(bottom, rb - shadeDepth);
            Color ca = shadeDepth > 0f ? Color.Lerp(tint, deep, (ra - da) / shadeDepth) : tint, cb = shadeDepth > 0f ? Color.Lerp(tint, deep, (rb - db) / shadeDepth) : tint;
            // Rows: bottom, the shade's floor, the reference height, the walk line.
            Vertex(new Vector2(a.x, bottom), ca); Vertex(new Vector2(a.x, da), ca); Vertex(new Vector2(a.x, Mathf.Min(ra, a.y)), tint); Vertex(a, tint);
            Vertex(new Vector2(b.x, bottom), cb); Vertex(new Vector2(b.x, db), cb); Vertex(new Vector2(b.x, Mathf.Min(rb, b.y)), tint); Vertex(b, tint);
            for (int r = 0; r < 3; r++) tris.AddRange(new[] { n + r, n + r + 1, n + 4 + r + 1, n + r, n + 4 + r + 1, n + 4 + r });
        }
        var mesh = Build("fill", v, uv, tris, tint);
        mesh.SetColors(colors);
        return mesh;
    }

    // A band of face art behind every riser (and the piece's open ends), toward the rock: it fades
    // out into the fill behind, and a ragged-edged face overhangs the riser line slightly.
    Mesh RiserMesh()
    {
        Vector2 size = WorldSize(face);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); var colors = new List<Color>();
        Color clear = new Color(tint.r, tint.g, tint.b, 0f);
        void Band(float x, float low, float high, float side)
        {
            if (high - low < .3f) return;
            // A short step gets a narrow band (a wide one reads as a smudge on the fill); a tall
            // wall gets the full depth.
            float depth = Mathf.Clamp((high - low) * .45f, .45f, faceDepth);
            float outward = faceEdgeU > 0f ? .3f : 0f;
            float xOut = x - side * outward, xMid = x + side * depth * .55f, xIn = x + side * depth;
            float uLine = faceEdgeU > 0f ? faceEdgeU - .06f : 0f;
            float U(float wx) => Mathf.Clamp01(uLine + (faceEdgeU > 0f ? -1f : 1f) * side * (wx - x) / size.x);
            // Three rows: the foot fades out under the lower ground (no hard edge where a ramp
            // or a tread meets the wall), then solid face up to just under the top's lip.
            float yFoot = Mathf.Max(bottom, low - 1.1f), y0 = Mathf.Max(bottom, low - .1f), y1 = high - .08f;
            int n = v.Count;
            foreach (var (wx, c) in new[] { (xOut, tint), (xMid, tint), (xIn, clear) })
                foreach (var (wy, fade) in new[] { (yFoot, true), (y0, false), (y1, false) })
                {
                    v.Add(new Vector3(wx, wy)); uv.Add(new Vector2(U(wx), (wy + Origin.y) / size.y));
                    colors.Add(fade ? new Color(c.r, c.g, c.b, 0f) : c);
                }
            // Columns of 3 rows: vertex (col, row) = n + col * 3 + row.
            for (int col = 0; col < 2; col++)
                for (int row = 0; row < 2; row++)
                {
                    int a = n + col * 3 + row, b = a + 1, c2 = a + 3, d = a + 4;
                    tris.AddRange(new[] { a, b, d, a, d, c2 });
                }
            if (faceEdgeU <= 0f && roughContour) Contour(x, low, high, side);
        }
        // A natural rock edge for an opaque face: the wall's silhouette broken by an irregular
        // outline (a talus flare at the foot, a ledge now and then, narrowing under the moss lip),
        // in the face art, with a darker weathered rim along it. Deterministic from the position.
        void Contour(float x, float low, float high, float side)
        {
            float H = high - low; if (H < .35f) return;
            float Hash(float k) { float h = Mathf.Sin((x + Origin.x) * 12.9898f + k * 78.233f) * 43758.5453f; return h - Mathf.Floor(h); }
            // Smooth, layered undulation (no regular teeth), a few rounded ledges, a talus foot, and
            // the profile drawn back under the moss lip.
            float p1 = Hash(1f) * 6.28f, p2 = Hash(2f) * 6.28f, p3 = Hash(3f) * 6.28f;
            int ledges = Mathf.FloorToInt(H / 1.8f);
            var ledgeAt = new List<float>();
            for (int l = 0; l < ledges; l++) ledgeAt.Add(low + H * (.2f + .6f * Hash(10f + l)));
            float Profile(float wy)
            {
                float o = .5f + .26f * Mathf.Sin(wy * 1.3f + p1) + .16f * Mathf.Sin(wy * 3.1f + p2) + .08f * Mathf.Sin(wy * 7.7f + p3);
                foreach (float l in ledgeAt) { float d = (wy - l) / .28f; o += .55f * Mathf.Exp(-d * d) * (wy < l ? 1f : .6f); }
                float foot = wy - low; if (foot < .7f) o += .6f * (1f - foot / .7f) * (1f - foot / .7f);
                float lip = high - wy; if (lip < .5f) o *= .2f + .8f * (lip / .5f);
                return Mathf.Clamp(o * contourReach, 0f, contourReach * 1.7f);
            }
            var ys = new List<float>(); var outs = new List<float>();
            for (float wy = Mathf.Max(bottom, low - .05f); wy < high - .03f; wy += .11f) { ys.Add(wy); outs.Add(Profile(wy)); }
            ys.Add(high - .02f); outs.Add(contourReach * .1f);
            Vector2 faceSize = WorldSize(face);
            float U(float d) => Mathf.Clamp01(.02f + d / faceSize.x);
            Color rim = new Color(tint.r * .5f, tint.g * .47f, tint.b * .44f, tint.a * .9f);
            int n = v.Count;
            for (int k = 0; k < ys.Count; k++)
            {
                float o = outs[k], wy = ys[k], v2 = (wy + Origin.y) / faceSize.y;
                // Rock: from the wall line out to the contour; rim: a thin dark band just inside the contour.
                v.Add(new Vector3(x, wy)); uv.Add(new Vector2(U(0f), v2)); colors.Add(tint);
                v.Add(new Vector3(x - side * Mathf.Max(0f, o - .07f), wy)); uv.Add(new Vector2(U(o - .07f), v2)); colors.Add(tint);
                v.Add(new Vector3(x - side * o, wy)); uv.Add(new Vector2(U(o), v2)); colors.Add(rim);
            }
            for (int k = 0; k < ys.Count - 1; k++)
            {
                int a = n + k * 3, b = a + 3;
                tris.AddRange(new[] { a, b, b + 1, a, b + 1, a + 1, a + 1, b + 1, b + 2, a + 1, b + 2, a + 2 });
            }
        }
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (Mathf.Abs(b.x - a.x) > 1e-4f) continue;
            // Going up, the rock is to the right; going down, to the left.
            Band(a.x, Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y), b.y > a.y ? 1f : -1f);
        }
        Vector2 first = surface[0], last = surface[surface.Length - 1];
        Band(first.x, Mathf.Max(bottom, first.y - 14f), first.y, 1f);
        Band(last.x, Mathf.Max(bottom, last.y - 14f), last.y, -1f);
        var mesh = Build("risers", v, uv, tris, tint);
        mesh.SetColors(colors);
        return mesh;
    }

    // The top strip along each tread or gentle slope: from the walk line it rises by the strip's
    // painted lip and hangs down by the rest of its height. Its lowest `BottomFade` fades out
    // (vertex alpha), so strips whose paint runs to their bottom edge still melt into the fill.
    // Only the strip's upper `MaxBelow` hangs below the walk line: legacy kit strips are opaque all
    // the way down, and on stairs a deeper strip would cover the tread below in hard columns.
    const float BottomFade = .6f;
    [Tooltip("How far the top strip hangs below the walk line: 1.4 for strips that feather into the fill, about 0.5 (the moss lip) for opaque legacy strips.")]
    [Min(.2f)] public float stripDepth = 1.4f;
    Mesh TopMesh()
    {
        Vector2 size = WorldSize(top);
        float above = walkLinePx / top.pixelsPerUnit, below = Mathf.Min(size.y - above, stripDepth), fade = Mathf.Min(BottomFade, below * .5f);
        float v0 = 1f - (above + below) / size.y, vFade = v0 + fade / size.y;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); var colors = new List<Color>();
        Color solidTint = tint, clear = new Color(tint.r, tint.g, tint.b, 0f);
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            float run = b.x - a.x;
            if (run < 1e-4f || Mathf.Abs(b.y - a.y) / run > maxStripSlope) continue;
            // A moss lip over a wall below: at a riser going up into this tread, a drop after it,
            // and the piece's open ends.
            bool wallBefore = i == 0 || Mathf.Abs(surface[i - 1].x - a.x) < 1e-4f && surface[i - 1].y < a.y - .2f;
            bool wallAfter = i + 2 >= surface.Length || Mathf.Abs(surface[i + 2].x - b.x) < 1e-4f && surface[i + 2].y < b.y - .2f;
            if (wallBefore) a.x -= lipOverhang;
            if (wallAfter) b.x += lipOverhang;
            int n = v.Count;
            float ua = (a.x + Origin.x) / size.x, ub = (b.x + Origin.x) / size.x;
            // Bottom row (clear), fade row (solid), top row (solid): two quads per segment.
            v.Add(new Vector3(a.x, a.y - below)); v.Add(new Vector3(a.x, a.y - below + fade)); v.Add(new Vector3(a.x, a.y + above));
            v.Add(new Vector3(b.x, b.y - below)); v.Add(new Vector3(b.x, b.y - below + fade)); v.Add(new Vector3(b.x, b.y + above));
            uv.Add(new Vector2(ua, v0)); uv.Add(new Vector2(ua, vFade)); uv.Add(new Vector2(ua, 1f));
            uv.Add(new Vector2(ub, v0)); uv.Add(new Vector2(ub, vFade)); uv.Add(new Vector2(ub, 1f));
            colors.AddRange(new[] { clear, solidTint, solidTint, clear, solidTint, solidTint });
            tris.AddRange(new[] { n, n + 1, n + 4, n, n + 4, n + 3, n + 1, n + 2, n + 5, n + 1, n + 5, n + 4 });
        }
        var mesh = Build("top", v, uv, tris, tint);
        mesh.SetColors(colors);
        return mesh;
    }

    // A cliff face standing on the piece's end, its painted face toward the open side.
    Mesh FaceMesh(Vector2 edge, float side)
    {
        Vector2 size = WorldSize(face);
        float w = Mathf.Min(size.x, 2.5f), x0 = side > 0f ? edge.x - w * .6f : edge.x - w * .4f, x1 = x0 + w;
        var v = new List<Vector3> { new Vector3(x0, bottom), new Vector3(x0, edge.y), new Vector3(x1, edge.y), new Vector3(x1, bottom) };
        float u0 = side > 0f ? 0f : 1f, u1 = side > 0f ? 1f : 0f;   // mirrored on the left end
        var uv = new List<Vector2>
        {
            new Vector2(u0, (bottom + Origin.y) / size.y), new Vector2(u0, (edge.y + Origin.y) / size.y),
            new Vector2(u1, (edge.y + Origin.y) / size.y), new Vector2(u1, (bottom + Origin.y) / size.y),
        };
        return Build("face", v, uv, new List<int> { 0, 1, 2, 0, 2, 3 }, tint);
    }

    // ---------------------------------------------------------------- the painted cliff kit

    // Every wall (riser) and open piece end, assembled from the kit's painted pieces, registered on
    // the collision line exactly as drawn (Batch 13 registration): the top corner with its moss lip
    // at the upper walk line, the side tiled down from the corner's bottom row (its repeat phase
    // anchored there), and the talus foot on the lower walk line. Each piece fades out over its
    // last 0.8 u into the rock (the fill and top strip carry on behind); a foot cropped by a short
    // wall fades out at its cut; a corner cropped by a short wall ends under the foot.
    sealed class Batch { public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> tris = new List<int>(); public readonly List<Color> colors = new List<Color>(); }

    void CliffParts()
    {
        var sideL = new Batch(); var sideR = new Batch(); var topL = new Batch(); var topR = new Batch(); var footL = new Batch(); var footR = new Batch();
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (Mathf.Abs(b.x - a.x) > 1e-4f || Mathf.Abs(b.y - a.y) < .4f) continue;
            bool up = b.y > a.y;   // going up, the rock is to the right: the L pieces
            Wall(a.x, Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y), up, up ? sideL : sideR, up ? topL : topR, up ? footL : footR, true);
        }
        Vector2 first = surface[0], last = surface[surface.Length - 1];
        Wall(first.x, Mathf.Max(bottom, first.y - 14f), first.y, true, sideL, topL, footL, false);
        Wall(last.x, Mathf.Max(bottom, last.y - 14f), last.y, false, sideR, topR, footR, false);
        Emit("Cliff sides (L)", cliffSideL, CliffSideOrder, sideL); Emit("Cliff sides (R)", cliffSideR, CliffSideOrder, sideR);
        Emit("Cliff corners (L)", cornerTopL, CliffCornerOrder, topL); Emit("Cliff corners (R)", cornerTopR, CliffCornerOrder, topR);
        Emit("Cliff feet (L)", cornerFootL, CliffFootOrder, footL); Emit("Cliff feet (R)", cornerFootR, CliffFootOrder, footR);
    }

    void Emit(string label, Sprite sprite, int order, Batch batch)
    {
        if (batch.v.Count == 0) return;
        var mesh = Build(label, batch.v, batch.uv, batch.tris, tint);
        mesh.SetColors(batch.colors);
        Part(label, sprite, order, mesh);
    }

    // One wall at x from `low` to `high`; rockRight: L pieces (rock to the right of the line).
    void Wall(float x, float low, float high, bool rockRight, Batch side, Batch top, Batch foot, bool withFoot)
    {
        Sprite sSide = rockRight ? cliffSideL : cliffSideR, sTop = rockRight ? cornerTopL : cornerTopR, sFoot = rockRight ? cornerFootL : cornerFootR;
        const float LinePx = 64f, Fade = 1.4f;
        // Columns: the art's open edge, the start of the fade into the rock, the rock edge.
        float[] Xs(Sprite s, out float[] us, out float[] alpha)
        {
            float w = s.rect.width / s.pixelsPerUnit, line = LinePx / s.pixelsPerUnit, f = Fade / w;
            if (rockRight) { float l = x - line; us = new[] { 0f, 1f - f, 1f }; alpha = new[] { 1f, 1f, 0f }; return new[] { l, l + w - Fade, l + w }; }
            float r = x + line; us = new[] { 0f, f, 1f }; alpha = new[] { 0f, 1f, 1f }; return new[] { r - w, r - w + Fade, r };
        }
        float ppuTop = sTop.pixelsPerUnit, hTop = sTop.rect.height / ppuTop;
        float topEdge = high + 96f / ppuTop, cornerBottom = topEdge - hTop;
        // The top corner, cut at the lower walk line on a short wall.
        float cTop = Mathf.Max(cornerBottom, low);
        {
            var xs = Xs(sTop, out var us, out var xa);
            Patch(top, xs, us, xa, new[] { cTop, topEdge }, new[] { (cTop - cornerBottom) / hTop, 1f }, new[] { 1f, 1f });
        }
        // The side, from the corner's bottom down past the lower walk line (or to the piece's base).
        float hSide = sSide.rect.height / sSide.pixelsPerUnit, sideEnd = withFoot ? low - .7f : low;
        if (cornerBottom > sideEnd)
        {
            var xs = Xs(sSide, out var us, out var xa);
            float vEnd = 1f - (cornerBottom - sideEnd) / hSide;
            // Under the lower ground the side fades out (no hard edge where it stops).
            float solidFrom = Mathf.Min(cornerBottom, low + .1f), vSolid = 1f - (cornerBottom - solidFrom) / hSide;
            if (withFoot) Patch(side, xs, us, xa, new[] { sideEnd, solidFrom, cornerBottom }, new[] { vEnd, vSolid, 1f }, new[] { 0f, 1f, 1f });
            else
            {
                // An open end: the rock fades out over its last unit.
                float yF = Mathf.Min(cornerBottom, sideEnd + 1f), vF = 1f - (cornerBottom - yF) / hSide;
                Patch(side, xs, us, xa, new[] { sideEnd, yF, cornerBottom }, new[] { vEnd, vF, 1f }, new[] { 0f, 1f, 1f });
            }
        }
        if (!withFoot) return;
        // The talus foot on the lower walk line, cropped (and faded at its cut) under the moss lip.
        float ppuFoot = sFoot.pixelsPerUnit, hFoot = sFoot.rect.height / ppuFoot;
        float footTop = low + 342f / ppuFoot, footBottom = footTop - hFoot, cut = Mathf.Min(footTop, high - .45f);
        if (cut < low + .25f) return;
        {
            var xs = Xs(sFoot, out var us, out var xa);
            float fadeFrom = Mathf.Max(footBottom + .05f, cut - .35f);
            bool cropped = cut < footTop - 1e-3f;
            float soft = Mathf.Min(fadeFrom - .02f, footBottom + .3f);   // its lowest rows melt into the tread
            Patch(foot, xs, us, xa, new[] { footBottom, soft, fadeFrom, cut }, new[] { 0f, (soft - footBottom) / hFoot, (fadeFrom - footBottom) / hFoot, (cut - footBottom) / hFoot }, new[] { 0f, 1f, 1f, cropped ? 0f : 1f });
        }
    }

    // A grid patch: columns (x, u, alpha) by rows (y, v, alpha), vertex alpha the product.
    void Patch(Batch b, float[] xs, float[] us, float[] xa, float[] ys, float[] vs, float[] ya)
    {
        int n = b.v.Count, cols = xs.Length;
        for (int r = 0; r < ys.Length; r++)
            for (int c = 0; c < cols; c++)
            {
                b.v.Add(new Vector3(xs[c], ys[r])); b.uv.Add(new Vector2(us[c], vs[r]));
                b.colors.Add(new Color(tint.r, tint.g, tint.b, tint.a * xa[c] * ya[r]));
            }
        for (int r = 0; r < ys.Length - 1; r++)
            for (int c = 0; c < cols - 1; c++)
            {
                int q = n + r * cols + c;
                b.tris.AddRange(new[] { q, q + cols, q + cols + 1, q, q + cols + 1, q + 1 });
            }
    }

    // The underside band: the kit's ceiling strip upside down, its crisp painted edge on the slab's
    // bottom line and its fade rising into the fill.
    Mesh UnderMesh()
    {
        Vector2 size = WorldSize(under);
        const float band = 1f, solidV = 120f / 384f;   // Ceiling_Under: the painted stones fill its top 120 of 384 px
        float x0 = surface[0].x, x1 = surface[surface.Length - 1].x;
        var v = new List<Vector3> { new Vector3(x0, bottom - .06f), new Vector3(x0, bottom + band), new Vector3(x1, bottom + band), new Vector3(x1, bottom - .06f) };
        float u0 = (x0 + Origin.x) / size.x, u1 = (x1 + Origin.x) / size.x;
        var uv = new List<Vector2> { new Vector2(u0, 1f), new Vector2(u0, 1f - solidV), new Vector2(u1, 1f - solidV), new Vector2(u1, 1f) };
        return Build("under", v, uv, new List<int> { 0, 1, 2, 0, 2, 3 }, tint * underTint);
    }

    Mesh Build(string label, List<Vector3> v, List<Vector2> uv, List<int> tris, Color color)
    {
        var mesh = new Mesh { name = name + " " + label + " (generated)", hideFlags = HideFlags.DontSave };
        if (v.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0);
        var colors = new Color[v.Count]; for (int i = 0; i < colors.Length; i++) colors[i] = color;
        mesh.colors = colors;
        mesh.RecalculateBounds();
        return mesh;
    }

    // The walk line's height at local x (the highest surface point there), for placing things on it.
    public float HeightAt(float x)
    {
        float best = float.NegativeInfinity;
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (x < Mathf.Min(a.x, b.x) - 1e-4f || x > Mathf.Max(a.x, b.x) + 1e-4f) continue;
            best = Mathf.Max(best, Mathf.Abs(b.x - a.x) < 1e-4f ? Mathf.Max(a.y, b.y) : Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x)));
        }
        return best;
    }
}

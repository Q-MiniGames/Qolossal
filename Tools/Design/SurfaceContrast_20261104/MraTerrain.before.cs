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
public sealed partial class MraTerrain : MonoBehaviour
{
    public Vector2[] surface = new Vector2[0];
    public float bottom = -8f;
    public Sprite fill, top, face;
    [Tooltip("A floating slab's underside: a stone band (a kit's Ceiling_Under, flipped) along the bottom edge, so it never shows a raw cut.")]
    public Sprite under;
    [Tooltip("Use the full, unflipped underside painting with attachment fades. Enabled only by reviewed regional integration.")]
    public bool fullUnderSilhouette;
    [Tooltip("Tints the underside band (to sit with the cave rock it hangs from).")] public Color underTint = Color.white;
    [Tooltip("Top strip: walk line in reference pixels at 120 PPU, from its top edge. Keep this value when swapping to exact-2x/240 PPU art.")] public float walkLinePx = 96f;
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
    [Tooltip("Town steps (Qvale): each top corner follows the tread beside its riser (cropped to a short tread, bent along a slope) and the talus feet are left off.")]
    public bool cornersFollowTread;
    [Tooltip("Town steps: a course of the corner's stone column (reference rows at 120 PPU from the top, joint to joint) repeated down a riser taller than the corner, in place of the side art. Zero: use the side art.")]
    public Vector2 townCourseL, townCourseR;
    [Tooltip("Town step set (Qvale, Batches 16e-16f): one painted piece per riser instead of the cliff kit. Short: risers up to 1.3 u; Medium: up to 2 u; taller: Outer, wall courses and Inner. Pivots on the riser line and walk line.")]
    public Sprite stepShortL, stepShortR, stepMediumL, stepMediumR, stepOuterL, stepOuterR, stepInnerL, stepInnerR;
    [Tooltip("Wall courses (2 u, mortar joints top and bottom), painted for L, mirrored for R.")] public Sprite[] stepWalls = new Sprite[0];
    [Tooltip("Slope-top steps (Batch 16g): the same pieces with the ground beside the top painted climbing, for a riser whose upper ground slopes. Placed unbent.")]
    public Sprite stepShortSlopeL, stepShortSlopeR, stepMediumSlopeL, stepMediumSlopeR, stepOuterSlopeL, stepOuterSlopeR;
    [Tooltip("Each slope-top piece's painted upper walk line in reference pixels at 120 PPU: (inland from the riser line, rise above its walk row).")]
    public Vector2[] shortSlopeRise = new Vector2[0], mediumSlopeRise = new Vector2[0], outerSlopeRise = new Vector2[0];
    [Tooltip("Each slope-top piece's upper walk row at the riser (reference pixels at 120 PPU from the top): Short, Medium, Outer.")]
    public Vector3 slopeTopRows = new Vector3(416f, 310f, 385f);
    [Tooltip("Natural step sets (Batch 23): every piece's upper and lower runs are the strip's own columns from the riser line (mirrored going left), slope tops included. Treads take their strip layout from any step beside them, switch layout at a strip joint where two steps claim one tread, keep seams inside short treads, and the piece's open ends keep the cliff kit.")]
    public bool stripDerivedRuns;
    [Tooltip("Natural sets: the pieces take the fill's depth shade, so their soil never shows lighter than the fill beside it.")]
    public bool shadeLikeFill;
    [Tooltip("Natural sets: on a wall too short for the Inner to stay under the Outer's joint, the Inner fades out under the Outer's kerb run, so it never covers the top's grass and kerb.")]
    public bool innerUnderKerb;
    [Tooltip("Natural sets: each piece's rock body fades into the fill at the wall courses' depth (1.33-2.13 u in), and a lower run's soil stays unbent and fades out near the face, so no band, seam or smear shows where a piece meets the fill.")]
    public bool bodyFade;
    [Tooltip("Off: tried 10 Oct for the Batch 23b-g Outers' bulging bottom (walls start higher, the Outer fades into them), but it bared the fill's straight edge at the riser. Kept for reference; the fix is art.")]
    public bool tidyOuterJoint;
    public const int StepWallOrder = -35, StepOrder = -34;
    public bool HasStepSet => stepShortL != null && stepShortR != null && stepMediumL != null && stepMediumR != null && stepOuterL != null && stepOuterR != null && stepInnerL != null && stepInnerR != null && stepWalls != null && stepWalls.Length > 0;
    public bool HasCliffKit => cliffSideL != null && cliffSideR != null && cornerTopL != null && cornerTopR != null && cornerFootL != null && cornerFootR != null;
    [Tooltip("Off for a wall that must not be climbed with Climbing Moss (a relic gate's face, a lintel).")] public bool clingable = true;
    public Color tint = Color.white;
    public int sortingOffset;
    [Tooltip("Exact generated art-part labels replaced by fitted scene artwork. Rendering only; the surface and collision remain authoritative.")]
    public string[] replacedPaintedParts = System.Array.Empty<string>();

    public const int FillOrder = -40, FaceOrder = -38, TopOrder = -36;
    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    readonly List<Object> owned = new List<Object>();

    void OnEnable() { Rebuild(); RefreshRegisteredTopJoinPeer(); }
    void OnDisable() { Clear(); RefreshRegisteredTopJoinPeer(); }

    void Clear()
    {
        foreach (var o in owned) if (o != null) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
        owned.Clear();
    }

    public void Rebuild()
    {
        Clear();
        risersCache = null; seamXs = null;
        if (surface == null || surface.Length < 2) return;
        if (fill != null) Part("Fill", fill, FillOrder, FillMesh());
        if (top != null) Part("Top", top, TopOrder, ApplyRegisteredTopJoin(BlendCaveTop(TopMesh())));
        if (face != null && leftFace) Part("Face (left)", face, FaceOrder, FaceMesh(surface[0], -1f));
        if (face != null && rightFace) Part("Face (right)", face, FaceOrder, FaceMesh(surface[surface.Length - 1], 1f));
        if (HasStepSet && top != null) { StepParts(); if (stripDerivedRuns && HasCliffKit) CliffEnds(); }
        else if (HasCliffKit) CliffParts();
        else if (face != null && riserFaces) Part("Riser faces", face, FaceOrder, BlendCaveRisers(RiserMesh()));
        if (under != null) Part("Underside", under, FaceOrder, UnderMesh());

        TryGetComponent(out PolygonCollider2D collider);
        if (solid)
        {
            gameObject.layer = LayerMask.NameToLayer("Ground");
            if (collider == null) collider = gameObject.AddComponent<PolygonCollider2D>();
            var path = new List<Vector2>(surface) { new Vector2(surface[surface.Length - 1].x, bottom), new Vector2(surface[0].x, bottom) };
            if (!BuildCaveCollision(collider)) { collider.pathCount = 1; collider.SetPath(0, path.ToArray()); }
            if (!TryGetComponent(out WallSurface wall)) wall = gameObject.AddComponent<WallSurface>();
            wall.allowsWallCling = clingable;
        }
        else if (collider != null) collider.enabled = false;
    }

    void Part(string label, Sprite sprite, int order, Mesh mesh)
    {
        if (replacedPaintedParts != null && System.Array.IndexOf(replacedPaintedParts, label) >= 0)
        {
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
            return;
        }
        ClipCaveMesh(mesh, label == "Riser faces" || label.StartsWith("Face") || label.Contains("wall") || label.Contains("outer") || label.Contains("inner"));
        var obj = new GameObject(label + " (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        obj.transform.SetParent(transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var view = obj.AddComponent<MeshRenderer>();
        view.sharedMaterial = TerrainMaterial.Shared;
        view.sortingOrder = ChamberPartOrder(label, order + sortingOffset + (label == "Top" && chamberBackdrop != null ? chamberTopPriority : 0));
        var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, sprite.texture); view.SetPropertyBlock(block);
        owned.Add(obj); owned.Add(mesh);
    }

    Vector2 WorldSize(Sprite s) => new Vector2(s.texture.width, s.texture.height) / s.pixelsPerUnit;
    Vector3 Origin => transform.position;

    // Columns under each segment, down to the bottom, with world-space texture coordinates. Each
    // column darkens from the walk line to `shadeFloor` over `shadeDepth`, then stays there.
    Mesh FillMesh()
    {
        if (CanBlendCave) return BlendedCaveFill();
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
            return Mathf.Min(p.y, EndRef(i));
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

    // The fill's shade at (x, y), as FillMesh draws it: each ground segment's columns darken from
    // their reference height (the lower ground's, at a wall) down to shadeFloor over shadeDepth,
    // the reference running straight between the segment's ends.
    float FillShade(float x, float y)
    {
        if (shadeDepth <= 0f) return 1f;
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (b.x - a.x < 1e-4f || x < a.x || x > b.x) continue;
            float ra = i > 0 && Mathf.Abs(surface[i - 1].x - a.x) < 1e-4f ? Mathf.Min(a.y, surface[i - 1].y) : Mathf.Min(a.y, EndRef(i));
            float rb = i + 2 < surface.Length && Mathf.Abs(surface[i + 2].x - b.x) < 1e-4f ? Mathf.Min(b.y, surface[i + 2].y) : Mathf.Min(b.y, EndRef(i + 1));
            float r = Mathf.Lerp(ra, rb, (x - a.x) / (b.x - a.x));
            return Mathf.Lerp(1f, shadeFloor, Mathf.Clamp01((r - y) / shadeDepth));
        }
        return 1f;
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
                float inset = Mathf.Max(0f, o - .07f);
                v.Add(new Vector3(x - side * inset, wy)); uv.Add(new Vector2(U(inset), v2)); colors.Add(tint);
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
    [Tooltip("Reviewed natural terrain: retain the kerb and shorten only its extra soil tail.")]
    public bool shortNaturalSoilTail;
    Mesh TopMesh()
    {
        Vector2 size = WorldSize(top);
        float above = walkLinePx / StepPpu, below = Mathf.Min(size.y - above, stripDepth), fade = Mathf.Min(BottomFade, below * .5f);
        if (shortNaturalSoilTail && HasStepSet && stripDerivedRuns && below > KerbPx / StepPpu)
        {
            below = Mathf.Min(below, .9f);
            fade = Mathf.Min(.2f, Mathf.Max(0f, below - KerbPx / StepPpu));
        }
        float v0 = 1f - (above + below) / size.y, vFade = v0 + fade / size.y, vWalk = 1f - above / size.y;
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
            // With a step set the strip takes over from each step piece's kerb run at a mortar joint
            // (StepSeams), lined up with the piece's own stones; the segment is split at those seams.
            bool steps = HasStepSet;
            var cuts = new List<float> { a.x, b.x };
            NativeStripLimits(i,out float nativeStart,out float nativeEnd,out float nativeBlend,out bool nativeLeft,out bool nativeRight);
            if(nativeBlend>0)
            {
                // Several fade samples keep this short optical join smooth even
                // on a long, otherwise single-quad tread. UV density is unchanged.
                for(int j=1;j<=4;j++)
                {
                    float l=nativeStart+nativeBlend*j/4f,r=nativeEnd-nativeBlend*j/4f;
                    if(nativeLeft&&l>a.x+.00001f&&l<b.x-.00001f)cuts.Add(l);
                    if(nativeRight&&r>a.x+.00001f&&r<b.x-.00001f)cuts.Add(r);
                }
                cuts.Sort();
            }
            if (steps)
            {
                StepSeams();
                foreach (float sx in seamXs) foreach (float e in new[] { sx - SeamBlend, sx + SeamBlend }) if (e > a.x && e < b.x) cuts.Add(e);
                foreach (var r in risersCache) foreach (float e in new[] { r.x - GrassEdgeTo, r.x - GrassEdgeFrom, r.x + GrassEdgeFrom, r.x + GrassEdgeTo }) if (e > a.x && e < b.x) cuts.Add(e);
                for (float x = a.x + .5f; x < b.x; x += stripDerivedRuns ? .1f : .5f) cuts.Add(x);
                // A tread that switches layout (natural sets) cross-fades the two over the switch.
                int tr = TreadAt((a.x + b.x) * .5f);
                if (!float.IsNaN(treadSwitch[tr])) foreach (float e in new[] { treadSwitch[tr] - SwitchBlend, treadSwitch[tr] + SwitchBlend }) if (e > a.x && e < b.x) cuts.Add(e);
                cuts.Sort();
            }
            int tread = steps ? TreadAt((a.x + b.x) * .5f) : 0;
            float sw = steps ? treadSwitch[tread] : float.NaN;
            for (int k = 0; k < cuts.Count - 1; k++)
            {
                if (cuts[k + 1] - cuts[k] < 1e-4f) continue;
                Vector2 p = Vector2.Lerp(a, b, (cuts[k] - a.x) / (b.x - a.x)), q = Vector2.Lerp(a, b, (cuts[k + 1] - a.x) / (b.x - a.x));
                float xm = (p.x + q.x) * .5f;
                bool blend = !float.IsNaN(sw) && Mathf.Abs(xm - sw) < SwitchBlend;
                // In the switch blend the quad is drawn twice: the left layout fading out, the right in.
                for (int pass = 0; pass < (blend ? 2 : 1); pass++)
                {
                bool rightLayout = blend ? pass == 1 : !float.IsNaN(sw) && xm > sw;
                float Ramp(float x) => !blend ? 1f : pass == 1 ? Mathf.InverseLerp(sw - SwitchBlend, sw + SwitchBlend, x) : Mathf.InverseLerp(sw + SwitchBlend, sw - SwitchBlend, x);
                // The kerb and soil (below the walk line) and the grass (above it) take over at a seam
                // separately: the kerb sharply at its mortar joint, the grass over a wider blend.
                float ap = steps ? StripAlpha(p.x, false) * Ramp(p.x) : 1f, aq = steps ? StripAlpha(q.x, false) * Ramp(q.x) : 1f;
                float gp = steps ? StripAlpha(p.x, true) * Ramp(p.x) : 1f, gq = steps ? StripAlpha(q.x, true) * Ramp(q.x) : 1f;
                float nativeAp=NativeStripAlpha(p.x,nativeStart,nativeEnd,nativeBlend,nativeLeft,nativeRight),nativeAq=NativeStripAlpha(q.x,nativeStart,nativeEnd,nativeBlend,nativeLeft,nativeRight);
                ap*=nativeAp;gp*=nativeAp;aq*=nativeAq;gq*=nativeAq;
                if (ap <= 0f && aq <= 0f && gp <= 0f && gq <= 0f) continue;
                Color sp = solidTint, sq = solidTint, tp = solidTint, tq = solidTint; sp.a *= ap; sq.a *= aq; tp.a *= gp; tq.a *= gq;
                int n = v.Count;
                float ua = steps ? StripU(p.x, tread, size.x, rightLayout) : (p.x + Origin.x) / size.x, ub = steps ? StripU(q.x, tread, size.x, rightLayout) : (q.x + Origin.x) / size.x;
                // Rows: bottom (clear), fade row and the kerb stones (sharp), the moss line on the walk
                // line and the grass (wide blend); four quads per segment.
                float moss = MossPx / StepPpu, vMoss = vWalk - moss / size.y;
                if (steps && stripDerivedRuns && below > KerbPx / StepPpu)
                {
                    // Natural sets: the soil under the kerb course cross-fades with each piece's soil over
                    // the same band (StripSoilAlpha is SeamAlpha's soil fade, reversed), so no band of bare
                    // fill and no sharp edge shows where the strip takes over; seven rows, the kerb foot
                    // twice (soil alpha under it, kerb alpha over it).
                    float kerb = KerbPx / StepPpu, vKerb = vWalk - kerb / size.y;
                    float fp = StripSoilAlpha(p.x) * Ramp(p.x), fq = StripSoilAlpha(q.x) * Ramp(q.x);
                    Color dp = solidTint, dq = solidTint; dp.a *= fp; dq.a *= fq;
                    float[] dys = { -below, -below + fade, -kerb, -kerb, -moss, 0f, above }, vs = { v0, vFade, vKerb, vKerb, vMoss, vWalk, 1f };
                    Color[] cp = { clear, dp, dp, sp, sp, tp, tp }, cq = { clear, dq, dq, sq, sq, tq, tq };
                    for (int r = 0; r < 7; r++) { v.Add(new Vector3(p.x, p.y + dys[r])); uv.Add(new Vector2(ua, vs[r])); colors.Add(cp[r]); }
                    for (int r = 0; r < 7; r++) { v.Add(new Vector3(q.x, q.y + dys[r])); uv.Add(new Vector2(ub, vs[r])); colors.Add(cq[r]); }
                    for (int row = 0; row < 6; row++) if (row != 2) tris.AddRange(new[] { n + row, n + row + 1, n + row + 8, n + row, n + row + 8, n + row + 7 });
                    continue;
                }
                v.Add(new Vector3(p.x, p.y - below)); v.Add(new Vector3(p.x, p.y - below + fade)); v.Add(new Vector3(p.x, p.y - moss)); v.Add(new Vector3(p.x, p.y)); v.Add(new Vector3(p.x, p.y + above));
                v.Add(new Vector3(q.x, q.y - below)); v.Add(new Vector3(q.x, q.y - below + fade)); v.Add(new Vector3(q.x, q.y - moss)); v.Add(new Vector3(q.x, q.y)); v.Add(new Vector3(q.x, q.y + above));
                uv.Add(new Vector2(ua, v0)); uv.Add(new Vector2(ua, vFade)); uv.Add(new Vector2(ua, vMoss)); uv.Add(new Vector2(ua, vWalk)); uv.Add(new Vector2(ua, 1f));
                uv.Add(new Vector2(ub, v0)); uv.Add(new Vector2(ub, vFade)); uv.Add(new Vector2(ub, vMoss)); uv.Add(new Vector2(ub, vWalk)); uv.Add(new Vector2(ub, 1f));
                colors.AddRange(new[] { clear, sp, sp, tp, tp, clear, sq, sq, tq, tq });
                for (int row = 0; row < 4; row++) tris.AddRange(new[] { n + row, n + row + 1, n + row + 6, n + row, n + row + 6, n + row + 5 });
                }
            }
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

    // ---- Town step set -------------------------------------------------------------------------
    // Each riser gets one painted piece (or Outer + wall courses + Inner when tall). A piece is
    // drawn as a grid whose columns move as a whole to follow the ground beside the riser (the
    // upper run along the upper tread, the lower run along the lower one), so a kerb run bends with
    // a slope without stretching. The strip takes over from each kerb run at a mortar joint.
    // Registration uses the original 120-PPU pixel grid. Serialized landmarks, slope tables,
    // joint columns and all *Px constants stay in that grid; never double scene/builder values.
    // Normalize each sprite independently so old and exact-2x/240-PPU art can share one terrain.
    // E.g. reference column 256 samples native column 512 on a 2x sprite, at the same world x.
    // Keeping the reference grid also preserves seam search tolerances and mesh subdivision.
    const float StepPpu = 120f, StepRiserPx = 256f, StepUpperPx = 160f, StepColPx = 32f;
    static float ReferenceWidth(Sprite s) => s.rect.width / (s.pixelsPerUnit / StepPpu);
    static float ReferenceHeight(Sprite s) => s.rect.height / (s.pixelsPerUnit / StepPpu);

    struct Riser { public float x, low, high; public int top, foot, index; public bool rockRight; }

    List<Riser> Risers()
    {
        var list = new List<Riser>();
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (Mathf.Abs(b.x - a.x) > 1e-4f || Mathf.Abs(b.y - a.y) < .4f) continue;
            bool up = b.y > a.y;
            list.Add(new Riser { x = a.x, low = Mathf.Min(a.y, b.y), high = Mathf.Max(a.y, b.y), top = up ? i + 1 : i, foot = up ? i : i + 1, index = list.Count, rockRight = up });
        }
        // Tidy sets (bodyFade): where this piece's open end stands over another piece's lower ground
        // (a wall between two route pieces), that wall is a step too, with its foot on the other
        // piece (foot -1: no lower run of its own). An end whose neighbour is level or higher is buried.
        if (bodyFade)
        {
            buriedFirst = buriedLast = false;
            Vector2 first = surface[0], last = surface[surface.Length - 1];
            if (NeighbourGround(first.x, true, out float lowL)) { if (lowL < first.y - .4f) list.Insert(0, new Riser { x = first.x, low = lowL, high = first.y, top = 0, foot = -1, rockRight = true }); else if (lowL > first.y - .4f) buriedFirst = lowL >= first.y - .05f; }
            if (NeighbourGround(last.x, false, out float lowR)) { if (lowR < last.y - .4f) list.Add(new Riser { x = last.x, low = lowR, high = last.y, top = surface.Length - 1, foot = -1, rockRight = false }); else buriedLast = lowR >= last.y - .05f; }
            for (int k = 0; k < list.Count; k++) { var r = list[k]; r.index = k; list[k] = r; }
        }
        return list;
    }

    bool buriedFirst, buriedLast;

    // Tidy sets: at an open end over another piece's lower ground, the fill shades from that ground
    // (as under any wall), so the two pieces' fills meet with the same shade.
    float EndRef(int i)
    {
        if (!bodyFade || (i != 0 && i != surface.Length - 1)) return float.MaxValue;
        return NeighbourGround(surface[i].x, i == 0, out float y) ? y : float.MaxValue;
    }
    bool EndIsStep(bool first) => risersCache != null && risersCache.Count > 0 && (first ? risersCache[0].foot < 0 && risersCache[0].rockRight : risersCache[risersCache.Count - 1].foot < 0 && !risersCache[risersCache.Count - 1].rockRight);

    // The ground height of another terrain piece (same parent) whose end meets this piece's end at x:
    // a neighbour ending there (for this piece's first point) or starting there (for its last).
    bool NeighbourGround(float x, bool atFirst, out float y)
    {
        y = 0f;
        if (transform.parent == null) return false;
        foreach (var t in transform.parent.GetComponentsInChildren<MraTerrain>(true))
        {
            if (t == this || t.surface == null || t.surface.Length < 2 || !t.solid) continue;
            Vector2 o = (Vector2)t.transform.position - (Vector2)transform.position;
            Vector2 e = (atFirst ? t.surface[t.surface.Length - 1] : t.surface[0]) + o;
            if (Mathf.Abs(e.x - x) < .05f) { y = e.y; return true; }
        }
        return false;
    }

    // The ground's height `dist` units from surface point `from`, walking in `dir` (+1 right,
    // -1 left) along the surface; it holds its height past the next riser or the piece's end.
    float GroundAlong(int from, int dir, float dist)
    {
        Vector2 p = surface[from]; float left = dist;
        for (int i = from; i + dir >= 0 && i + dir < surface.Length; i += dir)
        {
            Vector2 q = surface[i + dir]; float dx = Mathf.Abs(q.x - p.x);
            if (dx < 1e-4f) return p.y;
            if (left <= dx) return Mathf.Lerp(p.y, q.y, left / dx);
            left -= dx; p = q;
        }
        return p.y;
    }

    // Seams: where the strip takes over from a step piece's kerb run. A step's upper run is painted
    // from the strip itself (its column c is the strip's column c - 256, mirrored on R pieces), so
    // the strip on that tread is laid out to match it, stone for stone; each seam then sits on a
    // mortar joint of the strip (stripJointsPx), inside the run's opaque part, and the two meet in
    // a narrow blend across the joint, so no faded band shows.
    [Tooltip("Strip mortar-joint columns in reference pixels at 120 PPU, for step seams. Do not double these for exact-2x/240 PPU art.")]
    public float[] stripJointsPx = new float[0];
    const float SeamBlend = 6f / StepPpu, GrassBlend = .12f;
    List<Riser> risersCache;
    float[] upperSeam, lowerSeam;   // per riser: inland distance of the seam on each side
    float[] treadX0, treadSign;     // per tread (between risers): the strip's layout
    List<float> seamXs;

    int TreadAt(float x)
    {
        int k = 0;
        foreach (var r in risersCache) if (r.x < x) k++;
        return k;
    }

    // The strip's u at x on tread k (whole texture widths; the strip repeats in U). A tread claimed
    // by the steps at both its ends uses the left one's layout up to its switch, the right one's after.
    float StripU(float x, int k, float width) => StripU(x, k, width, treadSwitch != null && x > treadSwitch[k]);
    float StripU(float x, int k, float width, bool right) =>
        right && treadSwitch != null && !float.IsNaN(treadSwitch[k]) ? treadSignB[k] * (x - treadX0B[k]) / width : treadSign[k] * (x - treadX0[k]) / width;

    float[] treadX0B, treadSignB, treadSwitch;   // natural sets: the right-hand layout, and where it takes over (NaN: none)
    // The layout switch's cross-fade: wide enough to hide a step in the grass, but on a tidy set
    // (bodyFade) only a mortar joint's width, so two out-of-phase kerbs never show doubled.
    float SwitchBlend => bodyFade ? SeamBlend : .1f;

    void StepSeams()
    {
        if (seamXs != null) return;
        if (risersCache == null) risersCache = Risers();
        int n = risersCache.Count;
        upperSeam = new float[n]; lowerSeam = new float[n];
        treadX0 = new float[n + 1]; treadSign = new float[n + 1]; seamXs = new List<float>();
        treadX0B = new float[n + 1]; treadSignB = new float[n + 1]; treadSwitch = new float[n + 1];
        float width = top.rect.width / top.pixelsPerUnit;
        for (int k = 0; k <= n; k++)
        {
            // Tread k lies between riser k - 1 (on its left) and riser k (on its right). Its strip
            // matches the upper run beside it: an R riser on its right, or an L riser on its left.
            treadX0[k] = -Origin.x; treadSign[k] = 1f; treadSwitch[k] = float.NaN;
            if (stripDerivedRuns)
            {
                // Every run beside the tread is the strip's own columns from its riser line, so either
                // step may set the layout; with both, the tread switches from one to the other at a
                // strip joint shared by both layouts, near its middle.
                bool left = k > 0, right = k < n;
                if (left) { treadX0[k] = risersCache[k - 1].x; treadSign[k] = 1f; }
                if (right) { treadX0B[k] = risersCache[k].x; treadSignB[k] = -1f; }
                if (left && right) treadSwitch[k] = SwitchAt(risersCache[k - 1].x, risersCache[k].x, k, width);
                else if (right) { treadX0[k] = treadX0B[k]; treadSign[k] = -1f; }
                continue;
            }
            // (A slope-top piece's upper run is painted afresh, so it sets no layout.)
            if (k < n && !risersCache[k].rockRight && !SlopedTop(risersCache[k])) { treadX0[k] = risersCache[k].x; treadSign[k] = -1f; }
            else if (k > 0 && risersCache[k - 1].rockRight && !SlopedTop(risersCache[k - 1])) { treadX0[k] = risersCache[k - 1].x; treadSign[k] = 1f; }
        }
        for (int i = 0; i < n; i++)
        {
            var r = risersCache[i]; float dir = r.rockRight ? 1f : -1f;
            int upTread = r.rockRight ? i + 1 : i, lowTread = r.rockRight ? i : i + 1;
            if (stripDerivedRuns)
            {
                // Each seam stays on its own riser's side of the tread's switch (or end), so a short
                // tread between two steps keeps both runs and the strip between them.
                float upRoom = Room(i, upTread), lowRoom = Room(i, lowTread);
                upperSeam[i] = Joint(r.x, dir, upTread, Mathf.Min(1.0f, upRoom * .5f), Mathf.Min(2.55f, upRoom), Mathf.Min(1.8f, upRoom * .8f), width);
                lowerSeam[i] = Joint(r.x, -dir, lowTread, Mathf.Min(.45f, lowRoom * .4f), Mathf.Min(1.25f, lowRoom), Mathf.Min(.9f, lowRoom * .7f), width);
            }
            else
            {
                upperSeam[i] = Joint(r.x, dir, upTread, 1.0f, 2.55f, 1.8f, width);
                lowerSeam[i] = Joint(r.x, -dir, lowTread, .45f, 1.25f, .9f, width);
            }
            seamXs.Add(r.x + dir * upperSeam[i]); seamXs.Add(r.x - dir * lowerSeam[i]);
        }
    }

    // How far riser i's run may reach into tread k: to the tread's switch, else to its far end, less
    // the switch blend (a tread's ends are the risers beside it, or the piece's ends).
    float Room(int i, int k)
    {
        float rx = risersCache[i].x;
        bool treadIsRight = k == i + 1;
        float far = treadIsRight ? (k < risersCache.Count ? risersCache[k].x : surface[surface.Length - 1].x) : (k > 0 ? risersCache[k - 1].x : surface[0].x);
        if (!float.IsNaN(treadSwitch[k])) far = treadSwitch[k];
        return Mathf.Max(.2f, Mathf.Abs(far - rx) - SwitchBlend - SeamBlend);
    }

    // Where a tread claimed at both ends switches layout: the x nearest its middle where both
    // layouts sit on a strip joint (within 3 px), else its middle.
    float SwitchAt(float xL, float xR, int k, float width)
    {
        float texPx = ReferenceWidth(top), mid = (xL + xR) * .5f, best = mid, bestErr = float.MaxValue;
        float Gap(float u)
        {
            float px = (u - Mathf.Floor(u)) * texPx, gap = float.MaxValue;
            foreach (float j in stripJointsPx) gap = Mathf.Min(gap, Mathf.Abs(Mathf.DeltaAngle(px / texPx * 360f, j / texPx * 360f)) / 360f * texPx);
            return gap;
        }
        float span = (xR - xL) * .35f;
        for (float x = mid - span; x <= mid + span; x += .5f / StepPpu)
        {
            float g = Mathf.Max(Gap((x - xL) / width), Gap(-(x - xR) / width)), err = g * 4f + Mathf.Abs(x - mid);
            if (g < 3f && err < bestErr) { bestErr = err; best = x; }
        }
        return best;
    }

    // Whether the ground beside a riser's top climbs away from it (a slope-top step).
    bool SlopedTop(Riser r) => GroundAlong(r.top, r.rockRight ? 1 : -1, .5f) - r.high > .05f;

    // A slope-top piece's painted rise (u) at `t` u inland, from its registered walk line.
    static float PaintedRise(Vector2[] rise, float t)
    {
        if (rise == null || rise.Length == 0) return 0f;
        float px = t * StepPpu;
        if (px <= rise[0].x) return rise[0].y / StepPpu;
        for (int i = 1; i < rise.Length; i++)
            if (px <= rise[i].x) return Mathf.Lerp(rise[i - 1].y, rise[i].y, (px - rise[i - 1].x) / (rise[i].x - rise[i - 1].x)) / StepPpu;
        return rise[rise.Length - 1].y / StepPpu;
    }

    // The inland distance (between `near` and `far`, nearest `want`) from the riser at rx, going
    // `dir`, where the strip on tread k has a mortar joint; `want` itself if none.
    float Joint(float rx, float dir, int k, float near, float far, float want, float width)
    {
        float best = want, bestErr = float.MaxValue;
        float texPx = ReferenceWidth(top);
        for (float d = near; d <= far; d += .5f / StepPpu)
        {
            float u = StripU(rx + dir * d, k, width), px = (u - Mathf.Floor(u)) * texPx, gap = float.MaxValue;
            foreach (float j in stripJointsPx) gap = Mathf.Min(gap, Mathf.Abs(Mathf.DeltaAngle(px / texPx * 360f, j / texPx * 360f)) / 360f * texPx);
            float err = gap * 4f + Mathf.Abs(d - want);
            if (gap < 3f && err < bestErr) { bestErr = err; best = d; }
        }
        return best;
    }

    // The strip's opacity at x: 0 under a step piece's kerb run, 1 past its seam; its grass and moss
    // line carry on under the piece up to GrassEdgeFrom from the riser.
    const float GrassEdgeFrom = .05f, GrassEdgeTo = .35f;
    float StripAlpha(float x, bool grass)
    {
        StepSeams();
        float a = 1f;
        for (int i = 0; i < risersCache.Count; i++)
        {
            var r = risersCache[i];
            float t = (x - r.x) * (r.rockRight ? 1f : -1f);   // > 0 on the upper side
            float seam = t >= 0f ? upperSeam[i] : lowerSeam[i];
            // The grass runs on over the step pieces (they draw over it, and theirs is shorter), so
            // the grass line keeps one height along the whole tread; it stops just short of the edge.
            a = Mathf.Min(a, grass ? Mathf.InverseLerp(GrassEdgeFrom, GrassEdgeTo, Mathf.Abs(t)) : Mathf.InverseLerp(seam - SeamBlend, seam + SeamBlend, Mathf.Abs(t)));
        }
        return a;
    }

    // Natural sets: the strip's soil (under its kerb course) at x, the reverse of each piece's soil
    // fade at its seam (SeamAlpha), so the two always sum to one.
    float StripSoilAlpha(float x)
    {
        StepSeams();
        float a = 1f;
        for (int i = 0; i < risersCache.Count; i++)
        {
            var r = risersCache[i];
            float t = (x - r.x) * (r.rockRight ? 1f : -1f), seam = t >= 0f ? upperSeam[i] : lowerSeam[i], band = SoilBand(seam);
            a = Mathf.Min(a, Mathf.InverseLerp(seam - band, seam, Mathf.Abs(t)));
        }
        return a;
    }

    // The soil cross-fade's width at a seam: SoilBlend, or less on a short run (natural sets).
    float SoilBand(float seam) => stripDerivedRuns ? Mathf.Min(SoilBlend, seam * .6f) : SoilBlend;

    void StepParts()
    {
        StepSeams();
        // Higher steps draw over lower ones (their kerb runs overlap the step below).
        var order = new List<Riser>(risersCache);
        order.Sort((p, q) => p.high.CompareTo(q.high));
        for (int k = 0; k < order.Count; k++)
        {
            var r = order[k]; float z = -.002f * (k + 1), h = r.high - r.low;
            string id = "Step " + r.x.ToString("0.0");
            // A step whose upper ground climbs gets its slope-top piece (when there is one), placed
            // unbent by its painted walk line: its face stays upright.
            bool slope = SlopedTop(r);
            Sprite shortS = r.rockRight ? stepShortSlopeL : stepShortSlopeR, mediumS = r.rockRight ? stepMediumSlopeL : stepMediumSlopeR, outerS = r.rockRight ? stepOuterSlopeL : stepOuterSlopeR;
            if (h <= 1.3f)
            {
                if (slope && shortS != null) StepPiece(id + " short (slope top)", shortS, r, slopeTopRows.x + 125f, true, z, slopeTopRows.x, shortSlopeRise, Drop(ShortLowerDrop));
                else StepPiece(id + " short", r.rockRight ? stepShortL : stepShortR, r, 285f, true, z, StepUpperPx, null, Drop(ShortLowerDrop));
            }
            else if (h <= 2.05f)
            {
                if (slope && mediumS != null) StepPiece(id + " medium (slope top)", mediumS, r, slopeTopRows.y + 210f, true, z, slopeTopRows.y, mediumSlopeRise, Drop(MediumLowerDrop));
                else StepPiece(id + " medium", r.rockRight ? stepMediumL : stepMediumR, r, 370f, true, z, StepUpperPx, null, Drop(MediumLowerDrop));
            }
            else
            {
                // Wall courses from the Outer's joint (row 448) down to the lower walk line, the
                // last one under the Inner; the Outer and Inner fade over their ends.
                // (Tidy sets: the walls start 48 px higher, under the Outer's last plain stones, and the
                // Outer fades into them over 40 rows, so its bulging bottom course never shows as a
                // ledge where the flush wall courses begin.)
                float jointDrop = tidyOuterJoint ? OuterJointTidyPx : 448f - StepUpperPx;
                float y = r.high - jointDrop / StepPpu; int n = 0;
                while (y > r.low + 1e-3f)
                {
                    var wall = stepWalls[Mathf.Abs(Mathf.RoundToInt(r.x * 7f) + n * 2) % stepWalls.Length];
                    float hw = wall.rect.height / wall.pixelsPerUnit, yLow = Mathf.Max(r.low, y - hw);
                    StepWall(id + " wall " + n, wall, r, y, yLow, hw, z + .0005f);
                    y -= hw; n++;
                }
                Vector2 outerFade(float upperRow) => tidyOuterJoint ? new Vector2(upperRow + OuterJointTidyPx - 40f, upperRow + OuterJointTidyPx) : new Vector2(float.NaN, float.NaN);
                if (slope && outerS != null) StepPiece(id + " outer (slope top)", outerS, r, -1f, true, z, slopeTopRows.z, outerSlopeRise, 0f, float.NaN, outerFade(slopeTopRows.z));
                else StepPiece(id + " outer", r.rockRight ? stepOuterL : stepOuterR, r, -1f, true, z, StepUpperPx, null, 0f, float.NaN, outerFade(StepUpperPx));
                float innerTop = r.low + 352f / StepPpu, cut = r.high - (KerbPx + 24f) / StepPpu;
                float fadeFrom = innerUnderKerb && innerTop > cut ? cut : float.NaN;
                StepPiece(id + " inner", r.rockRight ? stepInnerL : stepInnerR, r, 352f, false, z - .0005f, StepUpperPx, null, Drop(InnerLowerDrop), fadeFrom);
            }
        }
    }

    // One step piece by its registration: the riser line at column 256 (L; mirrored for R), the
    // upper walk line at row 160 on the riser's top, and `lowerPx` (the lower walk row; < 0 for an
    // Outer, which has none) on its foot. An Inner (`upper` false) has only the lower walk row.
    void StepPiece(string label, Sprite s, Riser r, float lowerPx, bool upper, float z, float upperPx = StepUpperPx, Vector2[] painted = null, float lowerDrop = 0f, float topFade = float.NaN, Vector2? bottomFade = null)
    {
        float w = ReferenceWidth(s), hpx = ReferenceHeight(s), riserCol = r.rockRight ? StepRiserPx : w - StepRiserPx;
        // Rows (reference pixels at 120 PPU from the top) at the walk lines and at the foot of each kerb course
        // (KerbPx below its walk line), mapped to world heights; a Medium stretches between its
        // two walk lines. Rows down to a kerb's foot meet the strip in a narrow blend at the seam,
        // the soil under it in a wider one.
        var rows = new List<float> { 0f, hpx };
        if (upper) { rows.Add(upperPx); rows.Add(upperPx + MossPx); rows.Add(upperPx + KerbPx); }
        if (lowerPx > 0f) { rows.Add(lowerPx); rows.Add(lowerPx + MossPx); rows.Add(lowerPx + KerbPx); }
        // An Outer faded out over its bottom rows (bottomFade: opaque row, clear row).
        Vector2 bf = bottomFade ?? new Vector2(float.NaN, float.NaN);
        if (!float.IsNaN(bf.x)) foreach (float row in new[] { bf.x, bf.y }) if (row > 0f && row < hpx) rows.Add(row);
        // An Inner faded out at its top (topFade: world height where it is clear, opaque TopFadeSpan lower).
        if (!upper && !float.IsNaN(topFade))
            foreach (float y in new[] { topFade, topFade - TopFadeSpan }) { float row = lowerPx - (y - r.low) * StepPpu; if (row > 0f && row < hpx) rows.Add(row); }
        rows.Sort();
        float Y(float row)
        {
            if (!upper) return r.low - (row - lowerPx) / StepPpu;
            if (lowerPx <= 0f || row <= upperPx) return r.high - (row - upperPx) / StepPpu;
            if (row >= lowerPx) return r.low - (row - lowerPx) / StepPpu;
            return Mathf.Lerp(r.high, r.low, (row - upperPx) / (lowerPx - upperPx));
        }
        var ys = rows.ConvertAll(Y);
        float upperKerbFoot = upper ? upperPx + KerbPx : -1f, lowerKerbFoot = lowerPx > 0f ? lowerPx + KerbPx : -1f;
        Grid(label, s, r, riserCol, w, hpx, rows, ys, upper, lowerPx > 0f, false, z, StepOrder, upperKerbFoot, lowerKerbFoot, painted, lowerDrop, topFade, bf);
    }

    const float TopFadeSpan = .45f;
    // bodyFade: the Outer's joint with the wall courses, px under its walk row (the art's is 288; the
    // Batch 23b-g Outers bulge 0.4-0.5 u over their last 20 rows, so the tidy joint sits above that).
    const float OuterJointTidyPx = 240f;
    // bodyFade: the wall courses' solid and faded depth (128 px riser column, 96 px fade on 384 px),
    // and the lower soil's fade from the face.
    const float BodyFrom = 1.33f, BodyTo = 2.13f, LowSoilFrom = .25f, LowSoilTo = .6f;

    const float KerbPx = 72f, MossPx = 36f, SoilBlend = .5f;
    // A lower kerb run's bottom edge (the pale ledge under its stones) sits a few px off the strip's
    // (56 px under the walk row), measured on 9 Oct: Short 46, Medium 58, Inner 57.5. Each lower run
    // is moved by the difference away from the face, easing in between LowerDropFrom and
    // LowerDropTo from the riser, so the two kerbs' bottoms meet level at the seam.
    const float ShortLowerDrop = 10f / StepPpu, MediumLowerDrop = -2f / StepPpu, InnerLowerDrop = -1.5f / StepPpu;
    const float LowerDropFrom = .25f, LowerDropTo = .45f;
    // (Natural sets: the lower runs are the strip's own rows, so their ledge already meets it.)
    float Drop(float d) => stripDerivedRuns ? 0f : d;

    // A piece's opacity at inland distance t near its seam: grass and the moss line (down to MossPx
    // under the walk row) blend out over GrassBlend, the kerb stones sharply across the joint, the
    // soil over SoilBlend.
    float SeamAlpha(float t, float seam, float row, float walkRow, float kerbFoot)
    {
        if (row < walkRow + MossPx - .5f) return Mathf.InverseLerp(seam + SeamBlend, seam - GrassBlend, t);
        if (row <= kerbFoot + .5f) return Mathf.InverseLerp(seam + SeamBlend, seam - SeamBlend, t);
        return Mathf.InverseLerp(seam, seam - SoilBand(seam), t);
    }

    // A wall course (painted for L at column 128): from yTop down, cut at yLow (under the Inner).
    void StepWall(string label, Sprite s, Riser r, float yTop, float yLow, float hWorld, float z)
    {
        float w = ReferenceWidth(s), hpx = ReferenceHeight(s), riserCol = r.rockRight ? 128f : w - 128f;
        float cut = (yTop - yLow) / hWorld * hpx;
        Grid(label, s, r, riserCol, w, hpx, new List<float> { 0f, cut }, new List<float> { yTop, yLow }, false, false, !r.rockRight, z, StepWallOrder);
    }

    // A piece as a grid of whole columns (each moved to follow the ground beside the riser), cut at
    // its seams with the strip: an upper kerb run (bendUpper) ends at the riser's upper seam, a
    // lower one (bendLower) at its lower seam; kerb rows (down to the kerb's foot row) blend over
    // SeamBlend across the seam's mortar joint, the soil below over SoilBlend before it.
    void Grid(string label, Sprite s, Riser r, float riserCol, float w, float hpx, List<float> rows, List<float> ys, bool bendUpper, bool bendLower, bool mirror, float z, int order, float upperKerbFoot = -1f, float lowerKerbFoot = -1f, Vector2[] painted = null, float lowerDrop = 0f, float topFade = float.NaN, Vector2? bottomFade = null)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>(); var colors = new List<Color>();
        float upSeam = bendUpper ? upperSeam[r.index] : float.PositiveInfinity, lowSeam = bendLower ? lowerSeam[r.index] : float.PositiveInfinity;
        float side = r.rockRight ? 1f : -1f;
        // Column positions (reference pixels at 120 PPU): every StepColPx, plus the seam blend edges, up to the seams.
        var pxs = new List<float>();
        for (float px = 0f; px < w; px += StepColPx) pxs.Add(px);
        pxs.Add(w);
        foreach (float d in new[] { upSeam - SoilBand(upSeam), upSeam - GrassBlend, upSeam - SeamBlend, upSeam, upSeam + SeamBlend })
            if (!float.IsInfinity(d)) pxs.Add(riserCol + side * d * StepPpu);
        foreach (float d in new[] { lowSeam - SoilBand(lowSeam), lowSeam - GrassBlend, lowSeam - SeamBlend, lowSeam, lowSeam + SeamBlend })
            if (!float.IsInfinity(d)) pxs.Add(riserCol - side * d * StepPpu);
        if (bodyFade && order == StepOrder)
        {
            foreach (float d in new[] { BodyFrom, BodyTo }) pxs.Add(riserCol + side * d * StepPpu);
            foreach (float d in new[] { LowSoilFrom, LowSoilTo }) pxs.Add(riserCol - side * d * StepPpu);
        }
        pxs.RemoveAll(px => px < 0f || px > w || (px - riserCol) * side > upSeam * StepPpu + SeamBlend * StepPpu + .01f || (riserCol - px) * side > lowSeam * StepPpu + SeamBlend * StepPpu + .01f);
        pxs.Sort();
        int cols = 0;
        for (int c = 0; c < pxs.Count; c++)
        {
            float px = pxs[c];
            if (c > 0 && px - pxs[c - 1] < .01f) continue;
            float x = r.x + (px - riserCol) / StepPpu;
            float t = (x - r.x) * side;   // > 0 on the upper (rock) side
            float dy = 0f;
            // (Only what the painting does not already climb: nothing, for a slope-top piece on its slope.)
            if (t > 0f && bendUpper) dy = GroundAlong(r.top, r.rockRight ? 1 : -1, t) - r.high - PaintedRise(painted, t);
            else if (t < 0f && bendLower && r.foot >= 0) dy = GroundAlong(r.foot, r.rockRight ? -1 : 1, -t) - r.low - lowerDrop * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LowerDropFrom, LowerDropTo, -t));
            float srcPx = mirror ? w - px : px;
            for (int k = 0; k < rows.Count; k++)
            {
                float a = 1f, rowDy = dy;
                if (t > 0f && bendUpper) a = SeamAlpha(t, upSeam, rows[k], upperKerbFoot - KerbPx, upperKerbFoot);
                else if (t < 0f && bendLower) a = SeamAlpha(-t, lowSeam, rows[k], lowerKerbFoot - KerbPx, lowerKerbFoot);
                if (bodyFade && order == StepOrder)
                {
                    // The rock body under the upper tread (or all of an Inner's rock side) gives way to the
                    // fill where the wall courses do; the lower run's soil stays put and fades near the face.
                    if (t > 0f && (!bendUpper || rows[k] > upperKerbFoot + .5f)) a *= Mathf.InverseLerp(BodyTo, BodyFrom, t);
                    if (t < 0f && bendLower && rows[k] > lowerKerbFoot + .5f) { rowDy = 0f; a *= Mathf.InverseLerp(LowSoilTo, LowSoilFrom, -t); }
                    else if (t < 0f && bendLower && r.foot < 0 && rows[k] > lowerKerbFoot - KerbPx - 48f) a *= Mathf.InverseLerp(.4f, .1f, -t);
                }
                if (!float.IsNaN(topFade)) a *= Mathf.InverseLerp(topFade, topFade - TopFadeSpan, ys[k]);
                if (bottomFade.HasValue && !float.IsNaN(bottomFade.Value.x)) a *= Mathf.InverseLerp(bottomFade.Value.y, bottomFade.Value.x, rows[k]);
                Color col = tint; col.a *= a;
                if (shadeLikeFill) { float sh = FillShade(x, ys[k] + rowDy); col.r *= sh; col.g *= sh; col.b *= sh; }
                v.Add(new Vector3(x, ys[k] + rowDy, z)); uv.Add(new Vector2(srcPx / w, 1f - rows[k] / hpx)); colors.Add(col);
            }
            cols++;
        }
        int nr = rows.Count;
        for (int c = 0; c < cols - 1; c++)
            for (int k = 0; k < nr - 1; k++)
            {
                int q = c * nr + k;
                tris.AddRange(new[] { q, q + 1, q + nr + 1, q, q + nr + 1, q + nr });
            }
        var mesh = Build(label, v, uv, tris, tint);
        mesh.SetColors(colors);
        Part(label, s, order, mesh);
    }

    void CliffParts()
    {
        var sideL = new Batch(); var sideR = new Batch(); var topL = new Batch(); var topR = new Batch(); var footL = new Batch(); var footR = new Batch();
        for (int i = 0; i < surface.Length - 1; i++)
        {
            Vector2 a = surface[i], b = surface[i + 1];
            if (Mathf.Abs(b.x - a.x) > 1e-4f || Mathf.Abs(b.y - a.y) < .4f) continue;
            bool up = b.y > a.y;   // going up, the rock is to the right: the L pieces
            Wall(a.x, Mathf.Min(a.y, b.y), Mathf.Max(a.y, b.y), up, up ? sideL : sideR, up ? topL : topR, up ? footL : footR, true, up ? i + 1 : i);
        }
        Vector2 first = surface[0], last = surface[surface.Length - 1];
        Wall(first.x, Mathf.Max(bottom, first.y - 14f), first.y, true, sideL, topL, footL, false, 0);
        Wall(last.x, Mathf.Max(bottom, last.y - 14f), last.y, false, sideR, topR, footR, false, surface.Length - 1);
        Emit("Cliff sides (L)", cliffSideL, CliffSideOrder, sideL); Emit("Cliff sides (R)", cliffSideR, CliffSideOrder, sideR);
        Emit("Cliff corners (L)", cornerTopL, CliffCornerOrder, topL); Emit("Cliff corners (R)", cornerTopR, CliffCornerOrder, topR);
        Emit("Cliff feet (L)", cornerFootL, CliffFootOrder, footL); Emit("Cliff feet (R)", cornerFootR, CliffFootOrder, footR);
    }

    // A natural step set covers the risers; the piece's two open ends keep the cliff kit's walls.
    void CliffEnds()
    {
        var sideL = new Batch(); var sideR = new Batch(); var topL = new Batch(); var topR = new Batch(); var footL = new Batch(); var footR = new Batch();
        Vector2 first = surface[0], last = surface[surface.Length - 1];
        // (Tidy sets: not where the end is a step onto the next piece, or buried in it.)
        if (!(bodyFade && (EndIsStep(true) || buriedFirst))) Wall(first.x, Mathf.Max(bottom, first.y - 14f), first.y, true, sideL, topL, footL, false, 0);
        if (!(bodyFade && (EndIsStep(false) || buriedLast))) Wall(last.x, Mathf.Max(bottom, last.y - 14f), last.y, false, sideR, topR, footR, false, surface.Length - 1);
        Emit("Cliff sides (L)", cliffSideL, CliffSideOrder, sideL); Emit("Cliff sides (R)", cliffSideR, CliffSideOrder, sideR);
        Emit("Cliff corners (L)", cornerTopL, CliffCornerOrder, topL); Emit("Cliff corners (R)", cornerTopR, CliffCornerOrder, topR);
    }

    void Emit(string label, Sprite sprite, int order, Batch batch)
    {
        if (batch.v.Count == 0) return;
        var mesh = Build(label, batch.v, batch.uv, batch.tris, tint);
        mesh.SetColors(batch.colors);
        Part(label, sprite, order, mesh);
    }

    // The ground beside a riser's top (surface point `at`) on the rock side: how far it runs before
    // the next riser, and its rise per unit going into the rock. A riser beside it: no run.
    void TreadBeside(int at, bool rockRight, out float run, out float slope)
    {
        run = 0f; slope = 0f;
        int next = rockRight ? at + 1 : at - 1;
        if (at < 0 || at >= surface.Length || next < 0 || next >= surface.Length) return;
        Vector2 p = surface[at], q = surface[next];
        float dx = Mathf.Abs(q.x - p.x);
        if (dx < 1e-4f) return;
        run = dx; slope = (q.y - p.y) / dx;
    }

    // One wall at x from `low` to `high`; rockRight: L pieces (rock to the right of the line).
    // `at`: the surface point at its top (for town steps, the tread beside it shapes the corner).
    void Wall(float x, float low, float high, bool rockRight, Batch side, Batch top, Batch foot, bool withFoot, int at)
    {
        Sprite sSide = rockRight ? cliffSideL : cliffSideR, sTop = rockRight ? cornerTopL : cornerTopR, sFoot = rockRight ? cornerFootL : cornerFootR;
        const float LinePx = 64f, Fade = 1.4f;
        // Columns: the art's open edge, the start of the fade into the rock, the rock edge.
        float[] Xs(Sprite s, out float[] us, out float[] alpha)
        {
            float w = s.rect.width / s.pixelsPerUnit, line = LinePx / StepPpu, f = Fade / w;
            if (rockRight) { float l = x - line; us = new[] { 0f, 1f - f, 1f }; alpha = new[] { 1f, 1f, 0f }; return new[] { l, l + w - Fade, l + w }; }
            float r = x + line; us = new[] { 0f, f, 1f }; alpha = new[] { 0f, 1f, 1f }; return new[] { r - w, r - w + Fade, r };
        }
        float ppuTop = sTop.pixelsPerUnit, hTop = sTop.rect.height / ppuTop;
        float topEdge = high + 96f / StepPpu, cornerBottom = topEdge - hTop;
        // The top corner, cut at the lower walk line on a short wall.
        float cTop = Mathf.Max(cornerBottom, low);
        Vector2 course = rockRight ? townCourseL : townCourseR;
        bool courses = cornersFollowTread && course.y > course.x;
        // Town steps on a tall riser: the corner stops at a joint in its stone column, and that
        // column's courses carry on down the riser (see below).
        if (courses) cTop = Mathf.Max(topEdge - course.y / StepPpu, low);
        float[] cxs = null, cus = null, cxa = null, cdy = null;
        {
            var xs = Xs(sTop, out var us, out var xa);
            float[] dy = null;
            // Natural step sets retain a cliff-kit cap at an open end. Its inland
            // lip must stop at the first tread too, or it floats over descending steps.
            if (cornersFollowTread || (HasStepSet && stripDerivedRuns && !withFoot))
            {
                // The corner's stone course runs along the tread beside it: as far as a
                // short tread goes (then fades under the next step), and bent to follow a slope, so
                // its level lip never pokes out under the ground above.
                TreadBeside(at, rockRight, out float run, out float slope);
                float wTop = xs[2] - xs[0], x0 = xs[0], openEdge = rockRight ? xs[0] : xs[2], rockEdge = rockRight ? xs[2] : xs[0];
                float reach = Mathf.Abs(slope) < .02f ? run : Mathf.Min(run, 2.4f), FadeOut = .5f;
                float end = rockRight ? Mathf.Min(rockEdge, x + Mathf.Max(reach, 1f)) : Mathf.Max(rockEdge, x - Mathf.Max(reach, 1f));
                bool cropped = Mathf.Abs(end - rockEdge) > 1e-3f;
                float fadeFrom = rockRight ? Mathf.Max(x, end - (cropped ? FadeOut : Fade)) : Mathf.Min(x, end + (cropped ? FadeOut : Fade));
                xs = rockRight ? new[] { openEdge, x, fadeFrom, end } : new[] { end, fadeFrom, x, openEdge };
                us = new float[4]; xa = new float[4]; dy = new float[4];
                for (int c = 0; c < 4; c++)
                {
                    us[c] = (xs[c] - x0) / wTop;
                    float into = rockRight ? xs[c] - x : x - xs[c];   // distance into the rock
                    dy[c] = into > 0f ? slope * into : 0f;
                }
                xa = rockRight ? new[] { 1f, 1f, 1f, 0f } : new[] { 0f, 1f, 1f, 1f };
            }
            Patch(top, xs, us, xa, new[] { cTop, topEdge }, new[] { (cTop - cornerBottom) / hTop, 1f }, new[] { 1f, 1f }, dy);
            cxs = xs; cus = us; cxa = xa; cdy = dy;
        }
        if (courses)
        {
            // The stone column's courses, joint to joint, down to the lower walk line (or the base).
            float vHi = 1f - course.x / ReferenceHeight(sTop), vLo = 1f - course.y / ReferenceHeight(sTop), hCourse = (course.y - course.x) / StepPpu;
            float stop = withFoot ? low : low, y = cTop;
            while (y > stop + 1e-3f)
            {
                float yLow = Mathf.Max(stop, y - hCourse), vAt = vHi - (y - yLow) / hCourse * (vHi - vLo);
                Patch(top, cxs, cus, cxa, new[] { yLow, y }, new[] { vAt, vHi }, new[] { 1f, 1f }, cdy);
                y = yLow;
            }
            return;   // town steps: no side art and no talus foot
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
        if (!withFoot || cornersFollowTread) return;   // town steps: no talus at a step's foot
        // The talus foot on the lower walk line, cropped (and faded at its cut) under the moss lip.
        float ppuFoot = sFoot.pixelsPerUnit, hFoot = sFoot.rect.height / ppuFoot;
        float footTop = low + 342f / StepPpu, footBottom = footTop - hFoot, cut = Mathf.Min(footTop, high - .45f);
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
    // `dy`: an optional rise per column (a patch bent along a slope).
    void Patch(Batch b, float[] xs, float[] us, float[] xa, float[] ys, float[] vs, float[] ya, float[] dy = null)
    {
        int n = b.v.Count, cols = xs.Length;
        for (int r = 0; r < ys.Length; r++)
            for (int c = 0; c < cols; c++)
            {
                b.v.Add(new Vector3(xs[c], ys[r] + (dy != null ? dy[c] : 0f))); b.uv.Add(new Vector2(us[c], vs[r]));
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
        if (fullUnderSilhouette) return FullUnderMesh();
        Vector2 size = WorldSize(under);
        const float band = 1f, solidV = 120f / 384f;   // Ceiling_Under: the painted stones fill its top 120 of 384 px
        float x0 = surface[0].x, x1 = surface[surface.Length - 1].x;
        var v = new List<Vector3> { new Vector3(x0, bottom - .06f), new Vector3(x0, bottom + band), new Vector3(x1, bottom + band), new Vector3(x1, bottom - .06f) };
        float u0 = (x0 + Origin.x) / size.x, u1 = (x1 + Origin.x) / size.x;
        var uv = new List<Vector2> { new Vector2(u0, 1f), new Vector2(u0, 1f - solidV), new Vector2(u1, 1f - solidV), new Vector2(u1, 1f) };
        return Build("under", v, uv, new List<int> { 0, 1, 2, 0, 2, 3 }, tint * underTint);
    }

    Mesh FullUnderMesh()
    {
        Vector2 size = WorldSize(under);
        float x0 = surface[0].x, x1 = surface[surface.Length - 1].x, upper = bottom + .8f;
        float edge = Mathf.Min(.15f, Mathf.Max(0f, (x1 - x0) * .25f));
        float[] xs = { x0, x0 + edge, x1 - edge, x1 };
        float[] ys = { upper, upper - Mathf.Min(.25f, size.y), upper - size.y };
        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        var colors = new List<Color>(); var triangles = new List<int>();
        Color color = tint * underTint;
        for (int row = 0; row < ys.Length; row++)
            for (int column = 0; column < xs.Length; column++)
            {
                vertices.Add(new Vector3(xs[column], ys[row], -.001f));
                uv.Add(new Vector2((xs[column] + Origin.x) / size.x, 1f - (upper - ys[row]) / size.y));
                colors.Add(new Color(color.r, color.g, color.b, color.a * (row == 0 ? 0f : 1f) * (column == 0 || column == 3 ? 0f : 1f)));
            }
        for (int row = 0; row < 2; row++)
            for (int column = 0; column < 3; column++)
            {
                int q = row * 4 + column;
                triangles.AddRange(new[] { q, q + 4, q + 5, q, q + 5, q + 1 });
            }
        Mesh mesh = Build("full underside", vertices, uv, triangles, color);
        mesh.SetColors(colors);
        return mesh;
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



using System.Collections.Generic;
using UnityEngine;

// A solid rectangle of terrain dressed with a TerrainKit. The transform is the block's
// top-left corner; the top edge is Qori's walk line. Art is regenerated from the size and
// flags whenever the block is enabled or edited, so only these settings are saved.
// Strips and fills use world-aligned texture coordinates, so neighbouring blocks tile
// into each other without seams; where two different pieces overlap, the upper one
// fades out along its inner edge.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class TerrainBlock : MonoBehaviour
{
    public enum Surface { Rock, Climbable, Slippery }

    public TerrainKit kit;
    [Min(.5f)] public float width = 8f, height = 6f;
    public Surface surface = Surface.Rock;
    public bool top = true, leftFace, rightFace, bottom;
    [Tooltip("Fade the left/right edge over this distance, to blend into a neighbouring piece such as a slope.")]
    [Min(0f)] public float blendLeft, blendRight;
    [Tooltip("Start the collider this far in from the left edge, where the art overlaps a neighbouring piece (e.g. a slope's crest) that should carry the walk surface.")]
    [Min(0f)] public float colliderInsetLeft;
    [Tooltip("Added to every sorting order, to layer overlapping blocks.")] public int sortingOffset;

    public const int FillOrder = -40, FaceOrder = -38, CeilingOrder = -37, TopOrder = -36, CornerOrder = -34;
    // Distances (world units) over which pieces fade into what's beneath them.
    const float TopFade = 1.4f, CeilingFade = .5f, FaceFade = .9f, CornerFade = .9f, FaceFillInset = .3f, CeilingFillInset = .3f;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private readonly List<Object> owned = new List<Object>();
    private Transform art;
    private bool dirty;
    private bool roughLeft, roughRight;
    const float ChipStep = .3f, ChipMin = .02f, ChipMax = .26f;   // chipped-edge rhythm and depth (world units)
    const float RimWidth = .16f, RimShade = .62f;                  // darker rim along chipped edges

    private void OnEnable() => Rebuild();
    private void OnDisable() => Clear();
    private void OnValidate() => dirty = true;

    private void Update()
    {
        if (!dirty) return;
        dirty = false;
        Rebuild();
    }

    public void Rebuild()
    {
        Clear();
        var box = GetComponent<BoxCollider2D>();
        float inset = Mathf.Clamp(colliderInsetLeft, 0f, width - .1f);
        box.size = new Vector2(width - inset, height);
        box.offset = new Vector2(inset + (width - inset) * .5f, -height * .5f);
        if (surface == Surface.Slippery && !TryGetComponent(out WallSurface _))
            gameObject.AddComponent<WallSurface>().allowsWallCling = false;
        else if (surface != Surface.Slippery && TryGetComponent(out WallSurface cling) && !cling.allowsWallCling)
            cling.allowsWallCling = true;
        if (kit == null) return;

        art = new GameObject("Art (generated)").transform;
        art.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        art.SetParent(transform, false);
        owned.Add(art.gameObject);

        bool rock = surface == Surface.Rock;
        Sprite fill = rock ? kit.groundFill : surface == Surface.Climbable ? kit.wallClimbable : kit.wallSlippery;
        // Keep the fill back from painted faces so their irregular outline isn't boxed in.
        float fillLeft = rock && leftFace ? FaceFillInset : 0f, fillRight = rock && rightFace ? FaceFillInset : 0f;
        float fillBottom = rock && bottom ? CeilingFillInset : 0f;
        // Exposed sides with no painted face (climbable / slippery walls) get a chipped silhouette
    // that sticks out past the straight collider.
        roughLeft = leftFace && !rock; roughRight = rightFace && !rock;
        Quad("Fill", fill, new Rect(fillLeft, -height + fillBottom, width - fillLeft - fillRight, height - fillBottom),
            true, true, false, FillOrder, new Vector4(blendLeft, blendRight, 0f, 0f));

        if (top) HorizontalStrip("Top", kit.groundTop, 0f, kit.groundTopWalkLine, TopOrder, new Vector4(blendLeft, blendRight, TopFade, 0f));
        if (bottom && rock) HorizontalStrip("Ceiling", kit.ceilingUnder, -height, kit.ceilingUnderside, CeilingOrder, new Vector4(0f, 0f, 0f, CeilingFade));

        float topCornerDrop = top ? (kit.cornerOuterTopRight.rect.height - kit.cornerOuterTopRightLedge.y) / Ppu(kit.cornerOuterTopRight) - CornerFade : 0f;
        float bottomCornerRise = bottom ? kit.cornerOuterBottomRightEdge.y / Ppu(kit.cornerOuterBottomRight) - CornerFade : 0f;
        for (int side = 0; side < 2; side++)
        {
            bool exposed = side == 0 ? rightFace : leftFace;
            if (!exposed || !rock) continue;
            float edge = side == 0 ? width : 0f;
            bool mirror = side == 1;
            float faceTop = -topCornerDrop, faceBottom = -height + bottomCornerRise;
            if (faceTop > faceBottom) VerticalStrip("Face", kit.wallSide, edge, kit.wallSideFace, faceBottom, faceTop, mirror);
            if (top) Corner("Ledge corner", kit.cornerOuterTopRight, kit.cornerOuterTopRightLedge, new Vector2(edge, 0f), mirror, false);
            if (bottom) Corner("Under corner", kit.cornerOuterBottomRight, kit.cornerOuterBottomRightEdge, new Vector2(edge, -height), mirror, true);
        }
    }

    private static float Ppu(Sprite sprite) => sprite.pixelsPerUnit;

    // A strip that repeats along x, with the landmark row (px from the sprite's top) at localY.
    private void HorizontalStrip(string label, Sprite sprite, float localY, float landmarkFromTop, int order, Vector4 fade)
    {
        float ppu = Ppu(sprite), h = sprite.rect.height / ppu;
        float below = (sprite.rect.height - landmarkFromTop) / ppu;
        Quad(label, sprite, new Rect(0, localY - below, width, h), true, false, false, order, fade);
    }

    // A strip that repeats along y, with its rock face (px from the sprite's left) on the block edge.
    private void VerticalStrip(string label, Sprite sprite, float edge, float faceFromLeft, float yMin, float yMax, bool mirror)
    {
        float ppu = Ppu(sprite), w = sprite.rect.width / ppu, inset = faceFromLeft / ppu;
        var rect = mirror ? new Rect(edge - (w - inset), yMin, w, yMax - yMin) : new Rect(edge - inset, yMin, w, yMax - yMin);
        // Fade the inner side (away from the face) into the fill.
        var fade = mirror ? new Vector4(0f, FaceFade, 0f, 0f) : new Vector4(FaceFade, 0f, 0f, 0f);
        Quad(label, sprite, rect, false, true, mirror, FaceOrder, fade);
    }

    // Places a right-hand corner (or its mirror) so its landmark sits on `at`, fading the
    // edges that lie inside the block.
    private void Corner(string label, Sprite sprite, Vector2 landmark, Vector2 at, bool mirror, bool underside)
    {
        float ppu = Ppu(sprite), w = sprite.rect.width / ppu, h = sprite.rect.height / ppu;
        float fromLeft = landmark.x / ppu, fromTop = landmark.y / ppu;
        float yMax = at.y + fromTop;
        float xMin = mirror ? at.x - (w - fromLeft) : at.x - fromLeft;
        float inner = CornerFade;
        var fade = new Vector4(mirror ? 0f : inner, mirror ? inner : 0f, underside ? 0f : inner, underside ? inner : 0f);
        Quad(label, sprite, new Rect(xMin, yMax - h, w, h), false, false, mirror, CornerOrder, fade);
    }

    // A textured quad, split into a 3x3 grid so each side can fade to transparent over
    // `fade` (left, right, bottom, top). World-aligned axes repeat the texture by world
    // position, so separate blocks line up; other axes stretch the texture across the rect.
    private void Quad(string label, Sprite sprite, Rect rect, bool worldU, bool worldV, bool mirrorU, int order, Vector4 fade)
    {
        if (sprite == null || rect.width <= 0f || rect.height <= 0f) return;
        var obj = new GameObject(label);
        obj.transform.SetParent(art, false);
        float ppu = Ppu(sprite);
        Vector2 tile = new Vector2(sprite.rect.width / ppu, sprite.rect.height / ppu);
        Vector3 origin = transform.position;
        bool chipLeft = roughLeft && Mathf.Abs(rect.xMin) < .001f, chipRight = roughRight && Mathf.Abs(rect.xMax - width) < .001f;
        if ((chipLeft || chipRight) && !mirrorU)
        {
            ChippedQuad(obj, sprite, rect, worldU, worldV, order, fade, chipLeft, chipRight, tile, origin);
            return;
        }
        float[] xs = { rect.xMin, rect.xMin + Mathf.Min(fade.x, rect.width * .5f), rect.xMax - Mathf.Min(fade.y, rect.width * .5f), rect.xMax };
        float[] ys = { rect.yMin, rect.yMin + Mathf.Min(fade.z, rect.height * .5f), rect.yMax - Mathf.Min(fade.w, rect.height * .5f), rect.yMax };
        var vertices = new Vector3[16];
        var uv = new Vector2[16];
        var colors = new Color[16];
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
            {
                int i = row * 4 + column;
                Vector2 c = new Vector2(xs[column], ys[row]);
                vertices[i] = c;
                float u = worldU ? (origin.x + c.x) / tile.x : (c.x - rect.xMin) / rect.width;
                float v = worldV ? (origin.y + c.y) / tile.y : (c.y - rect.yMin) / rect.height;
                if (mirrorU) u = worldU ? -u : 1f - u;
                uv[i] = new Vector2(u, v);
                bool clear = (column == 0 && fade.x > 0f) || (column == 3 && fade.y > 0f) ||
                    (row == 0 && fade.z > 0f) || (row == 3 && fade.w > 0f);
                colors[i] = clear ? new Color(1f, 1f, 1f, 0f) : Color.white;
            }
        var triangles = new int[54];
        for (int row = 0, t = 0; row < 3; row++)
            for (int column = 0; column < 3; column++, t += 6)
            {
                int i = row * 4 + column;
                triangles[t] = i; triangles[t + 1] = i + 4; triangles[t + 2] = i + 1;
                triangles[t + 3] = i + 1; triangles[t + 4] = i + 4; triangles[t + 5] = i + 5;
            }
        var mesh = new Mesh { name = label, hideFlags = HideFlags.DontSave, vertices = vertices, uv = uv, colors = colors, triangles = triangles };
        mesh.RecalculateBounds();
        owned.Add(mesh);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = TerrainMaterial.Shared;
        renderer.sortingOrder = order + sortingOffset;
        var block = new MaterialPropertyBlock();
        block.SetTexture(MainTex, sprite.texture);
        renderer.SetPropertyBlock(block);
    }

    // A quad whose left/right edges follow a chipped-rock profile. Rows follow the chip rhythm
    // plus the vertical fade boundaries; alpha fades along y exactly like Quad.
    private void ChippedQuad(GameObject obj, Sprite sprite, Rect rect, bool worldU, bool worldV, int order, Vector4 fade,
        bool chipLeft, bool chipRight, Vector2 tile, Vector3 origin)
    {
        var rows = new List<float> { rect.yMin, rect.yMax };
        if (fade.z > 0f) rows.Add(rect.yMin + Mathf.Min(fade.z, rect.height * .5f));
        if (fade.w > 0f) rows.Add(rect.yMax - Mathf.Min(fade.w, rect.height * .5f));
        for (float y = Mathf.Ceil((origin.y + rect.yMin) / (ChipStep * .25f)) * ChipStep * .25f - origin.y; y < rect.yMax; y += ChipStep * .25f)
            if (y > rect.yMin) rows.Add(y);
        rows.Sort();
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var triangles = new List<int>();
        foreach (float y in rows)
        {
            float alpha = 1f;
            if (fade.z > 0f) alpha = Mathf.Min(alpha, Mathf.Clamp01((y - rect.yMin) / Mathf.Min(fade.z, rect.height * .5f)));
            if (fade.w > 0f) alpha = Mathf.Min(alpha, Mathf.Clamp01((rect.yMax - y) / Mathf.Min(fade.w, rect.height * .5f)));
            float worldY = origin.y + y;
            // Chips stick OUT past the collider, so a wall slide or grab always touches painted rock.
            float left = rect.xMin - (chipLeft ? Chip(worldY, origin.x + rect.xMin) : 0f);
            float right = rect.xMax + (chipRight ? Chip(worldY, origin.x + rect.xMax + 17.3f) : 0f);
            // Darken a narrow rim along chipped edges so the cut reads as a rock edge with depth.
            float rimLeft = chipLeft ? Mathf.Min(RimWidth, (right - left) * .3f) : 0f, rimRight = chipRight ? Mathf.Min(RimWidth, (right - left) * .3f) : 0f;
            float[] xs = { left, left + rimLeft, right - rimRight, right };
            float[] shade = { chipLeft ? RimShade : 1f, 1f, 1f, chipRight ? RimShade : 1f };
            for (int c = 0; c < 4; c++)
            {
                float x = xs[c];
                vertices.Add(new Vector3(x, y));
                float u = worldU ? (origin.x + x) / tile.x : (x - rect.xMin) / rect.width;
                float v = worldV ? worldY / tile.y : (y - rect.yMin) / rect.height;
                uv.Add(new Vector2(u, v));
                colors.Add(new Color(shade[c], shade[c], shade[c], alpha));
            }
        }
        for (int i = 0; i + 1 < rows.Count; i++)
            for (int c = 0; c < 3; c++)
            {
                int a = i * 4 + c;
                triangles.AddRange(new[] { a, a + 4, a + 1, a + 1, a + 4, a + 5 });
            }
        var mesh = new Mesh { name = obj.name, hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        owned.Add(mesh);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = TerrainMaterial.Shared;
        renderer.sortingOrder = order + sortingOffset;
        var block = new MaterialPropertyBlock();
        block.SetTexture(MainTex, sprite.texture);
        renderer.SetPropertyBlock(block);
    }

    // Inward chip depth at a world height: two octaves of piecewise-linear value noise (large
    // slabs and small chips, so it reads as broken rock rather than a regular zigzag) plus a slow lean.
    private static float Chip(float worldY, float seed)
    {
        float Value(float t, float salt)
        {
            int i = Mathf.FloorToInt(t);
            float Hash(int k) => Mathf.Abs(Mathf.Sin(k * 12.9898f + seed * 78.233f + salt) * 43758.5453f) % 1f;
            return Mathf.Lerp(Hash(i), Hash(i + 1), t - i);
        }
        float slabs = Value(worldY / (ChipStep * 3.1f), 3.7f), chips = Value(worldY / (ChipStep * .83f), 9.1f);
        float lean = .5f + .5f * Mathf.Sin(worldY * .37f + seed);
        float n = Mathf.Clamp01(.5f * slabs + .32f * chips * chips + .18f * lean);
        return Mathf.Lerp(ChipMin, ChipMax, n);
    }

    private void Clear()
    {
        foreach (Object item in owned)
            if (item != null)
            {
                if (Application.isPlaying) Destroy(item);
                else DestroyImmediate(item);
            }
        owned.Clear();
        art = null;
        // Remove art left behind by a domain reload, which drops the owned list.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "Art (generated)") continue;
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = surface == Surface.Slippery ? new Color(.3f, .6f, 1f) : surface == Surface.Climbable ? new Color(.4f, 1f, .4f) : new Color(1f, .8f, .3f);
        Vector3 p = transform.position;
        Gizmos.DrawWireCube(p + new Vector3(width * .5f, -height * .5f), new Vector3(width, height));
    }
}

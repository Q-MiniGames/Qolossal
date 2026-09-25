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
    [Tooltip("Added to every sorting order, to layer overlapping blocks.")] public int sortingOffset;

    public const int FillOrder = -40, FaceOrder = -38, CeilingOrder = -37, TopOrder = -36, CornerOrder = -34;
    // Distances (world units) over which pieces fade into what's beneath them.
    const float TopFade = 1.4f, CeilingFade = .5f, FaceFade = .9f, CornerFade = .9f, FaceFillInset = .3f, CeilingFillInset = .3f;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static Material sharedMaterial;
    private readonly List<Object> owned = new List<Object>();
    private Transform art;
    private bool dirty;

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
        box.size = new Vector2(width, height);
        box.offset = new Vector2(width * .5f, -height * .5f);
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
        renderer.sharedMaterial = Material;
        renderer.sortingOrder = order + sortingOffset;
        var block = new MaterialPropertyBlock();
        block.SetTexture(MainTex, sprite.texture);
        renderer.SetPropertyBlock(block);
    }

    private static Material Material
    {
        get
        {
            if (sharedMaterial != null) return sharedMaterial;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            sharedMaterial = new Material(shader) { name = "Terrain Strip (Runtime)", hideFlags = HideFlags.DontSave };
            return sharedMaterial;
        }
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

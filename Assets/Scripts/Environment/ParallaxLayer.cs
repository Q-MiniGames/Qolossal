using UnityEngine;

// A background layer that follows the camera at a fraction of its movement. `follow` is how
// much of the camera's motion the layer copies: 1 = locked to the camera (infinitely far),
// 0 = fixed in the world like the terrain. Repeating layers tile horizontally forever. Below
// the painting, a flat band of `belowColor` fades in so the layer never shows a lower edge.
// A plain texture can stand in for the sprite (a painting imported as a texture, at `texturePixelsPerUnit`),
// and `tint` multiplies the painting (atmospheric haze toward the sky on the far layers).
//
// Climb progression (optional): instead of following the camera at its own vertical rate, a layer
// is placed relative to the camera centre (`baseY` is then its bottom when the camera stands at the
// bottom of `climbRange`) and sinks as the camera climbs, measured on a smoothed camera height
// shared by every layer, so a single jump or landing barely moves it.
// Real vertical parallax (`verticalRate` > 0, the default): a layer behind the terrain sinks at the
// rate its distance gives it, `verticalRate` / depth of the climb (the terrain moves 1), so the
// near hills drop toward the frame's bottom while the far peaks sink slowly; on a climb longer than
// `climbBudget` the rate is slowed so the paintings last the whole region. Without a
// depth, the distance comes from the horizontal follow (follow.x .9: 10). `sinkLimit` bounds the
// sinking (u at composedForSize); below 0 it is automatic: the farthest planes (depth 20 and over)
// and the depthless painting stop at 3 u so a skyline stays in view, nearer planes may leave.
// Planes in front of the terrain (depth < 1) and `verticalRate` 0 keep the earlier, gentle mode:
// they sink by `climbShift` across `climbRange` and hold their place otherwise.
// Layers run after the camera (execution order), so they never lag it by a frame.
//
// Depth (optional, `depth` > 0): the layer's distance from the camera relative to the gameplay
// terrain (terrain = 1, a far ridge = 25, a foreground plant nearer than the terrain < 1). It sets
// the horizontal rate as a real camera would (screen speed 1/depth: follow.x = 1 - 1/depth, so a
// foreground layer below 1 moves faster than the terrain), and it makes zoom behave like a camera
// pulling back instead of a picture being scaled: zoomed out by k, a layer at depth D keeps
// D / (D + k - 1) of its size on screen relative to the terrain, so the far ridges hardly change,
// the near hills shrink a little, and a foreground plane shrinks more than the terrain.
// Depth 0 keeps the earlier behaviour exactly (follow as set; every layer zooms by k together).
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(10000)]
public sealed class ParallaxLayer : MonoBehaviour
{
    public Sprite sprite;
    [Tooltip("Used when there is no sprite: a painting imported as a plain texture.")] public Texture2D texture;
    [Min(1f)] public float texturePixelsPerUnit = 100f;
    [Tooltip("Multiplies the painting (white: unchanged).")] public Color tint = Color.white;
    [Tooltip("0: off. Otherwise the camera size the layer is composed for: when the view zooms out (a lookout), the layer's " +
             "size and its height on screen scale with it, so its skyline keeps its place in the frame.")]
    [Min(0f)] public float composedForSize;
    [Tooltip("Hold a fixed height on screen and sink by climbShift across climbRange (see the class notes).")] public bool climbProgression;
    [Tooltip("The camera heights (world y) the climb runs between.")] public Vector2 climbRange = new Vector2(0f, 100f);
    [Tooltip("How far the layer sinks on screen from the bottom of the climb to its top (u, at composedForSize); the gentle mode only.")] public float climbShift;
    [Tooltip("Real vertical parallax: the share of 1 / depth the layer sinks per unit climbed (1: as a real camera). 0: the gentle climbShift mode.")]
    [Min(0f)] public float verticalRate = 1f;
    [Tooltip("The most the layer sinks with real vertical parallax (u at composedForSize); below 0: automatic (see the class notes).")]
    public float sinkLimit = -1f;
    [Tooltip("Real vertical parallax over a long climb: a plane at depth d sinks at most climbBudget / d across climbRange (0: no fitting).")]
    [Min(0f)] public float climbBudget = 35f;
    [Tooltip("Layer height in world units; 0 uses the sprite's own size.")] [Min(0f)] public float height;
    public bool repeat = true;
    [Tooltip("Share of the camera's movement the layer copies, per axis.")] public Vector2 follow = new Vector2(.8f, .8f);
    [Tooltip("World y of the layer's bottom edge when the camera is at y = 0.")] public float baseY;
    [Tooltip("World x of the layer's centre when the camera is at x = 0 (non-repeating layers).")] public float baseX;
    [Tooltip("Depth of the flat band under the painting; 0 for none.")] [Min(0f)] public float extendBelow = 12f;
    [Tooltip("Colour of the band under the painting, usually the average of its bottom row.")] public Color belowColor = Color.white;
    [Tooltip("How far up into the painting the band fades in.")] [Min(0f)] public float belowBlend = .6f;
    public int sortingOrder = -90;
    [Tooltip("0: off (follow and zoom as set). Otherwise distance from the camera relative to the terrain (1): sets the horizontal rate and the zoom response.")]
    [Min(0f)] public float depth;
    [Tooltip("The part of the painting drawn, in UV (x, y, width, height from bottom-left); (0, 0, 1, 1): all of it.")]
    public Rect uvRect = new Rect(0f, 0f, 1f, 1f);
    public Camera targetCamera;

    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private Mesh paintingMesh, bandMesh;
    private MeshRenderer paintingRenderer, bandRenderer;
    private GameObject band;
    private (float, float, float, float, float, float, float, Color) paintingKey;
    private (float, float, float, float, Color) bandKey;

    // Copied straight into the meshes, so one set serves every layer.
    static readonly Vector3[] Quad = new Vector3[4], Band = new Vector3[6];
    static readonly Vector2[] QuadUv = new Vector2[4], BandUv = new Vector2[6];
    static readonly Color[] QuadColors = new Color[4], BandColors = new Color[6];
    static readonly int[] QuadTriangles = { 0, 2, 1, 1, 2, 3 }, BandTriangles = { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };

    private Texture2D Painting => sprite != null ? sprite.texture : texture;
    private float Height => height > 0f ? height : sprite != null ? sprite.rect.height / sprite.pixelsPerUnit : texture != null ? texture.height / texturePixelsPerUnit : 0f;
    private float Width => (sprite != null ? Height * sprite.rect.width / sprite.rect.height : texture != null ? Height * texture.width / texture.height : 1f)
                           * (uvRect.height > 0f ? uvRect.width / uvRect.height : 1f);

    /// <summary>The horizontal share of the camera's motion the layer copies (from depth when set).</summary>
    public float FollowX => depth > 0f ? 1f - 1f / depth : follow.x;

    // The layer's size multiplier for a zoom of k (the view's size over composedForSize): k for an
    // infinitely far layer (it holds its size on screen), 1 for the terrain (world-fixed).
    float ZoomScale(float k) => depth > 0f ? k * depth / (depth + k - 1f) : k;

    // Real vertical parallax applies to planes behind the terrain.
    bool RealVertical => verticalRate > 0f && (depth >= 1f || depth <= 0f && FollowX < 1f);
    float VerticalDepth => depth > 0f ? depth : 1f / Mathf.Max(.01f, 1f - FollowX);
    float SinkLimit => sinkLimit >= 0f ? sinkLimit : depth > 0f && depth < 20f ? float.PositiveInfinity : 3f;

    /// <summary>How far the layer has sunk on screen with the camera (smoothed) at height y.</summary>
    public float Sink(float y) => RealVertical ? Mathf.Clamp((y - climbRange.x) * verticalRate * ClimbFit / VerticalDepth, 0f, SinkLimit) : 0f;

    // A long climb would carry the paintings out of the frame early; the rate is then slowed so a
    // plane at depth d sinks at most climbBudget / d over the whole climb (the near hills, depth 5,
    // 7 u: down to the frame's bottom at the top of the region), keeping the depth ratios.
    float ClimbFit => climbBudget > 0f ? Mathf.Min(1f, climbBudget / Mathf.Max(1f, climbRange.y - climbRange.x)) : 1f;

    private void OnEnable()
    {
        paintingMesh = NewMesh("Parallax " + name);
        paintingRenderer = Setup(gameObject, paintingMesh);
        band = new GameObject("Below band (generated)") { hideFlags = HideFlags.HideAndDontSave };
        band.transform.SetParent(transform, false);
        bandMesh = NewMesh("Parallax band " + name);
        bandRenderer = Setup(band, bandMesh);
        Apply();
    }

    private void OnDisable()
    {
        Remove(paintingMesh); Remove(bandMesh); Remove(band);
    }

    // OnValidate also runs while the editor loads or reloads, when the generated band object
    // may already be gone; only re-apply when both renderers are alive.
    private void OnValidate() { if (paintingRenderer != null && bandRenderer != null) Apply(); }

    private void LateUpdate() => Refresh(targetCamera != null ? targetCamera : Camera.main);

    private static Mesh NewMesh(string label)
    {
        var mesh = new Mesh { name = label, hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        return mesh;
    }

    private static MeshRenderer Setup(GameObject obj, Mesh mesh)
    {
        if (!obj.TryGetComponent(out MeshFilter filter)) filter = obj.AddComponent<MeshFilter>();
        filter.hideFlags = HideFlags.HideInInspector;
        filter.sharedMesh = mesh;
        if (!obj.TryGetComponent(out MeshRenderer renderer)) renderer = obj.AddComponent<MeshRenderer>();
        renderer.hideFlags = HideFlags.HideInInspector;
        renderer.sharedMaterial = TerrainMaterial.Shared;
        return renderer;
    }

    private void Apply()
    {
        paintingRenderer.sortingOrder = sortingOrder;
        bandRenderer.sortingOrder = sortingOrder + 1;
        var block = new MaterialPropertyBlock();
        if (Painting != null) block.SetTexture(MainTex, Painting);
        paintingRenderer.SetPropertyBlock(block);
        var white = new MaterialPropertyBlock();
        white.SetTexture(MainTex, Texture2D.whiteTexture);
        bandRenderer.SetPropertyBlock(white);
        Refresh(targetCamera != null ? targetCamera : Camera.main);
    }

    // Repositions the layer for the camera's current position.
    public void Refresh(Camera view)
    {
        if (view == null || paintingMesh == null || Painting == null) return;
        Vector3 cam = view.transform.position;
        float halfWidth = view.orthographicSize * view.aspect + 1f;
        float k = composedForSize > 0f && view.orthographic ? view.orthographicSize / composedForSize : 1f;
        k = ZoomScale(k);
        float h = Height * k, w = Width * k;
        float y;
        if (climbProgression && RealVertical)
            y = cam.y + (baseY - Sink(ClimbHeight(view))) * k;
        else if (climbProgression)
        {
            float t = Mathf.InverseLerp(climbRange.x, climbRange.y, ClimbHeight(view));
            y = cam.y + (baseY - climbShift * (t - .5f)) * k;
        }
        else
        {
            y = baseY + cam.y * follow.y;
            if (k != 1f) y = cam.y + (y - cam.y) * k;
        }
        transform.SetPositionAndRotation(new Vector3(cam.x, y, transform.position.z), Quaternion.identity);
        transform.localScale = Vector3.one;

        // Where the painting's centre sits relative to the camera, in world units.
        // Zoomed (composedForSize), the offset scales with the layer, so every layer zooms about the
        // camera centre together instead of sliding against the others.
        float drift = (baseX + cam.x * FollowX - cam.x) * k;
        float left = -halfWidth, right = halfWidth;
        if (!repeat) { left = Mathf.Max(left, drift - w * .5f); right = Mathf.Min(right, drift + w * .5f); }
        // A sub-rectangle (uvRect) maps onto the quad; repeating layers wrap within the whole texture.
        float U(float x) => uvRect.x + ((x - drift) / w + .5f) * uvRect.width;
        float v0 = uvRect.y, v1 = uvRect.y + uvRect.height;

        // The meshes are rewritten only when something changed, from arrays kept for it, so a frame allocates nothing.
        var painted = (left, right, h, U(left), U(right), v0, v1, tint);
        if (painted != paintingKey || paintingMesh.vertexCount == 0)
        {
            paintingKey = painted;
            Quad[0] = new Vector3(left, 0f); Quad[1] = new Vector3(right, 0f); Quad[2] = new Vector3(left, h); Quad[3] = new Vector3(right, h);
            QuadUv[0] = new Vector2(U(left), v0); QuadUv[1] = new Vector2(U(right), v0); QuadUv[2] = new Vector2(U(left), v1); QuadUv[3] = new Vector2(U(right), v1);
            QuadColors[0] = QuadColors[1] = QuadColors[2] = QuadColors[3] = tint;
            paintingMesh.Clear();
            paintingMesh.vertices = Quad;
            paintingMesh.uv = QuadUv;
            paintingMesh.colors = QuadColors;
            paintingMesh.triangles = QuadTriangles;
            paintingMesh.RecalculateBounds();
        }

        if (extendBelow <= 0f) { if (bandMesh.vertexCount > 0) bandMesh.Clear(); return; }
        float extend = extendBelow * k;
        // The band fades from clear (belowBlend up inside the painting) to solid at its bottom edge.
        float blend = Mathf.Min(belowBlend, h);
        Color clear = new Color(belowColor.r, belowColor.g, belowColor.b, 0f);
        var banded = (left, right, extend, blend, belowColor);
        if (banded == bandKey && bandMesh.vertexCount > 0) return;
        bandKey = banded;
        Band[0] = new Vector3(left, -extend); Band[1] = new Vector3(right, -extend); Band[2] = new Vector3(left, 0f);
        Band[3] = new Vector3(right, 0f); Band[4] = new Vector3(left, blend); Band[5] = new Vector3(right, blend);
        BandColors[0] = BandColors[1] = BandColors[2] = BandColors[3] = belowColor; BandColors[4] = BandColors[5] = clear;
        bandMesh.Clear();
        bandMesh.vertices = Band;
        bandMesh.uv = BandUv;
        bandMesh.colors = BandColors;
        bandMesh.triangles = BandTriangles;
        bandMesh.RecalculateBounds();
    }

    // The camera's height, smoothed over about a second (once per frame, shared by all layers): a
    // climb moves the layers with it, a single jump hardly does;
    // outside Play mode (editor views, captures) the camera's own height.
    static float climbHeight, climbVelocity; static int climbFrame = -1; static Camera climbCamera;
    /// <summary>The smoothed camera height the climb layers sink by (for fades tied to the same climb).</summary>
    public static float SmoothedClimbHeight(Camera view) => ClimbHeight(view);

    static float ClimbHeight(Camera view)
    {
        float y = view.transform.position.y;
        if (!Application.isPlaying) return y;
        if (climbCamera != view || climbFrame < 0 || Mathf.Abs(y - climbHeight) > 30f) { climbCamera = view; climbHeight = y; climbVelocity = 0f; climbFrame = Time.frameCount; }
        else if (climbFrame != Time.frameCount)
        {
            climbFrame = Time.frameCount;
            climbHeight = Mathf.SmoothDamp(climbHeight, y, ref climbVelocity, 1f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
        return climbHeight;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetClimb() { climbFrame = -1; climbCamera = null; }

    private static void Remove(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}

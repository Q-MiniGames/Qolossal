using UnityEngine;

// A background layer that follows the camera at a fraction of its movement. `follow` is how
// much of the camera's motion the layer copies: 1 = locked to the camera (infinitely far),
// 0 = fixed in the world like the terrain. Repeating layers tile horizontally forever. Below
// the painting, a flat band of `belowColor` fades in so the layer never shows a lower edge.
// A plain texture can stand in for the sprite (a painting imported as a texture, at `texturePixelsPerUnit`),
// and `tint` multiplies the painting (atmospheric haze toward the sky on the far layers).
//
// Climb progression (optional): instead of following the camera at its own vertical rate, a layer
// sits at a fixed height on screen (`baseY` is then its bottom relative to the camera centre) and
// sinks by `climbShift` as the camera climbs through `climbRange`, measured on a slowly smoothed
// camera height shared by every layer. Jumps, landings and the camera's framing leads don't move
// it; a sustained climb lowers the near layers more than the far ones, in a fixed depth order.
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
    [Tooltip("How far the layer sinks on screen from the bottom of the climb to its top (u, at composedForSize).")] public float climbShift;
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

    private Texture2D Painting => sprite != null ? sprite.texture : texture;
    private float Height => height > 0f ? height : sprite != null ? sprite.rect.height / sprite.pixelsPerUnit : texture != null ? texture.height / texturePixelsPerUnit : 0f;
    private float Width => (sprite != null ? Height * sprite.rect.width / sprite.rect.height : texture != null ? Height * texture.width / texture.height : 1f)
                           * (uvRect.height > 0f ? uvRect.width / uvRect.height : 1f);

    /// <summary>The horizontal share of the camera's motion the layer copies (from depth when set).</summary>
    public float FollowX => depth > 0f ? 1f - 1f / depth : follow.x;

    // The layer's size multiplier for a zoom of k (the view's size over composedForSize): k for an
    // infinitely far layer (it holds its size on screen), 1 for the terrain (world-fixed).
    float ZoomScale(float k) => depth > 0f ? k * depth / (depth + k - 1f) : k;

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
        if (climbProgression)
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

        paintingMesh.Clear();
        paintingMesh.vertices = new[] { new Vector3(left, 0f), new Vector3(right, 0f), new Vector3(left, h), new Vector3(right, h) };
        paintingMesh.uv = new[] { new Vector2(U(left), v0), new Vector2(U(right), v0), new Vector2(U(left), v1), new Vector2(U(right), v1) };
        paintingMesh.colors = new[] { tint, tint, tint, tint };
        paintingMesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        paintingMesh.RecalculateBounds();

        bandMesh.Clear();
        if (extendBelow <= 0f) return;
        float extend = extendBelow * k;
        // The band fades from clear (belowBlend up inside the painting) to solid at its bottom edge.
        float blend = Mathf.Min(belowBlend, h);
        Color clear = new Color(belowColor.r, belowColor.g, belowColor.b, 0f);
        bandMesh.vertices = new[] { new Vector3(left, -extend), new Vector3(right, -extend), new Vector3(left, 0f),
            new Vector3(right, 0f), new Vector3(left, blend), new Vector3(right, blend) };
        bandMesh.uv = new Vector2[6];
        bandMesh.colors = new[] { belowColor, belowColor, belowColor, belowColor, clear, clear };
        bandMesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
        bandMesh.RecalculateBounds();
    }

    // The camera's height, smoothed over a couple of seconds (once per frame, shared by all layers);
    // outside Play mode (editor views, captures) the camera's own height.
    static float climbHeight, climbVelocity; static int climbFrame = -1; static Camera climbCamera;
    static float ClimbHeight(Camera view)
    {
        float y = view.transform.position.y;
        if (!Application.isPlaying) return y;
        if (climbCamera != view || climbFrame < 0 || Mathf.Abs(y - climbHeight) > 30f) { climbCamera = view; climbHeight = y; climbVelocity = 0f; climbFrame = Time.frameCount; }
        else if (climbFrame != Time.frameCount)
        {
            climbFrame = Time.frameCount;
            climbHeight = Mathf.SmoothDamp(climbHeight, y, ref climbVelocity, 2.2f, Mathf.Infinity, Time.unscaledDeltaTime);
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

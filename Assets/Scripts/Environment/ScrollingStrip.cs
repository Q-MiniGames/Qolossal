using UnityEngine;

// A rectangle of repeating art whose texture can drift at `scroll` tiles per second: flowing
// water (Water_Surface, Water_Body, Waterfall_Column) without extra frames, or a still, darkened
// back wall of terrain fill. The transform is the rectangle's top-left corner. Texture
// coordinates follow world position, so neighbouring strips line up.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollingStrip : MonoBehaviour
{
    public Sprite sprite;
    [Min(.1f)] public float width = 4f, height = 1f;
    [Tooltip("Tiles per second along x and y.")] public Vector2 scroll = new Vector2(.05f, 0f);
    [Tooltip("Stretch the art to the full height instead of repeating it (a surface strip).")] public bool fitHeight;
    [Tooltip("Stretch the art to the full width instead of repeating it (a waterfall column).")] public bool fitWidth;
    public Color tint = new Color(1f, 1f, 1f, .75f);
    [Tooltip("Fade to transparent over this distance at the top (0: no fade).")] [Min(0f)] public float fadeTop;
    public int sortingOrder = 20;

    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    Mesh mesh; MeshRenderer view; readonly Vector2[] uv = new Vector2[6];

    void OnEnable()
    {
        var art = transform.Find("Strip (generated)");
        if (art == null)
        {
            art = new GameObject("Strip (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable }.transform;
            art.SetParent(transform, false);
        }
        if (!art.TryGetComponent(out MeshFilter filter)) filter = art.gameObject.AddComponent<MeshFilter>();
        if (!art.TryGetComponent(out view)) view = art.gameObject.AddComponent<MeshRenderer>();
        mesh = new Mesh { name = "Scrolling strip", hideFlags = HideFlags.DontSave };
        // Two rows: solid up to the fade band, then fading to the top edge.
        float band = -Mathf.Min(fadeTop, height);
        mesh.vertices = new Vector3[] { new Vector3(0f, -height), new Vector3(width, -height), new Vector3(0f, band), new Vector3(width, band),
                                        new Vector3(0f, 0f), new Vector3(width, 0f) };
        Color top = fadeTop > 0f ? new Color(tint.r, tint.g, tint.b, 0f) : tint;
        mesh.colors = new[] { tint, tint, tint, tint, top, top };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3, 3, 4, 5 };
        filter.sharedMesh = mesh;
        view.sharedMaterial = TerrainMaterial.Shared;
        view.sortingOrder = sortingOrder;
        if (sprite != null) { var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, sprite.texture); view.SetPropertyBlock(block); }
        UpdateUv(0f);
    }

    void OnDisable()
    {
        var art = transform.Find("Strip (generated)");
        if (art != null) { if (Application.isPlaying) Destroy(art.gameObject); else DestroyImmediate(art.gameObject); }
        if (mesh != null) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
    }

    void Update() { if (mesh != null) UpdateUv(Application.isPlaying ? Time.time : 0f); }

    void UpdateUv(float time)
    {
        if (sprite == null) return;
        float tw = sprite.rect.width / sprite.pixelsPerUnit, th = sprite.rect.height / sprite.pixelsPerUnit;
        Vector3 p = transform.position;
        float u0 = fitWidth ? 0f : p.x / tw + scroll.x * time, u1 = fitWidth ? 1f : u0 + width / tw;
        float v1 = fitHeight ? 1f : p.y / th + scroll.y * time, v0 = fitHeight ? 0f : v1 - height / th;
        float vb = Mathf.Lerp(v1, v0, Mathf.Min(fadeTop, height) / height);
        uv[0] = new Vector2(u0, v0); uv[1] = new Vector2(u1, v0); uv[2] = new Vector2(u0, vb); uv[3] = new Vector2(u1, vb);
        uv[4] = new Vector2(u0, v1); uv[5] = new Vector2(u1, v1);
        mesh.uv = uv;
        mesh.RecalculateBounds();
    }
}

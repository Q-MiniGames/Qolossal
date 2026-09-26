using UnityEngine;

// A rectangle of repeating art whose texture can drift at `scroll` tiles per second: flowing
// water (Water_Surface, Water_Body, Waterfall_Column) without extra frames, a still, darkened
// back wall of terrain fill, or a hazard strip. Each edge can fade to transparent so the strip
// melts into what's around it instead of ending in a straight cut. The transform is the
// rectangle's top-left corner. Texture coordinates follow world position, so neighbouring
// strips line up.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollingStrip : MonoBehaviour
{
    public Sprite sprite;
    [Min(.1f)] public float width = 4f, height = 1f;
    [Tooltip("Tiles per second along x and y.")] public Vector2 scroll = new Vector2(.05f, 0f);
    [Tooltip("Stretch the art to the full height instead of repeating it (a surface strip).")] public bool fitHeight;
    [Tooltip("Stretch the art to the full width instead of repeating it (a waterfall column).")] public bool fitWidth;
    public Color tint = new Color(1f, 1f, 1f, .75f);
    [Tooltip("Fade to transparent over these distances at each edge (0: a hard edge).")]
    [Min(0f)] public float fadeTop, fadeBottom, fadeSides;
    public int sortingOrder = 20;

    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    Mesh mesh; MeshRenderer view;
    float[] xs, ys;
    readonly Vector2[] uv = new Vector2[16];

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

        // A 3x3 grid: solid in the middle, fading across each edge band.
        float side = Mathf.Min(fadeSides, width * .5f), top = Mathf.Min(fadeTop, height * .5f), bottom = Mathf.Min(fadeBottom, height * .5f);
        xs = new[] { 0f, side, width - side, width };
        ys = new[] { -height, -height + bottom, -top, 0f };
        var vertices = new Vector3[16]; var colors = new Color[16];
        Color clear = new Color(tint.r, tint.g, tint.b, 0f);
        for (int row = 0; row < 4; row++)
            for (int c = 0; c < 4; c++)
            {
                int i = row * 4 + c;
                vertices[i] = new Vector3(xs[c], ys[row]);
                bool faded = (c == 0 || c == 3) && fadeSides > 0f || row == 0 && fadeBottom > 0f || row == 3 && fadeTop > 0f;
                colors[i] = faded ? clear : tint;
            }
        var triangles = new int[54];
        for (int row = 0, t = 0; row < 3; row++)
            for (int c = 0; c < 3; c++, t += 6)
            {
                int i = row * 4 + c;
                triangles[t] = i; triangles[t + 1] = i + 4; triangles[t + 2] = i + 1;
                triangles[t + 3] = i + 1; triangles[t + 4] = i + 4; triangles[t + 5] = i + 5;
            }
        mesh = new Mesh { name = "Scrolling strip", hideFlags = HideFlags.DontSave, vertices = vertices, colors = colors, triangles = triangles };
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

    void Update() { if (mesh != null && scroll != Vector2.zero) UpdateUv(Application.isPlaying ? Time.time : 0f); }

    void UpdateUv(float time)
    {
        if (sprite == null) return;
        float tw = sprite.rect.width / sprite.pixelsPerUnit, th = sprite.rect.height / sprite.pixelsPerUnit;
        Vector3 p = transform.position;
        for (int row = 0; row < 4; row++)
            for (int c = 0; c < 4; c++)
            {
                float u = fitWidth ? xs[c] / width : (p.x + xs[c]) / tw + scroll.x * time;
                float v = fitHeight ? 1f + ys[row] / height : (p.y + ys[row]) / th + scroll.y * time;
                uv[row * 4 + c] = new Vector2(u, v);
            }
        mesh.uv = uv;
        mesh.RecalculateBounds();
    }
}

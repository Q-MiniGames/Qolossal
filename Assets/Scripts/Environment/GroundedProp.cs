using UnityEngine;

// Seats a prop standing on terrain so it reads as part of the ground rather than pasted on top:
// a soft contact shadow under its base, and a strip of the ground's own moss lip (Ground_Top,
// texture-aligned with the terrain beneath, so it's seamless) drawn in front of the base with
// faded ends. Built at edit and play time from these settings; nothing generated is saved.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class GroundedProp : MonoBehaviour
{
    public Sprite groundTop;
    [Tooltip("Ground_Top's walk line, px from its top edge (the kit's groundTopWalkLine).")] public float walkLinePx = 96f;
    [Tooltip("World y of the walk surface the prop stands on.")] public float groundY;
    [Tooltip("Width of the prop's base, world units.")] [Min(.1f)] public float width = 1.5f;
    [Tooltip("Sorting order of the prop art; the shadow goes just behind it, the moss in front.")] public int propOrder = -15;
    public bool shadow = true, mossLip = true;

    const float LipBelow = .32f, LipAbove = .22f, LipFade = .35f, LipOverhang = .3f;
    static readonly int MainTex = Shader.PropertyToID("_MainTex");
    static Texture2D shadowTexture; static Sprite shadowSprite;
    GameObject built; Mesh lipMesh;

    void OnEnable()
    {
        Clear();
        built = new GameObject("Grounding (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        built.transform.SetParent(transform, false);
        built.transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        if (shadow) BuildShadow();
        if (mossLip && groundTop != null) BuildLip();
    }

    void OnDisable() => Clear();

    void Clear()
    {
        var old = transform.Find("Grounding (generated)");
        if (old != null) Remove(old.gameObject);
        if (built != null) Remove(built);
        if (lipMesh != null) Remove(lipMesh);
        built = null; lipMesh = null;
    }

    void BuildShadow()
    {
        if (shadowSprite == null)
        {
            const int size = 64;
            shadowTexture = new Texture2D(size, size / 2, TextureFormat.RGBA32, false) { name = "Contact shadow", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var pixels = new Color[size * size / 2];
            for (int y = 0; y < size / 2; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / (size / 2) * 2f - 1f).magnitude;
                    float a = Mathf.Clamp01(1f - d); pixels[y * size + x] = new Color(0f, 0f, 0f, a * a);
                }
            shadowTexture.SetPixels(pixels); shadowTexture.Apply(false, true);
            shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, size, size / 2), new Vector2(.5f, .5f), size);
            shadowSprite.hideFlags = HideFlags.DontSave;
        }
        var r = new GameObject("Contact shadow").AddComponent<SpriteRenderer>();
        r.transform.SetParent(built.transform, false);
        r.transform.localPosition = new Vector3(0f, .04f, 0f);
        r.transform.localScale = new Vector3(width * 1.25f, .9f, 1f);
        r.sprite = shadowSprite; r.color = new Color(1f, 1f, 1f, .38f); r.sortingOrder = propOrder - 1;
    }

    // A quad over [base - overhang, base + overhang] showing the Ground_Top texture exactly where
    // the terrain shows it (u follows world x, v follows height above the walk line).
    void BuildLip()
    {
        float ppu = groundTop.pixelsPerUnit, tileW = groundTop.rect.width / ppu, tileH = groundTop.rect.height / ppu;
        float below = (groundTop.rect.height - walkLinePx) / ppu;   // walk line height above the strip's bottom
        float half = width * .5f + LipOverhang;
        float[] xs = { -half, -half + LipFade, half - LipFade, half };
        float[] ys = { -LipBelow, LipAbove };
        var vertices = new Vector3[8]; var uv = new Vector2[8]; var colors = new Color[8];
        for (int row = 0; row < 2; row++)
            for (int c = 0; c < 4; c++)
            {
                int i = row * 4 + c;
                vertices[i] = new Vector3(xs[c], ys[row]);
                uv[i] = new Vector2((transform.position.x + xs[c]) / tileW, (below + ys[row]) / tileH);
                colors[i] = c == 0 || c == 3 ? new Color(1f, 1f, 1f, 0f) : Color.white;
            }
        lipMesh = new Mesh { name = "Moss lip", hideFlags = HideFlags.DontSave, vertices = vertices, uv = uv, colors = colors,
            triangles = new[] { 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 6, 2, 6, 3, 3, 6, 7 } };
        lipMesh.RecalculateBounds();
        var obj = new GameObject("Moss lip");
        obj.transform.SetParent(built.transform, false);
        obj.AddComponent<MeshFilter>().sharedMesh = lipMesh;
        var renderer = obj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = TerrainMaterial.Shared;
        renderer.sortingOrder = propOrder + 5;
        var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, groundTop.texture); renderer.SetPropertyBlock(block);
    }

    static void Remove(Object item) { if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
}

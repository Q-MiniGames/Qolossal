using System.Collections.Generic;
using UnityEngine;

// Palm prototype (world redesign proposal, section 8): a placeholder piece of the titan's body
// drawn as a flat filled outline, shaded darker toward its base, with a moss line along its walk
// surface. When solid it is ground: a polygon collider on the Ground layer that Qori can't cling
// to (bare skin and nail). The mesh is rebuilt whenever the component is enabled, so the scene
// only stores the outline.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class BodyShape : MonoBehaviour
{
    [Tooltip("Closed outline in local space, either winding; empty for a line only.")] public Vector2[] outline = new Vector2[0];
    [Tooltip("Optional open polyline in local space, drawn as the moss line along the walk surface.")] public Vector2[] rim = new Vector2[0];
    public Color top = new Color(.62f, .55f, .45f), bottom = new Color(.36f, .31f, .27f);
    public Color rimColor = new Color(.46f, .6f, .34f);
    [Min(0f)] public float rimWidth = .3f;
    public int sortingOrder;
    public bool solid = true;

    Mesh mesh;
    static readonly int MainTex = Shader.PropertyToID("_MainTex");

    void OnEnable() => Rebuild();

    void OnDisable()
    {
        foreach (var m in new[] { mesh, rimMesh }) if (m != null) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
        mesh = rimMesh = null;
    }

    public void Rebuild()
    {
        var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, Texture2D.whiteTexture);
        if (outline != null && outline.Length >= 3) BuildFill(block);
        BuildRim(block);
    }

    void BuildFill(MaterialPropertyBlock block)
    {
        if (!TryGetComponent(out MeshFilter filter)) filter = gameObject.AddComponent<MeshFilter>();
        if (!TryGetComponent(out MeshRenderer view)) view = gameObject.AddComponent<MeshRenderer>();
        view.sharedMaterial = TerrainMaterial.Shared; view.sortingOrder = sortingOrder; view.SetPropertyBlock(block);

        if (mesh == null) mesh = new Mesh { name = name + " (generated)", hideFlags = HideFlags.DontSave };
        mesh.Clear();
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var p in outline) { lo = Mathf.Min(lo, p.y); hi = Mathf.Max(hi, p.y); }
        var vertices = new Vector3[outline.Length]; var colors = new Color[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            vertices[i] = outline[i];
            colors[i] = Color.Lerp(bottom, top, Mathf.InverseLerp(lo, hi, outline[i].y));
        }
        mesh.vertices = vertices; mesh.colors = colors;
        mesh.uv = new Vector2[outline.Length];
        mesh.triangles = Triangulate(outline);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;

        TryGetComponent(out PolygonCollider2D collider);
        if (solid)
        {
            gameObject.layer = LayerMask.NameToLayer("Ground");
            if (collider == null) collider = gameObject.AddComponent<PolygonCollider2D>();
            collider.pathCount = 1; collider.SetPath(0, outline);
            if (!TryGetComponent(out WallSurface wall)) wall = gameObject.AddComponent<WallSurface>();
            wall.allowsWallCling = false;
        }
        else if (collider != null) collider.enabled = false;
    }

    // The moss line (or, on a shape with no outline, a crease line) along `rim`: a strip mesh
    // `rimWidth` thick, hanging just below the line so it sits on the surface.
    Mesh rimMesh;
    void BuildRim(MaterialPropertyBlock block)
    {
        var child = transform.Find("Rim");
        if (rim == null || rim.Length < 2 || rimWidth <= 0f) { if (child != null) child.gameObject.SetActive(false); return; }
        if (child == null) { child = new GameObject("Rim").transform; child.SetParent(transform, false); }
        child.gameObject.SetActive(true);
        if (!child.TryGetComponent(out MeshFilter filter)) filter = child.gameObject.AddComponent<MeshFilter>();
        if (!child.TryGetComponent(out MeshRenderer view)) view = child.gameObject.AddComponent<MeshRenderer>();
        view.sharedMaterial = TerrainMaterial.Shared; view.sortingOrder = sortingOrder + 1; view.SetPropertyBlock(block);
        if (rimMesh == null) rimMesh = new Mesh { name = name + " rim (generated)", hideFlags = HideFlags.DontSave };
        rimMesh.Clear();
        int n = rim.Length;
        var vertices = new Vector3[n * 2]; var colors = new Color[n * 2]; var tris = new int[(n - 1) * 6];
        Color under = new Color(rimColor.r, rimColor.g, rimColor.b, 0f);
        for (int i = 0; i < n; i++)
        {
            Vector2 along = (rim[Mathf.Min(n - 1, i + 1)] - rim[Mathf.Max(0, i - 1)]).normalized;
            Vector2 up = new Vector2(-along.y, along.x);
            if (up.y < 0f) up = -up;
            vertices[i * 2] = rim[i] + up * rimWidth * .25f;
            vertices[i * 2 + 1] = rim[i] - up * rimWidth * .75f;
            colors[i * 2] = rimColor; colors[i * 2 + 1] = outline != null && outline.Length >= 3 ? under : rimColor;
        }
        for (int i = 0; i < n - 1; i++)
        {
            int t = i * 6, v = i * 2;
            tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
            tris[t + 3] = v + 1; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
        }
        rimMesh.vertices = vertices; rimMesh.colors = colors; rimMesh.uv = new Vector2[n * 2];
        rimMesh.triangles = tris; rimMesh.RecalculateBounds();
        filter.sharedMesh = rimMesh;
    }

    // Ear clipping for a simple polygon of either winding.
    static int[] Triangulate(Vector2[] p)
    {
        int n = p.Length;
        float area = 0f;
        for (int i = 0, j = n - 1; i < n; j = i++) area += p[j].x * p[i].y - p[i].x * p[j].y;
        var index = new List<int>(n);
        for (int i = 0; i < n; i++) index.Add(area > 0f ? i : n - 1 - i);   // counter-clockwise
        var tris = new List<int>((n - 2) * 3);
        int guard = n * n;
        while (index.Count > 3 && guard-- > 0)
        {
            bool clipped = false;
            for (int k = 0; k < index.Count; k++)
            {
                int a = index[(k + index.Count - 1) % index.Count], b = index[k], c = index[(k + 1) % index.Count];
                if (Cross(p[a], p[b], p[c]) <= 1e-6f) continue;   // reflex
                bool inside = false;
                foreach (int o in index)
                    if (o != a && o != b && o != c && InTriangle(p[o], p[a], p[b], p[c])) { inside = true; break; }
                if (inside) continue;
                tris.Add(a); tris.Add(b); tris.Add(c);
                index.RemoveAt(k); clipped = true; break;
            }
            if (!clipped) break;   // degenerate outline: draw what we have
        }
        if (index.Count == 3) { tris.Add(index[0]); tris.Add(index[1]); tris.Add(index[2]); }
        return tris.ToArray();
    }

    static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c) =>
        Cross(a, b, p) >= 0f && Cross(b, c, p) >= 0f && Cross(c, a, p) >= 0f;
}

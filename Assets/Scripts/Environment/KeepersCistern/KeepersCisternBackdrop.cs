using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Route A (A01-A08 and their passages) background: "The Keepers' Cistern" (Batch 49, Claude Reviews 97-105).
// Runtime port of the Editor-only KeepersCisternPreview: same pieces, placements, layer motions,
// shared room light maps and depth pass. Builds only in Play, as unsaved children of this object.
// Switch: Qolossal > Backgrounds > Route A Keepers' Cistern (or disable this GameObject); the old
// cave art underneath is untouched and shows again when this is off.
public sealed partial class KeepersCisternBackdrop : MonoBehaviour
{
    [SerializeField] Shader shader;
    [SerializeField] List<Texture2D> paintings = new();

    public bool Near { get; private set; } = true;
    public string[] MissingPaintings => missingPaint.Distinct().ToArray();

    const int PixelsPerUnit = 216;
    GameObject root;
    Dictionary<string, Texture2D> byName;
    readonly List<Piece> pieces = new();
    readonly List<UnityEngine.Object> owned = new();
    readonly List<string> missingPaint = new();

    sealed class Piece { public Transform t; public Vector3 basis; public Vector2 anchor, motion; public Renderer r; public bool near; public Rect rect, clip; }

    static readonly Rect A01 = new(1480, 170, 36, 22), A02 = new(1528, 126, 16, 60), A03 = new(1490, 102, 48, 12), A04 = new(1552, 84, 64, 36),
        A05 = new(1628, 92, 16, 48), A06 = new(1554, 154, 60, 32), A07 = new(1628, 170, 42, 14), A08 = new(1684, 168, 52, 28),
        T1 = new(1538, 106, 14, 4), T2 = new(1616, 106, 12, 4);

    public void Configure(Shader s, IEnumerable<Texture2D> textures) { shader = s; paintings = textures.Where(t => t).OrderBy(t => t.name).ToList(); }
    public IReadOnlyList<Texture2D> Paintings => paintings;

    void OnEnable() { RenderPipelineManager.beginCameraRendering += BeforeCamera; Camera.onPreCull += Apply; }
    void OnDisable() { RenderPipelineManager.beginCameraRendering -= BeforeCamera; Camera.onPreCull -= Apply; if (Application.isPlaying) Clear(); }

    IEnumerator Start()
    {
        if (!Application.isPlaying) yield break;
        Build();
        // Terrain contact shadows follow the real Ground colliders, some of which are generated at start.
        float until = Time.time + 1f;
        while (Time.time < until) yield return null;
        if (root) { AddTerrainContact(); Apply(Camera.main); }
    }

    void BeforeCamera(ScriptableRenderContext c, Camera cam) => Apply(cam);

    public void SetNear(bool value) { Near = value; foreach (var p in pieces) if (p.r) p.r.enabled = !p.near || value; }

    public void Clear()
    {
        pieces.Clear();
        if (root) Kill(root);
        root = null;
        foreach (var o in owned) if (o) Kill(o);
        owned.Clear();
    }

    static void Kill(UnityEngine.Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }

    // Edit-mode build for the batch checker and captures; nothing it creates is saved.
    public void BuildForCheck(bool withTerrainContact) { byName = null; Build(); if (root && withTerrainContact) AddTerrainContact(); }
    public void ApplyForCheck(Camera cam) => ApplyInternal(cam, true);

    void Apply(Camera cam) => ApplyInternal(cam, false);

    void ApplyInternal(Camera cam, bool anyCamera)
    {
        if (!cam || !root || (!anyCamera && cam.cameraType != CameraType.Game)) return;
        Vector2 c = cam.transform.position;
        foreach (var p in pieces)
        {
            var d = c - p.anchor;
            p.t.position = p.basis + new Vector3(d.x * (1 - p.motion.x), d.y * (1 - p.motion.y), 0);
        }
    }

    Texture2D Paint(string name)
    {
        byName ??= paintings.Where(t => t).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        return byName.TryGetValue(name, out var t) ? t : null;
    }

    void Quad(string name, Rect rect, Rect clip, int order, bool near, Vector2 motion, Vector2 anchor, bool solid = false)
    {
        var tex = solid ? Texture2D.whiteTexture : Paint(name);
        if (!tex) { missingPaint.Add(name); return; }
        var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(rect.center.x, rect.center.y, 0);
        var mesh = new Mesh { hideFlags = HideFlags.DontSave };
        float w = rect.width / 2, h = rect.height / 2;
        mesh.vertices = new[] { new Vector3(-w, -h), new Vector3(w, -h), new Vector3(w, h), new Vector3(-w, h) };
        float u = solid ? 1 : rect.width * PixelsPerUnit / tex.width, v = solid ? 1 : rect.height * PixelsPerUnit / tex.height;
        mesh.uv = new[] { Vector2.zero, new Vector2(u, 0), new Vector2(u, v), new Vector2(0, v) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        owned.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mat = new Material(shader) { hideFlags = HideFlags.DontSave };
        mat.mainTexture = tex;
        mat.SetVector("_Clip", new Vector4(clip.xMin, clip.yMin, clip.xMax, clip.yMax));
        mat.SetFloat("_Solid", solid ? 1 : 0);
        mat.SetFloat("_Light", order >= -76 && order <= -55 ? 1 : 0);
        bool stain = name.StartsWith("CK_Wall") || name.StartsWith("CK_Column");
        mat.SetFloat("_Tide", stain ? 1 : 0);
        if (stain) mat.SetTexture("_TideTex", Paint("CK_Band_Tide"));
        owned.Add(mat);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = order;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.enabled = !near || Near;
        pieces.Add(new Piece { t = go.transform, basis = go.transform.position, anchor = anchor, motion = motion, r = r, near = near, rect = rect, clip = clip });
    }

    Piece Last => pieces.Count > 0 ? pieces[^1] : null;

    void Put(string n, float x, float y, Rect clip, int order = -65)
    {
        var t = Paint(n);
        if (!t) { missingPaint.Add(n); return; }
        Quad(n, new Rect(x - t.width / 432f, y - t.height / 432f, t.width / 216f, t.height / 216f), clip, order, true, Vector2.one, clip.center);
    }

    void Wall(Rect r, Rect clip, string n = "CK_Wall_Masonry") => Quad(n, r, clip, -76, true, Vector2.one, clip.center);

    void Pier(float x, float top, Rect room, string name)
    {
        int n = 0; float end = top;
        while (end > room.yMin)
        {
            bool first = n == 0; float visible = first ? 12 : 10.6f; float bottom = Mathf.Max(room.yMin, end - visible);
            float centre = first ? end - 6 : end - 4.6f;
            var clip = Rect.MinMaxRect(room.xMin, bottom, room.xMax, end);
            Put(n % 2 == 0 ? name : (name.EndsWith("01") ? "CK_Column_02" : "CK_Column_01"), x, centre, clip);
            end = bottom; n++;
        }
    }

    void Tunnel(Rect r)
    {
        Wall(r, r);
        int i = 0;
        foreach (float offset in new[] { .5f, 3.6f, 6.3f, 10.1f, 13.3f }) if (offset < r.width) Put("CK_Rib_Tunnel_" + (++i % 3 + 1).ToString("00"), r.x + offset, 108, r);
        Put("CK_Niche_Lantern_02", r.center.x + 1.4f, 108.7f, r);
        Quad("CK_Pipe_H", new Rect(r.x, r.yMax - .35f, r.width, 1), r, -64, true, Vector2.one, r.center);
    }

    public void Build()
    {
        Clear(); missingPaint.Clear();
        if (!shader) { Debug.LogWarning("Keepers' Cistern backdrop: no shader assigned; run Qolossal > Backgrounds > Route A Keepers' Cistern > Install."); return; }
        root = new GameObject("Keepers' Cistern (built at runtime)") { hideFlags = HideFlags.DontSave };
        root.transform.SetParent(transform, false);

        foreach (var r in new[] { A03, A04, A05, T1, T2 }) Quad("Backing", r, r, -85, false, Vector2.one, r.center, true);
        for (int row = 0; row < 2; row++) for (int col = 0; col < 3; col++)
                Quad($"FAR_A04_Colonnade_r{row}c{col}", new Rect(1584 - 21.5f + col * 43f / 3, 102 - 14 + row * 14, 43f / 3, 14), A04, -80, false, new Vector2(.5f, .6f), A04.center);
        Wall(A03, A03);
        foreach (var c in new[] { new Vector2(1496, 112), new Vector2(1512, 111.5f), new Vector2(1528, 112) }) Put("CK_Vault_Span", c.x, c.y, A03);
        foreach (float x in new[] { 1490f, 1504f, 1536f }) Pier(x, 116, A03, "CK_Column_01");
        Put("CK_Window_Arch", 1506, 104.2f, A03); Put("CK_Niche_Lantern_01", 1510.8f, 107.8f, A03);
        Put("CK_Roots_Curtain_03", 1517.8f, 113.3f, A03); Put("CK_Roots_Curtain_02", 1524.5f, 112, A03); Put("CK_Roots_Curtain_01", 1504, 113, A03);
        Quad("CK_Pipe_H", new Rect(1490, 110.6f, 13, 1), A03, -64, true, Vector2.one, A03.center); Put("CK_Pipe_Elbow", 1503, 110.6f, A03); Put("CK_Pipe_V", 1503, 105.6f, A03);
        Put("CK_Mound_02", 1507, 102, A03, -54); Quad("CK_Band_Silt", new Rect(1490, 101, 48, 3), A03, -57, true, Vector2.one, A03.center);

        Wall(new Rect(1552, 84, 4, 36), A04); Wall(new Rect(1612, 84, 4, 36), A04); Wall(new Rect(1552, 116, 64, 4), A04);
        foreach (var b in new[] { new Vector2(1564, 94), new Vector2(1583, 113), new Vector2(1599, 115.5f), new Vector2(1615, 112) })
        {
            if (b.y + 2 < 120) Wall(new Rect(b.x - 8, b.y + 2, 16, 120 - b.y - 2), A04);
            Put("CK_Vault_Span", b.x, b.y, A04);
        }
        foreach (float x in new[] { 1556f, 1575.8f, 1607f, 1612f }) Pier(x, 120, A04, "CK_Column_02");
        NicheSupport(1572, 83, 96, A04); NicheSupport(1590.4f, 83, 116, A04);
        Put("CK_Niche_Lantern_02", 1572, 90.8f, A04); Put("CK_Niche_Lantern_03", 1590.4f, 100.8f, A04); Put("CK_Niche_Lantern_01", 1613.3f, 108.8f, A04);
        Put("CK_Roots_Curtain_01", 1575.6f, 112.5f, A04); Put("CK_Roots_Curtain_02", 1590f, 115, A04); Put("CK_Roots_Curtain_03", 1606.5f, 116, A04);
        Quad("MID_A04_GaugeTower", new Rect(1584.5f, 89, 5, 13), Rect.MinMaxRect(1552, 91.1f, 1616, 120), -74, false, new Vector2(.88f, .94f), A04.center);
        Quad("MID_A04_GaugeTower", new Rect(1584.5f, 83, 5, 13), Rect.MinMaxRect(1552, 84, 1616, 87), -75, false, new Vector2(.88f, .94f), A04.center);
        for (float y = 87; y < 91.1f; y += 2.5f)
            Quad("MID_A04_GaugeTower", new Rect(1584.5f, y - 8, 5, 13), Rect.MinMaxRect(1552, y, 1616, Mathf.Min(91.1f, y + 2.5f)), -75, false, new Vector2(.88f, .94f), A04.center);
        Put("CK_Mound_01", 1587, 84, A04, -54); Quad("CK_Band_Silt", new Rect(1552, 82, 64, 3), A04, -53, true, Vector2.one, A04.center);
        Tunnel(T1); Tunnel(T2);
        Put("CK_Mouth_Arch_01", 1553.2f, 108.3f, A04, -54); Put("CK_Mouth_Arch_02", 1614.8f, 108.3f, A04, -54);

        Wall(A05, A05); Recess(new Rect(1633.65f, 92, 4.7f, 14), A05); Last?.r.sharedMaterial.SetFloat("_RecessArch", 1);
        Put("CK_Recess_Arch", 1636, 104.5f, A05, -64); if (Last?.t.name == "CK_Recess_Arch") Mat(Last).SetFloat("_Soffit", 1);
        Put("CK_Vault_Span", 1636, 106, A05); Put("CK_Vault_Span", 1636, 116, A05);
        Pier(1628.2f, 142, A05, "CK_Column_01"); Pier(1643.5f, 142, A05, "CK_Column_02");
        Pier(1633.65f, 107.8f, A05, "CK_Column_01"); Pier(1638.35f, 107.8f, A05, "CK_Column_02");
        foreach (float x in new[] { 1633.65f, 1638.35f }) { Quad("CK_Basin_Coping", new Rect(x - .9f, 105.75f, 1.8f, .5f), A05, -63, true, Vector2.one, A05.center); if (Last != null) Last.t.name = "Capital_Coping"; }
        Quad("MID_A05_WeirStair", new Rect(1627.5f, 91, 6, 10), A05, -59, false, new Vector2(.88f, .94f), new Vector2(1636, 104));
        Quad("CK_Basin_Coping", new Rect(1627.5f, 100.75f, 1.55f, .5f), A05, -58, false, new Vector2(.88f, .94f), new Vector2(1636, 104)); if (Last != null) Last.t.name = "MID_A05_WeirCoping";
        Put("CK_Niche_Lantern_03", 1640.6f, 103.8f, A05); Put("CK_Roots_Curtain_03", 1639, 113, A05);
        Put("CK_Pipe_V", 1642.5f, 106, A05); Put("CK_Pipe_Valve", 1642.5f, 101, A05); Put("CK_Mound_02", 1631, 92, A05, -53); Put("CK_Water_Sheet_V", 1630, 106, A05, -62);
        foreach (var p in new[] { new Vector2(1501, 116), new Vector2(1560, 100), new Vector2(1575.8f, 115.5f), new Vector2(1590.4f, 116), new Vector2(1607, 118), new Vector2(1614, 117), new Vector2(1643, 113) })
        {
            var r = p.x < 1538 ? A03 : p.x < 1616 ? A04 : A05;
            Quad("FG_Roots_01", new Rect(p.x - 2, p.y - 3, 4, 6), r, -25, true, new Vector2(1.1f, 1.05f), r.center);
        }
        Quad("FG_Chain_Hook", new Rect(1580, 110.2f, 2, 8), A04, -24, true, new Vector2(1.1f, 1.05f), A04.center);
        BuildRestOfRoute();
        AddDepthPass();
        AddSharedLighting();
        Apply(Camera.main);
        if (missingPaint.Count > 0) Debug.LogWarning("Keepers' Cistern backdrop: missing paintings: " + string.Join(", ", MissingPaintings));
    }

    public int PieceCount => pieces.Count;
}

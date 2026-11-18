using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public sealed partial class KeepersCisternBackdrop
{
    static readonly Vector2[] DepthLights = { new(1511.05f, 107), new(1546.65f, 108), new(1572.25f, 90), new(1590.65f, 100), new(1613.55f, 108), new(1623.65f, 108), new(1640.85f, 103), new(1504, 112), new(1517.8f, 113.3f), new(1575.6f, 112.5f), new(1590, 115), new(1606.5f, 116), new(1484.25f, 173.2f), new(1512.65f, 180.2f), new(1541.95f, 174.2f), new(1541.95f, 150.2f), new(1541.95f, 134.2f), new(1640.95f, 123.2f), new(1556.75f, 162.2f), new(1573.45f, 175.2f), new(1611.75f, 177.2f), new(1630.25f, 177.2f), new(1667.25f, 176.2f), new(1703.15f, 172.8f), new(1716.8f, 173.5f), new(1703.6f, 182.8f), new(1716.6f, 183.8f), new(1640.66f, 137.7f), new(1642.25f, 134.77f), new(1567.87f, 162.94f), new(1568f, 160f), new(1599.44f, 161.58f), new(1599.5f, 157.35f), new(1605.03f, 156.12f) };
    static Vector2 ClosestLight(Vector2 p) => DepthLights.OrderBy(l => (l - p).sqrMagnitude).First();
    static Material Mat(Piece p) => p.r.sharedMaterial;

    Piece Effect(Piece source, string label, Rect rect, int order, float mode)
    {
        var go = new GameObject(label) { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(rect.center.x, rect.center.y, 0);
        var mesh = new Mesh { hideFlags = HideFlags.DontSave };
        float w = rect.width / 2, h = rect.height / 2;
        mesh.vertices = new[] { new Vector3(-w, -h), new Vector3(w, -h), new Vector3(w, h), new Vector3(-w, h) };
        float px = (rect.width - source.rect.width) / 2 / source.rect.width, py = (rect.height - source.rect.height) / 2 / source.rect.height;
        mesh.uv = new[] { new Vector2(-px, -py), new Vector2(1 + px, -py), new Vector2(1 + px, 1 + py), new Vector2(-px, 1 + py) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();
        owned.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var m = new Material(Mat(source)) { hideFlags = HideFlags.DontSave };
        m.SetFloat("_Mode", mode); m.SetFloat("_Light", 0); m.SetFloat("_Tide", 0); m.SetFloat("_Water", 0); m.SetFloat("_Solid", 0);
        m.SetVector("_NativeSize", new Vector4(source.rect.width, source.rect.height, 0, 0));
        owned.Add(m);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = m; renderer.sortingOrder = order; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.enabled = !source.near || Near;
        var p = new Piece { t = go.transform, basis = go.transform.position, anchor = source.anchor, motion = source.motion, r = renderer, near = source.near, rect = rect, clip = source.clip };
        pieces.Add(p);
        return p;
    }

    void NicheSupport(float x, float bottom, float top, Rect room)
    {
        Quad("CK_Wall_Masonry", new Rect(x - 1.7f, bottom, 3.4f, top - bottom), room, -66, true, Vector2.one, room.center);
        var front = Last; if (front == null) return;
        front.t.name = "Support_Limestone"; Mat(front).SetFloat("_Support", 1); Mat(front).SetVector("_NativeSize", new Vector4(3.4f, top - bottom, 0, 0));
    }

    void Recess(Rect area, Rect room)
    {
        Wall(area, room);
        var p = Last; if (p == null) return;
        p.t.name = "Recess_Limestone"; Mat(p).SetFloat("_Recess", 1); p.r.sortingOrder = -75;
    }

    void AddDepthPass()
    {
        Quad("CK_Basin_Submerged", new Rect(1552, 83, 64, 2), A04, -68, true, Vector2.one, A04.center);
        Quad("CK_Basin_Coping", new Rect(1552, 85, 64, .5f), A04, -67, true, Vector2.one, A04.center);
        if (Last?.t.name == "CK_Basin_Coping") Mat(Last).SetFloat("_BasinWater", 1);
        // Local soft gradient between the far and the near; no full-frame tint.
        Quad("Backing", new Rect(1554, 84, 60, 35), A04, -78, true, Vector2.one, A04.center, true);
        var mist = Last; mist.t.name = "DepthMist"; Mat(mist).SetFloat("_Mode", 3); Mat(mist).SetFloat("_Solid", 0);
        Quad("Backing", new Rect(1554, 154, 60, 32), A06, -78, true, Vector2.one, A06.center, true);
        var engineMist = Last; engineMist.t.name = "DepthMist_A06"; Mat(engineMist).SetFloat("_Mode", 3); Mat(engineMist).SetFloat("_Solid", 0);
        Mat(engineMist).SetVector("_MistCentre", new Vector4(1584, 165, 24, 0)); Mat(engineMist).SetVector("_MistUpper", new Vector4(1584, 171, 14, 0));
        foreach (var p in pieces.ToArray())
        {
            string n = p.t.name;
            if (n.StartsWith("MID")) Mat(p).SetFloat("_MidSoft", 1);
            bool pier = n.StartsWith("CK_Column");
            bool support = n.StartsWith("Support_");
            bool stone = pier || support || n.StartsWith("CK_Mouth") || n.StartsWith("CK_Recess") || n.StartsWith("CK_Niche") || n.StartsWith("CK_Window") || n.StartsWith("CK_Vault");
            if (!stone) continue;
            Vector2 centre = p.rect.center;
            var light = ClosestLight(centre); var away = centre - light;
            bool ambient = away.magnitude > 10;
            if (n.StartsWith("CK_Niche")) p.r.sortingOrder = -61;
            if (pier) { Mat(p).SetFloat("_Mirror", light.x > centre.x ? 1 : 0); Mat(p).SetFloat("_PierReturn", 2); if (centre.x > 1552 && centre.x < 1616) Mat(p).SetFloat("_Water", 1); }
            if (support) Mat(p).SetFloat("_Water", 1);
            if (pier && p.clip.yMin >= 154 && p.clip.xMin >= 1554 && p.clip.xMax <= 1614) { Mat(p).SetFloat("_Water", 1); Mat(p).SetFloat("_WaterY", 154.6f); Mat(p).SetFloat("_PierReturn", 2); }
            if (pier && p.clip.xMin >= 1528 && p.clip.xMax <= 1544) { Mat(p).SetFloat("_PierReturn", 2); Mat(p).SetFloat("_ShaftBlend", 1); Mat(p).SetVector("_ShaftBounds", new Vector4(p.clip.yMin, p.clip.yMax, 0, 0)); }
            // Shadow is a blurred sample of this piece's own alpha, never its rectangular canvas.
            Vector2 dir = ambient ? Vector2.zero : new Vector2(Mathf.Sign(away.x), -.25f).normalized;
            Rect r = p.rect; r.position += dir * .12f; r.xMin -= .6f; r.xMax += .6f; r.yMin -= .6f; r.yMax += .6f;
            var shadow = Effect(p, "Shadow_" + n, r, p.r.sortingOrder - 1, 1); Mat(shadow).SetFloat("_Strength", .30f); Mat(shadow).SetFloat("_Blur", .42f);
            if ((pier || support) && p.clip.yMin <= 85 && p.clip.yMax > 85 && centre.x > 1552 && centre.x < 1616)
            {
                r = p.rect; r.center = new Vector2(r.center.x, 170 - r.center.y);
                var refl = Effect(p, "Reflection_" + n, r, -56, 4); Mat(refl).SetVector("_Clip", new Vector4(1552, 84, 1616, 85)); Mat(refl).SetFloat("_Blur", .10f); Mat(refl).SetFloat("_Strength", .32f);
            }
        }
    }

    void AddTerrainContact()
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tris = new List<int>();
        Rect bounds = new(1479, 67, 276, 132);
        int ground = LayerMask.NameToLayer("Ground"); int groundMask = LayerMask.GetMask("Ground");
        var domains = MainRooms.Concat(RoutePassages.Select(p => p.rect)).ToArray();
        foreach (var c in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!c.enabled || c.isTrigger || c.gameObject.layer != ground || !c.gameObject.activeInHierarchy) continue;
            if (c.attachedRigidbody && c.attachedRigidbody.bodyType != RigidbodyType2D.Static) continue;
            if (!bounds.Overlaps(new Rect(c.bounds.min.x, c.bounds.min.y, c.bounds.size.x, c.bounds.size.y))) continue;
            var paths = new List<Vector2[]>();
            if (c is PolygonCollider2D poly) for (int i = 0; i < poly.pathCount; i++) paths.Add(poly.GetPath(i));
            else if (c is BoxCollider2D box) { var h = box.size * .5f; paths.Add(new[] { new Vector2(-h.x, -h.y), new Vector2(h.x, -h.y), new Vector2(h.x, h.y), new Vector2(-h.x, h.y) }); }
            else if (c is EdgeCollider2D edge) paths.Add(edge.points);
            else if (c is CompositeCollider2D composite) for (int i = 0; i < composite.pathCount; i++) { var points = new Vector2[composite.GetPathPointCount(i)]; composite.GetPath(i, points); paths.Add(points); }
            foreach (var path in paths)
            {
                var world = path.Select(p => (Vector2)c.transform.TransformPoint(p + c.offset)).ToArray();
                float area = 0; for (int i = 0; i < world.Length; i++) { var a = world[i]; var b = world[(i + 1) % world.Length]; area += a.x * b.y - b.x * a.y; }
                int count = c is EdgeCollider2D ? world.Length - 1 : world.Length;
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = world[i], b = world[(i + 1) % world.Length], t = b - a; if (t.sqrMagnitude < .00001f) continue;
                    var domain = domains.FirstOrDefault(r => r.Contains((a + b) * .5f)); if (domain.width == 0) continue;
                    Vector2 n = new Vector2(t.y, -t.x).normalized * (area >= 0 ? 1 : -1); Vector2 mid = (a + b) * .5f;
                    if (c.OverlapPoint(mid + n * .03f)) n = -n;
                    if (c is EdgeCollider2D) { bool pos = domains.Any(r => r.Contains(mid + n * .1f)), neg = domains.Any(r => r.Contains(mid - n * .1f)); if (neg && !pos) n = -n; }
                    var test = mid + n * .05f;
                    if (Physics2D.OverlapPointAll(test, groundMask).Any(o => o != c && !o.isTrigger)) continue;
                    Vector2 Bound(Vector2 point) => new(Mathf.Clamp(point.x, domain.xMin, domain.xMax), Mathf.Clamp(point.y, domain.yMin, domain.yMax));
                    int k = v.Count; v.Add(Bound(a)); v.Add(Bound(b)); v.Add(Bound(b + n * .6f)); v.Add(Bound(a + n * .6f));
                    uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
                    tris.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                }
            }
        }
        var go = new GameObject("TerrainContact_actualColliderContours") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(root.transform, false);
        var mesh = new Mesh { hideFlags = HideFlags.DontSave, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds(); owned.Add(mesh);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mat = new Material(shader) { hideFlags = HideFlags.DontSave };
        mat.SetFloat("_Mode", 2); mat.SetFloat("_Strength", .45f); mat.SetVector("_Clip", new Vector4(1480, 68, 1754, 196)); owned.Add(mat);
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.sortingOrder = -21; r.shadowCastingMode = ShadowCastingMode.Off; r.enabled = Near;
        pieces.Add(new Piece { t = go.transform, basis = Vector3.zero, anchor = Vector2.zero, motion = Vector2.one, r = r, near = true });
    }

    void AddSharedLighting()
    {
        foreach (var p in pieces)
        {
            var m = Mat(p);
            if (m.GetFloat("_Beam") > .5f || m.GetFloat("_Mode") > .5f || p.t.name.StartsWith("FAR") || p.t.name == "Backing") continue;
            Vector2 c = p.clip.center; Rect r = default; string name = null;
            // Select by room/connection domain, not x alone: upper and lower rooms overlap in x.
            for (int i = 0; i < MainRooms.Length; i++) if (MainRooms[i].Contains(c)) { r = MainRooms[i]; name = RoomIds[i]; break; }
            if (name == null) foreach (var passage in RoutePassages) if (passage.rect.Contains(c)) { r = passage.rect; name = passage.name; break; }
            if (name == null) continue;
            var map = Paint("LM_" + name); if (!map) { missingPaint.Add("LM_" + name); continue; }
            m.SetTexture("_RoomMap", map); m.SetVector("_RoomRect", new Vector4(r.x, r.y, r.width, r.height)); m.SetFloat("_RoomLit", 1); m.SetFloat("_Light", 0); m.SetFloat("_RoomDay", name == "A01" ? 1 : 0);
            if (p.t.name.StartsWith("CK_Niche") || p.t.name.StartsWith("SET_")) m.SetFloat("_Emissive", 1);
        }
    }
}

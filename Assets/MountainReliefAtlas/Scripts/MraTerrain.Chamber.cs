using System.Collections.Generic;
using UnityEngine;

// Opt-in for reviewed flat chamber pieces. Rendering changes only; collider paths stay authored.
public sealed partial class MraTerrain
{
    public MraTerrain chamberBackdrop;
    [Tooltip("Explicit chamber Top ordering, assigned by regional integration; higher walklines cover buried lower lips.")]
    public int chamberTopPriority;
    [HideInInspector] public string[] chamberPartLabels = new string[0];
    [HideInInspector] public int[] chamberPartOrders = new int[0];
    int ChamberPartOrder(string label, int fallback)
    {
        if (chamberBackdrop == null || chamberPartLabels == null || chamberPartOrders == null || chamberPartLabels.Length != chamberPartOrders.Length) return fallback;
        int index = System.Array.IndexOf(chamberPartLabels, label);
        return index < 0 ? fallback : chamberPartOrders[index];
    }
    public bool chamberSolidColumn;
    public bool chamberBlendLeft = true, chamberBlendRight = true;
    bool CanBlendCave => chamberBackdrop != null && chamberBackdrop != this &&
        fill != null && chamberBackdrop.fill == fill && surface != null && surface.Length == 2 &&
        surface[1].x - surface[0].x > .001f && Mathf.Abs(surface[1].y - surface[0].y) < .0001f;

    Color ChamberColor(float x, float y)
    {
        float left = surface[0].x, right = surface[1].x;
        float reach = Mathf.Min(chamberSolidColumn ? .12f : .8f, (right - left) * .45f);
        float distance = Mathf.Min(chamberBlendLeft ? x - left : 1000f, chamberBlendRight ? right - x : 1000f);
        float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(distance / reach));
        Vector3 local = chamberBackdrop.transform.InverseTransformPoint(transform.TransformPoint(new Vector3(x, y, 0)));
        Color foreground = tint * FillShade(x, y); foreground.a = tint.a * (chamberSolidColumn ? Mathf.InverseLerp(bottom, bottom + .5f, y) : 1f);
        if (chamberSolidColumn) return foreground;
        Color background = chamberBackdrop.tint * chamberBackdrop.FillShade(local.x, local.y); background.a = tint.a;
        return Color.Lerp(background, foreground, weight);
    }

    Vector2 ChamberUV(Vector3 local)
    {
        Vector3 world = transform.TransformPoint(local); Vector2 size = WorldSize(fill);
        return new Vector2(world.x / size.x + (chamberSolidColumn ? .27f : 0f), world.y / size.y + (chamberSolidColumn ? .13f : 0f));
    }

    Mesh BlendedCaveFill()
    {
        float left = surface[0].x, right = surface[1].x, high = surface[0].y;
        float reach = Mathf.Min(chamberSolidColumn ? .12f : .8f, (right - left) * .45f);
        float[] xs = { left, left + reach, right - reach, right };
        var ys = new List<float>();
        for (float y = bottom; y < high; y += .5f) ys.Add(y);
        ys.Add(high);
        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        var colors = new List<Color>(); var triangles = new List<int>();
        foreach (float y in ys) foreach (float x in xs)
        {
            var p = new Vector3(x, y, 0); vertices.Add(p); uv.Add(ChamberUV(p)); colors.Add(ChamberColor(x, y));
        }
        for (int row = 0; row < ys.Count - 1; row++) for (int col = 0; col < 3; col++)
        {
            int q = row * 4 + col; triangles.AddRange(new[] { q, q + 4, q + 5, q, q + 5, q + 1 });
        }
        Mesh mesh = Build("chamber fill", vertices, uv, triangles, tint); mesh.SetColors(colors); return mesh;
    }

    Mesh BlendCaveRisers(Mesh mesh)
    {
        if (!CanBlendCave || face != fill) return mesh;
        var vertices = mesh.vertices; var uv = mesh.uv; var colors = mesh.colors;
        for (int i = 0; i < vertices.Length; i++)
        {
            uv[i] = ChamberUV(vertices[i]); Color color = ChamberColor(vertices[i].x, vertices[i].y);
            float rim = colors[i].r < tint.r * .7f ? (chamberSolidColumn ? .55f : .9f) : 1f;
            colors[i] = new Color(color.r * rim, color.g * rim, color.b * rim, colors[i].a * (chamberSolidColumn ? Mathf.InverseLerp(bottom, bottom + .5f, vertices[i].y) : 1f));
        }
        mesh.uv = uv; mesh.colors = colors; return mesh;
    }

    Mesh BlendCaveTop(Mesh mesh)
    {
        if (!CanBlendCave) return mesh;
        var vertices = mesh.vertices; var uv = mesh.uv; var colors = mesh.colors; var triangles = mesh.triangles;
        float left = surface[0].x, right = surface[1].x, fade = Mathf.Min(.22f, (right - left) * .25f);
        var outV = new List<Vector3>(); var outU = new List<Vector2>(); var outC = new List<Color>(); var outT = new List<int>();
        List<(Vector3 v, Vector2 uv, Color c)> Clip(List<(Vector3 v, Vector2 uv, Color c)> input, float plane, bool greater)
        {
            var output = new List<(Vector3 v, Vector2 uv, Color c)>(); if (input.Count == 0) return output;
            var a = input[input.Count - 1]; bool insideA = greater ? a.v.x >= plane : a.v.x <= plane;
            foreach (var b in input)
            {
                bool insideB = greater ? b.v.x >= plane : b.v.x <= plane;
                if (insideA != insideB)
                {
                    float t = (plane - a.v.x) / (b.v.x - a.v.x);
                    output.Add((Vector3.Lerp(a.v, b.v, t), Vector2.Lerp(a.uv, b.uv, t), Color.Lerp(a.c, b.c, t)));
                }
                if (insideB) output.Add(b); a = b; insideA = insideB;
            }
            return output;
        }
        for (int q = 0; q < triangles.Length; q += 3) for (int zone = 0; zone < 3; zone++)
        {
            var polygon = new List<(Vector3 v, Vector2 uv, Color c)>();
            for (int k = 0; k < 3; k++) { int n = triangles[q + k]; polygon.Add((vertices[n], uv[n], colors[n])); }
            if (zone == 0) polygon = Clip(polygon, left + fade, false);
            if (zone == 1) { polygon = Clip(polygon, left + fade, true); polygon = Clip(polygon, right - fade, false); }
            if (zone == 2) polygon = Clip(polygon, right - fade, true);
            if (polygon.Count < 3) continue; int start = outV.Count;
            foreach (var p in polygon)
            {
                float weight = Mathf.Clamp01(Mathf.Min(p.v.x - left, right - p.v.x) / fade);
                outV.Add(p.v); outU.Add(p.uv); outC.Add(new Color(p.c.r, p.c.g, p.c.b, p.c.a * weight));
            }
            for (int k = 1; k < polygon.Count - 1; k++) outT.AddRange(new[] { start, start + k, start + k + 1 });
        }
        mesh.Clear(); mesh.SetVertices(outV); mesh.SetUVs(0, outU); mesh.SetColors(outC); mesh.SetTriangles(outT, 0); mesh.RecalculateBounds();
        return mesh;
    }
}

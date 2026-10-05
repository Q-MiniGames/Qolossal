using Mra;
using UnityEngine;

// Where a point of the climb lies on the regional relief map. The map is an authored cartographic
// drawing, not the level's geometry: each route edge has its own Chart polyline. A position is
// projected onto the nearest gameplay surface (preferring the current edge and its neighbours, so
// the marker never jumps to a nearby route), its arc-length progress along that surface is taken,
// and the same progress is read off the edge's Chart line. UVs are 0-1 from the map's bottom-left.
public static class MraRoute
{
    public struct Fix { public int edge; public float progress, distance; public Vector2 uv; }

    public static Fix Locate(Region region, Vector2 position, int hint = -1)
    {
        Fix best = new Fix { edge = -1, distance = float.PositiveInfinity };
        for (int i = 0; i < region.edges.Length; i++)
        {
            var e = region.edges[i];
            float progress = Project(e.points, position, out float distance);
            // The current edge and its neighbours win ties within a few units.
            if (hint >= 0 && Mathf.Abs(i - hint) <= 1) distance -= 1.5f;
            if (distance < best.distance) best = new Fix { edge = i, progress = progress, distance = distance };
        }
        if (best.edge >= 0) best.uv = Along(region.edges[best.edge].chartLine, best.progress);
        return best;
    }

    // Arc-length fraction (0-1) of the closest point on a polyline, and its distance.
    public static float Project(Vector2[] line, Vector2 p, out float distance)
    {
        float total = Length(line), walked = 0f, bestAt = 0f;
        distance = float.PositiveInfinity;
        for (int i = 0; i < line.Length - 1; i++)
        {
            Vector2 a = line[i], b = line[i + 1], ab = b - a;
            float len = ab.magnitude;
            float t = len > 1e-5f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / (len * len)) : 0f;
            float d = Vector2.Distance(p, a + ab * t);
            if (d < distance) { distance = d; bestAt = walked + t * len; }
            walked += len;
        }
        return total > 1e-5f ? bestAt / total : 0f;
    }

    // The point at arc-length fraction `progress` along a polyline.
    public static Vector2 Along(Vector2[] line, float progress)
    {
        if (line == null || line.Length == 0) return Vector2.zero;
        if (line.Length == 1) return line[0];
        float target = Mathf.Clamp01(progress) * Length(line), walked = 0f;
        for (int i = 0; i < line.Length - 1; i++)
        {
            float len = Vector2.Distance(line[i], line[i + 1]);
            if (walked + len >= target) return Vector2.Lerp(line[i], line[i + 1], len > 1e-5f ? (target - walked) / len : 0f);
            walked += len;
        }
        return line[line.Length - 1];
    }

    public static float Length(Vector2[] line)
    {
        float total = 0f;
        for (int i = 0; i < line.Length - 1; i++) total += Vector2.Distance(line[i], line[i + 1]);
        return total;
    }
}

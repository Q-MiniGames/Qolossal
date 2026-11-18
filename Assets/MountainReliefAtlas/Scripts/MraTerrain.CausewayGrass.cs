using System.Collections.Generic;
using UnityEngine;

public sealed partial class MraTerrain
{
    // Approved grass-edge trial. Rendering only: generated children share the
    // terrain's lifetime; no saved surfaces, collider paths or cave art change.
    const float GrassWidth = 6f;
    const float GrassBaselinePixel = 545f;
    const float GrassTopPixel = 335f, GrassBottomPixel = 600f;
    static Texture2D causewayGrass;

    void BuildCausewayGrass()
    {
        if (gameObject.scene.name != "MRAtlas_MR02_Causeway" ||
            !SurfaceTerrainContrast.IsUpperTerrain(this)) return;
        if (causewayGrass == null) causewayGrass = Resources.Load<Texture2D>("Terrain/CausewayGrass/GrassEdge");
        if (causewayGrass == null) return;

        float ppu = causewayGrass.width / GrassWidth;
        float above = (GrassBaselinePixel - GrassTopPixel) / ppu;
        float below = (GrassBottomPixel - GrassBaselinePixel) / ppu;
        float vTop = 1f - GrassTopPixel / causewayGrass.height;
        float vBottom = 1f - GrassBottomPixel / causewayGrass.height;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>();
        var colors = new List<Color>(); var triangles = new List<int>();
        var protectedMouths = new List<Vector3>();
        foreach (var owner in Resources.FindObjectsOfTypeAll<MraTerrain>())
        {
            if (owner.gameObject.scene != gameObject.scene || !owner.gameObject.activeInHierarchy || owner.caveOpenings == null) continue;
            foreach (var hole in owner.caveOpenings)
            {
                if (hole.outline == null || hole.outline.Length == 0) continue;
                float lo = float.PositiveInfinity, hi = float.NegativeInfinity, ceiling = float.NegativeInfinity;
                foreach (var p in hole.outline)
                {
                    Vector3 local = transform.InverseTransformPoint(owner.transform.TransformPoint(p));
                    lo = Mathf.Min(lo, local.x); hi = Mathf.Max(hi, local.x); ceiling = Mathf.Max(ceiling, local.y);
                }
                protectedMouths.Add(new Vector3(lo - 2f, hi + 2f, ceiling + .6f));
            }
        }
        bool Walkable(int i) => i >= 0 && i < surface.Length - 1 &&
            surface[i + 1].x - surface[i].x > .0001f &&
            Mathf.Abs(surface[i + 1].y - surface[i].y) / (surface[i + 1].x - surface[i].x) <= maxStripSlope;
        float Protection(Vector2 p)
        {
            float a = 1f;
            foreach (var span in protectedMouths)
            {
                float outside = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Max(span.x - p.x, p.x - span.y) / 4f));
                float high = Mathf.SmoothStep(0, 1, Mathf.Clamp01(p.y - below - span.z));
                a = Mathf.Min(a, Mathf.Max(outside, high));
            }
            return a;
        }
        for (int first = 0; first < surface.Length - 1;)
        {
            if (!Walkable(first)) { first++; continue; }
            int end = first; float length = 0;
            while (Walkable(end)) { length += Vector2.Distance(surface[end], surface[end + 1]); end++; }
            float travelled = 0;
            for (int segment = first; segment < end; segment++)
            {
                Vector2 a = surface[segment], b = surface[segment + 1]; float distance = Vector2.Distance(a, b);
                int pieces = Mathf.Max(1, Mathf.CeilToInt(distance / .12f));
                for (int j = 0; j < pieces; j++)
                {
                    int start = vertices.Count;
                    for (int side = 0; side < 2; side++)
                    {
                        float t = (float)(j + side) / pieces; Vector2 p = Vector2.Lerp(a, b, t);
                        float along = travelled + distance * t;
                        float alpha = Mathf.SmoothStep(0, 1, Mathf.Min(along, length - along) / .16f) * Protection(p);
                        float u = (surface[first].x + along) / GrassWidth;
                        vertices.Add(new Vector3(p.x, p.y - below, 0)); uv.Add(new Vector2(u, vBottom)); colors.Add(new Color(1, 1, 1, alpha));
                        vertices.Add(new Vector3(p.x, p.y + above, 0)); uv.Add(new Vector2(u, vTop)); colors.Add(new Color(1, 1, 1, alpha));
                    }
                    triangles.AddRange(new[]{start, start+1, start+2, start+2, start+1, start+3});
                }
                travelled += distance;
            }
            first = end;
        }
        if (vertices.Count == 0) return;
        var mesh = new Mesh { name = "Causeway grassy walking rim", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
        var obj = new GameObject("Grass walking edge (generated)") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
        obj.transform.SetParent(transform, false); obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = TerrainMaterial.Shared;
        renderer.sortingOrder = StepOrder + 1 + sortingOffset;
        var block = new MaterialPropertyBlock(); block.SetTexture(MainTex, causewayGrass); renderer.SetPropertyBlock(block);
        owned.Add(obj); owned.Add(mesh);
    }
}

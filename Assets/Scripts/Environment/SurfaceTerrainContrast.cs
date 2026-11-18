using System;
using System.Collections.Generic;
using UnityEngine;

// Rendering-only value separation for the seven upper routes. Source pixels, generated
// positions/UV0/alpha, collision and all cave/background renderers keep their authored state.
public static class SurfaceTerrainContrast
{
    const int MaxSpans = 16;
    static Material material;
    static Material causewayEdgeMaterial;
    static readonly int Grade = Shader.PropertyToID("_SurfaceGrade");
    static readonly int Spans = Shader.PropertyToID("_ProtectedSpans");

    public static bool IsUpperTerrain(MraTerrain terrain)
    {
        if (Array.IndexOf(RegionalSceneCatalog.Surface, terrain.gameObject.scene.name) < 0 ||
            terrain.chamberBackdrop != null || terrain.GetComponentInParent<CaveConnectionPoint>() != null)
            return false;
        // Allow the actual route and its outdoor traversal floors, not decorative backings.
        if (terrain.name.StartsWith("Route MR", StringComparison.Ordinal)) return true;
        for (Transform t = terrain.transform.parent; t != null; t = t.parent)
            if (t.name == "Traversal mechanics") return true;
        return false;
    }

    public static void Apply(MraTerrain terrain, MeshRenderer renderer, Mesh mesh, MaterialPropertyBlock block)
    {
        if (!IsUpperTerrain(terrain)) return;
        if (material == null)
        {
            var shader = Resources.Load<Shader>("FX/SurfaceTerrainContrast");
            if (shader == null || !shader.isSupported) return;
            material = new Material(shader) { name = "Upper terrain value separation", hideFlags = HideFlags.DontSave };
        }
        int region = Array.IndexOf(RegionalSceneCatalog.Surface, terrain.gameObject.scene.name);
        // Linear-light multipliers: retain each region's own painted hue and fine detail.
        float body = region == 0 ? .40f : region == 1 ? .46f : region == 2 ? .48f :
            region == 3 ? .43f : region == 4 ? .60f : region == 5 ? .42f : .48f;
        float edge = region == 4 ? 1.10f : .82f;
        // Causeway: the user rejected the repaint and requested only a darker walking rim.
        if (region == 1) { body = 1f; edge = .22f; }
        var spans = new Vector4[MaxSpans]; int count = 0;
        // Include the whole scene's mouths: an outdoor catch floor/overhang can overlap
        // a cave belonging to another terrain piece, so local holes alone are insufficient.
        // OnEnable can rebuild a mesh while its scene is still loading, when
        // Scene.GetRootGameObjects throws. Serialized scene objects already exist here.
        foreach (var owner in Resources.FindObjectsOfTypeAll<MraTerrain>())
        {
            if (owner.gameObject.scene != terrain.gameObject.scene || !owner.gameObject.activeInHierarchy) continue;
            if (owner.caveOpenings == null) continue;
            foreach (var hole in owner.caveOpenings)
            {
                if (hole.outline == null || hole.outline.Length == 0) continue;
                // Fail closed if future authoring exceeds this bounded shader mask.
                if (count == MaxSpans) return;
                float lo = float.PositiveInfinity, hi = float.NegativeInfinity, ceiling = float.NegativeInfinity;
                foreach (var p in hole.outline)
                {
                    Vector3 local = terrain.transform.InverseTransformPoint(owner.transform.TransformPoint(p));
                    float x = local.x;
                    lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x);
                    ceiling = Mathf.Max(ceiling, local.y);
                }
                // Preserve the full mouth and adjoining rim; feather only the outside terrain.
                spans[count++] = new Vector4(lo - 2f, hi + 2f, 4f, region == 1 ? ceiling + .6f : 0);
            }
        }
        var vertices = mesh.vertices;
        var depth = new List<Vector2>(vertices.Length);
        foreach (var v in vertices)
        {
            float y = WalkHeight(terrain.surface, v.x);
            // Unclamped depth interpolates correctly down large fill triangles.
            depth.Add(new Vector2(y - v.y, v.x));
        }
        mesh.SetUVs(2, depth);
        block.SetVector(Grade, new Vector4(body, edge, region == 1 ? .26f : .9f, count));
        block.SetVectorArray(Spans, spans);
        renderer.sharedMaterial = material;
        if (region == 1)
        {
            if (causewayEdgeMaterial == null)
            {
                var shader = Resources.Load<Shader>("FX/CausewayWalkingEdge");
                if (shader != null && shader.isSupported)
                    causewayEdgeMaterial = new Material(shader) { name = "Causeway walking edge only", hideFlags = HideFlags.DontSave };
            }
            if (causewayEdgeMaterial != null) renderer.sharedMaterial = causewayEdgeMaterial;
        }
        renderer.SetPropertyBlock(block);
    }

    static float WalkHeight(Vector2[] surface, float x)
    {
        float y = float.NegativeInfinity;
        for (int i = 0; i < surface.Length - 1; i++)
        {
            var a = surface[i]; var b = surface[i + 1];
            if (b.x - a.x < .0001f || x < a.x - .0001f || x > b.x + .0001f) continue;
            y = Mathf.Max(y, Mathf.Lerp(a.y, b.y, (x - a.x) / (b.x - a.x)));
        }
        if (!float.IsNegativeInfinity(y)) return y;
        return x < surface[0].x ? surface[0].y : surface[surface.Length - 1].y;
    }
}

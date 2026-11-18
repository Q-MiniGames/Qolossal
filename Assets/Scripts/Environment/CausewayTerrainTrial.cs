using UnityEngine;

// User-requested playable trial of the darker painted Causeway concept.
// Only generated upper-terrain renderers are eligible; scene and source art stay intact.
public static class CausewayTerrainTrial
{
    public const float NativePixelsPerUnit = 256f;
    static Texture2D painting;
    static Material material;

    public static void Apply(MraTerrain terrain, MeshRenderer renderer, MaterialPropertyBlock block)
    {
        if (terrain.gameObject.scene.name != "MRAtlas_MR02_Causeway" ||
            !SurfaceTerrainContrast.IsUpperTerrain(terrain)) return;
        if (painting == null) painting = Resources.Load<Texture2D>("Terrain/CausewayRepaintTrial/DarkBedrock");
        if (painting == null) return;
        if (material == null)
        {
            var shader = Resources.Load<Shader>("FX/CausewayTerrainTrial");
            if (shader == null || !shader.isSupported) return;
            material = new Material(shader) { name = "Causeway new painted bedrock trial", hideFlags = HideFlags.DontSave };
        }
        block.SetTexture("_RepaintTex", painting);
        // Overlap the outer fifth of adjacent painted tiles, without mirrored rock shapes.
        block.SetVector("_RepaintTile", new Vector4(painting.width * .8f / NativePixelsPerUnit,
            painting.height * .8f / NativePixelsPerUnit, .38f, 0));
        renderer.sharedMaterial = material;
        renderer.SetPropertyBlock(block);
    }
}

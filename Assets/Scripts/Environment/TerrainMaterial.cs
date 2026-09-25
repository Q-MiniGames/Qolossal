using UnityEngine;

// The shared lit sprite material used by generated terrain and background meshes.
// Each renderer supplies its own texture through a MaterialPropertyBlock.
public static class TerrainMaterial
{
    private static Material shared;

    public static Material Shared
    {
        get
        {
            if (shared != null) return shared;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            shared = new Material(shader) { name = "Generated Sprite Mesh (Runtime)", hideFlags = HideFlags.DontSave };
            return shared;
        }
    }
}

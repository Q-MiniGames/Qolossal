using UnityEngine;

// One native-density distant painting in a continuous cavern. Narrative paintings never
// repeat: only the low-contrast mineral field outside the landmark uses a texture repeat.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldRefreshCaveVista : MonoBehaviour
{
    public Texture2D vista;
    public Texture2D extension;
    public Texture2D leftDepth, rightDepth, upperVault;
    public Color extensionTint = Color.white;
    [Min(216f)] public float pixelsPerUnit = 216f;
    [Range(0f, 1f)] public float strength = .78f;
    public float centerBelowCamera = .75f;
    Material material, previous;
    SpriteRenderer art;
    UndergroundRoom room;

    void OnEnable() => Refresh();
    void OnValidate() => Refresh();
    public void Refresh()
    {
        art = GetComponent<SpriteRenderer>();
        room = GetComponentInParent<UndergroundRoom>();
        if (art == null || art.sprite == null || vista == null) return;
        var shader = Shader.Find("Qolossal/WorldRefreshCaveVista");
        if (shader == null) return;
        if (material == null)
        {
            previous = art.sharedMaterial;
            material = new Material(shader) { name = "Cave vista " + vista.name, hideFlags = HideFlags.HideAndDontSave };
        }
        material.SetTexture("_Vista", vista);
        material.SetTexture("_Extension", extension != null ? extension : art.sprite.texture);
        var field = extension != null ? extension : art.sprite.texture;
        material.SetVector("_ExtensionTile", new Vector4(field.width / Mathf.Max(216f,pixelsPerUnit), field.height / Mathf.Max(216f,pixelsPerUnit), 0, 0));
        material.SetColor("_ExtensionTint", extensionTint);
        SetPiece("_Left",leftDepth); SetPiece("_Right",rightDepth); SetPiece("_Vault",upperVault);
        material.SetFloat("_HasDepth",leftDepth != null && rightDepth != null && upperVault != null ? 1 : 0);
        material.SetVector("_BaseTile", art.sprite.rect.size / Mathf.Max(216f, pixelsPerUnit));
        material.SetVector("_VistaTile", new Vector4(vista.width / Mathf.Max(216f, pixelsPerUnit), vista.height / Mathf.Max(216f, pixelsPerUnit), 0, 0));
        material.SetFloat("_Strength", strength);
        material.SetFloat("_CenterBelow", centerBelowCamera);
        material.SetColor("_EdgeColor", EdgeColor(vista.name));
        var anchor = room != null ? room.WorldBounds.center : art.bounds.center;
        material.SetVector("_RoomAnchor", new Vector4(anchor.x, anchor.y, 0, 0));
        art.sharedMaterial = material;
        CaveDepth.Apply(Camera.main);
    }
    void OnWillRenderObject() => CaveDepth.Apply(Camera.current);

    void SetPiece(string name,Texture2D texture)
    {
        material.SetTexture(name,texture != null ? texture : vista);
        var source=texture != null ? texture : vista;
        material.SetVector(name+"Tile",new Vector4(source.width / Mathf.Max(216,pixelsPerUnit),source.height / Mathf.Max(216,pixelsPerUnit),0,0));
    }

    // Mean outer12% of unchanged native paintings: the extended cavern shares the painted
    // rock palette instead of fading into black rectangular cell borders.
    public static Color EdgeColor(string name)
    {
        if (name.StartsWith("MR01_")) return new Color(.3219f,.3067f,.1974f);
        if (name.StartsWith("MR02_")) return new Color(.2871f,.2780f,.2098f);
        if (name.StartsWith("MR03_")) return new Color(.3703f,.2592f,.1514f);
        if (name.StartsWith("MR05_")) return new Color(.2460f,.2647f,.1723f);
        if (name.StartsWith("MR06_")) return new Color(.4072f,.3838f,.5032f);
        if (name.StartsWith("MR07_")) return new Color(.4393f,.3602f,.3559f);
        if (name.StartsWith("A_")) return new Color(.1556f,.2437f,.2022f);
        if (name.StartsWith("C_")) return new Color(.2077f,.2585f,.1849f);
        if (name.StartsWith("D_")) return new Color(.2715f,.3337f,.3761f);
        if (name.StartsWith("E_")) return new Color(.3808f,.2757f,.2920f);
        if (name.StartsWith("H_")) return new Color(.3472f,.2349f,.2258f);
        return new Color(.2551f,.2446f,.1770f);
    }
    void OnDisable()
    {
        if (art != null && art.sharedMaterial == material) art.sharedMaterial = previous;
        if (material != null)
        {
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
        }
        material = null;
    }
}

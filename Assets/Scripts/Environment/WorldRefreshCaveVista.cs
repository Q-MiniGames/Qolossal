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

    public Bounds RoomBounds => GetComponentInParent<UndergroundRoom>().WorldBounds;

    // Also used by the connecting passage: identical textures, registration and
    // grading on both sides of a room edge, without altering the source paintings.
    public void ApplyTo(Material target, string prefix = "_")
    {
        float density = Mathf.Max(216f, pixelsPerUnit);
        SetTexture(target,prefix,"Vista",vista,density);
        SetTexture(target,prefix,"Left",leftDepth != null ? leftDepth : vista,density);
        SetTexture(target,prefix,"Right",rightDepth != null ? rightDepth : vista,density);
        SetTexture(target,prefix,"Vault",upperVault != null ? upperVault : vista,density);
        SetTexture(target,prefix,"Extension",extension != null ? extension : GetComponent<SpriteRenderer>().sprite.texture,density);
        target.SetColor(prefix+"ExtensionTint",extensionTint);
        target.SetColor(prefix+"EdgeColor",EdgeColor(vista.name));
        target.SetFloat(prefix+"HasDepth",leftDepth != null && rightDepth != null && upperVault != null ? 1 : 0);
        target.SetFloat(prefix+"CenterBelow",centerBelowCamera);
        var bounds=RoomBounds;
        // Register to the authored playable arrival height, not the midpoint of
        // a tall shaft. Some rooms enter on a high walkway above their floor.
        var checkpoint=GetComponentInParent<UndergroundRoom>().GetComponentInChildren<Checkpoint>();
        float anchorY=checkpoint!=null?checkpoint.SpawnPosition.y+2.84f:bounds.min.y+3.5f;
        target.SetVector(prefix+"RoomAnchor",new Vector4(bounds.center.x,anchorY,0,0));
        target.SetVector(prefix+"RoomRect",new Vector4(bounds.min.x,bounds.min.y,bounds.max.x,bounds.max.y));
    }

    static void SetTexture(Material target,string prefix,string name,Texture2D texture,float density)
    {
        target.SetTexture(prefix+name,texture);
        target.SetVector(prefix+name+"Tile",new Vector4(texture.width/density,texture.height/density,0,0));
    }

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
        ApplyTo(material);
        art.sharedMaterial = material;
        CaveDepth.Apply(Camera.main);
    }
    void OnWillRenderObject() => CaveDepth.Apply(Camera.current);

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

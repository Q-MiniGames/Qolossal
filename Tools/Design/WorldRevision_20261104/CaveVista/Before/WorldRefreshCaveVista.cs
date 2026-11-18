using UnityEngine;

// Native-density scenic detail inside the existing room backdrop. The camera-height band
// avoids vertically repeating rivers/architecture; the original rock field fills the rest.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldRefreshCaveVista : MonoBehaviour
{
    public Texture2D vista;
    [Min(216f)] public float pixelsPerUnit = 216f;
    [Range(0f, 1f)] public float strength = .78f;
    public float centerBelowCamera = .75f;
    Material material, previous;
    SpriteRenderer art;

    void OnEnable() => Refresh();
    void OnValidate() => Refresh();
    public void Refresh()
    {
        art = GetComponent<SpriteRenderer>();
        if (art == null || art.sprite == null || vista == null) return;
        var shader = Shader.Find("Qolossal/WorldRefreshCaveVista");
        if (shader == null) return;
        if (material == null)
        {
            previous = art.sharedMaterial;
            material = new Material(shader) { name = "Cave vista " + vista.name, hideFlags = HideFlags.HideAndDontSave };
        }
        material.SetTexture("_Vista", vista);
        material.SetVector("_BaseTile", art.sprite.rect.size / Mathf.Max(216f, pixelsPerUnit));
        material.SetVector("_VistaTile", new Vector4(vista.width / Mathf.Max(216f, pixelsPerUnit), vista.height / Mathf.Max(216f, pixelsPerUnit), 0, 0));
        material.SetFloat("_Strength", strength);
        material.SetFloat("_CenterBelow", centerBelowCamera);
        art.sharedMaterial = material;
        CaveDepth.Apply(Camera.main);
    }
    void OnWillRenderObject() => CaveDepth.Apply(Camera.current);
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

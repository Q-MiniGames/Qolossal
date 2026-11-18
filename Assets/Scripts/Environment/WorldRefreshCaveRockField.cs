using UnityEngine;

// One quad with world-space native texture repetition, even for the gallery-wide stone field.
// Avoid SpriteRenderer's tiled mesh vertex limit before runtime CaveDepth starts.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldRefreshCaveRockField : MonoBehaviour
{
    [Min(216f)] public float pixelsPerUnit=216f;
    [Range(0,1)] public float parallaxRate=.3f;
    Material material,previous;SpriteRenderer art;
    void OnEnable()=>Refresh();
    void OnValidate()=>Refresh();
    public void Refresh()
    {
        art=GetComponent<SpriteRenderer>();if(art==null||art.sprite==null)return;
        // A passage's connected vista owns this renderer while installed. Editor
        // validation must not replace it with the standalone repeating rock field.
        if(art.sharedMaterial!=null&&art.sharedMaterial.shader!=null&&
            art.sharedMaterial.shader.name=="Qolossal/CavePassageVista") return;
        var shader=Shader.Find("Qolossal/CaveDepthBackdrop");if(shader==null)return;
        if(material==null){previous=art.sharedMaterial;material=new Material(shader){name="Native cave rock field",hideFlags=HideFlags.HideAndDontSave};}
        material.SetVector("_Tile",art.sprite.rect.size/Mathf.Max(216,pixelsPerUnit));
        material.SetFloat("_Rate",parallaxRate);art.sharedMaterial=material;
        CaveDepth.Apply(Camera.main);
    }
    void OnWillRenderObject()=>CaveDepth.Apply(Camera.current);
    void OnDisable()
    {
        if(art!=null&&art.sharedMaterial==material)art.sharedMaterial=previous;
        if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        material=null;
    }
}

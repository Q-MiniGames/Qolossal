using UnityEngine;

// Keeps the four existing SpriteRenderers and their awakening tint/alpha references intact.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldRefreshHeartSurface : MonoBehaviour
{
    public Texture2D detail;
    [Min(216f)] public float pixelsPerUnit = 216f;
    Material material, previous;
    SpriteRenderer art;
    void OnEnable() => Refresh();
    void OnValidate() => Refresh();
    public void Refresh()
    {
        art=GetComponent<SpriteRenderer>();
        if(art==null || detail==null)return;
        var shader=Shader.Find("Qolossal/WorldRefreshHeartSurface");
        if(shader==null)return;
        if(material==null)
        {
            previous=art.sharedMaterial;
            material=new Material(shader){name="Native Great Heart surface",hideFlags=HideFlags.HideAndDontSave};
        }
        material.SetTexture("_Detail",detail);
        material.SetVector("_QuarterFlip",new Vector4(art.flipX?1:0,art.flipY?1:0,0,0));
        material.SetVector("_DetailTile",new Vector4(detail.width/Mathf.Max(216,pixelsPerUnit),detail.height/Mathf.Max(216,pixelsPerUnit),0,0));
        art.sharedMaterial=material;
    }
    void OnDisable()
    {
        if(art!=null && art.sharedMaterial==material)art.sharedMaterial=previous;
        if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        material=null;
    }
}

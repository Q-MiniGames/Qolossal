using UnityEngine;

// A continuous sky gradient rendered at the output resolution. No enlarged bitmap.
[ExecuteAlways, DefaultExecutionOrder(10002)]
public sealed class WorldRefreshSky : MonoBehaviour
{
    public Camera targetCamera;
    public Color horizon = new Color(.95f,.64f,.55f), middle = new Color(.78f,.55f,.62f), zenith = new Color(.54f,.49f,.63f);
    public int sortingOrder = -120;
    Mesh mesh; Material material; GameObject surface;
    readonly Vector3[] vertices=new Vector3[6];
    readonly Color[] colors=new Color[6];
    static readonly Vector2[] UV=new Vector2[6];
    static readonly int[] Triangles={0,2,1,1,2,3,2,4,3,3,4,5};
    float lastWidth=-1,lastHeight=-1;
    Color lastHorizon,lastMiddle,lastZenith;
    void OnEnable()
    {
        surface=new GameObject("Continuous sky") {hideFlags=HideFlags.HideAndDontSave}; surface.transform.SetParent(transform,false);
        mesh=new Mesh {name="Output resolution sky gradient",hideFlags=HideFlags.HideAndDontSave};lastWidth=-1;
        surface.AddComponent<MeshFilter>().sharedMesh=mesh;
        material=new Material(Shader.Find("Sprites/Default")) {hideFlags=HideFlags.HideAndDontSave}; material.mainTexture=Texture2D.whiteTexture;
        var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=sortingOrder;
        Refresh();
    }
    void LateUpdate()=>Refresh();
    public void Refresh()
    {
        var view=targetCamera!=null?targetCamera:Camera.main;if(view==null||mesh==null)return;
        transform.position=new Vector3(view.transform.position.x,view.transform.position.y,0);
        float h=view.orthographicSize+1,w=h*view.aspect+1;
        if(w==lastWidth&&h==lastHeight&&horizon==lastHorizon&&middle==lastMiddle&&zenith==lastZenith)return;
        for(int row=0;row<3;row++){float y=(row-1)*h;vertices[row*2]=new Vector3(-w,y);vertices[row*2+1]=new Vector3(w,y);colors[row*2]=colors[row*2+1]=row==0?horizon:row==1?middle:zenith;}
        mesh.Clear();mesh.vertices=vertices;mesh.colors=colors;mesh.uv=UV;mesh.triangles=Triangles;mesh.RecalculateBounds();
        lastWidth=w;lastHeight=h;lastHorizon=horizon;lastMiddle=middle;lastZenith=zenith;
    }
    void OnDisable(){if(Application.isPlaying){Destroy(surface);Destroy(mesh);Destroy(material);}else{DestroyImmediate(surface);DestroyImmediate(mesh);DestroyImmediate(material);}}
}

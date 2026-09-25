using UnityEngine;

// A fixed strip of scenery. Only its slow parallax offset follows the camera;
// individual landmarks never recenter, wrap or fade based on player location.
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent,RequireComponent(typeof(SpriteRenderer))]
public sealed class CameraBackground : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 framingOffset=Vector2.zero;
    private SpriteRenderer painting;
    private Transform landscape;
    private Mesh[] meshes;
    private Material[] materials;
    private GameObject fog;
    private float backgroundZ;
    private const float PanelHeight=14f,Overlap=6.4f;
    public Vector3 LandmarkPosition=>landscape!=null?landscape.TransformPoint(new Vector3(2,1,0)):Vector3.zero;
    public float ScrollFactor=>.9f;
    private void Awake()
    {
        painting=GetComponent<SpriteRenderer>();backgroundZ=transform.position.z;
        if(painting.sprite==null)return;
        var textures=new Texture2D[4];textures[0]=painting.sprite.texture;
        string[] names={"Aqueduct","AncientGrove","FallsSanctuary"};
        for(int i=0;i<3;i++)textures[i+1]=Resources.Load<Texture2D>("WorldBackground/"+names[i]);
        landscape=new GameObject("World landscape - fixed landmarks").transform;
        meshes=new Mesh[5];materials=new Material[5];
        float left=-17.5f;
        for(int i=0;i<4;i++)
        {
            if(textures[i]==null)continue;
            float width=PanelHeight*textures[i].width/textures[i].height;
            CreatePanel(i,textures[i],left,width,PanelHeight,i==0?0:Overlap,painting.color);
            left+=width-Overlap;
        }
        // Distant atmosphere outside the authored strip, never repeating landmarks.
        fog=CreatePanel(4,Texture2D.whiteTexture,-400,800,200,0,new Color(.65f,.73f,.74f));
        fog.GetComponent<MeshRenderer>().sortingOrder=painting.sortingOrder-1;
        painting.enabled=false;
    }
    private GameObject CreatePanel(int index,Texture2D texture,float left,float width,float height,float feather,Color tint)
    {
        var obj=new GameObject(index==4?"Distant atmosphere":"Landscape section "+index);obj.transform.SetParent(landscape,false);
        const int columns=24;var vertices=new Vector3[(columns+1)*2];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var triangles=new int[columns*6];
        for(int i=0;i<=columns;i++)
        {
            float t=i/(float)columns;int v=i*2;
            vertices[v]=new Vector3(left+width*t,1-height*.5f,0);vertices[v+1]=new Vector3(left+width*t,1+height*.5f,0);
            uv[v]=new Vector2(t,0);uv[v+1]=new Vector2(t,1);
            var color=tint;color.a*=feather>0?Mathf.SmoothStep(0,1,Mathf.Clamp01(t*width/feather)):1;colors[v]=colors[v+1]=color;
            if(i==columns)continue;int k=i*6;triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+2;triangles[k+3]=v+2;triangles[k+4]=v+1;triangles[k+5]=v+3;
        }
        var mesh=new Mesh{name=obj.name};mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();meshes[index]=mesh;
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();
        var material=new Material(Shader.Find("Sprites/Default"));material.mainTexture=texture;materials[index]=material;renderer.sharedMaterial=material;
        renderer.sortingLayerID=painting.sortingLayerID;renderer.sortingOrder=painting.sortingOrder+index;
        return obj;
    }
    private void LateUpdate()
    {
        if(landscape==null)return;if(targetCamera==null)targetCamera=Camera.main;if(targetCamera==null)return;
        var cameraPosition=targetCamera.transform.position;
        // 90% horizontal screen travel: a ruin exits the view as Qori passes it.
        // Vertical depth remains distant while jumping and climbing.
        landscape.position=new Vector3(cameraPosition.x*.1f+framingOffset.x,cameraPosition.y*.9f+framingOffset.y,backgroundZ);
    }
    private void OnDisable(){if(landscape!=null)landscape.gameObject.SetActive(false);}
    private void OnEnable(){if(landscape!=null)landscape.gameObject.SetActive(true);}
    private void OnDestroy()
    {
        if(landscape!=null)Destroy(landscape.gameObject);
        if(meshes!=null)foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        if(materials!=null)foreach(var material in materials)if(material!=null)Destroy(material);
    }
}

using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class GroundCreatureVisual : MonoBehaviour
{
    [SerializeField, Min(0f)] private float groundOffset = .022f;
    private SpriteRenderer source;
    private GroundCreature creature;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private Transform display;
    private bool sourceEnabled;
    private float phase, facing = -1f, blend;
    private Part shell;
    private readonly Part[] legs = new Part[4];
    // Near hind, near front, far hind, far front: four independent footfalls.
    private readonly float[] offsets = { 0f, .5f, .25f, .75f };
    private readonly float[] hips = { -.31f, .17f, -.19f, .31f };
    private sealed class Part
    {
        public Mesh mesh;
        public Material material;
        public Vector3[] rest, points;
        public Vector2 hip, knee, ankle;
        public float bend;
    }

    private void Start()
    {
        source = GetComponent<SpriteRenderer>(); creature = GetComponent<GroundCreature>();
        body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>();
        Shader shader = Resources.Load<Shader>("WorldProps/LeafChromaKey");
        Texture2D torso = Resources.Load<Texture2D>("Creatures/CrawlerBody_v2");
        Texture2D front = Resources.Load<Texture2D>("Creatures/CrawlerFrontLeg_v2");
        Texture2D hind = Resources.Load<Texture2D>("Creatures/CrawlerHindLeg_v2");
        if (shader == null || torso == null || front == null || hind == null) { enabled = false; return; }
        display = new GameObject("Four Leg Crawler Rig").transform; display.SetParent(transform, false);
        var group = display.gameObject.AddComponent<SortingGroup>();
        group.sortingLayerID = source.sortingLayerID; group.sortingOrder = source.sortingOrder;
        shell = CreatePart("Stable armored body", torso, shader, 1, false, false);
        for (int i = 0; i < 4; i++)
        {
            bool isHind = i % 2 == 0;
            legs[i] = CreatePart((i < 2 ? "Near " : "Far ") + (isHind ? "hind leg" : "front leg"),
                isHind ? hind : front, shader, i < 2 ? 2 : 0, true, isHind);
        }
        sourceEnabled = source.enabled; source.enabled = false;
    }

    private Part CreatePart(string name, Texture2D texture, Shader shader, int order, bool limb, bool hind)
    {
        const int cols = 24, rows = 40;
        Part part = new Part();
        part.rest = new Vector3[(cols+1)*(rows+1)]; part.points = new Vector3[part.rest.Length];
        Vector2[] uv = new Vector2[part.rest.Length]; Color[] colors = new Color[part.rest.Length];
        int[] triangles = new int[cols*rows*6];
        for (int y=0; y<=rows; y++) for(int x=0; x<=cols; x++)
        {
            int i = y*(cols+1)+x; float u=x/(float)cols, v=y/(float)rows;
            uv[i]=new Vector2(u,v); colors[i]=Color.white;
            part.rest[i] = !limb ? new Vector3((u*1536f-772.5f)/1295f,(v*1024f-81f)/1295f)
                : hind ? new Vector3((u*1024f-500f)/700f*.21f,(v*1536f-220f)/1100f*.285f)
                : new Vector3((u*1254f-630f)/500f*.17f,(v*1254f-114f)/1040f*.285f);
            if (x==cols || y==rows) continue;
            int t=(y*cols+x)*6;
            triangles[t]=i;triangles[t+1]=i+cols+1;triangles[t+2]=i+1;
            triangles[t+3]=i+1;triangles[t+4]=i+cols+1;triangles[t+5]=i+cols+2;
        }
        part.hip = new Vector2(hind ? -.018f : .006f,.245f);
        part.knee = new Vector2(hind ? .065f : -.06f,.14f);
        part.ankle = new Vector2(0f,.035f); part.bend=hind ? 1f : -1f;
        part.mesh=new Mesh {name=name};part.mesh.vertices=part.rest;part.mesh.uv=uv;
        part.mesh.colors=colors;part.mesh.triangles=triangles;part.mesh.MarkDynamic();
        Transform child=new GameObject(name).transform; child.SetParent(display,false);
        child.gameObject.AddComponent<MeshFilter>().sharedMesh=part.mesh;
        var renderer=child.gameObject.AddComponent<MeshRenderer>();
        part.material=new Material(shader);part.material.mainTexture=texture;renderer.sharedMaterial=part.material;
        renderer.sortingOrder=order;
        return part;
    }

    private void LateUpdate()
    {
        if (display == null) return;
        float speed=Mathf.Abs(body.linearVelocity.x);
        float newFacing=speed>.05f ? Mathf.Sign(body.linearVelocity.x) : facing;
        if(newFacing!=facing) { phase=0f; blend=0f; facing=newFacing; }
        float width=shape.bounds.size.x*1.35f;
        bool walking=creature.IsAlive && speed>.05f && shape.IsTouchingLayers(LayerMask.GetMask("Ground"));
        blend=Mathf.MoveTowards(blend,walking ? 1f : 0f,Time.deltaTime*8f);
        const float stride=.20f;
        if(walking) phase=Mathf.Repeat(phase+Time.deltaTime*speed/(width*stride/.75f),1f);
        for(int i=0;i<4;i++)
        {
            float cycle=Mathf.Repeat(phase+offsets[i],1f);
            float stepX, lift;
            if(cycle<.75f) {stepX=Mathf.Lerp(stride*.5f,-stride*.5f,cycle/.75f);lift=0f;}
            else {float t=(cycle-.75f)/.25f;stepX=Mathf.Lerp(-stride*.5f,stride*.5f,t*t*(3f-2f*t));lift=Mathf.Sin(t*Mathf.PI)*.065f;}
            PoseLeg(legs[i],new Vector2(stepX,lift)*blend,hips[i]);
            legs[i].material.color=source.color*(i<2 ? Color.white : new Color(.72f,.72f,.72f,1f));
        }
        shell.material.color=source.color;
        Bounds bounds=shape.bounds;
        display.position=new Vector3(bounds.center.x,bounds.min.y-groundOffset,transform.position.z);
        display.rotation=Quaternion.identity;
        Vector3 scale=transform.lossyScale;
        display.localScale=new Vector3(facing*width/Mathf.Max(.001f,Mathf.Abs(scale.x)),width/Mathf.Max(.001f,Mathf.Abs(scale.y)),1f);
    }

    private void PoseLeg(Part part, Vector2 delta, float hipX)
    {
        Vector2 hip=part.hip, ankle=part.ankle+delta;
        float upper=Vector2.Distance(part.hip,part.knee),lower=Vector2.Distance(part.knee,part.ankle);
        Vector2 direction=(ankle-hip).normalized;
        float distance=Mathf.Min(Vector2.Distance(hip,ankle),upper+lower-.0001f);
        ankle=hip+direction*distance;
        float along=(upper*upper-lower*lower+distance*distance)/(2f*distance);
        float across=Mathf.Sqrt(Mathf.Max(0f,upper*upper-along*along));
        Vector2 knee=hip+direction*along+new Vector2(-direction.y,direction.x)*across*part.bend;
        float upperAngle=Vector2.SignedAngle(part.knee-part.hip,knee-hip)*Mathf.Deg2Rad;
        float lowerAngle=Vector2.SignedAngle(part.ankle-part.knee,ankle-knee)*Mathf.Deg2Rad;
        for(int i=0;i<part.rest.Length;i++)
        {
            Vector2 p=part.rest[i];
            Vector2 upperPoint=hip+Rotate(p-part.hip,upperAngle);
            Vector2 lowerPoint=knee+Rotate(p-part.knee,lowerAngle);
            Vector2 footPoint=p+ankle-part.ankle;
            float upperWeight=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.11f,.18f,p.y));
            Vector2 result=Vector2.Lerp(lowerPoint,upperPoint,upperWeight);
            result=Vector2.Lerp(footPoint,result,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.035f,.075f,p.y)));
            part.points[i]=new Vector3(result.x+hipX,result.y,0f);
        }
        part.mesh.vertices=part.points;part.mesh.RecalculateBounds();
    }
    private static Vector2 Rotate(Vector2 p,float a) => new Vector2(p.x*Mathf.Cos(a)-p.y*Mathf.Sin(a),p.x*Mathf.Sin(a)+p.y*Mathf.Cos(a));
    private void OnEnable() {if(display!=null) {display.gameObject.SetActive(true);source.enabled=false;}}
    private void OnDisable() {if(display!=null) {display.gameObject.SetActive(false);if(source!=null)source.enabled=sourceEnabled;}}
    private static void Release(Part p) {if(p==null)return;Destroy(p.mesh);Destroy(p.material);}
    private void OnDestroy() {if(display!=null)Destroy(display.gameObject);Release(shell);foreach(Part p in legs)Release(p);}
}




using UnityEngine;

// Authored front/side chest surfaces follow the existing neck and pelvis bones.
public sealed class QoriTorsoArt
{
    const int Columns=32,Rows=48;
    Mesh mesh; Material material; MeshRenderer display;
    Vector3[] vertices; Color[] colors; Vector2[] points;
    public bool Ready=>display!=null;
    public void Initialize(Transform parent,SpriteRenderer source)
    {
        Texture2D texture=Resources.Load<Texture2D>("QoriRig/Qori_ThreeQuarter_Torso_v1");
        Shader shader=Shader.Find("Sprites/Default");if(texture==null||shader==null)return;
        var obj=new GameObject("Qori three-quarter torso");obj.transform.SetParent(parent,false);
        display=obj.AddComponent<MeshRenderer>();display.sortingLayerID=source.sortingLayerID;display.sortingOrder=source.sortingOrder;
        material=new Material(shader){mainTexture=texture};display.sharedMaterial=material;
        int count=(Columns+1)*(Rows+1);vertices=new Vector3[count];colors=new Color[count];points=new Vector2[count];var uv=new Vector2[count];var triangles=new int[Columns*Rows*6];
        for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
        {
            int i=y*(Columns+1)+x;float px=texture.width*x/(float)Columns,py=texture.height*y/(float)Rows;
            float height=(770-py)/535;
            float center=Mathf.LerpUnclamped(600,565,height);
            points[i]=new Vector2((px-center)/535,height<0?height*.65f:height);
            uv[i]=new Vector2(px/texture.width,1-py/texture.height);
            if(x==Columns||y==Rows)continue;
            int t=(y*Columns+x)*6;triangles[t]=i;triangles[t+1]=i+1;triangles[t+2]=i+Columns+1;triangles[t+3]=i+1;triangles[t+4]=i+Columns+2;triangles[t+5]=i+Columns+1;
        }
        mesh=new Mesh{name="Qori authored three-quarter torso"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;
        obj.AddComponent<MeshFilter>().sharedMesh=mesh;display.enabled=false;
    }
    public void Show(Vector2 neck,Vector2 pelvis,float facing,Color tint)
    {
        if(!Ready)return;
        Vector2 spine=neck-pelvis;Vector2 axis=spine.normalized;
        Vector2 across=new Vector2(axis.y,-axis.x)*3.3f;
        for(int i=0;i<vertices.Length;i++)
        {Vector2 p=pelvis+across*points[i].x+(points[i].y<0?axis*3f:spine)*points[i].y;vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;}
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();display.enabled=true;
    }
    public void Hide(){if(display!=null)display.enabled=false;}
    public void Dispose(){if(display!=null)Object.Destroy(display.gameObject);if(mesh!=null)Object.Destroy(mesh);if(material!=null)Object.Destroy(material);}
    static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static float Stroke(Vector2 p,Vector2 a,Vector2 b,float width)
    {Vector2 d=b-a;return 1-Smooth(width,width+9,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude)));}
    // Retain only the original articulated limbs and weapon in front of the new torso.
    public static float Limbs(Vector2 p,int combatPose,bool thread)
    {
        if(combatPose>=0)
        {
            bool w=combatPose==1;
            Vector2 shoulder=w?new Vector2(913,503):new Vector2(701,503);
            Vector2 elbow=w?new Vector2(843,526):new Vector2(863,525);
            Vector2 hand=w?new Vector2(710,427):new Vector2(995,503);
            Vector2 otherShoulder=w?new Vector2(893,485):new Vector2(673,485);
            Vector2 otherElbow=w?new Vector2(679,500):new Vector2(799,500);
            Vector2 otherHand=w?new Vector2(595,385):new Vector2(943,518);
            float arms=Mathf.Max(Mathf.Max(Stroke(p,shoulder,elbow,25),Stroke(p,elbow,hand,31)),Mathf.Max(Stroke(p,otherShoulder,otherElbow,23),Stroke(p,otherElbow,otherHand,30)));
            // Blade/shaft lie beyond the hands, away from the tunic.
            float weapon=w?(1-Smooth(720,790,p.x))*(1-Smooth(450,470,p.y)):Smooth(990,1020,p.x);
            float legs=Mathf.Max(Stroke(p,w?new Vector2(727,711):new Vector2(535,711),w?new Vector2(590,806):new Vector2(410,806),30),
                Stroke(p,w?new Vector2(907,678):new Vector2(685,678),w?new Vector2(985,740):new Vector2(797,740),30));
            legs=Mathf.Max(legs,Mathf.Max(Stroke(p,w?new Vector2(590,806):new Vector2(410,806),w?new Vector2(440,950):new Vector2(295,950),29),
                Stroke(p,w?new Vector2(985,740):new Vector2(797,740),w?new Vector2(922,950):new Vector2(788,950),29)));
            legs=Mathf.Max(legs,Smooth(895,920,p.y));
            return Mathf.Max(Mathf.Max(arms,weapon),legs);
        }
        if(thread)
        {
            float arms=Mathf.Max(Mathf.Max(Stroke(p,new Vector2(703,564),new Vector2(660,697),25),Stroke(p,new Vector2(660,697),new Vector2(650,825),30)),
                Mathf.Max(Stroke(p,new Vector2(798,510),new Vector2(835,300),26),Stroke(p,new Vector2(835,300),new Vector2(865,145),35)));
            float weapon=Mathf.Max(Stroke(p,new Vector2(550,730),new Vector2(975,1260),23),Smooth(950,1020,p.y)*Smooth(780,840,p.x));
            return Mathf.Max(Mathf.Max(arms,weapon),Smooth(935,985,p.y));
        }
        float limbs=Mathf.Max(QoriSkinWeights.Evaluate(p).left+QoriSkinWeights.Evaluate(p).right,QoriSkinWeights.ArmWeight(p));
        limbs=Mathf.Max(limbs,Stroke(p,new Vector2(624,578),new Vector2(588,697),24));
        limbs=Mathf.Max(limbs,Stroke(p,new Vector2(588,697),new Vector2(647,824),27));
        limbs=Mathf.Max(limbs,Stroke(p,new Vector2(717,591),new Vector2(756,747),24));
        return Mathf.Max(limbs,Stroke(p,new Vector2(756,747),new Vector2(817,850),26));
    }
}

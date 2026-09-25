using System.Collections.Generic;
using UnityEngine;

// Two rigid-section legs reuse one approved painted leg. Source bitmap is untouched.
public sealed class QoriGroundLegRenderer
{
    readonly Mesh[] meshes=new Mesh[2];
    readonly MeshRenderer[] renderers=new MeshRenderer[2];
    readonly Vector3[][] vertices=new Vector3[2][];
    readonly Color[][] colors=new Color[2][];
    Vector2[] points;
    float[] knees,feet,alpha;
    Material authoredMaterial;
    public bool HasThreeQuarterArt=>authoredMaterial!=null;
    readonly Vector2 sourceHip=new Vector2(1.12f,-2.08f), sourceKnee=new Vector2(1.19f,-3.45f), sourceAnkle=new Vector2(1.10f,-4.87f);
    public float UpperLength=>Vector2.Distance(sourceHip,sourceKnee);
    public float LowerLength=>Vector2.Distance(sourceKnee,sourceAnkle);
    public Vector2 SoleOffset=>new Vector2(.28f,-1.07f);
    public void Initialize(Transform parent,Vector3[] rest,Vector2[] pixels,Vector2[] uv,int[] triangles,Material material,SpriteRenderer source)
    {
        Texture2D authored=Resources.Load<Texture2D>("QoriRig/Qori_ThreeQuarter_Leg_v1");
        if(authored!=null)
        {
            const int columns=40,rows=128;
            int authoredCount=(columns+1)*(rows+1);
            points=new Vector2[authoredCount];knees=new float[authoredCount];feet=new float[authoredCount];alpha=new float[authoredCount];
            var coords=new Vector2[authoredCount];var authoredIndices=new int[columns*rows*6];
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                int i=y*(columns+1)+x;float px=authored.width*x/(float)columns,py=authored.height*y/(float)rows;
                float t=py<565?(py-150)/415:(py-565)/535;
                Vector2 a=py<565?sourceHip:sourceKnee,b=py<565?sourceKnee:sourceAnkle;
                float center=py<565?Mathf.LerpUnclamped(500,620,t):Mathf.LerpUnclamped(620,550,t);
                Vector2 p=Vector2.LerpUnclamped(a,b,t)+Vector2.right*((px-center)*.0024f);
                if(py>=1100)p=sourceAnkle+new Vector2((px-550)*Mathf.Lerp(.0024f,.0035f,Mathf.InverseLerp(1100,1230,py)),-(py-1100)/320*1.07f);
                points[i]=p;knees[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(545,585,py));feet[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1065,1095,py));alpha[i]=1;
                coords[i]=new Vector2(px/authored.width,1-py/authored.height);
                if(x==columns||y==rows)continue;
                int j=(y*columns+x)*6;authoredIndices[j]=i;authoredIndices[j+1]=i+1;authoredIndices[j+2]=i+columns+1;authoredIndices[j+3]=i+1;authoredIndices[j+4]=i+columns+2;authoredIndices[j+5]=i+columns+1;
            }
            authoredMaterial=new Material(material){mainTexture=authored};
            CreateDisplays(parent,source,authoredMaterial,coords,authoredIndices);
            return;
        }
        var map=new Dictionary<int,int>();var selected=new List<int>();var indices=new List<int>();
        for(int t=0;t<triangles.Length;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            if(Mask(pixels[a])<=0 && Mask(pixels[b])<=0 && Mask(pixels[c])<=0)continue;
            for(int j=0;j<3;j++)
            {
                int index=triangles[t+j];
                if(!map.TryGetValue(index,out int compact)){compact=selected.Count;map.Add(index,compact);selected.Add(index);}
                indices.Add(compact);
            }
        }
        int count=selected.Count;points=new Vector2[count];knees=new float[count];feet=new float[count];alpha=new float[count];var texcoords=new Vector2[count];
        for(int i=0;i<count;i++)
        {
            int id=selected[i];points[i]=rest[id];texcoords[i]=uv[id];alpha[i]=Mask(pixels[id]);
            knees[i]=Mathf.SmoothStep(0,1,Mathf.InverseLerp(sourceKnee.y+.065f,sourceKnee.y-.065f,points[i].y));
            feet[i]=QoriSkinWeights.FootWeight(pixels[id],1);
        }
        CreateDisplays(parent,source,material,texcoords,indices.ToArray());
    }
    void CreateDisplays(Transform parent,SpriteRenderer source,Material material,Vector2[] texcoords,int[] indices)
    {
        int count=points.Length;
        for(int leg=0;leg<2;leg++)
        {
            GameObject obj=new GameObject(leg==0?"Qori far ground leg":"Qori near ground leg");obj.transform.SetParent(parent,false);
            vertices[leg]=new Vector3[count];colors[leg]=new Color[count];
            meshes[leg]=new Mesh{name=obj.name};meshes[leg].vertices=vertices[leg];meshes[leg].uv=texcoords;meshes[leg].triangles=indices;meshes[leg].MarkDynamic();
            obj.AddComponent<MeshFilter>().sharedMesh=meshes[leg];renderers[leg]=obj.AddComponent<MeshRenderer>();
            renderers[leg].sharedMaterial=material;renderers[leg].sortingLayerID=source.sortingLayerID;renderers[leg].sortingOrder=source.sortingOrder-2+leg;
            renderers[leg].enabled=false;
        }
    }
    static float Mask(Vector2 pixel)
    {
        float thigh=Mathf.SmoothStep(0,1,Mathf.InverseLerp(870,900,pixel.y))
            *Mathf.SmoothStep(0,1,Mathf.InverseLerp(658,678,pixel.x))
            *(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(748,770,pixel.x)));
        return Mathf.Max(QoriSkinWeights.Evaluate(pixel).right,thigh);
    }
    static Vector2 Rotation(Vector2 a,Vector2 b)
    {float radians=Vector2.SignedAngle(a,b)*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));}
    static Vector2 Rotate(Vector2 p,Vector2 r)=>new Vector2(p.x*r.x-p.y*r.y,p.x*r.y+p.y*r.x);
    public void Render(int leg,Vector2 hip,Vector2 knee,Vector2 ankle,float facing,Color tint,float footAngle=0)
    {
        if(renderers[leg]==null)return;
        renderers[leg].enabled=true;
        Vector2 upper=Rotation(sourceKnee-sourceHip,knee-hip),lower=Rotation(sourceAnkle-sourceKnee,ankle-knee);
        Vector2 footRotation=new Vector2(Mathf.Cos(footAngle*Mathf.Deg2Rad),Mathf.Sin(footAngle*Mathf.Deg2Rad));
        float upperScale=Vector2.Distance(hip,knee)/UpperLength,lowerScale=Vector2.Distance(knee,ankle)/LowerLength;
        Color color=tint;if(leg==0){color.r*=.84f;color.g*=.84f;color.b*=.84f;}
        for(int i=0;i<points.Length;i++)
        {
            Vector2 p=points[i];
            Vector2 mapped=Vector2.Lerp(Vector2.Lerp(hip+Rotate(p-sourceHip,upper)*upperScale,knee+Rotate(p-sourceKnee,lower)*lowerScale,knees[i]),ankle+Rotate(p-sourceAnkle,footRotation),feet[i]);
            vertices[leg][i]=new Vector3(mapped.x*facing,mapped.y,0);colors[leg][i]=color;colors[leg][i].a*=alpha[i];
        }
        meshes[leg].vertices=vertices[leg];meshes[leg].colors=colors[leg];meshes[leg].RecalculateBounds();
    }
    public void Hide(){foreach(var renderer in renderers)if(renderer!=null)renderer.enabled=false;}
    public void Dispose()
    {
        foreach(var mesh in meshes)if(mesh!=null)Object.Destroy(mesh);
        foreach(var renderer in renderers)if(renderer!=null)Object.Destroy(renderer.gameObject);
        if(authoredMaterial!=null)Object.Destroy(authoredMaterial);
    }
}

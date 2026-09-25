using UnityEngine;

// A painted ribbon follows the same centreline used by melee hit detection.
public sealed class QoriVineLash
{
    readonly Mesh mesh;readonly MeshRenderer renderer;readonly Material material;readonly Transform transform;
    readonly Vector3[] vertices=new Vector3[38];
    public QoriVineLash(Transform parent)
    {
        var obj=new GameObject("Painted thorn vine");obj.transform.SetParent(parent,false);transform=obj.transform;
        mesh=new Mesh{name="Flexible thorn artwork"};mesh.MarkDynamic();obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        renderer=obj.AddComponent<MeshRenderer>();renderer.sortingOrder=12;
        material=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=material;
        var sprite=QoriArmoryArt.Get("ThornLash");var uv=new Vector2[38];var indices=new int[108];
        if(sprite!=null)
        {
            material.mainTexture=sprite.texture;var rect=sprite.rect;
            for(int i=0;i<19;i++){float x=Mathf.Lerp(rect.xMin,rect.xMax,i/18f)/sprite.texture.width;uv[i*2]=new Vector2(x,rect.yMax/sprite.texture.height);uv[i*2+1]=new Vector2(x,rect.yMin/sprite.texture.height);}
        }
        for(int i=0;i<18;i++){int a=i*2,k=i*6;indices[k]=a;indices[k+1]=a+2;indices[k+2]=a+1;indices[k+3]=a+1;indices[k+4]=a+2;indices[k+5]=a+3;}
        mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;renderer.enabled=false;
    }
    public void Show(Vector3[] points)
    {
        renderer.enabled=points!=null;if(points==null)return;
        for(int i=0;i<19;i++)
        {
            Vector3 tangent=(points[Mathf.Min(i+1,18)]-points[Mathf.Max(i-1,0)]).normalized;
            Vector3 normal=new Vector3(-tangent.y,tangent.x,0)*Mathf.Lerp(.09f,.025f,i/18f);
            vertices[i*2]=transform.InverseTransformPoint(points[i]+normal);vertices[i*2+1]=transform.InverseTransformPoint(points[i]-normal);
        }
        mesh.vertices=vertices;mesh.RecalculateBounds();
    }
    public void Dispose(){Object.Destroy(mesh);Object.Destroy(material);Object.Destroy(transform.gameObject);}
}

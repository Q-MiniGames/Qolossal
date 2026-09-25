using System;
using System.Collections.Generic;
using UnityEngine;

// The painted shaft stays rigid when raised. Optional weapon art shares the same grip transform.
public sealed class QoriHeldWeaponArt
{
    Mesh mesh;MeshRenderer renderer;SpriteRenderer custom;
    Mesh palmMesh;MeshRenderer palmRenderer;
    Vector3[] source,vertices;Color[] colors;float[] alpha;
    Vector2[] originalUV,posedUV;
    public bool Ready=>mesh!=null;
    public void Show(bool active,Transform parent,Material material,MeshRenderer body,Vector3[] rest,Vector2[] uv,int[] triangles,Func<Vector2,Vector2> pose,float facing,Color tint,WeaponDefinition weapon,float oneHandWeight=0,Func<Vector2,Vector2> freePalmPose=null,float weaponYaw=0,float weaponRoll=0)
    {
        if(!active){Hide();return;}
        if(mesh==null)
        {
            var ids=new List<int>();var map=new Dictionary<int,int>();var indices=new List<int>();
            for(int t=0;t<triangles.Length;t+=3)
            {
                if(QoriWeaponCarry.WeaponMask(rest[triangles[t]])<=0&&QoriWeaponCarry.WeaponMask(rest[triangles[t+1]])<=0&&QoriWeaponCarry.WeaponMask(rest[triangles[t+2]])<=0)continue;
                for(int j=0;j<3;j++){int id=triangles[t+j];if(!map.TryGetValue(id,out int compact)){compact=ids.Count;map.Add(id,compact);ids.Add(id);}indices.Add(compact);}
            }
            source=new Vector3[ids.Count];vertices=new Vector3[ids.Count];colors=new Color[ids.Count];alpha=new float[ids.Count];var coords=new Vector2[ids.Count];
            for(int i=0;i<ids.Count;i++){source[i]=rest[ids[i]];coords[i]=uv[ids[i]];alpha[i]=QoriWeaponCarry.WeaponMask(source[i]);}
            originalUV=coords;posedUV=new Vector2[ids.Count];
            mesh=new Mesh{name="Qori rigid held weapon"};mesh.vertices=vertices;mesh.uv=coords;mesh.triangles=indices.ToArray();mesh.MarkDynamic();
            var obj=new GameObject(mesh.name);obj.transform.SetParent(parent,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+2;
            palmMesh=new Mesh{name="Qori released palm"};palmMesh.vertices=vertices;palmMesh.uv=coords;palmMesh.triangles=indices.ToArray();palmMesh.MarkDynamic();
            var palm=new GameObject(palmMesh.name);palm.transform.SetParent(parent,false);palm.AddComponent<MeshFilter>().sharedMesh=palmMesh;palmRenderer=palm.AddComponent<MeshRenderer>();palmRenderer.sharedMaterial=material;palmRenderer.sortingLayerID=body.sortingLayerID;palmRenderer.sortingOrder=body.sortingOrder+2;
            var replacement=new GameObject("Equipped weapon artwork");replacement.transform.SetParent(parent,false);custom=replacement.AddComponent<SpriteRenderer>();custom.sortingLayerID=body.sortingLayerID;custom.sortingOrder=body.sortingOrder+2;
        }
        bool replace=weapon!=null&&weapon.weaponArtwork!=null;
        renderer.enabled=true;custom.enabled=replace&&weapon.weaponId!="resin-sling";
        palmRenderer.enabled=oneHandWeight>.001f&&freePalmPose!=null;
        if(palmRenderer.enabled)
        {
            for(int i=0;i<vertices.Length;i++){Vector2 p=freePalmPose(source[i]);vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;colors[i].a*=QoriWeaponCarry.FreePalmMask(source[i])*oneHandWeight;}
            palmMesh.vertices=vertices;palmMesh.colors=colors;palmMesh.RecalculateBounds();
        }
        if(replace)
        {
            Vector2 grip=pose(new Vector2(.56f,-1.59f)),right=pose(new Vector2(1.56f,-1.59f))-grip;
            float angle=Vector2.SignedAngle(Vector2.right,right)+Vector2.SignedAngle(weapon.artworkTip,new Vector2(5.33f,-1.19f));
            custom.sprite=weapon.weaponArtwork;custom.color=tint;custom.transform.localPosition=new Vector3(grip.x*facing,grip.y,0);
            // Rotate the rigid cutout around its hand pivot into screen depth.
            custom.transform.localRotation=Quaternion.AngleAxis(angle*facing,Vector3.forward)*Quaternion.AngleAxis(weaponYaw,Vector3.up)*Quaternion.AngleAxis(weaponRoll,Vector3.right);custom.transform.localScale=new Vector3(facing,1,1)*weapon.artworkScale;
        }
        for(int i=0;i<vertices.Length;i++)
        {
            Vector2 p=pose(source[i]);vertices[i]=new Vector3(p.x*facing,p.y,0);colors[i]=tint;
            float releasedPalm=QoriWeaponCarry.FreePalmMask(source[i])*oneHandWeight;
            colors[i].a*=alpha[i]*Mathf.Lerp(1,QoriWeaponCarry.CleanGripShaft(source[i]),releasedPalm);
            if(replace)colors[i].a*=Mathf.Max(QoriWeaponCarry.FreePalmMask(source[i])*(1-oneHandWeight),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.34f,Vector2.Distance(source[i],new Vector2(.56f,-1.59f)))));
            // Replace the released hand's baked-in pixels with a neighbouring
            // clean section of the same painted shaft; the arm renders its own palm.
            posedUV[i]=originalUV[i]+new Vector2(83f/material.mainTexture.width,-10f/material.mainTexture.height)*releasedPalm;
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.uv=posedUV;mesh.RecalculateBounds();
    }
    public Vector3 CustomTip(WeaponDefinition weapon)=>custom.transform.TransformPoint(weapon.artworkTip);
    public void Hide(){if(renderer!=null)renderer.enabled=false;if(custom!=null)custom.enabled=false;if(palmRenderer!=null)palmRenderer.enabled=false;}
    public void Dispose(){if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(renderer!=null)UnityEngine.Object.Destroy(renderer.gameObject);if(custom!=null)UnityEngine.Object.Destroy(custom.gameObject);if(palmMesh!=null)UnityEngine.Object.Destroy(palmMesh);if(palmRenderer!=null)UnityEngine.Object.Destroy(palmRenderer.gameObject);}
}

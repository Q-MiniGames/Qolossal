using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Repairs static fitted-passage visuals only. No builder, portal or collider mutation.
public static class WorldRefreshTrimDensity
{
    const string Root = "Assets/Development/Layouts/WorldRefresh_20261104/NativeTrim/";
    const string Evidence = "Tools/Design/WorldRefresh_20261104/Surface/";
    sealed class Record
    {
        public string hierarchy, disposition, sourceMesh, sourceTexture, texture, mesh, secondaryMask;
        public double beforeDensity, afterDensity, requiredDensity;
        public float texturePixelsPerWorldUnit;
        public int sourceWidth,sourceHeight,textureWidth,textureHeight,requiredWidth,requiredHeight;
        public float[] worldBoundsCenter,worldBoundsSize;
        public bool geometryPreserved, colliderStatePreserved;
    }
    static string Hier(Transform t) { var p=new List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p); }
    static string Key(string text) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").Substring(0,20); }
    static IEnumerable<T> All<T>(Scene s) where T:Component => s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static int[] Size(Texture2D t) { int w=t.width,h=t.height;var i=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;if(i!=null)i.GetSourceTextureWidthAndHeight(out w,out h);return new[]{w,h}; }
    static double Norm(double a,double b,double c,double d) { double sum=a*a+b*b+c*c+d*d,det=a*d-b*c;return Math.Sqrt(Math.Max(0,(sum+Math.Sqrt(Math.Max(0,sum*sum-4*det*det)))/2)); }
    static double Density(Transform transform,Mesh mesh,Vector2[] uv,int w,int h)
    {
        var v=mesh.vertices;var tr=mesh.triangles;double worst=0;
        if(uv.Length!=v.Length)return 0;
        for(int i=0;i+2<tr.Length;i+=3)
        {
            int i0=tr[i],i1=tr[i+1],i2=tr[i+2];var a=transform.TransformPoint(v[i0]);var b=transform.TransformPoint(v[i1])-a;var c=transform.TransformPoint(v[i2])-a;
            var ub=uv[i1]-uv[i0];var uc=uv[i2]-uv[i0];double ux=ub.x*w,uy=ub.y*h,vx=uc.x*w,vy=uc.y*h,det=ux*vy-vx*uy;
            if(Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
            if(Math.Abs(det)<1e-10)return 0;
            worst=Math.Max(worst,Norm((b.x*vy-c.x*uy)/det,(-b.x*vx+c.x*ux)/det,(b.y*vy-c.y*uy)/det,(-b.y*vx+c.y*ux)/det));
        }
        return worst>0?1/worst:0;
    }
    static bool SameGeometry(Mesh a,Mesh b)
    {
        if(!a.vertices.SequenceEqual(b.vertices)||!a.triangles.SequenceEqual(b.triangles)||!a.colors.SequenceEqual(b.colors)||a.subMeshCount!=b.subMeshCount)return false;
        for(int channel=1;channel<8;channel++){var x=new List<Vector4>();var y=new List<Vector4>();a.GetUVs(channel,x);b.GetUVs(channel,y);if(!x.SequenceEqual(y))return false;}
        return a.bounds==b.bounds;
    }
    static string CollisionState(Scene s) => string.Join("\n",All<Collider2D>(s).Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));
    static Texture2D Tex(Renderer r)
    { var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block.GetTexture("_MainTex") as Texture2D ?? (r.sharedMaterial!=null?r.sharedMaterial.mainTexture as Texture2D:null); }
    static float WalkRow(string id) => id=="MR04"?333:id=="MR05"?327:id=="MR06"?324:id=="MR07"?321:309;

    // replaceAlphaTrim deliberately defaults false: filling a painted corner's rectangular
    // mesh changes its visible silhouette even when its vertex/collision geometry is unchanged.
    public static string ApplyCurrent(bool replaceAlphaTrim=false)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required.");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Save unrelated scene edits first.");
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));
        var region=world["regions"].FirstOrDefault(r=>(string)r["scene"]==scene.name);if(region==null)throw new InvalidOperationException("Open a surface region.");
        string id=(string)region["id"],folder=Root+id+"/";
        var fill=AssetDatabase.LoadAssetAtPath<Texture2D>(id=="MR01"?"Assets/Art/Codex/Terrain/Body/Cradle4K/Body_Fill_4K.png":WorldRefreshSurface.Art+id+"/Fill.png");
        var top=AssetDatabase.LoadAssetAtPath<Texture2D>(id=="MR01"?"Assets/Art/Codex/Terrain/CradleCompletion4K/Body_Top_Strip_MR01_360_B45.png":WorldRefreshSurface.Art+id+"/Top.png");
        if(fill==null||top==null)throw new InvalidOperationException("Import the native region art before repairing fitted meshes.");
        var cameras=All<Camera>(scene).Where(c=>c.orthographic).ToArray();if(cameras.Length==0)throw new InvalidOperationException("No orthographic camera.");
        float cameraSize=cameras.Min(c=>c.orthographicSize);foreach(var vista in All<VistaZone>(scene))cameraSize=Mathf.Min(cameraSize,vista.size);
        double required=2160.0/(2*cameraSize);string collisionBefore=CollisionState(scene);
        Directory.CreateDirectory(folder);Directory.CreateDirectory(Evidence);AssetDatabase.Refresh();
        var records=new List<Record>();
        foreach(var filter in All<MeshFilter>(scene).Where(f=>f.gameObject.activeInHierarchy&&f.GetComponentInParent<MraTerrain>()==null))
        {
            string hierarchy=Hier(filter.transform);if(!hierarchy.Contains("/Regional fitted passage/"))continue;
            var renderer=filter.GetComponent<MeshRenderer>();var original=filter.sharedMesh;
            if(renderer==null||!renderer.enabled||original==null||original.vertexCount==0||renderer.sharedMaterials.Length!=1)continue;
            string originalPath=AssetDatabase.GetAssetPath(original);if(string.IsNullOrEmpty(originalPath))continue;
            var oldTex=Tex(renderer);if(oldTex==null)continue;var oldSize=Size(oldTex);double before=Density(filter.transform,original,original.uv,oldSize[0],oldSize[1]);
            if(before>=required*.999)continue;
            var bc=renderer.bounds.center;var bs=renderer.bounds.size;
            var record=new Record{hierarchy=hierarchy,sourceMesh=originalPath,sourceTexture=AssetDatabase.GetAssetPath(oldTex),beforeDensity=before,requiredDensity=required,sourceWidth=oldSize[0],sourceHeight=oldSize[1],worldBoundsCenter=new[]{bc.x,bc.y,bc.z},worldBoundsSize=new[]{bs.x,bs.y,bs.z}};records.Add(record);
            if(originalPath.StartsWith(Root,StringComparison.Ordinal)){record.disposition="Already repaired but now below requested camera density; inspect changed camera or transform";continue;}
            string n=filter.name;
            bool solid=n=="Registered native outer rock"||n=="Continuous themed inner wall";
            bool floor=n=="Native walking surface";
            bool roof=n.StartsWith("Native roof ",StringComparison.Ordinal);
            string low=record.sourceTexture.ToLowerInvariant();
            bool topSource=low.Contains("top_strip")||low.Contains("ground_top");
            bool registeredTop=floor||(roof&&topSource);
            if(!solid&&!registeredTop&&!(roof&&replaceAlphaTrim)) {record.disposition="Preserved painted corner/face alpha; requires separate silhouette review";continue;}
            if(filter.GetComponents<MonoBehaviour>().Any()) {record.disposition="Preserved: local runtime component requires ownership review";continue;}
            Texture2D target=registeredTop?top:fill;var size=Size(target);float ppu=id=="MR01"&&!registeredTop?240:360;
            var oldImporter=AssetImporter.GetAtPath(record.sourceTexture) as TextureImporter;
            float oldPpu=oldImporter!=null?oldImporter.spritePixelsPerUnit:120;
            if(registeredTop&&oldPpu<=0)throw new InvalidOperationException("Missing source PPU: "+record.sourceTexture);
            float oldHeight=oldSize[1]/oldPpu,oldAbove=.8f;
            float sourceWalkRow=id=="MR01"?288:WalkRow(id);
            var verts=original.vertices;var oldUV=original.uv;var newUV=new Vector2[verts.Length];
            void Map(float density)
            {
                for(int k=0;k<verts.Length;k++)
                {
                    var w=filter.transform.TransformPoint(verts[k]);
                    // Fitted walking strips and copied native tops both encode this same
                    // vertical distance to the original ground; do not turn them into opaque quads.
                    float distanceToFloor=oldAbove-(1-oldUV[k].y)*oldHeight;
                    newUV[k]=registeredTop?new Vector2(w.x*density/size[0],1-sourceWalkRow/size[1]+distanceToFloor*density/size[1]):new Vector2(w.x*density/size[0],w.y*density/size[1]);
                }
            }
            Map(ppu);double after=Density(filter.transform,original,newUV,size[0],size[1]);
            if(after<=0)throw new InvalidOperationException("Degenerate UV mapping: "+hierarchy);
            if(after<required*1.005){ppu*= (float)(required*1.01/after);Map(ppu);after=Density(filter.transform,original,newUV,size[0],size[1]);}
            if(after<required*.999)throw new InvalidOperationException("Density repair failed: "+hierarchy);
            string key=Key(hierarchy),meshPath=folder+key+".asset",matPath=folder+key+".mat";
            if(AssetDatabase.LoadAssetAtPath<Mesh>(meshPath)!=null||AssetDatabase.LoadAssetAtPath<Material>(matPath)!=null)throw new InvalidOperationException("Orphan repair assets require review: "+meshPath);
            var mesh=Object.Instantiate(original);mesh.name=original.name+" native density";mesh.hideFlags=HideFlags.None;mesh.uv=newUV;
            if(!SameGeometry(original,mesh))throw new InvalidOperationException("Geometry changed unexpectedly: "+hierarchy);
            var material=new Material(renderer.sharedMaterial){name=renderer.sharedMaterial.name+" native density",hideFlags=HideFlags.None};material.mainTexture=target;material.mainTextureScale=Vector2.one;material.mainTextureOffset=Vector2.zero;
            if(material.HasProperty("_EdgeMask"))record.secondaryMask=AssetDatabase.GetAssetPath(material.GetTexture("_EdgeMask"));
            AssetDatabase.CreateAsset(mesh,meshPath);AssetDatabase.CreateAsset(material,matPath);
            filter.sharedMesh=mesh;renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(block.GetTexture("_MainTex")!=null){block.SetTexture("_MainTex",target);renderer.SetPropertyBlock(block);}
            EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);
            record.disposition=registeredTop?"Native top re-registered to original walking line":solid?"Solid wall world-tiled; secondary aperture channels preserved":"Painted corner replaced with natural stone over same mesh outline";
            record.mesh=meshPath;record.texture=AssetDatabase.GetAssetPath(target);record.afterDensity=after;record.texturePixelsPerWorldUnit=ppu;record.geometryPreserved=true;
            record.textureWidth=size[0];record.textureHeight=size[1];record.requiredWidth=(int)Math.Ceiling(size[0]*required/after-.001);record.requiredHeight=(int)Math.Ceiling(size[1]*required/after-.001);
        }
        bool collisionUnchanged=collisionBefore==CollisionState(scene);foreach(var r in records)r.colliderStatePreserved=collisionUnchanged;
        if(!collisionUnchanged)throw new InvalidOperationException("Collider state changed unexpectedly.");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string report=Evidence+id+"_trim_density.json";
        // Preserve the first application receipt on an idempotent second pass.
        if(records.Count>0||!File.Exists(report))File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,minCameraSize=cameraSize,requiredPixelsPerUnit=required,replaceAlphaTrim,secondaryMaskLimitation="Original aperture-mask UV channels and texture are retained. Surface Rim also samples its RGB; those secondary samples need separate review and are not certified by MainTex density.",records},Formatting.Indented));
        return JsonConvert.SerializeObject(new{scene=scene.name,repaired=records.Count(r=>r.mesh!=null),remaining=records.Count(r=>r.mesh==null),report,collisionUnchanged});
    }
}

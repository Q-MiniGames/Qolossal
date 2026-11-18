using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Root owns scene execution. Original approved mesh/material/image assets remain immutable.
public static class WorldRevisionSurfaceCaves
{
    const string Evidence="Tools/Design/WorldRevision_20261104/Surface";
    const string AssetsRoot="Assets/Development/Layouts/WorldRevision_20261104/Surface/";
    const string Original="Assets/Development/Layouts/CaveRollout_20261027/Upper_";
    const string ShaderName="Qolossal/WorldRevision/Approved Rim Native Attachment";
    static IEnumerable<T> All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static string Hier(Transform t){var names=new List<string>();while(t!=null){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);}
    static string Hash(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    static string Collision(Scene s)=>string.Join("\n",All<Collider2D>(s).Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));
    static string Region(Scene s)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");if(s.isDirty)throw new Exception("Save unrelated scene edits first");
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var regionData=world["regions"].FirstOrDefault(node=>(string)node["scene"]==s.name);if(regionData==null)throw new Exception("Open one of the seven upper scenes");return (string)regionData["id"];
    }
    static Mesh SaveMesh(string path,Mesh mesh)
    {
        mesh.hideFlags=HideFlags.None;var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
    }
    static Material SaveMaterial(string path,Material material)
    {
        material.hideFlags=HideFlags.None;var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing==null){AssetDatabase.CreateAsset(material,path);return material;}
        EditorUtility.CopySerialized(material,existing);Object.DestroyImmediate(material);EditorUtility.SetDirty(existing);return existing;
    }
    static int[] Size(Texture2D texture){int w=texture.width,h=texture.height;var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture))as TextureImporter;if(importer!=null)importer.GetSourceTextureWidthAndHeight(out w,out h);return new[]{w,h};}
    static double Norm(double a,double b,double c,double d){double sum=a*a+b*b+c*c+d*d,det=a*d-b*c;return Math.Sqrt(Math.Max(0,(sum+Math.Sqrt(Math.Max(0,sum*sum-4*det*det)))/2));}
    static object MappingAudit(MeshFilter filter,Texture2D texture,int channel,Vector4 mapping,double display,string role)
    {
        if(texture==null)return new{role,error="Missing texture"};var mesh=filter.sharedMesh;var v=mesh.vertices;var uv=new List<Vector4>();mesh.GetUVs(channel,uv);var tr=mesh.triangles;var size=Size(texture);double worst=0;int measured=0;
        if(uv.Count==v.Length)for(int i=0;i<tr.Length;i+=3)
        {
            int i0=tr[i],i1=tr[i+1],i2=tr[i+2];var a=filter.transform.TransformPoint(v[i0]);var b=filter.transform.TransformPoint(v[i1])-a;var c=filter.transform.TransformPoint(v[i2])-a;
            double ux=(uv[i1].x-uv[i0].x)*mapping.x*size[0],uy=(uv[i1].y-uv[i0].y)*mapping.y*size[1],vx=(uv[i2].x-uv[i0].x)*mapping.x*size[0],vy=(uv[i2].y-uv[i0].y)*mapping.y*size[1],det=ux*vy-vx*uy;
            if(Math.Abs(det)<1e-10||Math.Abs(b.x*c.y-b.y*c.x)<1e-10)continue;
            worst=Math.Max(worst,Norm((b.x*vy-c.x*uy)/det,(-b.x*vx+c.x*ux)/det,(b.y*vy-c.y*uy)/det,(-b.y*vx+c.y*ux)/det));measured++;
        }
        return new{role,asset=AssetDatabase.GetAssetPath(texture),sourceWidth=size[0],sourceHeight=size[1],nativeMinimumPPU=worst>0?1/worst:0,requiredWidth=(int)Math.Ceiling(size[0]*worst*display),requiredHeight=(int)Math.Ceiling(size[1]*worst*display),nativePass=worst>0&&worst*display<=1.001,importPass=worst>0&&worst*display<=Math.Min((double)texture.width/size[0],(double)texture.height/size[1])+.001,triangles=measured};
    }
    public static string RestoreCurrent()
    {
        var scene=SceneManager.GetActiveScene();string region=Region(scene),folder=AssetsRoot+region+"/";
        Directory.CreateDirectory(folder);Directory.CreateDirectory(Evidence);AssetDatabase.Refresh();
        var shader=Shader.Find(ShaderName);if(shader==null)throw new Exception("Import ApprovedSurfaceRim.shader first");
        string collisionBefore=Collision(scene);float camera=All<Camera>(scene).Where(c=>c.orthographic).Min(c=>c.orthographicSize);foreach(var vista in All<VistaZone>(scene))camera=Mathf.Min(camera,vista.size);double display=2160/(2.0*camera);
        var changes=new List<object>();var sources=new Dictionary<string,string>();var audits=new List<object>();var before=new List<object>();
        void Receipt(string path){if(File.Exists(path)&&!sources.ContainsKey(path))sources[path]=Hash(path);}
        foreach(var point in All<CaveConnectionPoint>(scene).Where(p=>p.isActiveAndEnabled&&!p.undergroundSide))
        {
            var group=point.transform.Find("Regional fitted passage");if(group==null){changes.Add(new{id=point.lowerRoomId,action="Separate approved assembly retained; no fitted replacement"});continue;}
            string id=point.lowerRoomId,sourceFolder=Original+id+"/",destination=folder+id+"/";Directory.CreateDirectory(destination);AssetDatabase.Refresh();
            foreach(var filter in group.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)continue;string path=Hier(filter.transform);
                if(filter.name.StartsWith("Native roof ",StringComparison.Ordinal))
                {
                    // The live terrain now owns the continuous native walking cap.
                    // These copied, clipped decorative quads caused floating/pasted roofs.
                    before.Add(new{path,mesh=AssetDatabase.GetAssetPath(filter.sharedMesh),material=AssetDatabase.GetAssetPath(renderer.sharedMaterial),renderer.enabled});renderer.enabled=false;EditorUtility.SetDirty(renderer);changes.Add(new{id,path,action="Disabled detached copied roof decoration; live terrain cap retained"});continue;
                }
                string materialName=filter.name=="Registered native outer rock"?"Native outer rock":filter.name=="Continuous themed inner wall"?"Inner wall":filter.name=="Passage shadow"?"Passage shadow":null;
                if(materialName==null)continue;
                string meshPath=sourceFolder+filter.name+".asset",matPath=sourceFolder+materialName+".mat";
                var originalMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);var originalMat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if(originalMesh==null||originalMat==null)throw new Exception("Missing approved fitted source: "+meshPath+" / "+matPath);
                Receipt(meshPath);Receipt(matPath);before.Add(new{path,mesh=AssetDatabase.GetAssetPath(filter.sharedMesh),material=AssetDatabase.GetAssetPath(renderer.sharedMaterial),renderer.enabled});
                var clone=Object.Instantiate(originalMesh);clone.name=originalMesh.name+" approved restoration";var material=new Material(originalMat){name=originalMat.name+" approved restoration"};
                if(filter.name=="Registered native outer rock")
                {
                    var fill=AssetDatabase.LoadAssetAtPath<Texture2D>(region=="MR01"?"Assets/Art/Codex/Terrain/Body/Cradle4K/Body_Fill_4K.png":WorldRefreshSurface.Art+region+"/Fill.png");if(fill==null)throw new Exception("Missing native outer fill");
                    // Copy properties before changing shader; the approved RGB source,
                    // crop/mirror registration and inward palette are retained exactly.
                    var edge=originalMat.GetTexture("_EdgeMask");var registration=originalMat.GetVector("_RimUV");var rimTint=originalMat.GetColor("_RimTint");
                    material.shader=shader;material.mainTexture=fill;material.SetTexture("_EdgeMask",edge);material.SetVector("_RimUV",registration);material.SetColor("_RimTint",rimTint);
                    var size=Size(fill);material.SetFloat("_PixelsPerUnit",region=="MR01"?240:360);material.SetVector("_NativeTextureSize",new Vector4(size[0],size[1],0,0));
                    var edgeUV=new List<Vector4>();clone.GetUVs(1,edgeUV);float width=clone.bounds.size.x;material.SetFloat("_AttachmentBlend",Mathf.Clamp(.45f/Mathf.Max(.5f,width),.01f,.15f));
                }
                var savedMesh=SaveMesh(destination+filter.name+".asset",clone);var savedMaterial=SaveMaterial(destination+materialName+".mat",material);
                filter.sharedMesh=savedMesh;renderer.sharedMaterial=savedMaterial;renderer.enabled=true;
                // Current native texture overrides must not mask the restored paintings.
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);if(block.GetTexture("_MainTex")!=null){block.SetTexture("_MainTex",savedMaterial.mainTexture);renderer.SetPropertyBlock(block);}
                EditorUtility.SetDirty(filter);EditorUtility.SetDirty(renderer);
                foreach(string property in new[]{"_MainTex","_EdgeMask"})if(savedMaterial.HasProperty(property)&&savedMaterial.GetTexture(property)!=null)Receipt(AssetDatabase.GetAssetPath(savedMaterial.GetTexture(property)));
                changes.Add(new{id,path,action=filter.name=="Registered native outer rock"?"Restored approved painted ceiling underside and inward facets; native Fill only at outer attachment":filter.name=="Continuous themed inner wall"?"Restored approved dark regional interior painting and depth tint":"Restored approved passage depth shadow",sourceMesh=meshPath,sourceMaterial=matPath,mesh=AssetDatabase.GetAssetPath(savedMesh),material=AssetDatabase.GetAssetPath(savedMaterial),vertexPositionsRestored=originalMesh.vertices.SequenceEqual(savedMesh.vertices)});
                if(filter.name=="Registered native outer rock")audits.Add(new{id,path,layer=MappingAudit(filter,savedMaterial.GetTexture("_EdgeMask")as Texture2D,1,savedMaterial.GetVector("_RimUV"),display,"Approved painted rim RGB and alpha")});
                if(filter.name=="Continuous themed inner wall")audits.Add(new{id,path,layer=MappingAudit(filter,savedMaterial.mainTexture as Texture2D,0,new Vector4(1,1,0,0),display,"Approved interior RGB")});
            }
        }
        bool collisionPreserved=collisionBefore==Collision(scene);if(!collisionPreserved)throw new Exception("Restoration changed collision unexpectedly");
        foreach(var pair in sources)if(Hash(pair.Key)!=pair.Value)throw new Exception("Source bytes changed: "+pair.Key);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string output=Evidence+"/"+region+"_approved_caves.json";File.WriteAllText(output,JsonConvert.SerializeObject(new{scene=scene.name,minCameraSize=camera,displayPPU=display,collisionPreserved,before,changes,sourceHashes=sources,densityAudits=audits,note="Painted rim and interior restored from retained approved assets. Native-density exceptions are explicit in densityAudits; no source upscaling or substitution is used to hide them."},Formatting.Indented));
        return JsonConvert.SerializeObject(new{scene=scene.name,changes=changes.Count,auditedPaintedLayers=audits.Count,collisionPreserved,output});
    }

    // Optional small physical bevel, kept separate for root's before/after review.
    // Only exposed convex upper corners change; concave floor joins stay fixed.
    public static string RoundUpperCornersCurrent(float radius=.18f)
    {
        var scene=SceneManager.GetActiveScene();string region=Region(scene);Directory.CreateDirectory(Evidence);
        if(radius<=0||radius>.25f)throw new Exception("Use a bounded corner radius up to .25u");
        var records=new List<object>();
        foreach(var terrain in All<MraTerrain>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.solid&&t.surface.Length>2&&t.top!=null&&AssetDatabase.GetAssetPath(t.top).Contains("WorldRefresh_20261104")))
        {
            var old=terrain.surface;var output=new List<Vector2>{old[0]};int corners=0;
            for(int i=1;i<old.Length-1;i++)
            {
                var p=old[i];var a=p-old[i-1];var b=old[i+1]-p;
                bool upToRight=Mathf.Abs(a.x)<.0001f&&a.y>.5f&&b.x>.5f&&Mathf.Abs(b.y)<.0001f;
                bool rightToDown=a.x>.5f&&Mathf.Abs(a.y)<.0001f&&Mathf.Abs(b.x)<.0001f&&b.y<-.5f;
                if(!upToRight&&!rightToDown){output.Add(p);continue;}
                // A cave cap can round only where at least .08u of solid roof
                // remains above the unchanged aperture after the full radius.
                if((terrain.caveOpenings??Array.Empty<MraTerrain.CaveOpening>()).Any(h=>h.outline!=null&&h.outline.Length>0&&p.x>=h.outline.Min(v=>v.x)-.25f&&p.x<=h.outline.Max(v=>v.x)+.25f&&p.y<=h.outline.Max(v=>v.y)+radius+.08f)){output.Add(p);continue;}
                float r=Mathf.Min(radius,Mathf.Min(a.magnitude,b.magnitude)*.2f);var start=p-a.normalized*r;var end=p+b.normalized*r;output.Add(start);
                for(int k=1;k<=6;k++){float t=k/6f;output.Add((1-t)*(1-t)*start+2*(1-t)*t*p+t*t*end);}corners++;
            }
            output.Add(old.Last());if(corners==0)continue;
            terrain.surface=output.ToArray();terrain.lipOverhang=0;terrain.Rebuild();EditorUtility.SetDirty(terrain);
            records.Add(new{path=Hier(terrain.transform),corners,radius,before=old.Select(p=>new[]{p.x,p.y}).ToArray(),after=terrain.surface.Select(p=>new[]{p.x,p.y}).ToArray(),endpointsPreserved=old[0]==terrain.surface[0]&&old.Last()==terrain.surface.Last()});
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string outputPath=Evidence+"/"+region+"_rounded_corners.json";File.WriteAllText(outputPath,JsonConvert.SerializeObject(new{scene=scene.name,radius,records,requiresMapRefresh=records.Count>0,requiresCollisionAndTraversalCheck=records.Count>0},Formatting.Indented));return outputPath;
    }
}

// STAGED: root imports only after the live fixture ends. Run AFTER entrance moves.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class WorldRevisionSurfaceJoins
{
    const string Evidence="Tools/Design/WorldRevision_20261104/Surface";
    const string Generated="Assets/Development/Layouts/WorldRevision_20261104/WalkingJoins/";
    static IEnumerable<T> All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static string Hier(Transform t){var p=new List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
    static string Ready(Scene scene)
    {
        if(EditorApplication.isPlaying||scene.isDirty)throw new Exception("Saved edit-mode scene required");
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var r=world["regions"].SingleOrDefault(n=>(string)n["scene"]==scene.name);if(r==null)throw new Exception("Open one of seven upper regions");
        Directory.CreateDirectory(Evidence);return (string)r["id"];
    }
    public static string BlendWalkingEndsCurrent()
    {
        var scene=SceneManager.GetActiveScene();string region=Ready(scene);var shader=Shader.Find("Qolossal/WorldRevision/Native Walking Join");if(shader==null)throw new Exception("Import NativeWalkingJoin.shader first");
        string folder=Generated+region+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var records=new List<object>();
        foreach(var point in All<CaveConnectionPoint>(scene).Where(p=>p.isActiveAndEnabled&&!p.undergroundSide))
        {
            var group=point.transform.Find("Regional fitted passage");var filter=group?.Find("Native walking surface")?.GetComponent<MeshFilter>();var renderer=filter?.GetComponent<MeshRenderer>();
            if(filter==null||filter.sharedMesh==null||renderer==null||!renderer.enabled)continue;
            var material=new Material(renderer.sharedMaterial){name=point.lowerRoomId+" native walking join",shader=shader};
            var vertices=filter.sharedMesh.vertices.Select(p=>filter.transform.TransformPoint(p)).ToArray();float min=vertices.Min(p=>p.x),max=vertices.Max(p=>p.x),blend=Mathf.Min(.3f,(max-min)*.08f);
            material.SetVector("_WorldBounds",new Vector4(min,max,blend,0));
            string path=folder+point.lowerRoomId+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved==null){AssetDatabase.CreateAsset(material,path);saved=material;}else{EditorUtility.CopySerialized(material,saved);Object.DestroyImmediate(material);EditorUtility.SetDirty(saved);}
            renderer.sharedMaterial=saved;EditorUtility.SetDirty(renderer);
            records.Add(new{id=point.lowerRoomId,path=Hier(filter.transform),minWorldX=min,maxWorldX=max,blendWorldUnits=blend,sourceUVAndVertexColorUnchanged=true,collisionUnchanged=true});
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string report=Evidence+"/"+region+"_walking_joins.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,records,note="Preserves native painted walking UV and approved inward cave art. Only the last .3u of each overlaid strip fades into underlying terrain or painted inward wall."},Formatting.Indented));return report;
    }
    public static string RoundSlopedCornersCurrent(float radius=.18f)
    {
        var scene=SceneManager.GetActiveScene();string region=Ready(scene);if(radius<=0||radius>.18f)throw new Exception("Bounded radius <= .18u required");var records=new List<object>();
        foreach(var terrain in All<MraTerrain>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.solid&&t.surface.Length>2&&t.top!=null&&AssetDatabase.GetAssetPath(t.top).Contains("WorldRefresh_20261104")))
        {
            var old=terrain.surface;var output=new List<Vector2>{old[0]};int corners=0;
            for(int i=1;i<old.Length-1;i++)
            {
                var p=old[i];var a=p-old[i-1];var b=old[i+1]-p;
                bool upToSlope=Mathf.Abs(a.x)<.0001f&&a.y>.5f&&b.x>.5f&&Mathf.Abs(b.y)<=b.x;
                bool slopeToDown=a.x>.5f&&Mathf.Abs(a.y)<=a.x&&Mathf.Abs(b.x)<.0001f&&b.y<-.5f;
                if((!upToSlope&&!slopeToDown)||a.x*b.y-a.y*b.x>=0){output.Add(p);continue;}
                if((terrain.caveOpenings??Array.Empty<MraTerrain.CaveOpening>()).Any(h=>h.outline!=null&&h.outline.Length>0&&p.x>=h.outline.Min(v=>v.x)-.25f&&p.x<=h.outline.Max(v=>v.x)+.25f&&p.y<=h.outline.Max(v=>v.y)+radius+.08f)){output.Add(p);continue;}
                float r=Mathf.Min(radius,Mathf.Min(a.magnitude,b.magnitude)*.2f);var start=p-a.normalized*r;var end=p+b.normalized*r;output.Add(start);
                for(int k=1;k<=6;k++){float t=k/6f;output.Add((1-t)*(1-t)*start+2*(1-t)*t*p+t*t*end);}corners++;
            }
            output.Add(old.Last());if(corners==0)continue;terrain.surface=output.ToArray();terrain.lipOverhang=0;terrain.Rebuild();EditorUtility.SetDirty(terrain);
            records.Add(new{path=Hier(terrain.transform),corners,radius,before=old.Select(p=>new[]{p.x,p.y}).ToArray(),after=terrain.surface.Select(p=>new[]{p.x,p.y}).ToArray(),endpointsPreserved=old[0]==terrain.surface[0]&&old.Last()==terrain.surface.Last()});
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string report=Evidence+"/"+region+"_rounded_sloped_corners.json";
        // Preserve nonempty first-pass evidence when this idempotent pass is repeated.
        if(records.Count>0||!File.Exists(report))File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,radius,records,requiresMapRefresh=records.Count>0,requiresCollisionAndTraversalCheck=records.Count>0},Formatting.Indented));return report;
    }
    public static string EnableNativeTopJoinsCurrent()
    {
        var scene=SceneManager.GetActiveScene();string region=Ready(scene);var reviewedPaths=new HashSet<string>();
        foreach(string suffix in new[]{"_rounded_corners.json","_rounded_sloped_corners.json"})
        {
            string receipt=Evidence+"/"+region+suffix;if(!File.Exists(receipt))continue;
            foreach(var record in JObject.Parse(File.ReadAllText(receipt))["records"]??new JArray())reviewedPaths.Add((string)record["path"]);
        }
        var records=new List<object>();
        foreach(var terrain in All<MraTerrain>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.solid&&t.top!=null&&AssetDatabase.GetAssetPath(t.top).Contains("WorldRefresh_20261104")))
        {
            string path=Hier(terrain.transform);bool cave=(terrain.caveOpenings??Array.Empty<MraTerrain.CaveOpening>()).Length>0;if(!cave&&!reviewedPaths.Contains(path))continue;
            var surface=terrain.surface.ToArray();var collider=terrain.GetComponent<PolygonCollider2D>();string before=collider!=null?EditorJsonUtility.ToJson(collider):"";
            terrain.nativeStripEndBlend=.25f;terrain.Rebuild();EditorUtility.SetDirty(terrain);
            if(!surface.SequenceEqual(terrain.surface)||(collider!=null&&before!=EditorJsonUtility.ToJson(collider)))throw new Exception("Optical top join changed geometry: "+path);
            records.Add(new{path,blendWorldUnits=.25f,scope=cave?"Fitted cave host":"Reviewed rounded convex corner",collisionAndSurfacePreserved=true});
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string output=Evidence+"/"+region+"_native_top_joins.json";File.WriteAllText(output,JsonConvert.SerializeObject(new{scene=scene.name,records,defaultOff=true,sourcePixelsAndUVScalePreserved=true},Formatting.Indented));return output;
    }
}

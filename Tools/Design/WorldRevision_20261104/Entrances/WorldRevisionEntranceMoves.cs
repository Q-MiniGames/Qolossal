// STAGED OUTSIDE ASSETS: root copies/imports only when the live test has ended.
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
using Object=UnityEngine.Object;

public static class WorldRevisionEntranceMoves
{
    const string Evidence="Tools/Design/WorldRevision_20261104/Entrances";
    const string Generated="Assets/Development/Layouts/WorldRevision_20261104/RelocatedEntrances/";
    static IEnumerable<T> All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static string Hier(Transform t){var p=new List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
    static string Key(string s){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","").Substring(0,20);}
    static void Ready(Scene s){if(EditorApplication.isPlaying)throw new Exception("Stop the live test first");if(s.isDirty)throw new Exception("Preserve unrelated unsaved scene changes first");}
    static void Backup(Scene s){Directory.CreateDirectory(Evidence+"/Before");string path=Evidence+"/Before/"+s.name+".unity";if(!File.Exists(path))File.Copy(s.path,path);}
    static string Invariant(CaveConnectionPoint p)
    {
        object destination=p.entrance is MraChamberDoor d?(object)new{type="chamber",d.chamberId,d.chamberScene,d.arrivalId,d.prerequisite}:p.entrance is MraUgPortal u?(object)new{type="underground",u.targetScene,u.arrivalId,u.requires,u.unlockFlag,u.discoveredFlag}:p.entrance is MraExit e?(object)new{type="exit",e.targetScene,e.arrivalId,e.requires}:throw new Exception("Unsupported entrance type");
        return JsonConvert.SerializeObject(new{p.connectionId,p.upperScene,p.upperRegion,p.lowerRoomId,p.lowerRoomName,p.requirements,p.ArrivalId,destination});
    }
    static float Ground(MraTerrain t,float worldX,bool lower)
    {
        float x=t.transform.InverseTransformPoint(new Vector3(worldX,0,0)).x;var hits=new List<float>();
        for(int i=0;i<t.surface.Length-1;i++){var a=t.surface[i];var b=t.surface[i+1];if(b.x-a.x>.00001f&&x>=a.x-.0001f&&x<=b.x+.0001f){float y=Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x));hits.Add(t.transform.TransformPoint(new Vector3(x,y,0)).y);}}
        if(hits.Count==0)throw new Exception("No natural terrain under x="+worldX);return lower?hits.Min():hits.Max();
    }
    static Mesh Save(string path,Mesh mesh)
    {
        mesh.hideFlags=HideFlags.None;var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;
    }
    static float Area(Vector2[] p){float a=0;for(int i=0;i<p.Length;i++){var q=p[(i+1)%p.Length];a+=p[i].x*q.y-q.x*p[i].y;}return a*.5f;}
    public static string ApplyCurrent()
    {
        var scene=SceneManager.GetActiveScene();Ready(scene);
        string id=scene.name=="MRAtlas_MR02_Causeway"?"PC04":scene.name=="MRAtlas_MR04_Qvale"?"A01":null;if(id==null)throw new Exception("Open Causeway or Qvale");
        string marker="World revision | relocated "+id+" 20261104";if(scene.GetRootGameObjects().Any(g=>g.name==marker))return "Already relocated "+id;
        var point=All<CaveConnectionPoint>(scene).Single(p=>p.isActiveAndEnabled&&!p.undergroundSide&&p.lowerRoomId==id);
        var presentation=point.entrance.GetComponent<CaveThresholdPresentation>();if(presentation==null||point.arrival is not MraArrival)throw new Exception("Expected preserved cave threshold and named arrival");
        var arrival=(MraArrival)point.arrival;var group=point.transform.Find("Regional fitted passage");var hill=point.GetComponentsInChildren<MraTerrain>(true).Single(t=>t.name=="Native hill | "+id);
        var rim=group!=null?group.Find("Registered native outer rock")?.GetComponent<MeshRenderer>():null;
        if(rim==null||rim.sharedMaterial.shader.name!="Qolossal/WorldRevision/Approved Rim Native Attachment")throw new Exception("Restore approved cave artwork before relocation");
        float oldMouth=id=="PC04"?81:142.6f,oldFloor=id=="PC04"?30:8;
        if(Vector2.Distance(presentation.mouth,new Vector2(oldMouth,oldFloor))>.02f||presentation.inward>=0)throw new Exception("Source mouth differs from audited mound placement");
        if(point.transform.position.sqrMagnitude>.0001f||Vector3.Distance(point.transform.lossyScale,Vector3.one)>.001f)throw new Exception("Expected identity source assembly transform");
        var opening=hill.caveOpenings.Single(h=>h.roomId==id);if(opening.outline==null||opening.outline.Length<3)throw new Exception("Missing mound aperture");
        var originalPolygon=opening.outline.Select(p=>(Vector2)hill.transform.TransformPoint(p)).ToArray();
        string invariant=Invariant(point);string oldParent=Hier(point.transform.parent);var oldPortal=point.EntrancePosition;var oldArrival=point.ArrivalPosition;
        string targetName=id=="PC04"?"Route MR02 piece 00":"Route MR04 piece 00";var terrain=All<MraTerrain>(scene).Single(t=>t.name==targetName&&t.gameObject.activeInHierarchy);var originalSurface=terrain.surface.ToArray();
        if(terrain.caveOpenings.Any(h=>h.roomId==id))throw new Exception("Target route already has this aperture; review before applying twice");
        float mouth=id=="PC04"?88.5f:21.178f,floor=id=="PC04"?30:20.625f,inward=id=="PC04"?1:-1;
        Vector2 Map(Vector2 p)=>id=="PC04"?new Vector2(169.5f-p.x,p.y):p+new Vector2(-121.422f,12.625f);
        var newWorldPolygon=originalPolygon.Select(Map).ToArray();var localPolygon=newWorldPolygon.Select(p=>(Vector2)terrain.transform.InverseTransformPoint(p)).ToArray();if(Area(localPolygon)<0)Array.Reverse(localPolygon);
        Backup(scene);Directory.CreateDirectory(Generated+id);AssetDatabase.Refresh();
        hill.gameObject.SetActive(false);EditorUtility.SetDirty(hill.gameObject);
        if(id=="PC04"){point.transform.position=new Vector3(169.5f,0,0);point.transform.localScale=new Vector3(-1,1,1);}
        else point.transform.position=new Vector3(-121.422f,12.625f,0);
        terrain.caveOpenings=terrain.caveOpenings.Concat(new[]{new MraTerrain.CaveOpening{roomId=id,outline=localPolygon}}).ToArray();terrain.Rebuild();EditorUtility.SetDirty(terrain);
        if(!terrain.surface.SequenceEqual(originalSurface))throw new Exception("Relocation changed the natural terrain profile");
        var meshRecords=new List<object>();
        foreach(var filter in group.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||!filter.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
            var source=filter.sharedMesh;var mesh=Object.Instantiate(source);mesh.name=source.name+" "+id+" natural wall";var vertices=mesh.vertices;var uv=mesh.uv;var boundary=new List<Vector4>();mesh.GetUVs(2,boundary);int movedVertices=0;
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var texture=block.GetTexture("_MainTex")as Texture2D??renderer.sharedMaterial.mainTexture as Texture2D;var importer=texture!=null?AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture))as TextureImporter:null;
            int sourceWidth=texture!=null?texture.width:1,sourceHeight=texture!=null?texture.height:1;if(importer!=null)importer.GetSourceTextureWidthAndHeight(out sourceWidth,out sourceHeight);
            for(int i=0;i<vertices.Length;i++)
            {
                var w=filter.transform.TransformPoint(vertices[i]);float d=(w.x-mouth)*inward;
                // Qvale's exterior descends from the cliff foot. Warp only the
                // exterior continuation to that exact existing walkline, so its
                // painting never floats above the player/collider slope.
                if(id=="A01"&&d<-.00001f){float delta=Ground(terrain,w.x,true)-floor;w.y+=delta;vertices[i]=filter.transform.InverseTransformPoint(w);if(Mathf.Abs(delta)>.00001f)movedVertices++;}
                if(boundary.Count==vertices.Length){var value=boundary[i];value.x=Ground(terrain,w.x,false)-w.y;boundary[i]=value;}
                if(filter.name=="Native walking surface"&&uv.Length==vertices.Length&&importer!=null)uv[i].x=w.x*importer.spritePixelsPerUnit/sourceWidth;
            }
            mesh.vertices=vertices;if(boundary.Count==vertices.Length)mesh.SetUVs(2,boundary);if(filter.name=="Native walking surface"&&uv.Length==vertices.Length)mesh.uv=uv;mesh.RecalculateBounds();
            string output=Generated+id+"/"+Key(Hier(filter.transform))+".asset";filter.sharedMesh=Save(output,mesh);EditorUtility.SetDirty(filter);meshRecords.Add(new{path=Hier(filter.transform),sourceMesh=AssetDatabase.GetAssetPath(source),mesh=output,movedExteriorVertices=movedVertices,approvedRimUV1Preserved=true});
        }
        float portalX=id=="PC04"?91.85f:17.828f,arrivalX=id=="PC04"?86.9f:22.778f,arrivalGround=Ground(terrain,arrivalX,true);
        point.entrance.transform.position=new Vector3(portalX,floor,point.entrance.transform.position.z);arrival.transform.position=new Vector3(arrivalX,arrivalGround+.66f,arrival.transform.position.z);arrival.facing=-inward;
        presentation.mouth=new Vector2(mouth,floor);presentation.inward=inward;presentation.emergenceFloorRise=floor-arrivalGround;presentation.connection=point;
        if(point.entrance is MraChamberDoor chamber){chamber.walkThroughEntrance=true;chamber.walkThroughDirection=inward;EditorUtility.SetDirty(chamber);}else if(point.entrance is MraUgPortal portal){portal.walkThroughEntrance=true;portal.walkThroughDirection=inward;EditorUtility.SetDirty(portal);}else if(point.entrance is MraExit exit){exit.direction=inward;exit.groundOffset=0;EditorUtility.SetDirty(exit);}
        point.activation=inward>0?"Walk right into the recessed cave passage":"Walk left into the recessed cave passage";
        if(id=="A01")
        {
            var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var node=world["regions"].Single(r=>(string)r["id"]=="MR04")["nodes"].Single(n=>(string)n["id"]=="MR04_N00");
            point.upperAreaId="MR04_N00";point.upperAreaName=(string)node["name"];
            var area=All<Transform>(scene).Single(t=>t.name.StartsWith("[MR04_N00]",StringComparison.Ordinal));var caveParent=area.Find("Cave access | paired underground rooms");if(caveParent==null){caveParent=new GameObject("Cave access | paired underground rooms").transform;caveParent.SetParent(area,false);}
            point.transform.SetParent(caveParent,true);point.name="[CAVE A01] MR04_N00 <-> Underground - "+point.lowerRoomName;
        }
        EditorUtility.SetDirty(point);EditorUtility.SetDirty(point.transform);EditorUtility.SetDirty(point.entrance.transform);EditorUtility.SetDirty(arrival);EditorUtility.SetDirty(arrival.transform);EditorUtility.SetDirty(presentation);
        if(Invariant(point)!=invariant)throw new Exception("Persistent entrance identity, predicates or destination changed");
        Physics2D.SyncTransforms();var support=Physics2D.RaycastAll(new Vector2(arrivalX,arrivalGround+.1f),Vector2.down,.25f,LayerMask.GetMask("Ground"));bool grounded=support.Any(h=>h.collider==terrain.GetComponent<PolygonCollider2D>()&&Mathf.Abs(h.point.y-arrivalGround)<.025f);if(!grounded)throw new Exception("Arrival is not supported by the actual natural route collider");
        for(float d=.3f;d<4.4f;d+=.5f)if(terrain.GetComponent<PolygonCollider2D>().OverlapPoint(new Vector2(mouth+inward*d,floor+1.1f)))throw new Exception("New aperture body clearance failed at depth "+d);
        new GameObject(marker);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string report=Evidence+"/"+id+"_relocated.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{id,scene=scene.name,oldParent,newParent=Hier(point.transform.parent),oldPortal=new[]{oldPortal.x,oldPortal.y},newPortal=new[]{point.EntrancePosition.x,point.EntrancePosition.y},oldArrival=new[]{oldArrival.x,oldArrival.y},newArrival=new[]{point.ArrivalPosition.x,point.ArrivalPosition.y},mouth=new[]{mouth,floor},inward,arrivalFacing=arrival.facing,arrivalGround,presentation.emergenceFloorRise,originalAperture=originalPolygon.Select(p=>new[]{p.x,p.y}).ToArray(),newAperture=newWorldPolygon.Select(p=>new[]{p.x,p.y}).ToArray(),artificialHillDisabled=!hill.gameObject.activeSelf,naturalSurfacePreserved=terrain.surface.SequenceEqual(originalSurface),persistentInvariantsPreserved=true,grounded,meshRecords,pending=id=="A01"?"Run SyncA01UndergroundCurrent in saved underground scene, then refresh scene-derived map and authoring placement metadata":"Refresh scene-derived map and authoring placement metadata"},Formatting.Indented));
        return report;
    }
    public static string SyncA01UndergroundCurrent()
    {
        var scene=SceneManager.GetActiveScene();Ready(scene);if(scene.name!=RegionalSceneCatalog.Underground)throw new Exception("Open saved consolidated underground scene");
        var point=All<CaveConnectionPoint>(scene).Single(p=>p.isActiveAndEnabled&&p.undergroundSide&&p.lowerRoomId=="A01");string invariant=Invariant(point);var beforePosition=point.transform.position;Backup(scene);
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var node=world["regions"].Single(r=>(string)r["id"]=="MR04")["nodes"].Single(n=>(string)n["id"]=="MR04_N00");
        point.upperAreaId="MR04_N00";point.upperAreaName=(string)node["name"];EditorUtility.SetDirty(point);
        if(Invariant(point)!=invariant||point.transform.position!=beforePosition)throw new Exception("Underground synchronization changed gameplay identity or placement");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string report=Evidence+"/A01_underground_area_sync.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,point.connectionId,point.lowerRoomId,point.upperAreaId,point.upperAreaName,positionUnchanged=true,persistentInvariantsPreserved=true},Formatting.Indented));return report;
    }
}

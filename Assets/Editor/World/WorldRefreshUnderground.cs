using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Bounded art and ordinary-block polish. Never rebuilds actors, puzzles, portals or room bounds.
public static class WorldRefreshUnderground
{
    public const string ScenePath = "Assets/Scenes/Game/Qolossal_Underground.unity";
    const string ArtRoot = "Assets/Art/Prototypes/WorldRefresh_20261104/Underground/";
    const string Output = "Tools/Design/WorldRefresh_20261104/Underground";
    static readonly string[] Sets = { "A", "C", "D", "E", "H", "MR01", "MR02", "MR03", "MR05", "MR06", "MR07", "P" };

    // Keep the exact original outside envelope. A small corner round removes corner catches
    // without widening a jump, raising its walk line or smoothing a deliberately steep wall.
    public static bool RoundOrdinary(BoxCollider2D box, string roomId)
    {
        if (box == null || box.isTrigger || box.usedByEffector || box.attachedRigidbody != null || roomId == "HEART" || box.edgeRadius > 0) return false;
        if (!box.name.StartsWith("UGP solid ") && !box.name.StartsWith("UGP rock ")) return false;
        const float radius = .06f;
        if (box.size.x < 1f || box.size.y < .6f) return false;
        box.size -= Vector2.one * (2 * radius); box.edgeRadius = radius;
        return true;
    }

    public static object Apply(bool save = true)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
        if (Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Preserve unsaved scenes before applying.");
        Directory.CreateDirectory(Output + "/Before");
        string backup = Output + "/Before/Qolossal_Underground.unity";
        if (!File.Exists(backup)) File.Copy(ScenePath,backup);
        foreach (string set in Sets) ImportVista(set);
        ImportTexture(ArtRoot+"Heart_Detail.png");
        UndergroundArtKit.Forget();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var rooms = Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(r=>r.roomId).ToArray();
        if (rooms.Length != 89 || rooms.Select(r=>r.roomId).Distinct().Count()!=89) throw new Exception("Expected the canonical 89 rooms.");
        var changes = new List<object>();
        foreach (var room in rooms)
        {
            var group=room.GetComponentInChildren<UndergroundPlatformingGroup>(true);
            if(group==null) throw new Exception("Missing platforming group: "+room.roomId);
            int rounded=0,painted=0;
            foreach(var box in group.GetComponentsInChildren<BoxCollider2D>(true))
            {
                var oldSize=box.size; float oldRadius=box.edgeRadius;
                Vector2 envelope=oldSize+Vector2.one*(oldRadius*2);
                if(RoundOrdinary(box,room.roomId))
                {
                    Vector2 after=box.size+Vector2.one*(box.edgeRadius*2);
                    if((after-envelope).sqrMagnitude>1e-8f) throw new Exception("Collider envelope changed: "+box.name);
                    rounded++;
                }
                if(!box.name.StartsWith("UGP "))continue;
                string kind=box.name.Substring(4).Split(' ')[0];
                if(kind=="safe" || kind=="look")continue;
                var art=box.transform.Find("Art")?.GetComponent<SpriteRenderer>();
                if(art!=null && UndergroundArtKit.Dress(art,room.roomId,kind,envelope,Mathf.RoundToInt(box.transform.position.x*7+box.transform.position.y*13)))painted++;
            }
            var far=group.transform.Find("UGP backdrop_far")?.GetComponent<SpriteRenderer>();
            var near=group.transform.Find("UGP backdrop_near")?.GetComponent<SpriteRenderer>();
            if(far==null || near==null)
            {
                // DressRoom owns only its backdrop/prop children; replacing these is idempotent.
                foreach(Transform child in group.transform.Cast<Transform>().ToArray())
                    if(child.name.StartsWith("UGP backdrop_") || child.name.StartsWith("UGP prop "))Object.DestroyImmediate(child.gameObject);
                UndergroundArtKit.DressRoom(room,group.transform);
                far=group.transform.Find("UGP backdrop_far")?.GetComponent<SpriteRenderer>();
                near=group.transform.Find("UGP backdrop_near")?.GetComponent<SpriteRenderer>();
            }
            if(far==null)throw new Exception("Missing far backdrop: "+room.roomId);
            string set=UndergroundArtKit.SetOf(room.roomId);
            ConfigureVista(far,set);
            foreach(var quarter in group.GetComponentsInChildren<SpriteRenderer>(true))
                if(quarter.transform.parent!=null && quarter.transform.parent.name.StartsWith("UGP heart quarter "))ConfigureHeart(quarter);
            if(near!=null)
            {
                // The original near texture is deliberately subtle; preserve readable vistas behind it.
                near.color=new Color(1f,1f,1f,.22f);
                EnsureDensity(near.sprite);
                ConfigureRockField(near,.12f);
            }
            changes.Add(new {room=room.roomId,set,rounded,painted});
        }
        // Connectors keep their original opaque stone field; their density is corrected too.
        foreach(var art in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(art.gameObject.scene==scene && art.name=="UGP passage backdrop"){EnsureDensity(art.sprite);ConfigureRockField(art,.3f);}
        Physics2D.SyncTransforms();
        File.WriteAllText(Output+"/application.json",JsonConvert.SerializeObject(changes,Formatting.Indented));
        EditorSceneManager.MarkSceneDirty(scene);
        if(save){EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
        return Audit();
    }

    static void ImportVista(string set)
    {
        string path=ArtRoot+set+"_Vista.png";
        ImportTexture(path);
    }
    static void ImportTexture(string path)
    {
        if(!File.Exists(path))throw new FileNotFoundException("Native scenic asset missing",path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.alphaSource=TextureImporterAlphaSource.None;
        imp.mipmapEnabled=true;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;
        imp.textureCompression=TextureImporterCompression.Uncompressed;imp.isReadable=false;
        imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();
    }
    static void EnsureDensity(Sprite sprite)
    {
        if(sprite==null)return;
        var path=AssetDatabase.GetAssetPath(sprite);var imp=AssetImporter.GetAtPath(path) as TextureImporter;
        if(imp==null || Mathf.Approximately(imp.spritePixelsPerUnit,216f))return;
        imp.spritePixelsPerUnit=216f;imp.SaveAndReimport();
    }
    public static void ConfigureHeart(SpriteRenderer art)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot+"Heart_Detail.png");
        if(texture==null)return;
        var surface=art.GetComponent<WorldRefreshHeartSurface>()??art.gameObject.AddComponent<WorldRefreshHeartSurface>();
        surface.detail=texture;surface.pixelsPerUnit=216f;surface.Refresh();
    }
    public static void ConfigureRoomArt(UndergroundRoom room,Transform group)
    {
        var far=group.Find("UGP backdrop_far")?.GetComponent<SpriteRenderer>();
        var near=group.Find("UGP backdrop_near")?.GetComponent<SpriteRenderer>();
        if(far!=null)ConfigureVista(far,UndergroundArtKit.SetOf(room.roomId));
        if(near!=null){near.color=new Color(1,1,1,.22f);ConfigureRockField(near,.12f);}
    }
    static void ConfigureVista(SpriteRenderer far,string set)
    {
        MakeBackdropQuad(far);
        var vista=far.GetComponent<WorldRefreshCaveVista>()??far.gameObject.AddComponent<WorldRefreshCaveVista>();
        vista.vista=AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot+set+"_Vista.png");
        vista.pixelsPerUnit=216f;vista.strength=.78f;vista.centerBelowCamera=.75f;vista.Refresh();
    }

    static void MakeBackdropQuad(SpriteRenderer art)
    {
        if(art==null||art.sprite==null||art.drawMode==SpriteDrawMode.Simple)return;
        var before=art.bounds;var size=art.size;var scale=art.transform.localScale;var native=art.sprite.bounds.size;
        art.drawMode=SpriteDrawMode.Simple;
        art.transform.localScale=new Vector3(scale.x*size.x/native.x,scale.y*size.y/native.y,scale.z);
        var after=art.bounds;
        if((before.center-after.center).sqrMagnitude>.0001f||(before.size-after.size).sqrMagnitude>.01f)
            throw new Exception("Backdrop quad changed envelope: "+art.name);
    }
    public static void ConfigureRockField(SpriteRenderer art,float rate)
    {
        MakeBackdropQuad(art);
        var field=art.GetComponent<WorldRefreshCaveRockField>()??art.gameObject.AddComponent<WorldRefreshCaveRockField>();
        field.pixelsPerUnit=216;field.parallaxRate=rate;field.Refresh();
    }
    public static object UpgradeBackdropMeshes(bool save=true)
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open underground in edit mode");
        var scene=SceneManager.GetActiveScene();var changed=new List<object>();int converted=0;
        foreach(var art in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.gameObject.scene==scene))
        {
            if(art.name!="UGP passage backdrop"&&art.name!="UGP backdrop_near"&&art.name!="UGP backdrop_far")continue;
            var bounds=art.bounds;bool wasTiled=art.drawMode==SpriteDrawMode.Tiled;if(wasTiled)converted++;
            if(art.GetComponent<WorldRefreshCaveVista>()!=null){MakeBackdropQuad(art);art.GetComponent<WorldRefreshCaveVista>().Refresh();}
            else ConfigureRockField(art,art.name=="UGP backdrop_near"?.12f:.3f);
            changed.Add(new {art.name,wasTiled,width=bounds.size.x,height=bounds.size.y,mode=art.drawMode.ToString(),nativePPU=216});
        }
        EditorSceneManager.MarkSceneDirty(scene);if(save)EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output+"/backdrop_quads.json",JsonConvert.SerializeObject(changed,Formatting.Indented));
        return new {count=changed.Count,converted,changes=changed};
    }

    static (Vector2 size,Vector2 offset) PlayerBody()
    {
        var player=Object.FindFirstObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        var box=player==null?null:player.GetComponent<BoxCollider2D>();
        if(box==null)return (new Vector2(.8f,1.2f),Vector2.zero);
        var scale=box.transform.lossyScale;
        return (Vector2.Scale(box.size,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y))),Vector2.Scale(box.offset,scale));
    }

    // Repair only the eight confirmed baseline arrival faults. Does not move doors or puzzle geometry.
    public static object RepairLandings(bool save=true)
    {
        var targets=new Dictionary<string,Vector2> {
            {"D04",new Vector2(18,1.86f)}, {"E05",new Vector2(29.5f,21.66f)},
            {"HEART",new Vector2(20,12.66f)}, {"MR01_C05",new Vector2(8,.66f)},
            {"MR02_C05",new Vector2(7.5f,.66f)}, {"MR05_C02",new Vector2(1.35f,.66f)},
            {"MR06_C02",new Vector2(1.35f,.66f)}, {"MR07_C02",new Vector2(1.35f,.66f)} };
        return RepairTargets(targets,new HashSet<string>(targets.Keys),new HashSet<string>{"D04","E05","HEART","MR01_C05","MR02_C05"},"landing_repairs.json",save);
    }
    public static object RepairLiveLandings(bool save=true)
    {
        var targets=new Dictionary<string,Vector2> {
            {"A04",new Vector2(12,.66f)}, {"D01",new Vector2(2,.66f)},
            {"E10",new Vector2(4,.66f)}, {"MR06_C01",new Vector2(15,.66f)},
            {"MR06_C03",new Vector2(7,.66f)}, {"MR06_C06",new Vector2(3,.66f)}, {"PC11",new Vector2(2,.66f)} };
        return RepairTargets(targets,new HashSet<string>{"A04","E10"},new HashSet<string>(targets.Keys),"live_landing_repairs.json",save);
    }

    static object RepairTargets(Dictionary<string,Vector2> targets,HashSet<string> arrivalRooms,HashSet<string> checkpointRooms,string reportFile,bool save)
    {
        if(EditorApplication.isPlaying || SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open underground in edit mode first.");
        var scene=SceneManager.GetActiveScene();var body=PlayerBody();
        var ground=new ContactFilter2D {useLayerMask=true,layerMask=LayerMask.GetMask("Ground"),useTriggers=false};
        var all=new ContactFilter2D {useTriggers=true};
        var repaired=new List<object>();var moves=new List<Action>();
        foreach(var room in Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(!targets.TryGetValue(room.roomId,out var local))continue;
            Vector2 wanted=(Vector2)room.WorldBounds.min+local;
            bool found=false;Vector2 chosen=default;
            foreach(float dx in new[]{0f,-.25f,.25f,-.5f,.5f,-1f,1f})
            {
                var rays=new List<RaycastHit2D>();
                Physics2D.Raycast(wanted+new Vector2(dx,2f),Vector2.down,ground,rays,5f);
                foreach(var hit in rays.Where(h=>h.collider!=null && h.normal.y>.6f && h.collider.attachedRigidbody==null))
                {
                    Vector2 candidate=hit.point+Vector2.up*(body.size.y*.5f+.06f)-body.offset;
                    if(!room.WorldBounds.Contains(new Vector3(candidate.x,candidate.y,room.WorldBounds.center.z)))continue;
                    var overlaps=new List<Collider2D>();
                    Physics2D.OverlapBox(candidate+body.offset,body.size-Vector2.one*.04f,0,ground,overlaps);
                    if(overlaps.Any(h=>!(h.TryGetComponent<PlatformEffector2D>(out var e)&&e.useOneWay)))continue;
                    overlaps.Clear();Physics2D.OverlapBox(candidate+body.offset,body.size,0,all,overlaps);
                    if(overlaps.Any(h=>h.isTrigger&&(h.GetComponent<Hazard>()||h.GetComponent<ThornHazard>()||h.GetComponent<MraTimedHazard>()||h.GetComponent<MraUgGust>())))continue;
                    bool supported=true;
                    foreach(float edge in new[]{-body.size.x*.5f+.05f,0f,body.size.x*.5f-.05f})
                    {
                        rays=new List<RaycastHit2D>();Physics2D.Raycast(candidate+new Vector2(edge,.15f),Vector2.down,ground,rays,1.2f);
                        if(!rays.Any(h=>h.normal.y>.6f && h.distance>.01f))supported=false;
                    }
                    if(!supported)continue;
                    found=true;chosen=candidate;break;
                }
                if(found)break;
            }
            if(!found)throw new Exception("No verified landing at targeted anchor for "+room.roomId);
            if(arrivalRooms.Contains(room.roomId))foreach(var arrival in room.GetComponentsInChildren<MraArrival>(true).Where(a=>a.gameObject.activeInHierarchy && a.arrivalId=="room:"+room.roomId))
            {
                var old=arrival.transform.position;var destination=new Vector3(chosen.x,chosen.y,old.z);moves.Add(()=>arrival.transform.position=destination);
                repaired.Add(new {room=room.roomId,id=arrival.arrivalId,fromX=old.x,fromY=old.y,toX=chosen.x,toY=chosen.y});
            }
            if(checkpointRooms.Contains(room.roomId))foreach(var checkpoint in room.GetComponentsInChildren<Checkpoint>(true).Where(c=>c.gameObject.activeInHierarchy && c.CheckpointId=="regional:"+room.roomId))
            {
                var old=checkpoint.SpawnPosition;var delta=(Vector3)(chosen-old);moves.Add(()=>checkpoint.transform.position+=delta);
                repaired.Add(new {room=room.roomId,id=checkpoint.CheckpointId,fromX=old.x,fromY=old.y,toX=chosen.x,toY=chosen.y});
            }
        }
        foreach(var move in moves)move();
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);if(save)EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/"+reportFile,JsonConvert.SerializeObject(repaired,Formatting.Indented));
        return new {repaired,audit=Audit()};
    }

    public static object Audit()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Open the underground scene for audit.");
        Physics2D.SyncTransforms();
        var scene=SceneManager.GetActiveScene();
        var rooms=Object.FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene).OrderBy(r=>r.roomId).ToArray();
        var ground=new ContactFilter2D {useLayerMask=true,layerMask=LayerMask.GetMask("Ground"),useTriggers=false};
        var report=new List<object>();var failures=new List<string>();var warnings=new List<string>();
        var body=PlayerBody();
        var connections=Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>c.gameObject.scene==scene && c.undergroundSide).ToArray();
        object Landing(string id,Vector2 center)
        {
            var hits=new List<Collider2D>();
            Physics2D.OverlapBox(center+body.offset,body.size-Vector2.one*.04f,0,ground,hits);
            var blocked=hits.Where(h=>h.enabled && h.gameObject.activeInHierarchy && !(h.TryGetComponent<PlatformEffector2D>(out var e)&&e.useOneWay)).Select(h=>h.name).ToArray();
            var support=new List<object>(); bool floor=true;
            foreach(float dx in new[]{-body.size.x*.5f+.05f,0f,body.size.x*.5f-.05f})
            {
                var rays=new List<RaycastHit2D>();
                Physics2D.Raycast(center+new Vector2(dx,.15f),Vector2.down,ground,rays,2f);
                var hit=rays.FirstOrDefault(h=>h.normal.y>.4f);
                bool okay=hit.collider!=null && hit.distance<1.2f;floor&=okay;
                support.Add(new {dx,okay,distance=hit.collider==null?-1:hit.distance,collider=hit.collider==null?"":hit.collider.name});
            }
            var all=new ContactFilter2D {useTriggers=true};hits.Clear();
            Physics2D.OverlapBox(center+body.offset,body.size-Vector2.one*.04f,0,all,hits);
            var hazards=hits.Where(h=>h.isTrigger && (h.GetComponent<Hazard>()!=null || h.GetComponent<ThornHazard>()!=null || h.GetComponent<MraTimedHazard>()!=null || h.GetComponent<MraUgGust>()!=null)).Select(h=>h.name).ToArray();
            if(blocked.Length>0 || hazards.Length>0 || !floor)failures.Add(id+": "+(blocked.Length>0?"body blocked ":"")+(hazards.Length>0?"hazard ":"")+(!floor?"floor support missing":""));
            return new {id,x=center.x,y=center.y,bodyClear=blocked.Length==0,blocked,floor,support,hazards};
        }
        foreach(var room in rooms)
        {
            var group=room.GetComponentInChildren<UndergroundPlatformingGroup>(true);
            var arrivals=room.GetComponentsInChildren<MraArrival>(true).Where(a=>a.gameObject.activeInHierarchy).Select(a=>Landing(a.arrivalId,a.transform.position)).ToArray();
            var checkpoints=room.GetComponentsInChildren<Checkpoint>(true).Where(c=>c.gameObject.activeInHierarchy).Select(c=>Landing(c.CheckpointId,c.SpawnPosition)).ToArray();
            var returns=connections.Where(c=>c.lowerRoomId==room.roomId).Select(c=>new {pair=c.connectionId,arrivalId=c.ArrivalId,entranceX=c.EntrancePosition.x,entranceY=c.EntrancePosition.y,landing=Landing("pair:"+c.connectionId,c.ArrivalPosition)}).ToArray();
            var vista=group==null?null:group.GetComponentInChildren<WorldRefreshCaveVista>(true);
            int greyboxes=group==null?0:group.GetComponentsInChildren<SpriteRenderer>(true).Count(a=>a.enabled&&a.gameObject.activeInHierarchy&&a.sprite!=null&&AssetDatabase.GetAssetPath(a.sprite).EndsWith("UGP_White.png"));
            if(greyboxes>0)warnings.Add(room.roomId+": "+greyboxes+" remaining greybox sprites (inspect debug/unsupported kinds)");
            if(vista==null||vista.vista==null)failures.Add(room.roomId+": missing native vista");
            report.Add(new {room=room.roomId,room.roomName,theme=UndergroundArtKit.SetOf(room.roomId),arrivals,checkpoints,returns,greyboxes,
                vista= vista==null||vista.vista==null?null:new {path=AssetDatabase.GetAssetPath(vista.vista),width=vista.vista.width,height=vista.vista.height,ppu=vista.pixelsPerUnit,worldWidth=vista.vista.width/vista.pixelsPerUnit,worldHeight=vista.vista.height/vista.pixelsPerUnit}});
        }
        var result=new {rooms=rooms.Length,returnCount=connections.Length,pairIds=connections.Select(c=>c.connectionId).Distinct().OrderBy(x=>x).ToArray(),playerBody=new {width=body.size.x,height=body.size.y,offsetX=body.offset.x,offsetY=body.offset.y,skin=.02f},failures,warnings,report};
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/audit.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        return result;
    }
}

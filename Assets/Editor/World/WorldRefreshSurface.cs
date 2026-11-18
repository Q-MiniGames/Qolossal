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
using Object = UnityEngine.Object;

// Works on the saved consolidated scenes. Does not invoke the retired scene builders.
public static class WorldRefreshSurface
{
    public const string Evidence = "Tools/Design/WorldRefresh_20261104/Surface";
    public const string Art = "Assets/Art/Prototypes/WorldRefresh_20261104/";
    const string Marker = "World refresh | surface flow 20261104";
    const float MaxGrade = 1f;
    sealed class Span { public float a,b; public string reason; public Span(float a,float b,string reason) { this.a=a;this.b=b;this.reason=reason; } }
    sealed class Edit { public float a,b,y0,y1,ease,maxGrade,maxDelta; public Vector2[] points; }
    static string PathOf(Transform t) { var a=new List<string>();while(t!=null){a.Add(t.name);t=t.parent;}a.Reverse();return string.Join("/",a); }
    static IEnumerable<T> InScene<T>(Scene scene) where T:Component => scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static float Y(Vector2[] p,float x)
    {
        for(int i=0;i<p.Length-1;i++) if(p[i+1].x-p[i].x>.0001f && x>=p[i].x-.00001f && x<=p[i+1].x+.00001f)
            return Mathf.Lerp(p[i].y,p[i+1].y,(x-p[i].x)/(p[i+1].x-p[i].x));
        return x<p[0].x?p[0].y:p[p.Length-1].y;
    }
    static float Curve(float x, float width,float rise,float ease)
    {
        float m=rise/(width-ease);
        if(x<ease)return m*(x*.5f-ease/(2*Mathf.PI)*Mathf.Sin(Mathf.PI*x/ease));
        if(x>width-ease)return rise-Curve(width-x,width,rise,ease);
        return m*(x-ease*.5f);
    }
    static List<Span> Merge(List<Span> spans)
    {
        var result=new List<Span>();
        foreach(var s in spans.OrderBy(s=>s.a))
        {
            if(result.Count>0 && s.a<=result[result.Count-1].b+.001f)result[result.Count-1].b=Mathf.Max(result[result.Count-1].b,s.b);
            else result.Add(new Span(s.a,s.b,s.reason));
        }
        return result;
    }
    static List<Edit> Plan(MraTerrain terrain,JObject region,Scene scene,out List<Span> protectedSpans)
    {
        Vector2[] src=terrain.surface;float ox=terrain.transform.position.x;
        var protection=new List<Span>();protectedSpans=protection;
        void Protect(float a,float b,string why)=>protection.Add(new Span(a-ox,b-ox,why));
        foreach(var n in region["nodes"]) {float x=(float)n["position"]["x"];Protect(x-2.8f,x+2.8f,"beat pad "+(string)n["id"]);}
        foreach(var c in InScene<CaveConnectionPoint>(scene).Where(c=>c.isActiveAndEnabled&&!c.undergroundSide))
        {Protect(c.EntrancePosition.x-6,c.EntrancePosition.x+6,"cave "+c.connectionId);Protect(c.ArrivalPosition.x-2.5f,c.ArrivalPosition.x+2.5f,"arrival "+c.connectionId);}
        foreach(var hole in terrain.caveOpenings??Array.Empty<MraTerrain.CaveOpening>())if(hole.outline!=null&&hole.outline.Length>0)
            protectedSpans.Add(new Span(hole.outline.Min(p=>p.x)-2,hole.outline.Max(p=>p.x)+2,"carved aperture "+hole.roomId));
        // Tall and deliberately repeated 4.2-u climbing walls remain challenges, not walking ramps.
        for(int i=0;i<src.Length-1;i++)if(Mathf.Abs(src[i+1].x-src[i].x)<.0001f)
        {float h=Mathf.Abs(src[i+1].y-src[i].y);if(h>5.4f||Mathf.Abs(h-4.2f)<.04f)protectedSpans.Add(new Span(src[i].x-1.35f,src[i].x+1.35f,"climb challenge"));}
        // Preserve solid props and actor/encounter landing footprints. Triggers spanning whole zones are excluded.
        foreach(var c in InScene<Collider2D>(scene).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.GetComponentInParent<MraTerrain>()==null))
        {
            var b=c.bounds;if(b.size.x>16 || b.size.x<.05f || b.size.y>12)continue;
            float x=b.center.x-ox;if(x<src[0].x||x>src[src.Length-1].x)continue;
            float ground=Y(src,x)+terrain.transform.position.y;
            if(b.min.y<ground+3.5f&&b.max.y>ground-.5f)Protect(b.min.x-1,b.max.x+1,"prop or encounter "+c.name);
        }
        var blocked=Merge(protectedSpans);
        var candidates=new List<Span>();
        for(int i=0;i<src.Length-1;i++)
        {
            float x=src[i].x,h=src[i+1].y-src[i].y;
            if(Mathf.Abs(src[i+1].x-x)>.0001f||Mathf.Abs(h)<.5f||Mathf.Abs(h)>5.4f||blocked.Any(s=>x>=s.a&&x<=s.b))continue;
            float left=Mathf.Max(src[0].x+.4f,x-6.5f),right=Mathf.Min(src[src.Length-1].x-.4f,x+6.5f);
            foreach(var s in blocked) {if(s.b<x)left=Mathf.Max(left,s.b+.15f);if(s.a>x)right=Mathf.Min(right,s.a-.15f);}
            if(right-left>=3f)candidates.Add(new Span(left,right,h>0?"rise":"descent"));
        }
        // Merge nearby ordinary stairs into one flowing connector, but never bridge a change of direction.
        var merged=new List<Span>();
        foreach(var c in candidates.OrderBy(c=>c.a))
        {
            if(merged.Count>0 && c.a<=merged.Last().b && c.reason==merged.Last().reason)merged.Last().b=Mathf.Max(merged.Last().b,c.b);
            else if(merged.Count==0 || c.a>=merged.Last().b)merged.Add(c);
        }
        var edits=new List<Edit>();
        foreach(var s in merged)
        {
            float width=s.b-s.a,y0=Y(src,s.a),y1=Y(src,s.b),rise=y1-y0;
            float ease=Mathf.Min(1.6f,width*.18f),grade=Mathf.Abs(rise)/(width-ease);
            if(grade>MaxGrade || Mathf.Abs(rise)<.25f)continue;
            int count=Mathf.CeilToInt(width/.6f);var points=new Vector2[count+1];float delta=0;
            for(int i=0;i<=count;i++) {float d=width*i/count;points[i]=new Vector2(s.a+d,y0+Curve(d,width,rise,ease));delta=Mathf.Max(delta,Mathf.Abs(points[i].y-Y(src,points[i].x)));}
            if(delta>3.2f)continue;
            edits.Add(new Edit{a=s.a,b=s.b,y0=y0,y1=y1,ease=ease,maxGrade=grade,maxDelta=delta,points=points});
        }
        return edits;
    }
    static Vector2[] ApplyEdits(Vector2[] src,List<Edit> edits)
    {
        if(edits.Count==0)return src;
        var result=new List<Vector2>();int i=0;
        foreach(var edit in edits)
        {
            while(i<src.Length&&src[i].x<edit.a-.0001f)result.Add(src[i++]);
            result.AddRange(edit.points);
            while(i<src.Length&&src[i].x<=edit.b+.0001f)i++;
        }
        while(i<src.Length)result.Add(src[i++]);
        return result.ToArray();
    }
    public static Sprite ImportSprite(string path)
    {
        if(!File.Exists(path))throw new FileNotFoundException("Missing native terrain artwork",path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=360;
        imp.mipmapEnabled=true;imp.alphaSource=TextureImporterAlphaSource.FromInput;imp.alphaIsTransparency=true;
        imp.sRGBTexture=true;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;
        imp.textureCompression=TextureImporterCompression.Uncompressed;imp.isReadable=false;
        bool strip=path.EndsWith("/Top.png",StringComparison.OrdinalIgnoreCase);
        imp.wrapModeU=strip?TextureWrapMode.Mirror:TextureWrapMode.Repeat;imp.wrapModeV=strip?TextureWrapMode.Clamp:TextureWrapMode.Repeat;imp.filterMode=FilterMode.Bilinear;
        imp.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void SetArt(MraTerrain t,Sprite top,Sprite fill,string region)
    {
        // Source rows measured from the first row whose alpha is >=250 across the whole strip.
        float walk=region=="MR04"?111:region=="MR05"?109:region=="MR06"?108:region=="MR07"?107:103;
        t.top=top;t.fill=fill;t.walkLinePx=walk;t.maxStripSlope=1f;
        // Preserve physics and wall-cling restrictions; retire the low-density painted overlays only.
        t.face=null;t.under=null;t.cliffSideL=null;t.cliffSideR=null;t.cornerTopL=null;t.cornerTopR=null;t.cornerFootL=null;t.cornerFootR=null;
        t.stepShortL=null;t.stepShortR=null;t.stepMediumL=null;t.stepMediumR=null;t.stepOuterL=null;t.stepOuterR=null;t.stepInnerL=null;t.stepInnerR=null;t.stepWalls=Array.Empty<Sprite>();
        t.stepShortSlopeL=null;t.stepShortSlopeR=null;t.stepMediumSlopeL=null;t.stepMediumSlopeR=null;t.stepOuterSlopeL=null;t.stepOuterSlopeR=null;
        t.stripDerivedRuns=false;t.shadeLikeFill=false;t.registeredTopJoinRole=MraTerrain.RegisteredTopJoinRole.None;t.registeredTopJoinPeer=null;t.registeredTopJoinSource=null;
    }
    public static string AnalyzeCurrent()
    {
        var scene=SceneManager.GetActiveScene();var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));
        var region=(JObject)world["regions"].FirstOrDefault(r=>(string)r["scene"]==scene.name);
        if(region==null)throw new Exception("Open a surface region");string id=(string)region["id"];
        var results=new List<object>();int total=0;
        foreach(var t in InScene<MraTerrain>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.name.StartsWith("Route "+id+" piece")&&t.surface.Length>10&&t.solid))
        {
            var edits=Plan(t,region,scene,out var protection);total+=edits.Count;
            results.Add(new{terrain=PathOf(t.transform),points=t.surface.Length,edits=edits.Select(e=>new{e.a,e.b,e.y0,e.y1,e.maxGrade,e.maxDelta}).ToArray(),protectedIntervals=protection.Select(p=>new{p.a,p.b,p.reason}).ToArray()});
        }
        Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/"+id+"_plan.json",JsonConvert.SerializeObject(results,Formatting.Indented));
        return JsonConvert.SerializeObject(new{region=id,connectors=total});
    }
    public static string ApplyCurrent(bool applyArt=true)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before changing terrain.");
        var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Preserve unsaved scene edits first.");
        if(scene.GetRootGameObjects().Any(g=>g.name==Marker))return "Already applied to "+scene.name;
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));
        var region=(JObject)world["regions"].FirstOrDefault(r=>(string)r["scene"]==scene.name);
        if(region==null || !new[]{"MR01","MR02","MR03","MR04","MR05","MR06","MR07"}.Contains((string)region["id"]))throw new InvalidOperationException("Open one of the seven surface scenes.");
        string id=(string)region["id"];Directory.CreateDirectory(Evidence+"/Before");string backup=Evidence+"/Before/"+scene.name+".unity";
        if(!File.Exists(backup))File.Copy(scene.path,backup);
        Sprite newTop=null,newFill=null;
        if(applyArt&&id!="MR01"){newTop=ImportSprite(Art+id+"/Top.png");newFill=ImportSprite(Art+id+"/Fill.png");}
        var terrains=InScene<MraTerrain>(scene).Where(t=>t.gameObject.activeInHierarchy&&t.surface.Length>=2).ToArray();
        var logs=new List<object>();var allEdits=new List<(MraTerrain terrain,Edit edit)>();var beforeByTerrain=new Dictionary<MraTerrain,Vector2[]>();
        foreach(var t in terrains)
        {
            var before=t.surface.ToArray();beforeByTerrain[t]=before;var originalFirst=before[0];var originalLast=before[before.Length-1];
            bool route=t.name.StartsWith("Route "+id+" piece") && t.surface.Length>10 && t.solid;
            var protection=new List<Span>();var edits=route?Plan(t,region,scene,out protection):new List<Edit>();
            t.surface=ApplyEdits(before,edits);
            if(t.surface[0]!=originalFirst||t.surface[t.surface.Length-1]!=originalLast)throw new Exception("Terrain endpoint changed: "+t.name);
            if(newTop!=null)SetArt(t,newTop,newFill,id);
            t.Rebuild();EditorUtility.SetDirty(t);
            foreach(var e in edits)allEdits.Add((t,e));
            float maxScale=Mathf.Max(Mathf.Abs(t.transform.lossyScale.x),Mathf.Abs(t.transform.lossyScale.y));
            float stripMinimum=t.top!=null?t.top.pixelsPerUnit/maxScale*(Mathf.Sqrt(t.maxStripSlope*t.maxStripSlope+4)-t.maxStripSlope)*.5f:0;
            logs.Add(new {path=PathOf(t.transform),changedSpans=edits.Select(e=>new{e.a,e.b,e.y0,e.y1,e.maxGrade,e.maxDelta}).ToArray(),before=before.Select(p=>new[]{p.x,p.y}).ToArray(),after=t.surface.Select(p=>new[]{p.x,p.y}).ToArray(),protectedIntervals=protection.Select(p=>new{p.a,p.b,p.reason}).ToArray(),top=AssetDatabase.GetAssetPath(t.top),fill=AssetDatabase.GetAssetPath(t.fill),transformScale=new[]{t.transform.lossyScale.x,t.transform.lossyScale.y},topPPU=t.top!=null?t.top.pixelsPerUnit:0,fillPPU=t.fill!=null?t.fill.pixelsPerUnit:0,minimumTopDensityAtMaximumGrade=stripMinimum,topWorldTile=t.top!=null?new[]{t.top.texture.width/t.top.pixelsPerUnit,t.top.texture.height/t.top.pixelsPerUnit}:null,fillWorldTile=t.fill!=null?new[]{t.fill.texture.width/t.fill.pixelsPerUnit,t.fill.texture.height/t.fill.pixelsPerUnit}:null});
        }
        // Loose grass/flowers sitting on edited travel connectors follow the new walkline; gameplay anchors were excluded.
        int movedDecor=0;
        foreach(var sr in InScene<SpriteRenderer>(scene).Where(s=>s.gameObject.activeInHierarchy&&s.GetComponentInParent<MraTerrain>()==null&&s.GetComponentInParent<Collider2D>()==null))
        {
            if(sr.GetComponentsInParent<MonoBehaviour>().Any(c=>c!=null))continue;
            foreach(var item in allEdits)
            {
                var t=item.terrain;var e=item.edit;float x=sr.transform.position.x-t.transform.position.x;
                if(x<e.a||x>e.b)continue;
                float oldY=Y(beforeByTerrain[t],x)+t.transform.position.y;
                if(Mathf.Abs(sr.bounds.min.y-oldY)>.65f)continue;
                float newY=Y(t.surface,x)+t.transform.position.y;
                sr.transform.position+=Vector3.up*(newY-oldY);EditorUtility.SetDirty(sr.transform);movedDecor++;break;
            }
        }
        var zones=region["nodes"].Select(n=>
        {
            float x=(float)n["position"]["x"];var arr=region["nodes"].ToArray();int k=Array.IndexOf(arr,n);
            float left=k==0?(float)region["cameraMin"]["x"]:((float)arr[k-1]["position"]["x"]+x)*.5f;
            float right=k==arr.Length-1?(float)region["cameraMax"]["x"]:((float)arr[k+1]["position"]["x"]+x)*.5f;
            int changes=allEdits.Count(e=>e.edit.b+e.terrain.transform.position.x>=left&&e.edit.a+e.terrain.transform.position.x<=right);
            return new{id=(string)n["id"],name=(string)n["name"],x,left,right,smoothedConnectors=changes,artRefreshed=newTop!=null,disposition=changes>0?"Flowing connector slopes with beat/cave/challenge pads retained":"Existing landmark, cave or challenge geometry retained; art/theme reviewed"};
        }).ToArray();
        new GameObject(Marker);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var report=new{scene=scene.path,region=id,anatomy=(string)region["body"],maxNewGrade=MaxGrade,sampleSpacing=.6f,movedDecor,edits=allEdits.Count,zones,terrains=logs};
        File.WriteAllText(Evidence+"/"+id+"_applied.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return JsonConvert.SerializeObject(new{region=id,zones=zones.Length,edits=allEdits.Count,movedDecor,art=newTop!=null});
    }
    public static string RetireWeakCradleOverlaysCurrent()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();
        if(scene.name!="MRAtlas_MR01_Cradle"||scene.isDirty)throw new Exception("Open the saved Cradle scene");
        var targets=new Dictionary<string,string>{{"Route MR01 piece 00","Cliff corners (R)"},{"Route MR01 piece 01 (past the ravine)","Step 253.6 inner"}};var records=new List<object>();
        foreach(var pair in targets)
        {
            var t=InScene<MraTerrain>(scene).Single(t=>t.name==pair.Key);var old=t.surface.ToArray();
            t.replacedPaintedParts=(t.replacedPaintedParts??Array.Empty<string>()).Concat(new[]{pair.Value}).Distinct().ToArray();t.Rebuild();EditorUtility.SetDirty(t);
            if(!old.SequenceEqual(t.surface))throw new Exception("Unexpected Cradle geometry change");
            records.Add(new{terrain=PathOf(t.transform),retiredOverlay=pair.Value,reason="Measured native density below 216 PPU; native top/fill remain, all other accepted kit fields preserved"});
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);string path=Evidence+"/MR01_weak_overlays.json";File.WriteAllText(path,JsonConvert.SerializeObject(records,Formatting.Indented));return path;
    }
    public static string ValidateCurrent()
    {
        if(EditorApplication.isPlaying)throw new Exception("Validate saved geometry in Edit mode");
        var scene=SceneManager.GetActiveScene();string id=scene.name.Substring(8,4);
        string file=Evidence+"/"+id+"_applied.json";if(!File.Exists(file))throw new FileNotFoundException("Apply report missing",file);
        var report=JObject.Parse(File.ReadAllText(file));var terrain=InScene<MraTerrain>(scene).ToDictionary(t=>PathOf(t.transform));
        int endpoints=0,protectedSamples=0,floorSamples=0,gradeChecks=0;var errors=new List<string>();Physics2D.SyncTransforms();
        foreach(var item in report["terrains"])
        {
            string path=(string)item["path"];if(!terrain.TryGetValue(path,out var t)){errors.Add("Missing terrain "+path);continue;}
            var before=item["before"].Select(p=>new Vector2((float)p[0],(float)p[1])).ToArray();
            if(t.surface[0]!=before[0]||t.surface[t.surface.Length-1]!=before[before.Length-1])errors.Add("Endpoint changed "+path);else endpoints+=2;
            foreach(var s in item["protectedIntervals"])
            {
                float a=Mathf.Max((float)s["a"],before[0].x),b=Mathf.Min((float)s["b"],before[before.Length-1].x);
                for(float x=a+.013f;x<b;x+=.5f){protectedSamples++;if(Mathf.Abs(Y(before,x)-Y(t.surface,x))>.002f)errors.Add("Protected ground changed "+path+" x="+x);}
            }
            foreach(var e in item["changedSpans"])
            {
                float a=(float)e["a"],b=(float)e["b"];
                foreach(var pair in t.surface.Zip(t.surface.Skip(1),(p,q)=>new{p,q}).Where(p=>p.p.x>=a-.001f&&p.q.x<=b+.001f))
                {gradeChecks++;if(pair.q.x<=pair.p.x||Mathf.Abs((pair.q.y-pair.p.y)/(pair.q.x-pair.p.x))>1.001f)errors.Add("New grade invalid "+path);}
                for(float x=a+.03f;x<b-.03f;x+=.65f)
                {
                    float y=Y(t.surface,x);var wp=t.transform.TransformPoint(new Vector3(x,y,0));
                    var hits=Physics2D.RaycastAll((Vector2)wp+Vector2.up*.18f,Vector2.down,.4f,LayerMask.GetMask("Ground"));
                    floorSamples++;if(!hits.Any(h=>h.collider==t.GetComponent<PolygonCollider2D>()&&Mathf.Abs(h.point.y-wp.y)<.03f))errors.Add("Rendered/raycast floor mismatch "+path+" x="+x);
                }
            }
        }
        var result=new{region=id,endpoints,protectedSamples,floorSamples,gradeChecks,errors};
        File.WriteAllText(Evidence+"/"+id+"_validation.json",JsonConvert.SerializeObject(result,Formatting.Indented));
        if(errors.Count>0)throw new Exception(JsonConvert.SerializeObject(result));return JsonConvert.SerializeObject(result);
    }
}

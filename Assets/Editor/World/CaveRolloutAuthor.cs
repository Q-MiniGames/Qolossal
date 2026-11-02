using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Newtonsoft.Json.Linq;

// Scene authoring for the user-approved cave rollout. The room catalog and gameplay data remain authoritative.
public static class CaveRolloutAuthor
{
    const string Output="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CaveRollout_20261027/";
    const string Root="Assets/Development/Layouts/CaveRollout_20261027";
    const string Art="Assets/Art/Prototypes/CaveRollout_20261027/";
    const string Rim="Assets/Art/Prototypes/CradleUndergroundExit_20261027/MR01_C01_ReturnRim.png";
    const string Edge="Assets/Art/Prototypes/CradleTerrainJoin_20261027/MR01_C01_CleanLeftEdge.png";
    static string[] Families={"","Causeway_Masonry","Terraces_Seepstone","Qvale_Sluice","Ribwood_Roots","Heights_Slate","Summit_QuietStone"};
    static BindingFlags Hidden=BindingFlags.NonPublic|BindingFlags.Instance;
    static float Smooth(float x)=>Mathf.SmoothStep(0,1,Mathf.Clamp01(x));
    static Vector2 V(JToken t)=>new Vector2((float)t[0],(float)t[1]);
    static void Ensure(string path){System.IO.Directory.CreateDirectory(path);AssetDatabase.Refresh();}
    public static void ImportArt(){foreach(var p in System.IO.Directory.GetFiles(Art,"*.png")){var i=(TextureImporter)AssetImporter.GetAtPath(p.Replace('\\','/'));if(i==null)continue;i.textureType=TextureImporterType.Default;i.maxTextureSize=4096;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.SaveAndReimport();}}
    static Material Mat(string path,string shader,Texture tex=null){var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find(shader);if(tex!=null)m.mainTexture=tex;EditorUtility.SetDirty(m);return m;}
    static Mesh SaveMesh(string path,Mesh m){var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(m,path);return m;}old.Clear();old.indexFormat=m.indexFormat;old.vertices=m.vertices;for(int ch=0;ch<3;ch++){var data=new List<Vector4>();m.GetUVs(ch,data);old.SetUVs(ch,data);}old.colors=m.colors;old.triangles=m.triangles;old.RecalculateBounds();old.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}
    static void Part(Transform group,string folder,string name,Mesh mesh,Material mat,int order){var go=new GameObject(name);go.transform.SetParent(group,true);go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(folder+"/"+name+".asset",mesh);var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingOrder=order;}
    struct Data{public Vector3 p;public Vector4 u0,u1,u2;public Color c;}
    static Mesh Grid(int nx,int ny,Func<float,float,Data> make){var v=new List<Vector3>();var u0=new List<Vector4>();var u1=new List<Vector4>();var u2=new List<Vector4>();var c=new List<Color>();var tr=new List<int>();for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++){var d=make((float)i/nx,(float)j/ny);v.Add(d.p);u0.Add(d.u0);u1.Add(d.u1);u2.Add(d.u2);c.Add(d.c);if(i<nx&&j<ny){int a=j*(nx+1)+i,b=a+nx+1;tr.AddRange(new[]{a,b,b+1,a,b+1,a+1});}}var m=new Mesh();m.SetVertices(v);m.SetUVs(0,u0);m.SetUVs(1,u1);m.SetUVs(2,u2);m.SetColors(c);m.SetTriangles(tr,0);m.RecalculateBounds();return m;}
    static float Height(MraTerrain t,float x){float y=-10000;for(int i=0;i<t.surface.Length-1;i++){var a=t.surface[i];var b=t.surface[i+1];if(b.x>a.x+.00001f&&x>=a.x-.001f&&x<=b.x+.001f)y=Mathf.Max(y,Mathf.LerpUnclamped(a.y,b.y,(x-a.x)/(b.x-a.x)));}return y;}
    static Texture2D Inner(string id,int region,out Vector2 cropY){cropY=new Vector2(0,1);string path=Art+Families[region]+".png";if(region==0){if(id=="MR01_C02"){path="Assets/Art/Prototypes/CradleTailored_20261027/Upper_MR01_C02.png";cropY=new Vector2(.24f,.82f);}else if(id=="MR01_C04"||id=="MR01_C06"||id=="PC03")path=Art+"Ribwood_Roots.png";else if(id=="PC01")path=Art+"Causeway_Masonry.png";else path="Assets/Art/Prototypes/CradleContinuousWall_20261027/MR01_C01_ContinuousWall.png";}var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(tex==null)throw new Exception(path);return tex;}
    static Transform CleanGroup(CaveConnectionPoint p){var old=p.transform.Find("Regional fitted passage");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);foreach(var r in p.GetComponentsInChildren<Renderer>(true))r.enabled=r.GetComponentInParent<MraTerrain>()!=null && r.GetComponentInParent<MraTerrain>().name!="Limestone floor lip";var go=new GameObject("Regional fitted passage");go.transform.SetParent(p.transform,true);return go.transform;}
    static void SetPortal(CaveConnectionPoint p,float dir,float mouth,float floor,float inset){if(p.entrance is MraChamberDoor d){d.walkThroughEntrance=true;d.walkThroughDirection=dir;EditorUtility.SetDirty(d);}if(p.entrance is MraUgPortal u){u.walkThroughEntrance=true;u.walkThroughDirection=dir;EditorUtility.SetDirty(u);}if(p.entrance is MraExit e){e.direction=dir;e.groundOffset=0;EditorUtility.SetDirty(e);}var box=p.entrance.GetComponent<BoxCollider2D>();box.size=new Vector2(1.6f,2.8f);box.offset=new Vector2(0,1.25f);EditorUtility.SetDirty(box);var show=p.entrance.GetComponent<CaveThresholdPresentation>();if(show==null)show=p.entrance.gameObject.AddComponent<CaveThresholdPresentation>();show.connection=p;show.inward=dir;show.mouth=new Vector2(mouth,floor);show.emergenceInset=inset;show.emergenceSeconds=Mathf.Clamp((inset+2)/3.5f+.8f,2.5f,5f);p.activation=dir>0?"Walk right into the recessed cave passage":"Walk left into the recessed cave passage";EditorUtility.SetDirty(show);EditorUtility.SetDirty(p);}
    static void BackAndShadow(Transform group,string folder,string id,int region,float mouth,float dir,float depth,float height,Func<float,float> floorAt,Func<float,float,float> visible,Color? wallTint=null){
        var tex=Inner(id,region,out Vector2 crop);var mat=Mat(folder+"/Inner wall.mat","Qolossal/Prototypes/Cave Rollout Inner Wall",tex);mat.SetTexture("_EdgeMask",AssetDatabase.LoadAssetAtPath<Texture2D>(Edge));
        if(region==6)SummitCaveArt.ConfigureInner(mat,folder.Contains("/Lower_"));
        float sourceHeight=height/(crop.y-crop.x),sourceWidth=sourceHeight*tex.width/tex.height,span=Mathf.Min(1,(depth+.2f)/sourceWidth);int seed=id.Sum(c=>(int)c);float offset=(1-span)*((seed%5)/4f);
        Part(group,folder,"Continuous themed inner wall",Grid(72,36,(u,v)=>{float d=Mathf.Lerp(-.10f,depth+.15f,u),x=mouth+dir*d,y=floorAt(d)+Mathf.Lerp(-.10f,height+.15f,v);float shade=Mathf.Lerp(.92f,.10f,Smooth(d/Mathf.Max(depth-.5f,1)));float a=1;return new Data{p=new Vector3(x,y),u0=new Vector4(offset+u*span,Mathf.Lerp(crop.x,crop.y,v)),u1=new Vector4((d+.1f)/.8f,v),u2=new Vector4(visible(x,y),0),c=(wallTint??Color.white)*new Color(shade,shade,shade,a)};}),mat,6);
        var shadow=Mat(folder+"/Passage shadow.mat","Sprites/Default",Texture2D.whiteTexture);
        Part(group,folder,"Passage shadow",Grid(48,18,(u,v)=>{float d=depth*u,x=mouth+dir*d,y=floorAt(d)+height*v;return new Data{p=new Vector3(x,y),u1=new Vector4(2,0),u2=new Vector4(visible(x,y),0),c=new Color(0,0,0,.62f*Smooth(d/depth)*Smooth(v*12))};}),shadow,120);
    }
    public static object BuildSurface(int region){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");EditorSceneManager.OpenScene(RegionalSceneCatalog.BuildPaths[region]);
        var plans=JArray.Parse(System.IO.File.ReadAllText(Output+"upper_plans.json")).Where(p=>(int)p["region"]==region && !CaveAccessPolicy.IsRetiredEntrance((string)p["id"])).ToArray();var points=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None);var receipt=new List<object>();
        foreach(var plan in plans){string id=(string)plan["id"];var p=points.Single(p=>p.lowerRoomId==id);var terrain=UnityEngine.Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).Single(t=>t.name==(string)plan["terrain"]);float mouth=(float)plan["mouth"],floor=(float)plan["floor"],dir=(float)plan["dir"],depth=(float)plan["depth"],rise=(float)plan["rise"],h=(float)plan["h"];bool existing=(bool)plan["existing"];
            if(existing&&id!="PC01"){float shift=0;while(shift<depth-3f&&Height(terrain,mouth+dir*shift)-floor<h+.12f)shift+=.1f;if(shift>=depth-3f){shift=0;while(shift<depth-3f&&Height(terrain,mouth+dir*shift)-floor<2.45f)shift+=.1f;h=Mathf.Min(h,Height(terrain,mouth+dir*shift)-floor-.10f);}mouth+=dir*shift;depth-=shift;}string folder=Root+"/Upper_"+id;Ensure(folder);var group=CleanGroup(p);
            if((bool?)plan["mound"]==true){var previousHill=p.transform.Find("Native hill | "+id);if(previousHill!=null)UnityEngine.Object.DestroyImmediate(previousHill.gameObject);var go=new GameObject("Native hill | "+id);go.transform.SetParent(p.transform,true);var hill=go.AddComponent<MraTerrain>();foreach(var f in typeof(MraTerrain).GetFields(BindingFlags.Instance|BindingFlags.Public))if(!f.IsStatic&&!f.IsInitOnly)f.SetValue(hill,f.GetValue(terrain));hill.surface=new[]{new Vector2(mouth-9.5f,floor),new Vector2(mouth-8f,floor+.35f),new Vector2(mouth-6.8f,floor+1.2f),new Vector2(mouth-5.6f,floor+2.35f),new Vector2(mouth-4.4f,floor+3.15f),new Vector2(mouth-3f,floor+3.5f),new Vector2(mouth-1.7f,floor+3.3f),new Vector2(mouth-.5f,floor+3.1f),new Vector2(mouth,floor+3f),new Vector2(mouth,floor)};hill.bottom=floor-.01f;hill.caveOpenings=Array.Empty<MraTerrain.CaveOpening>();hill.replacedPaintedParts=Array.Empty<string>();terrain=hill;}
            if(!existing){float end=mouth+dir*depth;var poly=new[]{new Vector2(mouth-dir*.15f,floor),new Vector2(end,floor+rise),new Vector2(end,floor+rise+h),new Vector2(mouth-dir*.15f,floor+h)};if(dir<0)Array.Reverse(poly);terrain.caveOpenings=terrain.caveOpenings.Where(c=>c.roomId!=id).Concat(new[]{new MraTerrain.CaveOpening{roomId=id,outline=poly}}).ToArray();terrain.Rebuild();EditorUtility.SetDirty(terrain);p.entrance.transform.position=V(plan["portal"]);
                if(p.arrival is Checkpoint){var ar=new GameObject("Cave return arrival | "+id);ar.transform.SetParent(p.transform,true);var a=ar.AddComponent<MraArrival>();a.arrivalId="cave-return:"+id;p.arrival=a;}p.arrival.transform.position=V(plan["arrival"]);if(p.arrival is MraArrival a2)a2.facing=-dir;EditorUtility.SetDirty(p.arrival);}
            // A terrain rebuild regenerates native layers, so older endpoint art remains disabled explicitly.
            foreach(var r in p.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(group)&&(r.GetComponentInParent<MraTerrain>()==null||r.GetComponentInParent<MraTerrain>().name=="Limestone floor lip"))r.enabled=false;
            Func<float,float> floorAt=d=>floor+rise*Mathf.Clamp01(d/depth);
            var steps=p.GetComponentsInChildren<EdgeCollider2D>(true).FirstOrDefault(e=>e.enabled&&(e.name.Contains("walkable worn steps")||e.name==id+" return approach ramp"));if(steps!=null){var pts=steps.points.Select(q=>(Vector2)steps.transform.TransformPoint(q+steps.offset)).OrderBy(q=>q.x).ToArray();floorAt=d=>{float x=mouth+dir*d;for(int i=0;i<pts.Length-1;i++)if(x>=pts[i].x&&x<=pts[i+1].x)return Mathf.Lerp(pts[i].y,pts[i+1].y,Mathf.InverseLerp(pts[i].x,pts[i+1].x,x));return x<pts[0].x?pts[0].y:pts.Last().y;};}
            Func<float,float,float> visible=(x,y)=>Height(terrain,x)-y;
            BackAndShadow(group,folder,id,region,mouth,dir,depth,h,floorAt,visible);
            var material=Mat(folder+"/Native outer rock.mat","Qolossal/Prototypes/Cave Rollout Surface Rim",terrain.fill.texture);material.SetTexture("_EdgeMask",AssetDatabase.LoadAssetAtPath<Texture2D>(Rim));
            CaveDepthArt.Surface(material,region);
            Vector2 size=new Vector2(terrain.fill.texture.width,terrain.fill.texture.height)/terrain.fill.pixelsPerUnit;var shadeFn=typeof(MraTerrain).GetMethod("FillShade",Hidden);
            Part(group,folder,"Registered native outer rock",Grid(80,50,(u,v)=>{float d=Mathf.Lerp(-.10f,depth+.6f,u),x=mouth+dir*d,y=floorAt(d)+Mathf.Lerp(-.3f,h+.25f,v);float sh=(float)shadeFn.Invoke(terrain,new object[]{x,y});Color col=terrain.tint*new Color(sh,sh,sh,Smooth((y-floorAt(d)+.02f)/.04f));return new Data{p=new Vector3(x,y),u0=new Vector4(x/size.x,y/size.y),u1=new Vector4(1-u,v),u2=new Vector4(visible(x,y),0),c=col};}),material,145);
            // Match the approach tread phase at the mouth, including the exact regional PPU and walking row.
            typeof(MraTerrain).GetMethod("StepSeams",Hidden).Invoke(terrain,null);float outside=Mathf.Max(.8f,Mathf.Abs((float)plan["mouth"]-mouth)+.8f);float refX=(float)plan["mouth"]-dir*.55f;int tread=(int)typeof(MraTerrain).GetMethod("TreadAt",Hidden).Invoke(terrain,new object[]{refX});var stripU=typeof(MraTerrain).GetMethod("StripU",Hidden,null,new[]{typeof(float),typeof(int),typeof(float)},null);Vector2 topSize=new Vector2(terrain.top.texture.width,terrain.top.texture.height)/terrain.top.pixelsPerUnit;
            float ua=(float)stripU.Invoke(terrain,new object[]{refX,tread,topSize.x}),du=((float)stripU.Invoke(terrain,new object[]{refX+.01f,tread,topSize.x})-ua)/.01f,above=terrain.walkLinePx/120f,below=terrain.shortNaturalSoilTail?.9f:terrain.stripDepth;
            Part(group,folder,"Native walking surface",Grid(90,28,(u,v)=>{float d=Mathf.Lerp(-outside,depth+.2f,u),x=mouth+dir*d,fy=floorAt(d),y=fy+Mathf.Lerp(-below,above,v),a=Smooth((d+outside)/.6f)*Smooth((depth+.2f-d)/.3f)*Smooth((y-fy+below)/.25f);return new Data{p=new Vector3(x,y),u0=new Vector4(ua+(x-refX)*du,1-(above+fy-y)/topSize.y),c=new Color(terrain.tint.r,terrain.tint.g,terrain.tint.b,a)};}),Mat(folder+"/Native floor.mat","Sprites/Default",terrain.top.texture),142);
            if(id=="PC01")p.entrance.transform.position=new Vector2(65.7f,floorAt(65.7f-mouth));float threshold=(p.EntrancePosition.x-mouth)*dir;SetPortal(p,dir,mouth,floor,Mathf.Max(.65f,threshold+.12f));p.entrance.GetComponent<CaveThresholdPresentation>().emergenceFloorRise=floorAt(Mathf.Max(.65f,threshold+.12f))-(p.ArrivalPosition.y-.66f);receipt.Add(new{id,mouth,floor,dir,depth,terrain=terrain.name,arrivalId=p.ArrivalId,art=AssetDatabase.GetAssetPath(Inner(id,region,out _))});
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();System.IO.File.WriteAllText(Output+"upper_result_"+region+".json",Newtonsoft.Json.JsonConvert.SerializeObject(receipt,Newtonsoft.Json.Formatting.Indented));return new{region,count=receipt.Count};
    }
    sealed class Sample {public Vector3 p;public Vector4 a,b;public Color c;}
    sealed class Triangle {public Sample a,b,c;public float minX,maxX,minY,maxY;}
    static Func<Vector3,Vector4> FloorCoordinates(float x,float y){
        Vector3[] bestP=null;Vector4[] bestUV=null;float best=float.MaxValue;var probe=new Vector3(x,y-.10f);
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)){
            var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||r.sharedMaterial==null||r.sharedMaterial.shader.name!="Qolossal/Review/RegionalPaintedEdge"||r.bounds.min.x>x+3||r.bounds.max.x<x-3||r.bounds.min.y>y+1||r.bounds.max.y<y-1)continue;
            var m=f.sharedMesh;var v=m.vertices;var uv=new List<Vector4>();m.GetUVs(2,uv);if(uv.Count!=v.Length)continue;var tr=m.triangles;
            for(int k=0;k<tr.Length;k+=3){var a=f.transform.TransformPoint(v[tr[k]]);var b=f.transform.TransformPoint(v[tr[k+1]]);var c=f.transform.TransformPoint(v[tr[k+2]]);float det=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(det)<1e-7f)continue;
                float u=((b.y-c.y)*(probe.x-c.x)+(c.x-b.x)*(probe.y-c.y))/det,w=((c.y-a.y)*(probe.x-c.x)+(a.x-c.x)*(probe.y-c.y))/det;
                var ua=uv[tr[k]];var ub=uv[tr[k+1]];var uc=uv[tr[k+2]];float yGradient=((ua.y-uc.y)*(c.x-b.x)+(ub.y-uc.y)*(a.x-c.x))/det;if(yGradient<.2f)continue;
                float score=(probe-(a+b+c)/3).sqrMagnitude+100*(Mathf.Max(0,-u)+Mathf.Max(0,-w)+Mathf.Max(0,u+w-1));
                if(score<best){best=score;bestP=new[]{a,b,c};bestUV=new[]{ua,ub,uc};}
            }
        }
        if(bestP==null)throw new Exception("No native walking-edge coordinates near "+probe);
        return p=>{var a=bestP[0];var b=bestP[1];var c=bestP[2];float det=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);float u=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/det,w=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/det;return bestUV[0]*u+bestUV[1]*w+bestUV[2]*(1-u-w);};
    }
    static Func<Vector3,Sample> Sampler(float x,float y){var triangles=new List<Triangle>();var samples=new List<Sample>();
        foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>(f.name.StartsWith("Rock_")||f.name.StartsWith("Rim_"))&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled).OrderByDescending(f=>f.GetComponent<Renderer>().sortingOrder)){var r=f.GetComponent<Renderer>();if(r.bounds.min.x>x+7||r.bounds.max.x<x-7||r.bounds.min.y>y+5||r.bounds.max.y<y-2)continue;var m=f.sharedMesh;var v=m.vertices;var c=m.colors;var a=new List<Vector4>();var b=new List<Vector4>();m.GetUVs(0,a);m.GetUVs(1,b);var tr=m.triangles;var cache=new Dictionary<int,Sample>();Func<int,Sample> get=i=>{if(!cache.TryGetValue(i,out var s)){s=new Sample{p=f.transform.TransformPoint(v[i]),a=a[i],b=b[i],c=c[i]};cache[i]=s;samples.Add(s);}return s;};for(int k=0;k<tr.Length;k+=3){var v0=v[tr[k]];var v1=v[tr[k+1]];var v2=v[tr[k+2]];float minX=Mathf.Min(v0.x,Mathf.Min(v1.x,v2.x)),maxX=Mathf.Max(v0.x,Mathf.Max(v1.x,v2.x)),minY=Mathf.Min(v0.y,Mathf.Min(v1.y,v2.y)),maxY=Mathf.Max(v0.y,Mathf.Max(v1.y,v2.y));if(maxX<x-7||minX>x+7||maxY<y-2||minY>y+5)continue;triangles.Add(new Triangle{a=get(tr[k]),b=get(tr[k+1]),c=get(tr[k+2]),minX=minX,maxX=maxX,minY=minY,maxY=maxY});}}
        if(samples.Count==0)throw new Exception("No native regional samples at "+x+","+y);
        return p=>{foreach(var t in triangles){if(p.x<t.minX||p.x>t.maxX||p.y<t.minY||p.y>t.maxY)continue;var a=t.a.p;var b=t.b.p;var c=t.c.p;float det=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(det)<1e-8f)continue;float u=((b.y-c.y)*(p.x-c.x)+(c.x-b.x)*(p.y-c.y))/det,v=((c.y-a.y)*(p.x-c.x)+(a.x-c.x)*(p.y-c.y))/det;if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)return new Sample{p=p,a=t.a.a*u+t.b.a*v+t.c.a*(1-u-v),b=t.a.b*u+t.b.b*v+t.c.b*(1-u-v),c=t.a.c*u+t.b.c*v+t.c.c*(1-u-v)};}Sample best=null;float distance=float.MaxValue;foreach(var s in samples){float d=(s.p-p).sqrMagnitude;if(d<distance){distance=d;best=s;}}return best;};
    }
    public static object BuildLower(int start,int count){
        if(EditorApplication.isPlaying)throw new Exception("Edit required");EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+RegionalSceneCatalog.Underground+".unity");
        var old=(JArray)JObject.Parse(System.IO.File.ReadAllText(Output+"../EmbeddedCradleReturns_20261026/build_result.json"))["plans"];var newer=JArray.Parse(System.IO.File.ReadAllText(Output+"lower_plans.json"));var all=old.Concat(newer).Where(q=>(string)q["id"]!="MR01_C01" && !CaveAccessPolicy.IsRetiredEntrance((string)q["id"])).OrderBy(q=>(string)q["id"]).ToArray();
        string overridePath="Assets/Development/Layouts/SummitCaves_20261028/LowerPlacementOverrides.json";
        if(System.IO.File.Exists(overridePath))foreach(var replacement in JArray.Parse(System.IO.File.ReadAllText(overridePath))){var target=(JObject)all.Single(p=>(string)p["id"]==(string)replacement["id"]);foreach(var property in ((JObject)replacement).Properties())target[property.Name]=property.Value.DeepClone();}
        var points=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None);var records=new List<object>();var rock=AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Layouts/UndergroundRegional_20261023/Regional_Rock_Blend.mat");
        foreach(var plan in all.Skip(start).Take(count)){string id=(string)plan["id"];var p=points.Single(p=>p.lowerRoomId==id);float mouth=(float)plan["wall"],floor=(float)plan["floor"],dir=(bool)plan["right"]?1:-1,depth=3.4f,rise=(float?)plan["rise"]??0,h=2.85f;int region=Array.IndexOf(RegionalSceneCatalog.Surface,p.upperScene);if(region<0){region=int.Parse(p.upperRegion.Substring(2))-1;}string folder=Root+"/Lower_"+id;Ensure(folder);var group=CleanGroup(p);var sample=Sampler(mouth,floor);
            Func<float,float> floorAt=d=>floor+rise*Mathf.Clamp01((d+.75f)/4.15f);var steps=p.GetComponentsInChildren<EdgeCollider2D>(true).FirstOrDefault(e=>e.enabled&&(e.name.Contains("walkable worn steps")||e.name==id+" return approach ramp"));if(steps!=null){var pts=steps.points.Select(q=>(Vector2)steps.transform.TransformPoint(q+steps.offset)).OrderBy(q=>q.x).ToArray();floorAt=d=>{float x=mouth+dir*d;for(int i=0;i<pts.Length-1;i++)if(x>=pts[i].x&&x<=pts[i+1].x)return Mathf.Lerp(pts[i].y,pts[i+1].y,Mathf.InverseLerp(pts[i].x,pts[i+1].x,x));return x<pts[0].x?pts[0].y:pts.Last().y;};}
            var palette=sample(new Vector3(mouth,floor+1)).b;Color innerTint=palette.z>.5f?new Color(.94f,.72f,.48f):Color.white;BackAndShadow(group,folder,id,region,mouth,dir,depth,h,floorAt,(x,y)=>1,innerTint);
            var mat=Mat(folder+"/Registered regional rim.mat","Qolossal/Prototypes/Cave Rollout Regional Rim");for(int n=0;n<8;n++)mat.SetTexture("_T"+n,rock.GetTexture("_T"+n));mat.SetVector("_RepeatA",rock.GetVector("_RepeatA"));mat.SetVector("_RepeatB",rock.GetVector("_RepeatB"));mat.SetTexture("_Rim",AssetDatabase.LoadAssetAtPath<Texture2D>(Rim));
            if(region==6)SummitCaveArt.ConfigureRim(mat,true);
            Part(group,folder,"Registered regional outer rock",Grid(80,50,(u,v)=>{float d=Mathf.Lerp(0,depth+1.2f,u),x=mouth+dir*d,y=floorAt(d)+Mathf.Lerp(-.3f,h+.25f,v);var s=sample(new Vector3(x,y));return new Data{p=new Vector3(x,y),u0=s.a,u1=s.b,u2=new Vector4(1-u,v),c=s.c};}),mat,145);
            // Registered regional rock beneath the cut, kept behind the concealing wall.
            Part(group,folder,"Registered regional floor",Grid(70,20,(u,v)=>{float d=Mathf.Lerp(id=="MR01_C03"?-3.2f:-.8f,depth+.6f,u),x=mouth+dir*d,fy=floorAt(d),y=Mathf.Lerp(id=="MR01_C03"?floor-.8f:fy-.8f,fy+.02f,v);var s=sample(new Vector3(x,y));var c=s.c;c.a=(id=="MR01_C03"?1:Smooth((d+.8f)/.4f))*Smooth(v*5);return new Data{p=new Vector3(x,y),u0=s.a,u1=s.b,c=c};}),rock,141);
            var edgeUV=FloorCoordinates(mouth-dir*3.2f,floor);var edgeMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Development/Layouts/UndergroundRegional_20261023/Regional_Painted_Edges.mat");
            Part(group,folder,"Native painted walking edge",Grid(90,28,(u,v)=>{float d=Mathf.Lerp(-3f,depth+.5f,u),x=mouth+dir*d,fy=floorAt(d),y=fy+Mathf.Lerp(-.7f,.12f,v);var s=sample(new Vector3(x,y));var c=s.c;var e=edgeUV(new Vector3(x,floor+y-fy));c.a=Smooth((d+3f)/.4f)*Smooth(v*5)*(1-Smooth((e.y-.99f)/.01f));return new Data{p=new Vector3(x,y),u0=s.a,u1=s.b,u2=e,c=c};}),edgeMat,142);
            p.entrance.transform.position=new Vector2(mouth+dir*2.3f,floorAt(2.3f));if(p.arrival is MraArrival a)a.facing=-dir;
            SetPortal(p,dir,mouth,floor,2.8f);p.entrance.GetComponent<CaveThresholdPresentation>().emergenceFloorRise=floorAt(2.8f)-(p.ArrivalPosition.y-.66f);if(id=="E01"&&p.entrance is MraUgPortal ep){ep.arrivalId="cave-return:E01";EditorUtility.SetDirty(ep);}records.Add(new{id,region,mouth,floor,dir,art=AssetDatabase.GetAssetPath(Inner(id,region,out _))});
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();System.IO.File.WriteAllText(Output+"lower_result_"+start+".json",Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));return new{start,count=records.Count};
    }
}












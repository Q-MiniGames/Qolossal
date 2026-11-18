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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Vector reconstruction of old alpha silhouettes; never resizes or changes the source bitmap.
public static class WorldRefreshTrimSilhouette
{
    const string Root="Assets/Development/Layouts/WorldRefresh_20261104/NativeSilhouettes/";
    const string ShaderPath="Assets/Art/Prototypes/WorldRefresh_20261104/NativeFittedStone.shader";
    static readonly float[] PrecisionPixels={4,4.5f,5,6,8,12,16,24,32};
    sealed class Work
    {
        public Transform door;public MeshFilter filter;public MeshRenderer renderer;public int tier;public Action<int> rebuild;
        public int Triangles=>renderer.enabled&&filter.sharedMesh!=null?filter.sharedMesh.triangles.Length/3:0;
    }
    struct V
    {
        public Vector3 p;public Color c;public Vector4 u0,u1,u2,u3,u4,u5,u6,u7;
        public static V Mix(V a,V b,float t)=>new V{p=Vector3.LerpUnclamped(a.p,b.p,t),c=Color.LerpUnclamped(a.c,b.c,t),u0=Vector4.LerpUnclamped(a.u0,b.u0,t),u1=Vector4.LerpUnclamped(a.u1,b.u1,t),u2=Vector4.LerpUnclamped(a.u2,b.u2,t),u3=Vector4.LerpUnclamped(a.u3,b.u3,t),u4=Vector4.LerpUnclamped(a.u4,b.u4,t),u5=Vector4.LerpUnclamped(a.u5,b.u5,t),u6=Vector4.LerpUnclamped(a.u6,b.u6,t),u7=Vector4.LerpUnclamped(a.u7,b.u7,t)};
    }
    // A conservative min/max mip pyramid lets fully solid triangles stay coarse.
    sealed class Mask
    {
        public int width,height;readonly List<byte[]> lo=new List<byte[]>(),hi=new List<byte[]>();readonly List<int> ws=new List<int>(),hs=new List<int>();
        public Mask(string path)
        {
            var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            try{if(!ImageConversion.LoadImage(tex,File.ReadAllBytes(path),false))throw new Exception("Cannot decode "+path);width=tex.width;height=tex.height;var pixels=tex.GetPixels32();var a=pixels.Select(p=>p.a).ToArray();lo.Add(a);hi.Add(a);ws.Add(width);hs.Add(height);
                int w=width,h=height;while(w>1||h>1){int nw=(w+1)/2,nh=(h+1)/2;var l=new byte[nw*nh];var r=new byte[nw*nh];var pl=lo.Last();var ph=hi.Last();for(int y=0;y<nh;y++)for(int x=0;x<nw;x++){int mn=255,mx=0;for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){int xx=x*2+dx,yy=y*2+dy;if(xx>=w||yy>=h)continue;mn=Math.Min(mn,pl[yy*w+xx]);mx=Math.Max(mx,ph[yy*w+xx]);}l[y*nw+x]=(byte)mn;r[y*nw+x]=(byte)mx;}lo.Add(l);hi.Add(r);ws.Add(nw);hs.Add(nh);w=nw;h=nh;}
            }finally{Object.DestroyImmediate(tex);}
        }
        public float Sample(Vector2 uv)
        {
            float x=Mathf.Clamp(uv.x*width-.5f,0,width-1),y=Mathf.Clamp(uv.y*height-.5f,0,height-1);int ix=(int)x,iy=(int)y,jx=Math.Min(ix+1,width-1),jy=Math.Min(iy+1,height-1);var a=lo[0];return Mathf.Lerp(Mathf.Lerp(a[iy*width+ix],a[iy*width+jx],x-ix),Mathf.Lerp(a[jy*width+ix],a[jy*width+jx],x-ix),y-iy)/255f;
        }
        public void Range(Vector2 a,Vector2 b,Vector2 c,out float minimum,out float maximum)
        {
            int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x))*width-.5f),0,width-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))*width-.5f),0,width-1);
            int y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y))*height-.5f),0,height-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))*height-.5f),0,height-1);
            int level=0,span=Math.Max(x1-x0+1,y1-y0+1);while(level+1<lo.Count&&(1<<(level+1))<=span)level++;
            int mn=255,mx=0,w=ws[level];for(int y=y0>>level;y<=y1>>level;y++)for(int x=x0>>level;x<=x1>>level;x++){mn=Math.Min(mn,lo[level][y*w+x]);mx=Math.Max(mx,hi[level][y*w+x]);}minimum=mn/255f;maximum=mx/255f;
        }
    }
    static string Hier(Transform t){var p=new List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
    static string Key(string s){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-","").Substring(0,20);}
    static IEnumerable<T> All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true));
    static string Collisions(Scene s)=>string.Join("\n",All<Collider2D>(s).Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));
    public static string ConfigureWorldUVMaterials()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");AssetDatabase.ImportAsset(ShaderPath,ImportAssetOptions.ForceSynchronousImport);
        var records=new List<object>();
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Root.TrimEnd('/')}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null||material.shader.name!="Qolossal/WorldRefresh/Native Fitted Stone")continue;
            var texture=material.mainTexture as Texture2D;if(texture==null)throw new Exception("Missing native fitted texture: "+path);
            string texturePath=AssetDatabase.GetAssetPath(texture);var importer=AssetImporter.GetAtPath(texturePath)as TextureImporter;
            int width=texture.width,height=texture.height;if(importer!=null)importer.GetSourceTextureWidthAndHeight(out width,out height);
            float density=path.Contains("/MR01/")?240:360;
            material.SetFloat("_PixelsPerUnit",density);material.SetVector("_NativeTextureSize",new Vector4(width,height,0,0));EditorUtility.SetDirty(material);
            records.Add(new{material=path,texture=texturePath,nativeWidth=width,nativeHeight=height,nativePixelsPerWorldUnit=density,worldTileWidth=width/density,worldTileHeight=height/density,requiredWidthAt216PPU=(int)Math.Ceiling(width*216/density),requiredHeightAt216PPU=(int)Math.Ceiling(height*216/density)});
        }
        AssetDatabase.SaveAssets();string report=WorldRefreshSurface.Evidence+"/trim_world_uv_materials.json";File.WriteAllText(report,JsonConvert.SerializeObject(new{mapping="Fragment world XY * _PixelsPerUnit / _NativeTextureSize; no baked UV dependence, no geometry change; native footprint stable under platform import resizing",records},Formatting.Indented));return JsonConvert.SerializeObject(new{materials=records.Count,report});
    }
    public static string ApplyCurrent()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required");var scene=SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save unrelated edits first");
        var world=JObject.Parse(File.ReadAllText("Assets/MountainReliefAtlas/Resources/MRA/mra_world.json"));var region=world["regions"].FirstOrDefault(r=>(string)r["scene"]==scene.name);if(region==null)throw new Exception("Open a surface scene");string id=(string)region["id"];
        string fillPath=id=="MR01"?"Assets/Art/Codex/Terrain/Body/Cradle4K/Body_Fill_4K.png":WorldRefreshSurface.Art+id+"/Fill.png";var fill=AssetDatabase.LoadAssetAtPath<Texture2D>(fillPath);if(fill==null)throw new Exception(fillPath);
        AssetDatabase.ImportAsset(ShaderPath,ImportAssetOptions.ForceSynchronousImport);var shader=Shader.Find("Qolossal/WorldRefresh/Native Fitted Stone");if(shader==null)throw new Exception("Missing native fitted stone shader");
        float camera=All<Camera>(scene).Where(c=>c.orthographic).Min(c=>c.orthographicSize);foreach(var v in All<VistaZone>(scene))camera=Mathf.Min(camera,v.size);
        float display=2160/(2*camera),ppu=Mathf.Max(id=="MR01"?240:360,display*1.01f);
        var importer=AssetImporter.GetAtPath(fillPath)as TextureImporter;int fw=fill.width,fh=fill.height;if(importer!=null)importer.GetSourceTextureWidthAndHeight(out fw,out fh);
        string collisionBefore=Collisions(scene),folder=Root+id+"/";Directory.CreateDirectory(folder);AssetDatabase.Refresh();var masks=new Dictionary<string,Mask>();var records=new List<object>();var workItems=new List<Work>();
        foreach(var f in All<MeshFilter>(scene).Where(f=>f.gameObject.activeInHierarchy&&f.GetComponentInParent<MraTerrain>()==null).ToArray())
        {
            string hierarchy=Hier(f.transform);if(!hierarchy.Contains("/Regional fitted passage/"))continue;var renderer=f.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||renderer.sharedMaterial==null||renderer.sharedMaterials.Length!=1||f.sharedMesh==null)continue;
            var material=renderer.sharedMaterial;var source=f.sharedMesh;string sourcePath=AssetDatabase.GetAssetPath(source);if(string.IsNullOrEmpty(sourcePath)||sourcePath.StartsWith(Root,StringComparison.Ordinal))continue;
            bool rim=material.shader.name=="Qolossal/Prototypes/Cave Rollout Surface Rim",inner=material.shader.name=="Qolossal/Prototypes/Cave Rollout Inner Wall";
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var sourceTex=block.GetTexture("_MainTex")as Texture2D??material.mainTexture as Texture2D;string texturePath=AssetDatabase.GetAssetPath(sourceTex),lower=texturePath.ToLowerInvariant();
            bool corner=f.name.StartsWith("Native roof ",StringComparison.Ordinal)&&!lower.Contains("top_strip")&&!lower.Contains("ground_top")&&!lower.Contains("worldrefresh_20261104");
            if(!rim&&!inner&&!corner)continue;if(f.GetComponents<MonoBehaviour>().Any())throw new Exception("Runtime-owned fitted mesh: "+hierarchy);
            Texture2D maskTex=corner?sourceTex:material.GetTexture("_EdgeMask")as Texture2D;string maskPath=AssetDatabase.GetAssetPath(maskTex);if(string.IsNullOrEmpty(maskPath))throw new Exception("Missing silhouette: "+hierarchy);
            if(!masks.TryGetValue(maskPath,out var mask)){mask=new Mask(maskPath);masks.Add(maskPath,mask);}
            Vector4 rimUV=rim?material.GetVector("_RimUV"):new Vector4(1,1,0,0);
            Vector2 MaskUV(V v)=>corner?(Vector2)v.u0:rim?new Vector2(v.u1.x*rimUV.x+rimUV.z,v.u1.y*rimUV.y+rimUV.w):(Vector2)v.u1;
            float Alpha(V v)=>rim&&v.u1.y>=.74f||inner&&v.u1.x>=1?1:mask.Sample(MaskUV(v));
            void Range(V a,V b,V c,out float minimum,out float maximum)
            {
                if(rim&&Mathf.Min(a.u1.y,Mathf.Min(b.u1.y,c.u1.y))>=.74f||inner&&Mathf.Min(a.u1.x,Mathf.Min(b.u1.x,c.u1.x))>=1){minimum=maximum=1;return;}
                mask.Range(MaskUV(a),MaskUV(b),MaskUV(c),out minimum,out maximum);
                if(rim&&Mathf.Max(a.u1.y,Mathf.Max(b.u1.y,c.u1.y))>=.74f||inner&&Mathf.Max(a.u1.x,Mathf.Max(b.u1.x,c.u1.x))>=1)maximum=1;
            }
            var positions=source.vertices;var colors=source.colors;var channels=new List<Vector4>[8];for(int ch=0;ch<8;ch++){channels[ch]=new List<Vector4>();source.GetUVs(ch,channels[ch]);}
            Vector4 UV(int ch,int k)=>channels[ch].Count==positions.Length?channels[ch][k]:Vector4.zero;
            V Read(int k)=>new V{p=positions[k],c=colors.Length==positions.Length?colors[k]:Color.white,u0=UV(0,k),u1=UV(1,k),u2=UV(2,k),u3=UV(3,k),u4=UV(4,k),u5=UV(5,k),u6=UV(6,k),u7=UV(7,k)};
            var door=f.transform;while(door!=null&&door.name!="Regional fitted passage")door=door.parent;
            var work=new Work{door=door,filter=f,renderer=renderer};workItems.Add(work);int recordIndex=records.Count;records.Add(null);
            void Build(int firstTier)
            {
            float edgeLimit=PrecisionPixels[firstTier]/display;
            var output=new List<V>();int leafCount=0;float largestLeaf=0;
            void Add(V a,V b,V c){if(output.Count>3000000)throw new Exception("Silhouette complexity guard: "+hierarchy);output.Add(a);output.Add(b);output.Add(c);}
            float Distance(V a,V b)=>Vector3.Distance(f.transform.TransformPoint(a.p),f.transform.TransformPoint(b.p));
            void Cut(V a,V b,V c,int depth)
            {
                Range(a,b,c,out float mn,out float mx);if(mn>=.5f){Add(a,b,c);return;}if(mx<.5f)return;
                float ab=Distance(a,b),bc=Distance(b,c),ca=Distance(c,a),longest=Mathf.Max(ab,Mathf.Max(bc,ca));
                if(longest>edgeLimit&&depth<26)
                {if(ab>=bc&&ab>=ca){var m=V.Mix(a,b,.5f);Cut(a,m,c,depth+1);Cut(m,b,c,depth+1);}else if(bc>=ca){var m=V.Mix(b,c,.5f);Cut(a,b,m,depth+1);Cut(a,m,c,depth+1);}else{var m=V.Mix(c,a,.5f);Cut(a,b,m,depth+1);Cut(m,b,c,depth+1);}return;}
                leafCount++;largestLeaf=Mathf.Max(largestLeaf,longest);var poly=new List<V>();var input=new[]{a,b,c};var prev=c;float pa=Alpha(prev);foreach(var current in input){float aa=Alpha(current);bool pi=pa>=.5f,ci=aa>=.5f;if(pi!=ci)poly.Add(V.Mix(prev,current,(.5f-pa)/(aa-pa)));if(ci)poly.Add(current);prev=current;pa=aa;}for(int k=1;k<poly.Count-1;k++)Add(poly[0],poly[k],poly[k+1]);
            }
            var triangles=source.triangles;
            // Weld subdivision seams. The replacement shader uses only UV0 and the
            // scalar terrain boundary in UV2; retaining eight float4 channels is wasteful.
            var welded=new List<V>();var indices=new List<int>();var lookup=new Dictionary<(long,long,long,uint,long),int>();
            int Index(V v){Color32 c=v.c;uint rgba=(uint)c.r|((uint)c.g<<8)|((uint)c.b<<16)|((uint)c.a<<24);var key=( (long)Math.Round(v.p.x*1000000), (long)Math.Round(v.p.y*1000000), (long)Math.Round(v.p.z*1000000),rgba,(long)Math.Round(v.u2.x*10000000));if(lookup.TryGetValue(key,out int ix))return ix;ix=welded.Count;lookup.Add(key,ix);welded.Add(v);return ix;}
            int triangleLimit=corner?5000:20000;
            for(int tier=firstTier;tier<PrecisionPixels.Length;tier++)
            {
                edgeLimit=PrecisionPixels[tier]/display;output.Clear();welded.Clear();indices.Clear();lookup.Clear();leafCount=0;largestLeaf=0;
                for(int k=0;k<triangles.Length;k+=3)Cut(Read(triangles[k]),Read(triangles[k+1]),Read(triangles[k+2]),0);
                for(int k=0;k<output.Count;k+=3){int a=Index(output[k]),b=Index(output[k+1]),c=Index(output[k+2]);if(a!=b&&b!=c&&a!=c){indices.Add(a);indices.Add(b);indices.Add(c);}}
                work.tier=tier;if(indices.Count/3<=triangleLimit)break;
            }
            if(indices.Count/3>triangleLimit)throw new Exception("Bounded automatic silhouette repair cannot meet triangle budget: "+hierarchy+" triangles="+indices.Count/3+" limit="+triangleLimit);
            if(output.Count==0){renderer.enabled=false;EditorUtility.SetDirty(renderer);records[recordIndex]=new{hierarchy,sourceMesh=sourcePath,sourceMask=maskPath,disposition="No visible pixels at original 50% alpha contour; disabled empty painted layer",sourceTriangles=triangles.Length/3,outputTriangles=0};return;}
            if(largestLeaf>edgeLimit*1.001f)throw new Exception("Silhouette detail limit exceeded: "+hierarchy);
            var mesh=new Mesh{name=source.name+" vector silhouette",indexFormat=welded.Count<65535?IndexFormat.UInt16:IndexFormat.UInt32};mesh.SetVertices(welded.Select(v=>v.p).ToList());mesh.SetColors(welded.Select(v=>{var c=v.c;if(rim)c.a*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.98f,1,v.u1.y));return (Color32)c;}).ToList());
            mesh.SetUVs(0,welded.Select(v=>{var w=f.transform.TransformPoint(v.p);return new Vector2(w.x*ppu/fw,w.y*ppu/fh);}).ToList());
            if(rim||inner)mesh.SetUVs(2,welded.Select(v=>new Vector2(v.u2.x,0)).ToList());
            mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            string key=Key(hierarchy),meshPath=folder+key+".asset",matPath=folder+key+".mat";
            var replacement=new Material(shader){name=material.name+" vector silhouette"};replacement.mainTexture=fill;replacement.SetFloat("_UseBoundary",rim||inner?1:0);replacement.SetColor("_WallTint",inner&&material.HasProperty("_WallTint")?material.GetColor("_WallTint"):Color.white);
            replacement.SetFloat("_PixelsPerUnit",ppu);replacement.SetVector("_NativeTextureSize",new Vector4(fw,fh,0,0));
            var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(savedMesh==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,savedMesh);Object.DestroyImmediate(mesh);mesh=savedMesh;EditorUtility.SetDirty(mesh);}
            var savedMaterial=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(savedMaterial==null)AssetDatabase.CreateAsset(replacement,matPath);else{EditorUtility.CopySerialized(replacement,savedMaterial);Object.DestroyImmediate(replacement);replacement=savedMaterial;EditorUtility.SetDirty(replacement);}
            f.sharedMesh=mesh;renderer.sharedMaterial=replacement;
            if(block.GetTexture("_MainTex")!=null){block.SetTexture("_MainTex",fill);renderer.SetPropertyBlock(block);}EditorUtility.SetDirty(f);EditorUtility.SetDirty(renderer);
            records[recordIndex]=new{hierarchy,sourceMesh=sourcePath,sourceMask=maskPath,maskWidth=mask.width,maskHeight=mask.height,sourceTriangles=triangles.Length/3,outputTriangles=indices.Count/3,outputVertices=welded.Count,estimatedVertexAndIndexBytes=welded.Count*(rim||inner?32:24)+indices.Count*(welded.Count<65535?2:4),selectedContourPrecisionPixels=PrecisionPixels[work.tier],refinedBoundaryLeaves=leafCount,maxBoundaryTriangleWorldEdge=largestLeaf,maxBoundaryTriangleScreenEdge=largestLeaf*display,alphaIsoContour=.5,texture=fillPath,nativePixelsPerWorldUnit=ppu,textureWidth=fw,textureHeight=fh,requiredWidth=(int)Math.Ceiling(fw*display/ppu),requiredHeight=(int)Math.Ceiling(fh*display/ppu),mesh=meshPath,noSecondaryRasterMask=true};
            }
            work.rebuild=Build;Build(0);
        }
        int DoorTriangles(Transform t)=>t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled).Sum(f=>f.sharedMesh.triangles.Length/3);
        // If several individually legal pieces exceed the doorway budget together,
        // refine the largest remaining contributor at the next bounded precision tier.
        foreach(var group in workItems.GroupBy(w=>w.door))
        {
            while(DoorTriangles(group.Key)>40000)
            {
                var next=group.Where(w=>w.renderer.enabled&&w.tier<PrecisionPixels.Length-1).OrderByDescending(w=>w.Triangles).FirstOrDefault();
                if(next==null)throw new Exception("Bounded automatic doorway budget cannot be met: "+Hier(group.Key));
                next.rebuild(next.tier+1);
            }
        }
        var doorBudgets=All<Transform>(scene).Where(t=>t.name=="Regional fitted passage"&&t.gameObject.activeInHierarchy).Select(t=>new{hierarchy=Hier(t),visibleTriangles=t.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null&&f.GetComponent<Renderer>()!=null&&f.GetComponent<Renderer>().enabled).Sum(f=>f.sharedMesh.triangles.Length/3)}).ToArray();
        foreach(var door in doorBudgets)if(door.visibleTriangles>40000)throw new Exception("Fitted doorway exceeds 40K triangle budget: "+door.hierarchy+" triangles="+door.visibleTriangles);
        bool collisionPreserved=collisionBefore==Collisions(scene);if(!collisionPreserved)throw new Exception("Unexpected collision mutation");AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string report="Tools/Design/WorldRefresh_20261104/Surface/"+id+"_trim_silhouettes.json";if(records.Count>0||!File.Exists(report))File.WriteAllText(report,JsonConvert.SerializeObject(new{scene=scene.name,minCameraSize=camera,displayPixelsPerUnit=display,collisionPreserved,visibleDoorTriangles=doorBudgets.Sum(d=>d.visibleTriangles),doorBudgets,method="Adaptive geometry reconstruction of original 50% alpha contour; native Fill color; no runtime mask or low-resolution rim RGB. Start at 4 output pixels, bounded automatic retries at 4.5/5/6/8/12/16/24/32 pixels only to satisfy trim/wall/door triangle budgets; actual achieved precision recorded per mesh. Bilinear edge interpolation, welded vertices, shader-required channels. Source contour detail is preserved, not newly invented.",records},Formatting.Indented));
        return JsonConvert.SerializeObject(new{scene=scene.name,repaired=records.Count,collisionPreserved,report});
    }
}

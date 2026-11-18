object Fix(){
// Final current-scene visual attachment pass. Root runs this after v1 in Edit mode.
if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("Save unrelated edits first");
string region=scene.name.Substring(8,4);if(region=="MR01")return new{scene=scene.name,skipped="Accepted Cradle kit"};
if(!new[]{"MR02","MR03","MR04","MR05","MR06","MR07"}.Contains(region))throw new Exception("Open upper region");
const string marker="World refresh | native cap joins v2 20261104";
if(scene.GetRootGameObjects().Any(g=>g.name==marker))return new{scene=scene.name,alreadyApplied=true};
T[] All<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
string Hier(Transform t){var names=new System.Collections.Generic.List<string>();while(t!=null){names.Add(t.name);t=t.parent;}names.Reverse();return string.Join("/",names);}
string CollisionState()=>string.Join("\n",All<Collider2D>().Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));
string collisionBefore=CollisionState();
var plans=Newtonsoft.Json.Linq.JArray.Parse(System.IO.File.ReadAllText("C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CaveRollout_20261027/upper_result_"+(int.Parse(region.Substring(2))-1)+".json"));
string folder="Assets/Development/Layouts/WorldRefresh_20261104/CapJoinsV2/"+region+"/";System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var records=new System.Collections.Generic.List<object>();
Mesh SaveMesh(MeshFilter filter,Mesh mesh)
{
    string key;using(var hash=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Hier(filter.transform)))).Replace("-","").Substring(0,20);
    string path=folder+key+".asset";mesh.hideFlags=HideFlags.None;var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
    if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}
    filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);return mesh;
}
foreach(var point in All<CaveConnectionPoint>().Where(p=>p.isActiveAndEnabled&&!p.undergroundSide))
{
    var group=point.transform.Find("Regional fitted passage");if(group==null)continue;
    var plan=plans.FirstOrDefault(p=>(string)p["id"]==point.lowerRoomId);if(plan==null)continue;
    float mouth=(float)plan["mouth"],direction=(float)plan["dir"],depth=(float)plan["depth"];
    string terrainName=(string)plan["terrain"];var terrain=All<MraTerrain>().FirstOrDefault(t=>t.name==terrainName&&t.gameObject.activeInHierarchy);
    float cliff=mouth,best=float.MaxValue;bool foundCliff=false;
    if(terrain!=null)for(int i=0;i<terrain.surface.Length-1;i++)
    {
        var a=terrain.transform.TransformPoint(terrain.surface[i]);var b=terrain.transform.TransformPoint(terrain.surface[i+1]);
        float error=Mathf.Abs(a.x-mouth);if(Mathf.Abs(a.x-b.x)<.001f&&Mathf.Abs(a.y-b.y)>.5f&&error<.5f&&error<best){cliff=a.x;best=error;foundCliff=true;}
    }
    foreach(var filter in group.GetComponentsInChildren<MeshFilter>(true))
    {
        var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||!filter.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
        var source=filter.sharedMesh;string hierarchy=Hier(filter.transform);
    if(region=="MR07"&&filter.name=="Registered native outer rock"&&!source.name.Contains("shallow roof arch"))
    {
        // Summit's legacy return-rim shader forced every pixel above edge.y=.74
        // opaque. That analytical step became an artificial horizontal roof edge
        // during silhouette reconstruction. Raise only that detected boundary into
        // a shallow arch; the lower walking aperture and both endpoints stay fixed.
        var archPoint=filter.GetComponentInParent<CaveConnectionPoint>();
        var grid=archPoint!=null?AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Development/Layouts/CaveRollout_20261027/Upper_"+archPoint.lowerRoomId+"/Registered native outer rock.asset"):null;
        if(grid!=null&&grid.vertexCount==81*51)
        {
            var originalGrid=grid.vertices;var archSource=filter.sharedMesh;var local=archSource.vertices;var world=local.Select(v=>filter.transform.TransformPoint(v)).ToArray();var tr=archSource.triangles;
            float Line(float x){float column=Mathf.Clamp((x-originalGrid[0].x)/(originalGrid[80].x-originalGrid[0].x)*80,0,80);int a=Mathf.FloorToInt(column),b=Mathf.Min(a+1,80);float ya=Mathf.Lerp(originalGrid[a].y,originalGrid[50*81+a].y,.74f),yb=Mathf.Lerp(originalGrid[b].y,originalGrid[50*81+b].y,.74f);return Mathf.Lerp(ya,yb,column-a);}
            (long,long) KeyPoint(int i)=>((long)Math.Round(world[i].x*10000),(long)Math.Round(world[i].y*10000));
            var edgeCounts=new System.Collections.Generic.Dictionary<(long,long,long,long),int>();var edgePoints=new System.Collections.Generic.Dictionary<(long,long,long,long),(int,int)>();
            void Edge(int a,int b){var ka=KeyPoint(a);var kb=KeyPoint(b);if(ka.Item1>kb.Item1||ka.Item1==kb.Item1&&ka.Item2>kb.Item2){var z=ka;ka=kb;kb=z;}var archKey=(ka.Item1,ka.Item2,kb.Item1,kb.Item2);edgeCounts.TryGetValue(archKey,out int count);edgeCounts[archKey]=count+1;edgePoints[archKey]=(a,b);}
            for(int i=0;i<tr.Length;i+=3){Edge(tr[i],tr[i+1]);Edge(tr[i+1],tr[i+2]);Edge(tr[i+2],tr[i]);}
            var ends=new System.Collections.Generic.List<float>();foreach(var pair in edgeCounts.Where(e=>e.Value==1)){var ids=edgePoints[pair.Key];var a=world[ids.Item1];var b=world[ids.Item2];if(Mathf.Abs(a.y-Line(a.x))<.025f&&Mathf.Abs(b.y-Line(b.x))<.025f){ends.Add(a.x);ends.Add(b.x);}}
            if(ends.Count>1&&ends.Max()-ends.Min()>.4f)
            {
                float left=ends.Min(),right=ends.Max(),maxRise=0;for(int i=0;i<world.Length;i++){var p=world[i];float t=Mathf.InverseLerp(left,right,p.x);float wave=Mathf.Pow(Mathf.Sin(Mathf.PI*t),2);float vertical=Mathf.SmoothStep(0,1,Mathf.Clamp01(1-Mathf.Abs(p.y-Line(p.x))/.45f));float rise=.24f*wave*vertical;p.y+=rise;maxRise=Mathf.Max(maxRise,rise);local[i]=filter.transform.InverseTransformPoint(p);}
                var archMesh=UnityEngine.Object.Instantiate(archSource);archMesh.name=archSource.name+" shallow roof arch";archMesh.hideFlags=HideFlags.None;archMesh.vertices=local;archMesh.RecalculateBounds();string archKey;using(var hash=System.Security.Cryptography.SHA256.Create())archKey=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hierarchy))).Replace("-","").Substring(0,20);string archPath=folder+archKey+".asset";var archSaved=AssetDatabase.LoadAssetAtPath<Mesh>(archPath);if(archSaved==null)AssetDatabase.CreateAsset(archMesh,archPath);else{EditorUtility.CopySerialized(archMesh,archSaved);UnityEngine.Object.DestroyImmediate(archMesh);archMesh=archSaved;EditorUtility.SetDirty(archSaved);}filter.sharedMesh=archMesh;EditorUtility.SetDirty(filter);
                records.Add(new{archPath=hierarchy,change="Raised inherited .74 flat cap into shallow native stone arch",left,right,maxRise,collisionChanged=false,sourceMesh=AssetDatabase.GetAssetPath(archSource),archMesh=archPath});
            }
        }
    }
    source=filter.sharedMesh;
        if(filter.name.StartsWith("Native roof ")&&foundCliff)
        {
            var verts=source.vertices;var colors=source.colors;var channels=new System.Collections.Generic.List<Vector4>[3];for(int ch=0;ch<3;ch++){channels[ch]=new System.Collections.Generic.List<Vector4>();source.GetUVs(ch,channels[ch]);}
            Vector4 UV(int ch,int k)=>channels[ch].Count==verts.Length?channels[ch][k]:Vector4.zero;
            (Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2) Read(int k)=>(verts[k],colors.Length==verts.Length?colors[k]:Color.white,UV(0,k),UV(1,k),UV(2,k));
            (Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2) Mix((Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2)a,(Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2)b,float t)=>(Vector3.LerpUnclamped(a.p,b.p,t),Color.LerpUnclamped(a.c,b.c,t),Vector4.LerpUnclamped(a.u0,b.u0,t),Vector4.LerpUnclamped(a.u1,b.u1,t),Vector4.LerpUnclamped(a.u2,b.u2,t));
            float Side(Vector3 p)=>direction*(filter.transform.TransformPoint(p).x-cliff);
            if(!verts.Any(v=>Side(v)<-.0001f))continue;
            var output=new System.Collections.Generic.List<(Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2)>();var indices=new System.Collections.Generic.List<int>();var tris=source.triangles;
            for(int i=0;i<tris.Length;i+=3)
            {
                var poly=new System.Collections.Generic.List<(Vector3 p,Color c,Vector4 u0,Vector4 u1,Vector4 u2)>();var input=new[]{Read(tris[i]),Read(tris[i+1]),Read(tris[i+2])};var previous=input[2];float oldSide=Side(previous.p);
                foreach(var current in input){float side=Side(current.p);if((side>=0)!=(oldSide>=0))poly.Add(Mix(previous,current,oldSide/(oldSide-side)));if(side>=0)poly.Add(current);previous=current;oldSide=side;}
                int start=output.Count;output.AddRange(poly);for(int k=1;k<poly.Count-1;k++){indices.Add(start);indices.Add(start+k);indices.Add(start+k+1);}
            }
            if(indices.Count==0){renderer.enabled=false;EditorUtility.SetDirty(renderer);records.Add(new{path=Hier(filter.transform),change="Retired roof decoration wholly outside supporting cliff",cliff});continue;}
            var clipped=new Mesh{name=source.name+" clipped to supporting cliff",indexFormat=output.Count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16};
            clipped.SetVertices(output.Select(v=>v.p).ToList());clipped.SetColors(output.Select(v=>(Color32)v.c).ToList());
            for(int ch=0;ch<3;ch++)if(channels[ch].Count==verts.Length)clipped.SetUVs(ch,output.Select(v=>ch==0?v.u0:ch==1?v.u1:v.u2).ToList());clipped.SetTriangles(indices,0);clipped.RecalculateBounds();clipped=SaveMesh(filter,clipped);
            records.Add(new{path=Hier(filter.transform),change="Copied roof contour terminates at supporting cliff",cliff,direction,sourceMesh=AssetDatabase.GetAssetPath(source),mesh=AssetDatabase.GetAssetPath(clipped),sourceTriangles=tris.Length/3,triangles=indices.Count/3});
        }
        else if(filter.name=="Registered native outer rock")
        {
            var clone=UnityEngine.Object.Instantiate(source);clone.name=source.name+" attachment feather";var verts=clone.vertices;var colors=clone.colors;if(colors.Length!=verts.Length)colors=Enumerable.Repeat(Color.white,verts.Length).ToArray();
            float back=mouth+direction*(depth+.6f);int feathered=0;
            for(int i=0;i<verts.Length;i++){float worldX=filter.transform.TransformPoint(verts[i]).x;float inwardDistance=(back-worldX)*direction;float alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(inwardDistance/.45f));if(alpha<.999f){var c=colors[i];c.a*=alpha;colors[i]=c;feathered++;}}
            clone.colors=colors;clone=SaveMesh(filter,clone);
            records.Add(new{path=Hier(filter.transform),change="Feathered back attachment over .45u beyond physical aperture",back,featheredVertices=feathered,apertureEnd=mouth+direction*depth,sourceMesh=AssetDatabase.GetAssetPath(source),mesh=AssetDatabase.GetAssetPath(clone),positionsUnchanged=source.vertices.SequenceEqual(clone.vertices)});
        }
    }
}
bool collisionPreserved=collisionBefore==CollisionState();if(!collisionPreserved)throw new Exception("Unexpected collider mutation");
new GameObject(marker);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
string outputPath="Tools/Design/WorldRefresh_20261104/Surface/"+region+"_cap_joins_v2.json";System.IO.File.WriteAllText(outputPath,Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.name,collisionPreserved,records},Newtonsoft.Json.Formatting.Indented));
return new{scene=scene.name,changes=records.Count,collisionPreserved,outputPath};

} var reports=new System.Collections.Generic.List<object>();foreach(var name in RegionalSceneCatalog.Surface.Skip(1)){UnityEditor.SceneManagement.EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder+name+".unity");reports.Add(Fix());} return reports;

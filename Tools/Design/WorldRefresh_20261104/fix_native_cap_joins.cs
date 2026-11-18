// Root executes in Edit mode on one saved upper scene; no scene opening or runtime recompilation.
if(EditorApplication.isPlaying)throw new Exception("Stop the playtest before cap-join authoring");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty)throw new Exception("Save or discard unrelated scene edits first");
string region=scene.name.Substring(8,4);
if(region=="MR01")return new{scene=scene.name,skipped="Accepted Cradle cliff kit retained"};
if(!new[]{"MR02","MR03","MR04","MR05","MR06","MR07"}.Contains(region))throw new Exception("Open an upper scene");
string marker="World refresh | native cap joins 20261104";
if(scene.GetRootGameObjects().Any(g=>g.name==marker))return new{scene=scene.name,alreadyApplied=true};
string Hier(Transform t){var p=new System.Collections.Generic.List<string>();while(t!=null){p.Add(t.name);t=t.parent;}p.Reverse();return string.Join("/",p);}
T[] All<T>()where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
string CollisionState()=>string.Join("\n",All<Collider2D>().Select(c=>Hier(c.transform)+EditorJsonUtility.ToJson(c)));
string beforeCollision=CollisionState();
string folder="Assets/Development/Layouts/WorldRefresh_20261104/CapJoins/"+region+"/";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var changes=new System.Collections.Generic.List<object>();
foreach(var terrain in All<MraTerrain>().Where(t=>t.gameObject.activeInHierarchy&&t.top!=null&&AssetDatabase.GetAssetPath(t.top).Contains("WorldRefresh_20261104")))
{
    float before=terrain.lipOverhang;if(Mathf.Abs(before)<.00001f)continue;
    var surface=terrain.surface.ToArray();terrain.lipOverhang=0;terrain.Rebuild();EditorUtility.SetDirty(terrain);
    if(!surface.SequenceEqual(terrain.surface))throw new Exception("Visual cap edit changed terrain shape");
    changes.Add(new{path=Hier(terrain.transform),change="Native top terminates at its supporting cliff edge",oldLipOverhang=before,newLipOverhang=0});
}
foreach(var filter in All<MeshFilter>().Where(f=>f.gameObject.activeInHierarchy&&f.GetComponentInParent<MraTerrain>()==null))
{
    string hierarchy=Hier(filter.transform);if(!hierarchy.Contains("/Regional fitted passage/"))continue;
    var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
    var texture=block.GetTexture("_MainTex")??(renderer.sharedMaterial!=null?renderer.sharedMaterial.mainTexture:null);
    string texturePath=AssetDatabase.GetAssetPath(texture);
    if(region=="MR07"&&filter.name=="Registered native outer rock")
    {
        // Summit's legacy return-rim shader forced every pixel above edge.y=.74
        // opaque. That analytical step became an artificial horizontal roof edge
        // during silhouette reconstruction. Raise only that detected boundary into
        // a shallow arch; the lower walking aperture and both endpoints stay fixed.
        var point=filter.GetComponentInParent<CaveConnectionPoint>();
        var grid=point!=null?AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Development/Layouts/CaveRollout_20261027/Upper_"+point.lowerRoomId+"/Registered native outer rock.asset"):null;
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
                changes.Add(new{archPath=hierarchy,change="Raised inherited .74 flat cap into shallow native stone arch",left,right,maxRise,collisionChanged=false,sourceMesh=AssetDatabase.GetAssetPath(archSource),archMesh=archPath});
            }
        }
        continue;
    }
    if(filter.name.StartsWith("Native roof ")&&texturePath.Contains("WorldRefresh_20261104")&&texturePath.EndsWith("/Top.png"))
    {
        // These are high-order copies of the pre-refresh top mesh; the actual terrain
        // now renders the correctly registered native top itself. Old copied overhang
        // vertices cannot remain after the adjacent cliff kit has been retired.
        bool currentTop=All<MraTerrain>().Any(t=>t.gameObject.activeInHierarchy&&t.top!=null&&!(t.replacedPaintedParts??Array.Empty<string>()).Contains("Top")&&t.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled&&r.name=="Top (generated)"&&r.bounds.Intersects(renderer.bounds)));
        if(currentTop){renderer.enabled=false;EditorUtility.SetDirty(renderer);changes.Add(new{path=hierarchy,change="Retired duplicate pre-refresh roof-top copy; current native terrain top remains"});}
        continue;
    }
    bool floor=filter.name=="Native walking surface"&&texturePath.EndsWith("/Top.png"),shadow=filter.name=="Passage shadow";
    if(!floor&&!shadow)continue;
    var source=filter.sharedMesh;var mesh=UnityEngine.Object.Instantiate(source);mesh.name=source.name+" native cap blend";mesh.hideFlags=HideFlags.None;
    var vertices=mesh.vertices;var uv=mesh.uv;var colors=mesh.colors;if(colors.Length!=vertices.Length)colors=Enumerable.Repeat(Color.white,vertices.Length).ToArray();
    for(int i=0;i<vertices.Length;i++)
    {
        float fade=1;
        if(floor&&uv.Length==vertices.Length)fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.075f,uv[i].y));
        if(shadow)
        {
            // The original shadow is a regular48x18 grid with a sloped ceiling.
            // Match each column's own top, preserving depth and the lower entrance.
            if(vertices.Length==49*19){int column=i%49;float top=vertices[18*49+column].y;fade=Mathf.SmoothStep(0,1,Mathf.Clamp01((top-vertices[i].y)/.35f));}
            else fade=Mathf.SmoothStep(0,1,Mathf.Clamp01((mesh.bounds.max.y-vertices[i].y)/.35f));
        }
        var c=colors[i];c.a*=fade;colors[i]=c;
    }
    mesh.colors=colors;
    string key;using(var hash=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hierarchy))).Replace("-","").Substring(0,20);
    string path=folder+key+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}
    filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
    changes.Add(new{path=hierarchy,change=shadow?"Tapered rectangular shadow ceiling over .35u":"Faded static walking strip before clamped source-bottom texels",sourceMesh=AssetDatabase.GetAssetPath(source),mesh=path,verticesUnchanged=source.vertices.SequenceEqual(mesh.vertices),trianglesUnchanged=source.triangles.SequenceEqual(mesh.triangles)});
}
bool collisionPreserved=beforeCollision==CollisionState();if(!collisionPreserved)throw new Exception("Cap join pass changed serialized collision");
new GameObject(marker);AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
string output="Tools/Design/WorldRefresh_20261104/Surface/"+region+"_cap_joins.json";
System.IO.File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.name,collisionPreserved,changes},Newtonsoft.Json.Formatting.Indented));
return new{scene=scene.name,changes=changes.Count,collisionPreserved,output};

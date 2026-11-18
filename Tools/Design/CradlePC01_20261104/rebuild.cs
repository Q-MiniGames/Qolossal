if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved edits");
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity");
var cave=UnityEngine.Object.FindObjectsByType<CaveConnectionPoint>(FindObjectsSortMode.None).Single(p=>p.lowerRoomId=="PC01");
string folder="Assets/Development/Layouts/CradlePC01_20261104";
System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();
var terrain=UnityEngine.Object.FindObjectsByType<MraTerrain>(FindObjectsSortMode.None).Single(t=>t.name=="Route MR01 piece 00");
var hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
string invariant=Newtonsoft.Json.JsonConvert.SerializeObject(new{cave.connectionId,cave.lowerRoomId,cave.upperAreaId,cave.ArrivalId,cave.requirements});
// Retire only PC01's patchwork roof, floating approach and downhill staircase.
foreach(var t in cave.GetComponentsInChildren<MraTerrain>(true))t.gameObject.SetActive(false);
foreach(var c in cave.GetComponentsInChildren<Collider2D>(true))if(!c.isTrigger)c.enabled=false;
foreach(var r in cave.GetComponentsInChildren<Renderer>(true))r.enabled=false;
var old=cave.transform.Find("Regional fitted passage");if(old!=null)old.gameObject.SetActive(false);
var prior=cave.transform.Find("PC01 compact limestone cave");if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
var group=new GameObject("PC01 compact limestone cave").transform;group.SetParent(cave.transform,true);
float mouth=60f,floor=24.7f,depth=3.9f,height=2.2f;
var line=terrain.surface.Where(p=>p.x<55f).ToList();
void Curve(Vector2 a,Vector2 b,int count){for(int i=0;i<=count;i++){float u=(float)i/count;line.Add(new Vector2(Mathf.Lerp(a.x,b.x,u),Mathf.Lerp(a.y,b.y,Mathf.SmoothStep(0,1,u))));}}
Curve(new Vector2(55,24.2344837f),new Vector2(58,floor),12);
line.Add(new Vector2(59.9f,floor));line.Add(new Vector2(59.9f,26.5f));
// Rounded shoulder connects directly to the original onward route.
for(int i=1;i<=12;i++){float a=Mathf.PI*.5f*(1-i/12f);line.Add(new Vector2(60.4f-.5f*Mathf.Sin(a),26.5f+.5f*Mathf.Cos(a)));}
Curve(new Vector2(60.4f,27.0f),new Vector2(63.5f,27.15f),12);
Curve(new Vector2(63.5f,27.15f),new Vector2(70,27.2f),20);
line.Add(new Vector2(74.2f,27.2f));Curve(new Vector2(74.2f,27.2f),new Vector2(81,20.889f),28);
line.AddRange(terrain.surface.Where(p=>p.x>81));terrain.surface=line.Distinct().ToArray();
var aperture=new MraTerrain.CaveOpening{roomId="PC01",outline=new[]{new Vector2(59.7f,floor),new Vector2(64.15f,floor),new Vector2(64.15f,26.9f),new Vector2(59.7f,27f)}};
terrain.caveOpenings=terrain.caveOpenings.Where(c=>c.roomId!="PC01").Concat(new[]{aperture}).ToArray();
terrain.caveFringeTrims=terrain.caveFringeTrims.Where(c=>c.roomId!="PC01").Concat(new[]{new MraTerrain.CaveOpening{roomId="PC01",outline=new[]{new Vector2(59.2f,23.95f),new Vector2(60.3f,23.95f),new Vector2(60.3f,26.67f),new Vector2(59.2f,26.67f)}}}).ToArray();
terrain.replacedPaintedParts=terrain.replacedPaintedParts.Concat(new[]{"Step 59.9 medium (slope top)"}).Distinct().ToArray();
terrain.Rebuild();EditorUtility.SetDirty(terrain);
float Ground(float x){for(int i=0;i<terrain.surface.Length-1;i++){var a=terrain.surface[i];var b=terrain.surface[i+1];if(b.x>a.x+.0001f&&x>=a.x&&x<=b.x)return Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x));}return 27.2f;}
Func<float,float> flat=d=>floor;Func<float,float,float> visible=(x,y)=>Ground(x)-y;
typeof(CaveRolloutAuthor).GetMethod("BackAndShadow",hidden).Invoke(null,new object[]{group,folder,"PC01",0,mouth,1f,depth,height,flat,visible,null});
Mesh Grid(int nx,int ny,Func<float,float,Vector3> pos,Func<float,float,Vector4> uv0,Func<float,float,Vector4> uv1,Func<float,float,Vector4> uv2,Func<float,float,Color> color){var v=new System.Collections.Generic.List<Vector3>();var a=new System.Collections.Generic.List<Vector4>();var b=new System.Collections.Generic.List<Vector4>();var c=new System.Collections.Generic.List<Vector4>();var col=new System.Collections.Generic.List<Color>();var tr=new System.Collections.Generic.List<int>();for(int j=0;j<=ny;j++)for(int i=0;i<=nx;i++){float u=(float)i/nx,w=(float)j/ny;v.Add(pos(u,w));a.Add(uv0(u,w));b.Add(uv1(u,w));c.Add(uv2(u,w));col.Add(color(u,w));if(i<nx&&j<ny){int n=j*(nx+1)+i,k=n+nx+1;tr.AddRange(new[]{n,k,k+1,n,k+1,n+1});}}var m=new Mesh();m.SetVertices(v);m.SetUVs(0,a);m.SetUVs(1,b);m.SetUVs(2,c);m.SetColors(col);m.SetTriangles(tr,0);m.RecalculateBounds();return m;}
Material Mat(string name,string shader,Texture tex){var m=AssetDatabase.LoadAssetAtPath<Material>(folder+"/"+name+".mat");if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");}m.shader=Shader.Find(shader);m.mainTexture=tex;EditorUtility.SetDirty(m);return m;}
void Part(string name,Mesh mesh,Material material,int order){typeof(CaveRolloutAuthor).GetMethod("Part",hidden).Invoke(null,new object[]{group,folder,name,mesh,material,order});}
Vector3 RimPos(float u,float v)=>new Vector3(Mathf.Lerp(59.9f+.25f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1f,v)),64.5f,u),Mathf.Lerp(floor-.3f,27.15f,v));
var rim=Mat("Native rim","Qolossal/WorldRevision/Approved Rim Native Attachment",terrain.fill.texture);CaveDepthArt.Surface(rim,0);
rim.SetFloat("_PixelsPerUnit",240);rim.SetVector("_NativeTextureSize",new Vector4(terrain.fill.texture.width,terrain.fill.texture.height,0,0));rim.SetFloat("_AttachmentBlend",.1f);rim.SetFloat("_OpaqueRoofStart",.74f);
Part("Rounded limestone mouth",Grid(80,50,RimPos,(u,v)=>Vector4.zero,(u,v)=>new Vector4(1-u,v),(u,v)=>new Vector4(visible(RimPos(u,v).x,RimPos(u,v).y),0),(u,v)=>new Color(1,1,1,Mathf.SmoothStep(0,1,(RimPos(u,v).y-floor+.02f)/.04f))),rim,145);
// Native tread uses the same phase as the route, faded at both ends.
var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
typeof(MraTerrain).GetMethod("StepSeams",flags).Invoke(terrain,null);
float refX=58.8f;int tread=(int)typeof(MraTerrain).GetMethod("TreadAt",flags).Invoke(terrain,new object[]{refX});var stripU=typeof(MraTerrain).GetMethod("StripU",flags,null,new[]{typeof(float),typeof(int),typeof(float)},null);
Vector2 topSize=new Vector2(terrain.top.texture.width,terrain.top.texture.height)/terrain.top.pixelsPerUnit;
float ua=(float)stripU.Invoke(terrain,new object[]{refX,tread,topSize.x}),du=((float)stripU.Invoke(terrain,new object[]{refX+.01f,tread,topSize.x})-ua)/.01f,above=terrain.walkLinePx/120f,below=.9f;
Part("Level native walking surface",Grid(90,28,(u,v)=>new Vector3(Mathf.Lerp(58.8f,64.5f,u),floor+Mathf.Lerp(-below,above,v)),(u,v)=>new Vector4(ua+(Mathf.Lerp(58.8f,64.5f,u)-refX)*du,1-(above-Mathf.Lerp(-below,above,v))/topSize.y),(u,v)=>Vector4.zero,(u,v)=>Vector4.zero,(u,v)=>new Color(1,1,1,Mathf.SmoothStep(0,1,u*5)*Mathf.SmoothStep(0,1,(1-u)*12)*Mathf.SmoothStep(0,1,v*5))),Mat("Native floor","Sprites/Default",terrain.top.texture),142);
cave.entrance.transform.position=new Vector3(62.9f,floor,0);cave.arrival.transform.position=new Vector3(58.4f,floor+.66f,0);if(cave.arrival is MraArrival arrival)arrival.facing=-1;
typeof(CaveRolloutAuthor).GetMethod("SetPortal",hidden).Invoke(null,new object[]{cave,1f,mouth,floor,3.02f});
var show=cave.entrance.GetComponent<CaveThresholdPresentation>();show.emergenceFloorRise=0;var trigger=cave.entrance.GetComponent<BoxCollider2D>();trigger.size=new Vector2(1.1f,1.7f);trigger.offset=new Vector2(0,.8f);EditorUtility.SetDirty(trigger);
EditorUtility.SetDirty(cave.arrival);EditorUtility.SetDirty(show);
if(invariant!=Newtonsoft.Json.JsonConvert.SerializeObject(new{cave.connectionId,cave.lowerRoomId,cave.upperAreaId,cave.ArrivalId,cave.requirements}))throw new Exception("Connection metadata changed");
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
return new{cave="PC01",mouth,floor,depth,height,portal=new[]{62.9f,floor},arrival=new[]{58.4f,floor+.66f},sourceImagesUnmodified=true};




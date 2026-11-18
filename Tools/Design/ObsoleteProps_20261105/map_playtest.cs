if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Edit mode required");
UnityEditor.AssetDatabase.ImportAsset("Assets/Resources/QolossalAtlas/AtlasData.json",UnityEditor.ImportAssetOptions.ForceUpdate|UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var expectedAtlas=JsonUtility.FromJson<Qolossal.Atlas.AtlasData>(System.IO.File.ReadAllText("Assets/Resources/QolossalAtlas/AtlasData.json"));
string output="C:/UnityProjects/Qolossal/Tools/Design/ObsoleteProps_20261105/Map/",originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
System.IO.Directory.CreateDirectory(output);
var hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;var fileField=typeof(GameSave).GetField("fileName",hidden);var dataField=typeof(GameSave).GetField("data",hidden);string oldName=(string)fileField.GetValue(null),normalPath=GameSave.FilePath,testSave=output+"ui_save_"+System.Guid.NewGuid().ToString("N")+".json";
System.Func<string,string> hash=p=>{if(!System.IO.File.Exists(p))return null;using(var s=System.IO.File.OpenRead(p))using(var sha=System.Security.Cryptography.SHA256.Create())return System.BitConverter.ToString(sha.ComputeHash(s));};var hashes=new[]{normalPath,normalPath+".bak",normalPath+".tmp"}.ToDictionary(p=>p,hash);
bool oldEnabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,oldBackground=Application.runInBackground;var oldOptions=UnityEditor.EditorSettings.enterPlayModeOptions;
var asm=typeof(UnityEditor.Editor).Assembly;var gvType=asm.GetType("UnityEditor.GameView");var gv=UnityEditor.EditorWindow.GetWindow(gvType);var sizeProp=gvType.GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);int oldSize=(int)sizeProp.GetValue(gv);
var sizesType=asm.GetType("UnityEditor.GameViewSizes");var sizes=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);var group=sizesType.GetProperty("currentGroup").GetValue(sizes);var groupType=group.GetType();int oldTotal=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null);
System.Action<int,int> resolution=(w,h)=>{var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=System.Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");var size=System.Activator.CreateInstance(sizeType,new object[]{kind,w,h,"Atlas temporary "+w});groupType.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null)-1;sizeProp.SetValue(gv,index);gv.Repaint();};
UnityEngine.InputSystem.Keyboard keyboard=null;UnityEngine.InputSystem.Mouse mouse=null;UnityEngine.InputSystem.Gamepad pad=null;
object oldData=dataField.GetValue(null);
var results=new System.Collections.Generic.List<object>();var errors=new System.Collections.Generic.List<string>();int step=0,lastFrame=-1;double at=0,deadline=UnityEditor.EditorApplication.timeSinceStartup+90;bool started=false,stopping=false,complete=false;float oldWidth=0;Vector2 oldCenter=Vector2.zero;int oldRoomCount=0,loadedFrame=0;
System.Func<Qolossal.Atlas.ConnectedAtlas> atlas=()=>MraChart.Instance.Atlas;
System.Action<bool,string> check=(yes,message)=>{results.Add(new{message,passed=yes,frame=Time.frameCount,resolution=new[]{Screen.width,Screen.height}});if(!yes)throw new System.Exception(message);};
System.Action<UnityEngine.InputSystem.Key[]> keys=k=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(k));
System.Action<UnityEngine.InputSystem.LowLevel.GamepadButton?> button=b=>{var state=new UnityEngine.InputSystem.LowLevel.GamepadState();if(b.HasValue)state=state.WithButton(b.Value);UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad,state);};
System.Action<Vector2,bool,float> pointer=(p,down,scroll)=>{var state=new UnityEngine.InputSystem.LowLevel.MouseState{position=p,scroll=new Vector2(0,scroll)};if(down)state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,state);};
System.Func<string,Vector2> uiPoint=name=>{var t=atlas().Canvas.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name).transform;return RectTransformUtility.WorldToScreenPoint(null,t.position);};
System.Action<string> capture=name=>{foreach(var g in atlas().Canvas.GetComponentsInChildren<Qolossal.Atlas.AtlasZoneGlyph>()){check(g.GetComponent<CanvasRenderer>()!=null,"Terrain selector has a renderer");using(var v=new UnityEngine.UI.VertexHelper()){typeof(Qolossal.Atlas.AtlasZoneGlyph).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly).Invoke(g,new object[]{v});check(v.currentVertCount>0,"Terrain selector contains silhouette geometry");}}UnityEngine.ScreenCapture.CaptureScreenshot(output+name+".png");};
System.Action labelCheck=()=>{check(atlas().Viewport.GetComponentsInChildren<UnityEngine.UI.Text>().Length==0,"No visible area names or numbers");check(atlas().Canvas.GetComponentsInChildren<Qolossal.Atlas.AtlasZoneGlyph>().Length==7,"Seven terrain silhouettes replace named tabs");check(atlas().Data.edges.Where(e=>e.kind=="cave"&&atlas().Nodes[e.from].region==atlas().FocusedRegion).All(e=>e.path.All(p=>atlas().ViewBounds.Contains(p))),"Focused view contains complete tunnel routes");var texts=atlas().Viewport.GetComponentsInChildren<UnityEngine.UI.Text>().Where(t=>t.gameObject.activeInHierarchy).ToArray();var bounds=texts.Select(t=>new Rect(t.rectTransform.anchoredPosition-t.rectTransform.sizeDelta/2,t.rectTransform.sizeDelta)).ToArray();for(int i=0;i<bounds.Length;i++){check(atlas().Viewport.rect.Contains(bounds[i].min)&&atlas().Viewport.rect.Contains(bounds[i].max),texts[i].name+" label stays inside map");for(int j=i+1;j<bounds.Length;j++)if(bounds[i].Overlaps(bounds[j]))throw new Exception("Labels overlap: "+texts[i].name+" / "+texts[j].name);}check(true,"Visible labels do not overlap in "+atlas().FocusedRegion);};
var steps=new System.Collections.Generic.List<(float wait,string name,System.Action act)>();
steps.Add((2,"devices and 1080p",()=>{keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();resolution(1920,1080);}));
steps.Add((1,"open with M",()=>keys(new[]{UnityEngine.InputSystem.Key.M})));
steps.Add((.25f,"release M",()=>keys(new UnityEngine.InputSystem.Key[0])));
steps.Add((1,"world view",()=>{check(MraChart.IsOpen&&atlas().IsShowing&&Time.timeScale==0&&GamePauseMenu.BlocksGameplayInput,"M opens atlas and blocks gameplay");check(atlas().Data.nodes.Length==173&&atlas().ActiveCaveCount==37,"173 areas/rooms and 37 active cave pairs");check(!atlas().Data.caves.Any(c=>CaveAccessPolicy.IsRetiredEntrance(c.lowerRoomId)),"No removed entrances above or below");check(atlas().PlayerMapPoint(out var p),"Qori has a live atlas marker");capture("World_1080p");}));
steps.Add((.5f,"discovered-world fixture",()=>{check(atlas().KnownRoomCount==0,"Fresh isolated save hides undiscovered caves");foreach(var n in expectedAtlas.nodes.Where(n=>n.type=="underground"))GameSave.SetFlag(Qolossal.Atlas.CaveDiscovery.Flag(n.id));GameSave.SetFlag(MraState.RevealFlag);MraChart.Instance.ShowWorldAtlas();}));
steps.Add((.5f,"discovered world",()=>{check(atlas().KnownRoomCount==89&&atlas().VisibleRoomCount==173,"Discovered fixture shows all 89 caves and 84 surface zones");check(atlas().Data.edges.Count(e=>e.kind=="cave"&&atlas().EdgeVisible(e))==37,"All 37 discovered cave links are visible");capture("Discovered_World_1080p");}));
steps.Add((.25f,"hover upper Causeway",()=>pointer(atlas().ScreenPoint(atlas().Nodes["MR02_N02"].point),false,0)));
steps.Add((.5f,"click upper Causeway",()=>pointer(atlas().ScreenPoint(atlas().Nodes["MR02_N02"].point),true,0)));
steps.Add((.2f,"release click",()=>pointer(atlas().ScreenPoint(atlas().Nodes["MR02_N02"].point),false,0)));
steps.Add((.6f,"combined region",()=>{check(atlas().FocusedRegion=="MR02","Clicking a surface area focuses its complete level");check(atlas().Data.nodes.Where(n=>n.region=="MR02").All(n=>atlas().ViewBounds.Contains(n.Bounds.min)&&atlas().ViewBounds.Contains(n.Bounds.max)),"Region frame includes every upper area and underground room");oldRoomCount=atlas().VisibleRoomCount;capture("Causeway_click_1080p");}));
steps.Add((.4f,"Cradle composition",()=>atlas().FocusRegion("MR01")));
steps.Add((.5f,"Cradle capture",()=>{labelCheck();capture("MR01_1080p");}));
foreach(var rid in new[]{"MR02","MR03","MR04","MR05","MR06","MR07"}){
 steps.Add((.25f,"focus "+rid,()=>atlas().FocusRegion(rid)));
 steps.Add((.35f,"verify "+rid,()=>{check(atlas().Data.nodes.Where(n=>n.region==rid).All(n=>atlas().ViewBounds.Contains(n.Bounds.min)&&atlas().ViewBounds.Contains(n.Bounds.max)),rid+" contains upper and underground");labelCheck();capture(rid+"_1080p");}));
}
steps.Add((.3f,"actual geometry",()=>{var actual=atlas().Data;check(actual.illustratedMap&&actual.sceneDerived&&actual.contours.Length==expectedAtlas.contours.Length&&actual.contours.Zip(expectedAtlas.contours,(a,b)=>a.region==b.region&&a.room==b.room&&a.mappingId==b.mappingId&&a.kind==b.kind&&a.underground==b.underground&&a.primary==b.primary&&a.path.SequenceEqual(b.path)).All(x=>x),"Actual terrain contours loaded and match canonical coordinates");check(!atlas().Canvas.GetComponentsInChildren<UnityEngine.UI.RawImage>(true).Single().enabled,"Old unregistered painting hidden");}));
steps.Add((.3f,"revised cave mouth registration",()=>{
 var a=atlas();
 check(a.Data.edges.Single(e=>e.id=="CAVE_A01").from=="MR04_N00","A01 tunnel begins in the west-mountain surface zone");
 foreach(string cid in new[]{"CAVE_A01","CAVE_PC04","CAVE_PC01"}){
  var actual=a.Data.caves.Single(c=>c.connectionId==cid);var expected=expectedAtlas.caves.Single(c=>c.connectionId==cid);
  check(Vector2.Distance(actual.upperPoint,expected.upperPoint)<.001f&&Vector2.Distance(actual.lowerPoint,expected.lowerPoint)<.001f,cid+" both mouth markers match fresh canonical extraction");
 }
 a.FocusRegion("MR01");labelCheck();capture("Cradle_PC01_updated_1080p");
}));
steps.Add((.3f,"per-room marker projection",()=>{
 var a=atlas();a.ShowWorld();var player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();var rootObject=player.transform.root.gameObject;var oldScene=rootObject.scene;var oldPosition=player.transform.position;
 var temporary=UnityEngine.SceneManagement.SceneManager.CreateScene("Qolossal_Underground");
 try{
  UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootObject,temporary);
  foreach(var n in a.Data.nodes.Where(n=>n.type=="underground"))foreach(float t in new[]{.15f,.5f,.85f}){
   player.transform.position=new Vector3(n.worldBounds[0]+n.worldBounds[2]*t,n.worldBounds[1]+n.worldBounds[3]*t,oldPosition.z);
   var expected=new Vector2(n.Bounds.x+n.Bounds.width*t,n.Bounds.yMax-n.Bounds.height*t);
   check(a.PlayerMapPoint(out var actual)&&Vector2.Distance(expected,actual)<.02f,"Marker projection "+n.id+" at "+t);
  }
 }finally{player.transform.position=oldPosition;UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rootObject,oldScene);UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(temporary);}
}));
steps.Add((.5f,"switch to 4K",()=>resolution(3840,2160)));
steps.Add((1,"world 4K",()=>{check(Screen.width==3840&&Screen.height==2160,"Native 3840 by 2160 output active");atlas().ShowWorld();capture("World_4K");}));
steps.Add((.5f,"Heights 4K",()=>atlas().FocusRegion("MR06")));
steps.Add((.5f,"Heights 4K capture",()=>{labelCheck();capture("MR06_4K");}));
steps.Add((.3f,"controller next input",()=>button(UnityEngine.InputSystem.LowLevel.GamepadButton.RightShoulder)));
steps.Add((.4f,"controller next region",()=>{button(null);check(atlas().FocusedRegion=="MR07","Controller shoulder focuses next region");labelCheck();capture("MR07_controller_4K");}));
steps.Add((.3f,"controller back",()=>button(UnityEngine.InputSystem.LowLevel.GamepadButton.East)));
steps.Add((.4f,"controller world",()=>{button(null);check(atlas().FocusedRegion==null&&atlas().IsShowing,"Controller back returns to world");}));
steps.Add((.3f,"controller close",()=>button(UnityEngine.InputSystem.LowLevel.GamepadButton.East)));
steps.Add((.4f,"controller closed",()=>{button(null);check(!MraChart.IsOpen&&!atlas().IsShowing,"Controller back closes world map");}));
Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors.Add(m);};Application.logMessageReceived+=log;
fileField.SetValue(null,testSave);dataField.SetValue(null,null);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=UnityEditor.EnterPlayModeOptions.DisableDomainReload;Application.runInBackground=true;
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity");
UnityEditor.EditorApplication.CallbackFunction tick=null;tick=()=>{try{
 if(stopping){if(UnityEditor.EditorApplication.isPlaying)return;UnityEditor.EditorApplication.update-=tick;Application.logMessageReceived-=log;if(keyboard!=null&&keyboard.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);if(pad!=null&&pad.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);fileField.SetValue(null,oldName);dataField.SetValue(null,oldData);UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;UnityEditor.EditorSettings.enterPlayModeOptions=oldOptions;Application.runInBackground=oldBackground;sizeProp.SetValue(gv,oldSize);while((int)groupType.GetMethod("GetTotalCount").Invoke(group,null)>oldTotal){int total=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null);groupType.GetMethod("RemoveCustomSize").Invoke(group,new object[]{total-1});if((int)groupType.GetMethod("GetTotalCount").Invoke(group,null)==total)break;}UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScene);System.IO.File.WriteAllText(output+"ui_tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{complete,passed=complete&&errors.Count==0,step,steps=steps.Count,results,errors,normalSavesPreserved=hashes.All(h=>hash(h.Key)==h.Value),settingsRestored=true,lastFrame},Newtonsoft.Json.Formatting.Indented));return;}
 UnityEditor.EditorApplication.QueuePlayerLoopUpdate();if(!UnityEditor.EditorApplication.isPlaying){if(UnityEditor.EditorApplication.timeSinceStartup>deadline)throw new System.Exception("Play start timed out");return;}if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;if(!started){started=true;at=Time.realtimeSinceStartupAsDouble;}
 if(steps[step].name=="underground chart"&&Time.frameCount-loadedFrame<4)return;
 if(Time.realtimeSinceStartupAsDouble-at<steps[step].wait)return;steps[step].act();System.IO.File.WriteAllText(output+"ui_progress.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{step,total=steps.Count,label=steps[step].name,frame=Time.frameCount}));step++;at=Time.realtimeSinceStartupAsDouble;if(step==steps.Count){complete=true;stopping=true;UnityEditor.EditorApplication.isPlaying=false;}
 }catch(System.Exception e){errors.Add(e.ToString());stopping=true;UnityEditor.EditorApplication.isPlaying=false;}};
UnityEditor.EditorApplication.update+=tick;UnityEditor.EditorApplication.isPlaying=true;return new{started=true,steps=steps.Count,testSave};







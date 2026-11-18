from pathlib import Path
p=Path(__file__).parent
old=(p.parent/'CisternLayers_20261107/verify_render.cs').read_text()
old=old.replace('CisternLayers_20261107','CisternNetwork_20261107')
start=old.index('var compareObject=')
end=old.index('var go=',start)
old=old[:start]+'''var compareObject=new GameObject("Network verifier");var comparison=compareObject.AddComponent<CisternNetworkReview>();comparison.Configure();
'''+old[end:]
old=old.replace('comparison.SetGentle(gentle,false)','comparison.SetLayered(gentle,false)')
start=old.index('art.sharedMaterial=vistas["A04"]')
end=old.index('var report=',start)
old=old[:start]+'''art.sharedMaterial=vistas["A04"].GetComponent<SpriteRenderer>().sharedMaterial;go.transform.position=new Vector3(1618,108,0);
var far=Shader.GetGlobalTexture("_CisternFar");
var black=new RenderTexture(far.width,far.height,0,RenderTextureFormat.ARGB32){dimension=UnityEngine.Rendering.TextureDimension.Tex2DArray,volumeDepth=9};black.Create();
for(int i=0;i<9;i++){Graphics.SetRenderTarget(black,0,CubemapFace.Unknown,i);GL.Clear(false,true,Color.black);}
foreach(string layer in new[]{"far","middle"}){
 comparison.SetLayered(layer=="middle",false);Shader.SetGlobalTexture("_CisternFar",layer=="middle"?black:far);Shader.SetGlobalFloat("_CisternEnclosureCount",0);
 foreach(var offset in new[]{Vector2.zero,new Vector2(2,0),new Vector2(0,2)}){
 cam.transform.position=new Vector3(1618+offset.x,108+offset.y,-50);CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
 System.IO.File.WriteAllBytes(output+layer+"_"+offset.x+"_"+offset.y+".png",tex.EncodeToPNG());
 }}
Shader.SetGlobalTexture("_CisternFar",far);black.Release();UnityEngine.Object.DestroyImmediate(black);
bool outsideUnchanged=vistas.Where(v=>!CisternNetworkReview.Includes(v.Key)).All(v=>v.Value.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_NetworkEnabled")==0);
UnityEngine.Object.DestroyImmediate(compareObject);
bool restored=vistas.All(v=>v.Value.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_NetworkEnabled")==0)&&passages.Values.All(v=>v.sharedMaterial.GetFloat("_NetworkEnabled")==0&&v.sharedMaterial.GetFloat("_B_NetworkEnabled")==0);
'''+old[end:]
(p/'verify_render.cs').write_text(old)
(p/'measure.py').write_text((p.parent/'CisternLayers_20261107/measure.py').read_text())
play=(p.parent/'CisternLayers_20261107/playtest.cs').read_text().replace('CisternLayers_20261107','CisternNetwork_20261107').replace('CaveParallaxComparison','CisternNetworkReview')
a=play.index('comparison=new GameObject(')
b=play.index('Place(1610);',a)
play=play[:a]+'''comparison=new GameObject("Network playtest").AddComponent<CisternNetworkReview>();comparison.Configure();if(!comparison.Healthy)throw new Exception("Missing network bindings");'''+play[b:]
# Keep the actual walking and glide regression, then visit each real arrival.
a=play.index('steps.Add(("surface load"')
b=play.index('fileField.SetValue',a)
play=play[:a]+'''foreach(int roomIndex in Enumerable.Range(0,8)) {
 int index=roomIndex;
 steps.Add(("room "+index+" settle",2.5,()=>{Neutral();comparison.VisitRoom(index);}));
 steps.Add(("room "+index+" measure",.7,()=>Begin(CisternNetworkReview.Rooms[index])));
 steps.Add(("room "+index+" capture",.2,()=>{if(!comparison.Healthy)errors.Add("Bindings lost in "+CisternNetworkReview.Rooms[index]);Finish(true);}));
}
steps.Add(("compare keyboard",.3,()=>{comparison.SetLayered(true,false);UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F8));}));
steps.Add(("keyboard result",.3,()=>{keyboardPassed=!comparison.Layered;UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());comparePress=true;}));
steps.Add(("controller result",.3,()=>{padPassed=comparison.Layered;comparePress=false;if(!keyboardPassed||!padPassed)errors.Add("Comparison input failed");}));
'''+play[b:]
(p/'playtest.cs').write_text(play)
launcher=(p.parent/'CisternLayers_20261107/start_user_trial.cs').read_text().replace('CisternLayers_20261107','CisternNetwork_20261107').replace('CaveParallaxComparison','CisternNetworkReview').replace('c.BindingCount==7&&c.HasContinuousPainting','c.Healthy').replace('comparison.SetGentle(true)','comparison.SetLayered(true)').replace('comparison.Gentle','comparison.Layered').replace('A04/A05 and matching passage ends only','A01-A08 and matching passage ends').replace('F6: return to start','F6: return to start; F7: next room')
(p/'start_user_trial.cs').write_text(launcher)

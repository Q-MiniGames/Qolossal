from pathlib import Path
p=Path(__file__).parent
s=Path('Tools/Design/PassageReview_20261104/playtest.cs').read_text()
s=s.replace('PassageReview_20261104/Play/','CaveParallaxCompare_20261105/Play/')
s=s.replace('string run="passages"','string run="fixed versus gentle"')
s=s.replace('UnityEngine.InputSystem.Gamepad pad=null;','CaveParallaxComparison comparison=null; UnityEngine.InputSystem.Keyboard keyboard=null; bool comparePress=false,keyboardPassed=false,padPassed=false; UnityEngine.InputSystem.Gamepad pad=null;')
a=s.index('steps.Add(("initialize"'); b=s.index('fileField.SetValue(null,testSave)',a)
s=s[:a]+'''steps.Add(("initialize",2,()=>{pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();foreach(var gate in UnityEngine.Object.FindObjectsByType<MraGate>(FindObjectsSortMode.None)){var room=gate.GetComponentInParent<UndergroundRoom>();if(room!=null&&CaveParallaxComparison.Includes(room.roomId))gate.SetOpen(true);}comparison=new GameObject("Cave comparison test").AddComponent<CaveParallaxComparison>();comparison.Configure(new Vector2(1610,107.5f));Place(1610);}));
foreach(var mode in new[]{"A","B"}){
 string current=mode;
 if(mode=="B"){
  steps.Add(("press F8",.2,()=>UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F8))));
  steps.Add(("release F8",.3,()=>{UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());keyboardPassed=comparison.Gentle;if(!keyboardPassed)errors.Add("F8 did not switch to gentle mode");}));
 }
 steps.Add((current+" settle",1,()=>Place(1610)));
 steps.Add((current+" approach",1,()=>Begin(current+"_approach")));
 steps.Add((current+" capture approach",.1,()=>{Finish(true);var bridge=CavePassagePresentation.Instance;if(bridge==null||bridge.ConnectedCount!=63||bridge.MissingCount!=0)errors.Add("Runtime passage setup incomplete");}));
 steps.Add((current+" walk into passage",4,()=>{Begin(current+"_walk_into_passage");moving=true;targetX=1624;}));
 steps.Add((current+" check crossing",.1,()=>{if(Player().transform.position.x<1623.5f)errors.Add(current+" passage crossing failed");Finish(true);}));
 steps.Add((current+" walk back",4,()=>{Begin(current+"_walk_back");moving=true;targetX=1610;}));
 steps.Add((current+" check return",.1,()=>{if(Player().transform.position.x>1610.5f)errors.Add(current+" passage return failed");Finish(true);}));
}
steps.Add(("controller mode switch",.2,()=>comparePress=true));
steps.Add(("release controller",.3,()=>{comparePress=false;padPassed=!comparison.Gentle;if(!padPassed)errors.Add("Left stick click did not switch to fixed mode");}));
steps.Add(("verify input controls",.1,()=>{if(!keyboardPassed||!padPassed)errors.Add("Input comparison checks failed");}));
''' + s[b:]
s=s.replace('if(jump)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);','if(jump)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.South);if(comparePress)input=input.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.LeftStick);')
s=s.replace('fileField.SetValue(null,oldName);','if(keyboard!=null&&keyboard.added)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);fileField.SetValue(null,oldName);')
s=s.replace('normalSavesPreserved=saves,settingsRestored=true','normalSavesPreserved=saves,settingsRestored=true,keyboardPassed,padPassed')
(p/'playtest.cs').write_text(s)

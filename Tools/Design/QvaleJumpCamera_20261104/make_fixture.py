from pathlib import Path

root = Path(__file__).resolve().parents[3]
task = Path(__file__).resolve().parent
source = (root / 'Tools/Design/QvaleVista_20261104/playtest_after.cs').read_text()
source = source.replace('QvaleVista_20261104/"+run', 'QvaleJumpCamera_20261104/"+run')
source = source.replace('float renderedSize=5;', 'float renderedSize=5; int oldFrameRate=Application.targetFrameRate,oldVSync=QualitySettings.vSyncCount;')
source = source.replace('VistaZone.CurrentLift});', 'VistaZone.CurrentLift,Time.time,Time.deltaTime,b.min.y,b.max.y});')
source = source.replace('Application.runInBackground=oldBackground;', 'Application.runInBackground=oldBackground;Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVSync;')
source = source.replace('Application.runInBackground=true;', 'Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;')
source = source.replace('bool passed=samples.Count>=10&&min>=.075f&&max<=.85f;', '''float maxCameraStep=samples.Count>1?samples.Skip(1).Select((s,i)=>Mathf.Abs(s[4]-samples[i][4])).Max():0;
float cameraTravel=samples.Count>0?samples.Max(s=>s[4])-samples.Min(s=>s[4]):0;
float playerTravel=samples.Count>0?samples.Max(s=>s[10])-samples.Min(s=>s[10]):0;
int airborneFrames=samples.Count(s=>s[6]==0);
bool ordinaryJump=label.Contains("jump");
bool passed=samples.Count>=10&&min>=.075f&&max<=.85f&&(!ordinaryJump||(maxCameraStep<.1f&&cameraTravel<.3f&&playerTravel>1.5f&&airborneFrames>8));''')
source = source.replace('frames=samples.Count,minFeet', 'frames=samples.Count,maxCameraStep,cameraTravel,playerTravel,airborneFrames,minFeet')
# Repeat ordinary jumps at two render caps, plus a running jump.
needle = ' steps.Add(("launch",3.8'
insert = ''' foreach(int fps in new[]{60,30}){
  int cap=fps;
  steps.Add(("jump cap setup",2,()=>{Place(129);Application.targetFrameRate=cap;}));
  steps.Add(("repeat jump",.45,()=>{Begin("repeat_jump_"+cap);jump=true;}));
  steps.Add(("release",2,()=>jump=false));steps.Add(("check repeat",.1,()=>Finish()));
 }
 steps.Add(("running jump setup",2,()=>{Place(175);Application.targetFrameRate=60;}));
 steps.Add(("run before jump",.8,()=>{moving=true;targetX=195;}));
 steps.Add(("running jump",.45,()=>{Begin("running_jump");jump=true;}));
 steps.Add(("release running jump",2,()=>jump=false));steps.Add(("check running jump",.1,()=>Finish()));
'''
source = source.replace(needle, insert+needle)
(task/'playtest_after.cs').write_text(source)
# A focused reproduction before the fix, using exactly the same motion checks.
start=source.index('if(run!="before"){')
end=source.index('fileField.SetValue(null,testSave);')
before = source[:start] + '''steps.Add(("jump setup",1,()=>Place(129)));
steps.Add(("jump",.45,()=>{Begin("street_jump");jump=true;}));
steps.Add(("release jump",2,()=>jump=false));steps.Add(("check jump",.1,()=>Finish()));
''' + source[end:]
before=before.replace('string run="after"','string run="before"')
(task/'playtest_before.cs').write_text(before)
normal = before.replace('string run="before"','string run="normal_zoom"').replace('Place(129)','Place(80)').replace('street_129','outside_80').replace('street_jump','outside_jump')
normal = normal.replace('fileField.SetValue(null,testSave);', '''steps.Add(("high launch",3.8,()=>{Begin("outside_high_launch");if(!Player().LaunchUp(18))throw new Exception("Launch refused");}));steps.Add(("check launch",.1,()=>Finish()));
fileField.SetValue(null,testSave);''')
(task/'playtest_normal_zoom.cs').write_text(normal)

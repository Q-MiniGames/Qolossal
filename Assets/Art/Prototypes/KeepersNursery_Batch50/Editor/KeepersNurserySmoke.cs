using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

[InitializeOnLoad]
public static class KeepersNurserySmoke {
 static PlayerMovement player;static Keyboard keyboard;static bool running,oldWalk;static int phase,index,oldCapture;static float start,next;static readonly List<object> states=new(),captures=new(),errors=new();
 static InputSettings originalSettings,temporarySettings;
 public static bool Running=>running;
 static void FeedInput(){if(!running||keyboard==null)return;InputSystem.QueueStateEvent(keyboard,phase==0?new KeyboardState(Key.D,Key.Space):phase==1?new KeyboardState(Key.D):new KeyboardState(Key.A));}
 static readonly string[] Names={"PLAY_B04_glide","PLAY_P_B04_B05_sample","PLAY_P_B03_B04_sample"};
 static readonly float[] Durations={2,2,2};
 static KeepersNurserySmoke(){EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Stop;Application.logMessageReceived+=Log;}
 static void Log(string message,string stack,LogType type){if(running&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(new{message,stack,type=type.ToString(),frame=Time.frameCount});}
 public static void Start(){if(!Application.isPlaying)throw new Exception("Play mode required");if(!GameSave.FilePath.Replace('\\','/').StartsWith(KeepersNurseryPreview.Delivery))throw new Exception("Isolated save required");if(GamePauseMenu.BlocksGameplayInput)throw new Exception("Gameplay input is blocked; wait for startup before testing");originalSettings=InputSystem.settings;temporarySettings=UnityEngine.Object.Instantiate(originalSettings);temporarySettings.hideFlags=HideFlags.HideAndDontSave;temporarySettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;temporarySettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporarySettings;InputSystem.onBeforeUpdate+=FeedInput;player=UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();if(!player)throw new Exception("Player missing");states.Clear();captures.Clear();errors.Clear();oldWalk=player.ForceWalk;oldCapture=Time.captureFramerate;Time.captureFramerate=60;keyboard=InputSystem.AddDevice<Keyboard>("NurserySmokeKeyboard");player.GetComponent<PlayerAbilityController>().Unlock(Relics.All.First(a=>a.abilityId==Relics.Glidecap));phase=0;running=true;Begin();}
 static void Begin(){index=0;player.ForceWalk=phase!=0;var position=phase==0?new Vector2(1582,285):phase==1?new Vector2(1629,284.66f):new Vector2(1652,319);player.PlaceAt(position,phase==2?-1:1,true);start=Time.time+.1f;next=start;InputSystem.QueueStateEvent(keyboard,phase==0?new KeyboardState(Key.D,Key.Space):phase==1?new KeyboardState(Key.D):new KeyboardState(Key.A));Write();}
 static void Tick(){if(!running)return;if(!Application.isPlaying){Stop();return;}if(Time.time<next)return;
 try{
  float elapsed=Time.time-start;var pos=player.transform.position;var rb=player.GetComponent<Rigidbody2D>();bool glide=player.IsGliding;
  float cx=pos.x,cy=pos.y+1.3f;if(phase==0){cx=Mathf.Clamp(cx,1572.9f,1619.1f);cy=Mathf.Clamp(cy,275,301);}else if(phase==1)cy=286;else cy=Mathf.Clamp(cy,306,318);
  var pose=new KeepersNurseryQA.Pose{name=Names[phase]+"_"+index,x=cx,y=cy,time=elapsed};
  states.Add(new{phase=Names[phase],index,time=Time.time,elapsed,frame=Time.frameCount,position=new[]{pos.x,pos.y},velocity=new[]{rb.linearVelocity.x,rb.linearVelocity.y},gliding=glide,grounded=player.IsGrounded,moveInput=player.MoveInput,jumpHeld=player.JumpHeld,inputBlocked=GamePauseMenu.BlocksGameplayInput,deviceEnabled=keyboard.enabled,save=GameSave.FilePath});
  foreach(int width in new[]{3840,1920}){
   captures.Add(KeepersNurseryQA.Capture(pose,width,"terrain",true));if(phase==0)captures.Add(KeepersNurseryQA.Capture(pose,width,"far",true));foreach(bool near in new[]{true,false})foreach(string kind in new[]{"context","background"})captures.Add(KeepersNurseryQA.Capture(pose,width,kind,near));
  }
  index++;if(index==5){phase++;if(phase==3){Stop();KeepersNurseryPreview.Visit();return;}Begin();}else next=start+Durations[phase]*index/4;
  Write();
 }catch(Exception e){errors.Add(new{message=e.ToString()});Stop();Debug.LogException(e);}}
 static void Write(){File.WriteAllText(KeepersNurseryPreview.Data+"play_smoke.json",JsonConvert.SerializeObject(new{running,phase,index,isolatedSave=GameSave.FilePath,simulationCaptureRate=60,states,errors,records=captures},Formatting.Indented));}
 public static void Stop(){if(!running)return;running=false;InputSystem.onBeforeUpdate-=FeedInput;if(keyboard!=null){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.ResetDevice(keyboard);InputSystem.Update();InputSystem.RemoveDevice(keyboard);keyboard=null;}if(player){player.ForceWalk=oldWalk;var body=player.GetComponent<Rigidbody2D>();if(body)body.linearVelocity=Vector2.zero;}if(originalSettings){InputSystem.settings=originalSettings;originalSettings=null;}if(temporarySettings){UnityEngine.Object.DestroyImmediate(temporarySettings);temporarySettings=null;}Time.captureFramerate=oldCapture;KeepersNurseryPreview.SetNear(true);Write();}
}

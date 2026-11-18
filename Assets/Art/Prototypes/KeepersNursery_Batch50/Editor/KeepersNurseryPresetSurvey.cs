using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
[InitializeOnLoad] public static class KeepersNurseryPresetSurvey {
 static Queue<(KeepersNurseryGrid.Pose pose,string kind)> jobs=new();
 static Dictionary<string,JObject> records=new();static float oldTime;static bool running;static string destination;static int completed,total;
 public static int Pending=>jobs.Count;
 static KeepersNurseryPresetSurvey(){EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cleanup;}
 public static void Start(string preset,bool pilot=false,string[] kinds=null){
  if(!Application.isPlaying||Pending>0)throw new Exception("Need idle runtime preview");
  KeepersNurseryPreview.SuspendLiveMeasurement=true;KeepersNurseryPreview.SetPreset(preset);destination=KeepersNurseryPreview.SurveyPath;Directory.CreateDirectory(destination+"QA");oldTime=Time.timeScale;running=true;Time.timeScale=0;
  var source="C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Art/Codex_v2_full/Batch50/50b-r5/sources/";
  records.Clear();var prior=JObject.Parse(File.ReadAllText(File.Exists(destination+"grid_receipt.json")?destination+"grid_receipt.json":source+"grid_receipt.json"));
  foreach(JObject r in prior["records"]){var file=(string)r["file"];if(!Path.IsPathRooted(file))r["file"]=source+"captures/"+file;records[Path.GetFileName(file)]=r;}
  var poses=JsonConvert.DeserializeObject<KeepersNurseryGrid.Config>(File.ReadAllText(KeepersNurseryPreview.Data+"grid.json")).poses;
  for(int i=0;i<poses.Length;i++)if(!pilot||i%10==0||poses[i].area=="USER")foreach(var kind in kinds??new[]{"context","background","far"})jobs.Enqueue((poses[i],kind));
  total=jobs.Count;completed=0;Write(null);
 }
 static void Tick(){if(Pending==0||EditorApplication.isCompiling||EditorApplication.isUpdating)return;try{var j=jobs.Dequeue();var r=JObject.FromObject(KeepersNurseryQA.Capture(j.pose,1920,j.kind,true));var key=(string)r["file"];r["file"]=KeepersNurseryQA.Output+key;r["preset"]=KeepersNurseryPreview.SelectedPreset;records[key]=r;completed++;if(completed%10==0||Pending==0)Write(null);if(Pending==0)Cleanup();}catch(Exception e){jobs.Clear();Write(e.ToString());Cleanup();Debug.LogException(e);}}
 static void Cleanup(){if(!running)return;Time.timeScale=oldTime;running=false;KeepersNurseryPreview.SuspendLiveMeasurement=false;}
 static void Write(string error){File.WriteAllText(destination+"progress.json",JsonConvert.SerializeObject(new{preset=KeepersNurseryPreview.SelectedPreset,total,completed,pending=Pending,error},Formatting.Indented));File.WriteAllText(destination+"grid_receipt.json",JsonConvert.SerializeObject(new{preset=KeepersNurseryPreview.SelectedPreset,runtime=true,records=records.Values},Formatting.Indented));}
}

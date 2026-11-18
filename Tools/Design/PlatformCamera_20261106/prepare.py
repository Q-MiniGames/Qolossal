from pathlib import Path
p=Path(__file__).parent
s=(p.parent/'CaveMotion_20261106/playtest.cs').read_text().replace('Tools/Design/CaveMotion_20261106/Play/','Tools/Design/PlatformCamera_20261106/Play/').replace('"Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png"','CisternContinuousPreview.ArtPath')
s=s.replace('cam.transform.position.x});','cam.transform.position.x,cam.GetComponent<CameraFollow>().PlatformFraming?1f:0f});')
s=s.replace('bool ordinaryJump=label.Contains("jump");','bool ordinaryJump=label=="qvale_jump";')
a=s.index('steps.Add(("edge settle"')
b=s.index('fileField.SetValue(null,testSave)',a)
s=s[:a]+'''
steps.Add(("ground idle",1,()=>Begin("ground_idle")));
steps.Add(("ground check",.2,()=>{if(Mathf.Abs(samples.Last()[1]-.15f)>.02f||samples.Last()[15]!=0)errors.Add("Solid ground framing changed");Finish(true);}));
steps.Add(("walk to platform",6,()=>{Begin("ground_to_platform");moving=true;targetX=1630;}));
steps.Add(("platform arrival check",.2,()=>{if(Player().transform.position.x<1629.5f)errors.Add("Did not reach platform");if(samples.Last()[15]!=1||Mathf.Abs(samples.Last()[1]-.4f)>.02f)errors.Add("Platform did not settle at 40 percent");if(samples.Skip(1).Select((r,i)=>Mathf.Abs(r[4]-samples[i][4])).Max()>.2f)errors.Add("Ground/platform transition snapped");Finish(true);}));
steps.Add(("platform jump",.35,()=>{Begin("platform_jump");jump=true;}));
steps.Add(("platform jump release",1.8,()=>jump=false));
steps.Add(("platform jump check",.2,()=>{if(samples.Count(r=>r[6]==0)<10)errors.Add("Platform jump did not leave floor");if(samples.Any(r=>r[15]!=1))errors.Add("Platform framing lost on takeoff");if(samples.Skip(1).Select((r,i)=>Mathf.Abs(r[4]-samples[i][4])).Max()>.25f)errors.Add("Platform jump camera snapped");Finish(true);}));
steps.Add(("platform settle",1.5,()=>{}));
steps.Add(("platform idle",.8,()=>Begin("platform_idle")));
steps.Add(("platform check",.2,()=>{if(Mathf.Abs(samples.Last()[1]-.4f)>.02f)errors.Add("Platform landing reverted to 15 percent");Finish();}));
steps.Add(("return to ground",6,()=>{Begin("platform_to_ground");moving=true;targetX=1610;}));
steps.Add(("ground return check",.2,()=>{if(Player().transform.position.x>1610.5f||samples.Last()[15]!=0||Mathf.Abs(samples.Last()[1]-.15f)>.02f)errors.Add("Returning to ground failed");if(samples.Skip(1).Select((r,i)=>Mathf.Abs(r[4]-samples[i][4])).Max()>.2f)errors.Add("Platform/ground transition snapped");Finish(true);}));
steps.Add(("platform classification",.2,()=>{var platforms=UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None).Where(e=>e.useOneWay&&e.isActiveAndEnabled).SelectMany(e=>e.GetComponents<Collider2D>()).Where(c=>c.usedByEffector).ToArray();if(platforms.Length<20||platforms.Any(c=>!CameraPlatformSurface.IsPlatform(c)))errors.Add("One-way platform classification failed");var movers=UnityEngine.Object.FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None);if(movers.Any(m=>m.solid!=null&&!CameraPlatformSurface.IsPlatform(m.solid)))errors.Add("Moving platform classification failed");var crumble=UnityEngine.Object.FindObjectsByType<CrumblePlatform>(FindObjectsSortMode.None);if(crumble.Any(m=>m.solid!=null&&!CameraPlatformSurface.IsPlatform(m.solid)))errors.Add("Crumble classification failed");System.IO.File.WriteAllText(output+"classification.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{oneWay=platforms.Length,movers=movers.Length,crumble=crumble.Length}));}));
steps.Add(("Qvale load",2,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MRAtlas_MR04_Qvale")));
steps.Add(("Qvale setup",2,()=>{var hit=Physics2D.RaycastAll(new Vector2(129,250),Vector2.down,300,LayerMask.GetMask("Ground")).First(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);Player().PlaceAt(new Vector2(129,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();}));
steps.Add(("Qvale jump",.45,()=>{Begin("qvale_jump");jump=true;}));
steps.Add(("Qvale release",2,()=>jump=false));
steps.Add(("Qvale check",.2,()=>{if(samples.Any(r=>r[15]!=0))errors.Add("Qvale terrain misclassified as platform");Finish(true);}));
''' + s[b:]
(p/'playtest.cs').write_text(s)

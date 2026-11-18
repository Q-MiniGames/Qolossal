from pathlib import Path
p=Path(__file__).parent
s=(p.parent/'PlatformCamera_20261106/playtest.cs').read_text()
s=s.replace('PlatformCamera_20261106/Play/','LedgeCamera_20261106/Play/')
s=s.replace('"ground_idle"','"solid_ledge_idle"').replace('"ground_to_platform"','"ledge_to_platform"').replace('"platform_to_ground"','"platform_to_ledge"')
s=s.replace('Mathf.Abs(samples.Last()[1]-.15f)>.02f||samples.Last()[15]!=0','Mathf.Abs(samples.Last()[1]-.4f)>.02f||samples.Last()[15]!=1')
s=s.replace('samples.Last()[15]!=0||Mathf.Abs(samples.Last()[1]-.15f)>.02f','samples.Last()[15]!=1||Mathf.Abs(samples.Last()[1]-.4f)>.02f')
s=s.replace('Solid ground framing changed','Solid ledge did not use platform framing').replace('Returning to ground failed','Returning to solid ledge failed')
at='steps.Add(("walk to platform"'
extra='''steps.Add(("solid ledge jump",.35,()=>{Begin("solid_ledge_jump");jump=true;}));
steps.Add(("solid ledge release",1.8,()=>jump=false));
steps.Add(("solid ledge jump check",1,()=>{if(samples.Any(r=>r[15]!=1)||samples.Count(r=>r[6]==0)<10)errors.Add("Solid ledge jump framing failed");Finish(true);}));
'''
s=s.replace(at,extra+at)
at='steps.Add(("Qvale load"'
extra='''steps.Add(("cave floor setup",2,()=>{var hit=Physics2D.RaycastAll(new Vector2(1610,102),Vector2.down,40,LayerMask.GetMask("Ground")).First(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f&&!CameraPlatformSurface.IsPlatform(h.collider));Player().PlaceAt(new Vector2(1610,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();}));
steps.Add(("cave floor idle",1,()=>Begin("cave_main_floor")));
steps.Add(("cave floor check",.2,()=>{if(samples.Last()[15]!=0||Mathf.Abs(samples.Last()[1]-.15f)>.02f)errors.Add("Main cave floor lost 15 percent framing");Finish(true);}));
steps.Add(("Cradle load",2,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MRAtlas_MR01_Cradle")));
steps.Add(("Cradle ledge setup",2,()=>{var hit=Physics2D.RaycastAll(new Vector2(219.8f,30),Vector2.down,12,LayerMask.GetMask("Ground")).First(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);if(!CameraPlatformSurface.IsPlatform(hit.collider))errors.Add("Cradle solid ledge not classified");Player().PlaceAt(new Vector2(219.8f,hit.point.y+.66f),1,true);Physics2D.SyncTransforms();}));
steps.Add(("Cradle ledge idle",1,()=>Begin("surface_ledge_idle")));
steps.Add(("Cradle ledge check",.2,()=>{if(samples.Last()[15]!=1||Mathf.Abs(samples.Last()[1]-.4f)>.02f||samples.Last()[12]>.01f)errors.Add("Surface solid ledge framing failed");Finish(true);}));
'''
s=s.replace(at,extra+at)
s=s.replace('steps.Add(("Qvale jump",','steps.Add(("Qvale floor idle",1,()=>Begin("qvale_main_ground")));\nsteps.Add(("Qvale floor check",.2,()=>{if(samples.Last()[15]!=0||Mathf.Abs(samples.Last()[1]-.15f)>.02f)errors.Add("Qvale ground framing changed");Finish(true);}));\nsteps.Add(("Qvale jump",')
(p/'playtest.cs').write_text(s)
print('Prepared ledge and ground runtime checks')

from pathlib import Path
p=Path(__file__).parent
s=(p.parent/'CisternContinuous_20261106/playtest.cs').read_text()
s=s.replace('Tools/Design/CisternContinuous_20261106/Play/','Tools/Design/CaveMotion_20261106/Play/')
s=s.replace('bool jump=false,moving=false,monitor=false;','bool manualPad=false;bool jump=false,moving=false,monitor=false;')
s=s.replace('void Neutral(){jump=false;','void Neutral(){manualPad=false;jump=false;')
s=s.replace('b.min.y,b.max.y});','b.min.y,b.max.y,cam.GetComponent<CameraFollow>().DownwardPreview,Player().IsGliding?1f:0f,cam.transform.position.x});')
s=s.replace('new Vector4(1620,109.5f,48,16)','CaveParallaxComparison.ContinuousReviewRect')
a=s.index('foreach(var mode in new[]{"A","B"})')
b=s.index('fileField.SetValue(null,testSave)',a)
s=s[:a]+'''
steps.Add(("edge settle",2,()=>Place(1627.8f)));
steps.Add(("cave edge idle",2,()=>Begin("cave_edge_idle")));
steps.Add(("edge check",.2,()=>{if(samples.Max(r=>r[12])>.001f)errors.Add("Automatic cave edge preview still active");Finish(true);}));
steps.Add(("manual cave look down",2,()=>{manualPad=true;Begin("manual_cave_look_down");}));
steps.Add(("manual check",.2,()=>{if(samples.Max(r=>r[12])<2)errors.Add("Manual look down lost");Finish();}));
steps.Add(("release settle",2.5,()=>{}));
steps.Add(("release measure",.5,()=>Begin("cave_release")));
steps.Add(("release check",.2,()=>{if(samples.Last()[12]>.05f)errors.Add("Manual preview did not release");Finish();}));
steps.Add(("glide setup",.15,()=>{var p=Player();p.GetComponent<PlayerAbilityController>().Unlock(Relics.All.First(a=>a.abilityId==Relics.Glidecap));p.PlaceAt(new Vector2(1590,106),-1,true);p.GetComponent<Rigidbody2D>().linearVelocity=new Vector2(-2,-2);Physics2D.SyncTransforms();jump=true;moving=true;targetX=1578;}));
steps.Add(("glide",5,()=>{Begin("cave_glide");jump=true;moving=true;targetX=1578;}));
steps.Add(("glide check",.2,()=>{if(samples.Count(r=>r[13]>0)<30)errors.Add("Glide did not remain active");if(samples.Max(r=>r[5])-samples.Min(r=>r[5])<4)errors.Add("Glide did not travel");if(samples.Max(r=>r[12])>.001f)errors.Add("Automatic preview during glide");Finish(true);}));
steps.Add(("surface load",2,()=>UnityEngine.SceneManagement.SceneManager.LoadScene("MRAtlas_MR05_Ribwood")));
steps.Add(("surface edge setup",2,()=>{var hit=Physics2D.RaycastAll(new Vector2(183,250),Vector2.down,300,LayerMask.GetMask("Ground")).First(h=>h.collider!=null&&!h.collider.isTrigger&&h.normal.y>.6f);Player().PlaceAt(new Vector2(183,hit.point.y+.66f),-1,true);Physics2D.SyncTransforms();}));
steps.Add(("surface edge",2,()=>Begin("surface_edge")));
steps.Add(("surface check",.2,()=>{if(samples.Max(r=>r[12])<.5f)errors.Add("Surface automatic preview changed");Finish(true);}));
''' +s[b:]
s=s.replace('if(jump)input=input.WithButton','input.rightStick=manualPad?Vector2.down:Vector2.zero;if(jump)input=input.WithButton')
(p/'playtest.cs').write_text(s)
# Keep the original isolated render fixture; use the new shared world rectangle.
r=(p.parent/'CisternContinuous_20261106/verify_render.cs').read_text().replace('Tools/Design/CisternContinuous_20261106/','Tools/Design/CaveMotion_20261106/').replace('new Vector4(1620,109.5f,48,16)','CaveParallaxComparison.ContinuousReviewRect')
(p/'verify_render.cs').write_text(r)
m=(p.parent/'CaveParallaxCompare_20261106/measure.py').read_text()
(p/'measure.py').write_text(m)

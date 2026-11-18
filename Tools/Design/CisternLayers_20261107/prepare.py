from pathlib import Path
import hashlib,json
p=Path(__file__).parent
(p/'scene_hashes_before.json').write_text(json.dumps({str(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in Path('Assets/Scenes/Game').glob('*.unity')}))
for name in ['verify_render.cs','playtest.cs']:
 s=(p.parent/'CaveMotion_20261106'/name).read_text().replace('CaveMotion_20261106','CisternLayers_20261107')
 s=s.replace('"Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png"','CisternContinuousPreview.ArtPath')
 s=s.replace('CaveParallaxComparison.ContinuousReviewRect);','CaveParallaxComparison.LayeredReviewRect);comparison.SetMiddlePainting(AssetDatabase.LoadAssetAtPath<Texture2D>(CisternContinuousPreview.MiddleArtPath),CaveParallaxComparison.MiddleReviewRect);comparison.SetGentle(true,false);')
 if name=='playtest.cs':
  key='steps.Add(("edge settle"'
  extra='''steps.Add(("solid ledge idle",1,()=>Begin("solid_ledge")));
steps.Add(("solid ledge capture",.2,()=>Finish(true)));
steps.Add(("walk through passage",6,()=>{Begin("passage_walk");moving=true;targetX=1630;}));
steps.Add(("passage walk check",.2,()=>{if(Player().transform.position.x<1629.5f)errors.Add("Passage walk incomplete");Finish(true);}));
'''
  s=s.replace(key,extra+key)
  s=s.replace('max<=.85f','max<=(label=="manual_cave_look_down"?.901f:.85f)')
 else:
  a=s.index('art.sharedMaterial=vistas["A04"]')
  b=s.index('bool outsideUnchanged=',a)
  s=s[:a]+'''art.sharedMaterial=vistas["A04"].GetComponent<SpriteRenderer>().sharedMaterial;go.transform.position=new Vector3(1618,108,0);
comparison.SetGentle(true,false);var material=art.sharedMaterial;var far=material.GetTexture("_Continuous");
foreach(string layer in new[]{"far","middle"}){
 material.SetFloat("_MiddleEnabled",layer=="middle"?1:0);material.SetTexture("_Continuous",layer=="middle"?Texture2D.blackTexture:far);
 foreach(var offset in new[]{Vector2.zero,new Vector2(2,0),new Vector2(0,2)}){
 cam.transform.position=new Vector3(1618+offset.x,108+offset.y,-50);CaveDepth.Apply(cam);cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
 System.IO.File.WriteAllBytes(output+layer+"_"+offset.x+"_"+offset.y+".png",tex.EncodeToPNG());
 }}
material.SetTexture("_Continuous",far);material.SetFloat("_MiddleEnabled",1);
''' +s[b:]
  s=s.replace('bool restored=vistas.All','bool middleRestored=vistas.All(v=>v.Value.GetComponent<SpriteRenderer>().sharedMaterial.GetFloat("_MiddleEnabled")==0)&&passages.Values.All(v=>v.sharedMaterial.GetFloat("_MiddleEnabled")==0&&v.sharedMaterial.GetFloat("_B_MiddleEnabled")==0);bool restored=middleRestored&&vistas.All')
 (p/name).write_text(s)
s=(p.parent/'LedgeCamera_20261106/start_user_trial.cs').read_text().replace('LedgeCamera_20261106','CisternLayers_20261107').replace('comparison.SetGentle(false)','comparison.SetGentle(true)')
(p/'start_user_trial.cs').write_text(s)
(p/'resume_save.json').write_bytes((p.parent/'LedgeCamera_20261106/resume_save.json').read_bytes())
print('Prepared shared-layer render and live traversal checks')

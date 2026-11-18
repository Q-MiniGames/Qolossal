from pathlib import Path
import json, hashlib
p=Path(__file__).parent
old=p.parent/'CaveParallaxCompare_20261105'
insert='comparison.SetContinuousPainting(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Prototypes/CisternContinuous_20261106/CisternPanorama.png"),new Vector4(1620,109.5f,48,16));'
marker='comparison.Configure(new Vector2(1610,107.5f));'
for name in ('verify_render.cs','playtest.cs','start_user_trial.cs'):
 s=(old/name).read_text().replace('Tools/Design/CaveParallaxCompare_20261105/','Tools/Design/CisternContinuous_20261106/').replace(marker,marker+insert)
 if name=='start_user_trial.cs':
  s=s.replace('comparison.SetGentle(true);','comparison.SetGentle(false);')
 if name=='playtest.cs':
  s=s.replace('targetX=1624','targetX=1630').replace('position.x<1623.5f','position.x<1629.5f').replace('passage",4,','passage",6,').replace('back",4,','back",6,')
 if name=='verify_render.cs':
  s=s.replace('v.sharedMaterial.GetVector("_B_Parallax").z==0','v.sharedMaterial.GetVector("_B_Parallax").z==0&&v.sharedMaterial.GetFloat("_ContinuousEnabled")==0&&v.sharedMaterial.GetFloat("_B_ContinuousEnabled")==0')
 (p/name).write_text(s)
(p/'resume_save.json').write_bytes((old/'resume_save.json').read_bytes())
scenes={str(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in Path('Assets/Scenes/Game').glob('*.unity')}
(p/'scene_hashes_before.json').write_text(json.dumps(scenes,indent=2))
(p/'resolution.json').write_text(json.dumps(dict(native=[2172,724],worldSize=[48,16],requiredPixelsPerUnit=216,requiredNative=[10368,3456],actualPixelsPerUnit=45.25,native4KPassed=False,status='Composition review draft only; new native detail required before production acceptance',sourceResized=False),indent=2))

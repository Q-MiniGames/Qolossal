from pathlib import Path
from PIL import Image
import json, hashlib, math
p=Path(__file__).parent
root=p.parents[2]
layout=json.loads((p/'Views/layout.json').read_text())
art=root/'Assets/Art/Prototypes'
far=[art/'CisternNetwork_20261107'/f'{name}.png' for name in ['SluiceArrival_Far','DrainWell_Far','Reservoir_Far','Spillway_Far','PumpVault_Far','SiltWalk_Far','KeeperGallery_Far','GuardianSluice_Far']]
far[2]=art/'CisternLayers_20261107/Reservoir_Far.png'
middle=[art/'CisternLayers_20261107/Waterworks_Middle_v2.png']+[art/'CisternNetwork_20261107'/f'{name}.png' for name in ['PumpWheel_Middle','KeeperRefuge_Middle','DrainMechanism_Middle']]
assets=[]
for paths,rects,kind in [(far,layout['farRects'],'far draft'),(middle,layout['middleRects'],'middle')]:
 for path,rect in zip(paths,rects):
  with Image.open(path) as image: size=image.size
  # Account for float round-off in Unity vector JSON; round a mathematical integer before ceil.
  required=[round(value*216) if abs(value*216-round(value*216))<.001 else math.ceil(value*216) for value in rect[2:]]
  assets.append(dict(path=str(path.resolve()),layer=kind,native=size,worldSize=rect[2:],required4K=required,nativeDensityPass=all(a>=b for a,b in zip(size,required)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
path=art/'CisternNetwork_20261107/PassageStone.png'
with Image.open(path) as image:size=image.size
assets.append(dict(path=str(path.resolve()),layer='attached passage stone',native=size,worldSize=[5.6,5.6],required4K=[1210,1210],nativeDensityPass=all(a>=1210 for a in size),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(p/'resolution.json').write_text(json.dumps(dict(output=[3840,2160],cameraHalfHeight=5,screenPixelsPerWorldUnit=216,production4KAccepted=False,assets=assets),indent=2))
scenes=json.loads((p/'scenes_before.json').read_text(encoding='utf-8-sig'))
same=all(hashlib.sha256(Path(x['Path']).read_bytes()).hexdigest().upper()==x['Hash'] for x in scenes)
render=json.loads((p/'render_checks.json').read_text())
motion=json.loads((p/'motion_measurements.json').read_text())
play=json.loads((p/'Play/result.json').read_text()) if (p/'Play/result.json').exists() else {}
qa=dict(scope='A01-A08 Editor review; A09/A10 and Heart interiors unchanged',sceneCount=len(scenes),sceneHashesUnchanged=same,bindings=render['overrides'],seamSamples=render['tested'],seamsPassed=not render['failures'],outsideUnchanged=render['outsideUnchanged'],restored=render['restored'],motionPassed=motion['passed'],playComplete=play.get('complete',False),playErrors=play.get('errors'),playRows=[dict(label=r['label'],passed=r['passed'],frames=r['frames']) for r in play.get('rows',[])],normalSavesPreserved=play.get('normalSavesPreserved',False),keyboardPassed=play.get('keyboardPassed',False),controllerPassed=play.get('padPassed',False),previewArrayBytes=layout['arrayBytes'],production4KAccepted=False,consoleHardware='not tested')
qa['passed']=all([same,not render['failures'],render['overrides']==27,render['outsideUnchanged'],render['restored'],motion['passed'],qa['playComplete'],not qa['playErrors'],qa['normalSavesPreserved'],qa['keyboardPassed'],qa['controllerPassed'],all(x['passed'] for x in qa['playRows'])])
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2))

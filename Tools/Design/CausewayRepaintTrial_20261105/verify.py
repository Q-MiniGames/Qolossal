import json,hashlib
from pathlib import Path
from PIL import Image
import numpy as np

p=Path(__file__).parent
records=json.loads((p/'Preview/captures.json').read_text())
inventory=json.loads(Path('Tools/Design/SurfaceContrast_20261104/inventory.json').read_text())
region=next(r for r in inventory if r['scene']=='MRAtlas_MR02_Causeway')
caves=[]
for r in records:
    if not r['label'].startswith('cave_'):continue
    name=Path(r['file']).name
    a=np.asarray(Image.open(Path('Tools/Design/SurfaceContrast_20261104/Before')/name)).astype(np.int16)
    b=np.asarray(Image.open(p/'Preview'/name)).astype(np.int16)
    cave=r['label'][5:]
    holes=[h for t in region['terrains'] if t['active'] for h in t['holes'] or [] if h['roomId']==cave]
    lo=min(v['x'] for h in holes for v in h['outline'])-1.9
    hi=max(v['x'] for h in holes for v in h['outline'])+1.9
    x=(np.arange(r['w'])+.5)/r['w']*(10*16/9)+r['x']-5*16/9
    delta=np.max(abs(a-b),axis=2)[:,(x>=lo)&(x<=hi)]
    caves.append({'mouth':cave,'maximumRGBDifference':int(delta.max())})
protected=json.loads((p/'protected_hashes.json').read_text())
hashes={f:hashlib.sha256(Path(f).read_bytes()).hexdigest()==sha for f,sha in protected.items()}
play=json.loads((p/'Play/result.json').read_text())
source=Path('Assets/Resources/Terrain/CausewayRepaintTrial/DarkBedrock.png')
qa={'scope':'Causeway upper terrain trial only','protectedCaves':caves,'protectedHashes':hashes,'import':json.loads((p/'import.json').read_text()),'play':play,'paintSHA256':hashlib.sha256(source.read_bytes()).hexdigest(),'tileRepeatWorldUnits':1254*.8/256,'tileNativeWorldCoverage':1254/256,'nativePixelsPerWorldUnit':256,'requiredPixelsPerWorldUnit':216,'sourceResized':False,'sampling':'20% source-border overlap, explicit mip gradients; no mirrored rocks','sceneGeometryAndCameraChanged':False,'platformReadiness':{'tested':'Windows Unity Editor 6000.3.19f1, URP, 1080p and 3840x2160','targets':['Windows PC','PS5','Xbox Series X','Xbox Series S','Switch 2 handheld','Switch 2 docked'],'physicalController':'Not tested; virtual gamepad movement verified','consoleHardware':'Not tested','standaloneBuild':'Not tested','cpuGpuFrameTimes':'Not measured','peakMemory':'Not measured','textureImport':'Native 1254x1254 sRGB, uncompressed, mipmaps, no NPOT resize'}}
assert all(hashes.values())
assert len(caves)==4 and all(c['maximumRGBDifference']==0 for c in caves)
assert play['complete'] and not play['errors'] and all(r['passed'] for r in play['rows']) and play['normalSavesPreserved']
assert not qa['import']['errors']
qa['passed']=True
(p/'QA.json').write_text(json.dumps(qa,indent=2))
print(json.dumps({'passed':True,'caves':caves,'play':play['rows'],'allProtectedHashesUnchanged':all(hashes.values())},indent=2))

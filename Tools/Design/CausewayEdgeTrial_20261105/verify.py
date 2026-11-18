import json,hashlib
from pathlib import Path
from PIL import Image
import numpy as np
p=Path(__file__).parent
rows=json.loads((p/'Preview/captures.json').read_text())
inventory=json.loads(Path('Tools/Design/SurfaceContrast_20261104/inventory.json').read_text())
region=next(r for r in inventory if r['scene']=='MRAtlas_MR02_Causeway')
checks=[]
for r in rows:
    if not r['label'].startswith('cave_'):continue
    name=Path(r['file']).name
    a=np.asarray(Image.open(Path('Tools/Design/SurfaceContrast_20261104/Before')/name)).astype(np.int16)
    b=np.asarray(Image.open(p/'Preview'/name)).astype(np.int16)
    holes=[h for t in region['terrains'] if t['active'] for h in t['holes'] or [] if h['roomId']==r['label'][5:]]
    lo=min(v['x'] for h in holes for v in h['outline'])-1.9
    hi=max(v['x'] for h in holes for v in h['outline'])+1.9
    ceiling=max(v['y'] for h in holes for v in h['outline'])+.5
    x=(np.arange(r['w'])+.5)/r['w']*(10*16/9)+r['x']-5*16/9
    y=r['y']+5-(np.arange(r['h'])+.5)/r['h']*10
    d=np.max(abs(a-b),axis=2)[np.ix_(y<=ceiling,(x>=lo)&(x<=hi))]
    checks.append({'mouth':r['label'][5:],'protectedRGBDifference':int(d.max())})
a=np.asarray(Image.open('Tools/Design/SurfaceContrast_20261104/Before/MR02_node0.png')).astype(np.int16)
b=np.asarray(Image.open(p/'Preview/MR02_node0.png')).astype(np.int16)
body=int(np.max(abs(a[-100:]-b[-100:])))
protected=json.loads(Path('Tools/Design/CausewayRepaintTrial_20261105/protected_hashes.json').read_text())
hashes={f:hashlib.sha256(Path(f).read_bytes()).hexdigest()==h for f,h in protected.items()}
play=json.loads((p/'Play/result.json').read_text())
assert body==0 and all(c['protectedRGBDifference']==0 for c in checks)
assert all(hashes.values()) and play['complete'] and not play['errors'] and all(r['passed'] for r in play['rows'])
assert play['normalSavesPreserved'] and play['settingsRestored']
qa={'passed':True,'newPaintingReverted':True,'bodyMatchesOriginalRGBMax':body,'caveChecks':checks,'protectedHashes':hashes,'edgeFadeWorldDepth':.26,'maximum4KPixelDepth':.26*216,'play':play,'sourceArtImportsAndGeometryUnchanged':True,'platform':'Windows Editor; consoles, builds and hardware performance not tested'}
(p/'QA.json').write_text(json.dumps(qa,indent=2));print(json.dumps(qa,indent=2))

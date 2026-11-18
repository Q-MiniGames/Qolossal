import json
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

p = Path(__file__).parent
records = json.loads((p/'Before/captures.json').read_text())
inventory = json.loads((p/'inventory.json').read_text())
rows=[]
for r in records:
    name=Path(r['file']).name
    a=np.asarray(Image.open(p/'Before'/name)).astype(np.int16)
    b=np.asarray(Image.open(p/'After'/name)).astype(np.int16)
    diff=np.max(abs(a-b),axis=2)
    row={'view':name,'changedPixels':int((diff>1).sum()),'topHalfMaxDifference':int(diff[:r['h']//2].max())}
    if r['label'].startswith('cave_'):
        cave=r['label'][5:]
        region=next(x for x in inventory if x['scene']==r['scene'])
        holes=[h for t in region['terrains'] if t['active'] for h in (t['holes'] or []) if h['roomId']==cave]
        # MRA route coordinates are registered in scene space (identity terrain transform).
        if holes:
            lo=min(v['x'] for h in holes for v in h['outline'])-1.9
            hi=max(v['x'] for h in holes for v in h['outline'])+1.9
            x=(np.arange(r['w'])+.5)/r['w']*(10*16/9)+(r['x']-5*16/9)
            protected=diff[:,(x>=lo)&(x<=hi)]
            row['protectedCaveMaxDifference']=int(protected.max()) if protected.size else None
            row['protectedCavePixelsAboveOne']=int((protected>1).sum())
    rows.append(row)
(p/'pixel_comparison.json').write_text(json.dumps(rows,indent=2))
# A small phone-friendly comparison using exact same-camera renders; no image repaint.
names=['MR01_node0.png','MR02_node0.png','MR05_node6.png','MR07_node0.png']
sheet=Image.new('RGB',(1440,len(names)*445),(23,29,34));d=ImageDraw.Draw(sheet)
labels=['Cradle','Causeway','Ribwood','Summit']
for i,(name,label) in enumerate(zip(names,labels)):
    y=i*445
    d.text((12,y+8),label+' | BEFORE',fill='white');d.text((732,y+8),label+' | AFTER',fill='white')
    for j,folder in enumerate(['Before','After']):
        sheet.paste(Image.open(p/folder/name).resize((720,405)),(j*720,y+30))
sheet.save(p/'comparison.jpg',quality=94)
print(json.dumps({'views':len(rows),'cavesChecked':sum('protectedCaveMaxDifference' in r for r in rows),'maxProtectedCaveDifference':max(r.get('protectedCaveMaxDifference',0) or 0 for r in rows),'caveFailures':[r for r in rows if r.get('protectedCavePixelsAboveOne',0)>0]},indent=2))

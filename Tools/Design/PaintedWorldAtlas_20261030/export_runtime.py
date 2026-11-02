"""Export the approved connected atlas into Unity's serializable point format."""
raise SystemExit('Superseded: use Tools/Design/MainWorldLayout/export_runtime.py for the approved actual scene layout.')
from pathlib import Path
import json
PROJECT=Path(__file__).resolve().parents[3]
REVIEW=Path(r"C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/PaintedWorldAtlas_20261030")
d=json.loads((REVIEW/'world-map-data.json').read_text(encoding='utf-8'))
world=json.loads((PROJECT/'Assets/MountainReliefAtlas/Resources/MRA/mra_world.json').read_text(encoding='utf-8-sig'))
colors=['#a8cca2','#d6ba82','#afd6cb','#e4c89b','#a7c389','#aac9eb','#c6badd']
d['regions']=[dict(id=r['id'],name=r['name'],color=c) for r,c in zip(world['regions'],colors)]
d['mappings']=[dict(id=k,**v) for k,v in d['transforms'].items()]
for n in d['nodes']:
 if 'mapBounds' not in n:
  t=d['transforms'][n['scene']];x,y,w,h=n['worldBounds']
  n['mapBounds']=[x*t['sx']+t['ox'],t['oy']-(y+h)*t['sy'],w*t['sx'],h*t['sy']]
 n['point']=dict(zip(('x','y'),n['position']))
for e in d['edges']:e['path']=[dict(zip(('x','y'),p)) for p in e['points']]
for c in d['caves']:
 c['upperPoint']=dict(zip(('x','y'),c['mapUpper']));c['lowerPoint']=dict(zip(('x','y'),c['mapLower']))
retired={f'MR{r:02d}_C{c:02d}' for r in range(1,4) for c in range(1,6)}
assert not any(c['lowerRoomId'] in retired for c in d['caves'])
assert len(d['caves'])==37 and len(d['nodes'])==173
out=PROJECT/'Assets/Resources/QolossalAtlas/AtlasData.json'
previous=json.loads(out.read_text(encoding='utf-8-sig')) if out.exists() else None
out.write_text(json.dumps(d,ensure_ascii=False,indent=2),encoding='utf-8')
print('Exported',len(d['nodes']),'nodes,',len(d['caves']),'pairs. Data unchanged:',previous==d)

"""Cartographic assembly using the approved reclining route; never edits playable scenes."""
from pathlib import Path
import json,importlib.util,bisect,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
if (ROOT/'Tools/Design/MainWorldLayout/layout-policy.json').exists():
 raise SystemExit('Superseded by the user-approved actual-terrain layout. Use Tools/Design/MainWorldLayout/build_layout.py; do not warp the map back to the illustrative Titan contour.')
BASE=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal')
OUT=BASE/'DesignReview/PaintedWorldAtlas_20261030'
BEFORE=BASE/'Tools/AtlasTitanContour_20261030/Before'
spec=importlib.util.spec_from_file_location('approved_route',ROOT/'Tools/Design/reclining_route_map.py');ref=importlib.util.module_from_spec(spec);spec.loader.exec_module(ref)
_,samples=ref.catmull(ref.TOP)
route=samples[20*ref.ROUTE_FROM:20*ref.ROUTE_TO+1]
# Use distance along the side profile, so the folded neck/face cannot jump to a different x crossing.
ends=[(152,632),(330,770),(720,412),(1110,752),(1170,742),(1530,540),(1640,565),(1780,272)]
profiles=[]
for a,b in zip(ends,ends[1:]):
 _,poly=ref.catmull(ref.TOP[ref.TOP.index(a):ref.TOP.index(b)+1]);dist=[0.0]
 for p,q in zip(poly,poly[1:]):dist.append(dist[-1]+((p[0]-q[0])**2+(p[1]-q[1])**2)**.5)
 profiles.append([(x/dist[-1],p[1]+80) for x,p in zip(dist,poly)])
d=json.loads((BEFORE/'world-map-data.json').read_text(encoding='utf-8'))
old={n['id']:dict(n) for n in d['nodes']};warps={};bands=[]
colors=['#a8cca2','#d6ba82','#afd6cb','#e4c89b','#a7c389','#aac9eb','#c6badd']
def lerp(x,points):
 i=max(0,min(len(points)-2,bisect.bisect_right([p[0] for p in points],x)-1));a,b=points[i:i+2];t=max(0,min(1,(x-a[0])/(b[0]-a[0])));return a[1]+(b[1]-a[1])*t
for i,reg in enumerate(ref.REGIONS):
 rid=reg[0];ns=sorted([n for n in d['nodes'] if n['region']==rid and n['type']=='surface'],key=lambda n:n['position'][0]);xmin=ns[0]['position'][0];xmax=ns[-1]['position'][0]
 left=152 if i==0 else reg[3];right=1780 if i==6 else reg[4]
 # Preserve the original regional horizontal spacing to keep the 89 underground rooms readable.
 target=[]
 for n in ns:
  x,y=n['position'];u=(x-xmin)/(xmax-xmin);rx=left+u*(right-left)
  ry=lerp(u,profiles[i])
  target.append((x,ry-y))
 warps[rid]=target
 def move(p):return [p[0],p[1]+lerp(p[0],target)]
 for n in ns:
  n['position']=move(n['position'])
  m=d['transforms'][n['scene']];x,y,w,h=n['worldBounds'];bx=x*m['sx']+m['ox'];by=m['oy']-(y+h)*m['sy'];bw=w*m['sx'];bh=h*m['sy']
  n['mapBounds']=[bx,by+lerp(old[n['id']]['position'][0],target),bw,bh]
 for e in d['edges']:
  if e['kind']=='surface' and old[e['from']]['region']==rid:e['points']=[move(p) for p in e['points']]
 # Dense contour samples retain the approved broad rises and falls without adding any route.
 band=[]
 for k in range(161):
  x=xmin+(xmax-xmin)*k/160;u=k/160;rx=left+u*(right-left);ry=lerp(u,profiles[i])
  band.append([x,ry])
 bands.append(dict(region=rid,points=band))
# Join existing region transitions as continuous walk lines, never as new connections.
nodes={n['id']:n for n in d['nodes']}
for e in d['edges']:
 if e['kind']=='scene-transition':
  a=nodes[e['from']]['position'];b=nodes[e['to']]['position'];e['points']=[a,b]
  bands.append(dict(region=old[e['from']]['region'],points=[a,b],connector=True))
for c in d['caves']:
 rid=old[c['upperAreaId']]['region'];x,y=c['mapUpper'];c['mapUpper']=[x,y+lerp(x,warps[rid])]
for e in d['edges']:
 if e['kind']=='cave':
  c=next(c for c in d['caves'] if c['connectionId']==e['id']);a,b=c['mapUpper'],c['mapLower'];rail=min(b[1]-30,max(a[1]+85,(a[1]+b[1])/2))
  e['points']=[a,[a[0],rail],[b[0],rail],b]
d['surfaceBands']=[dict(region=b['region'],connector=b.get('connector',False),path=[dict(x=x,y=y) for x,y in b['points']]) for b in bands]
d['surfaceAssembly']={'reference':'Tools/Design/QOLOSSAL_RECLINING_TITAN_PROPOSAL.md, approved 8 Oct 2026','profile':'Tools/Design/reclining_route_map.py TOP / REGIONS','verticalOffset':80,'regionWarps':warps,'sceneGeometryChanged':False}
d['layout']='Upper route follows the approved reclining landform in one continuous contour. Regional widths are expanded for readable room networks; cave transitions are diagrammatic, not physical shafts.'
(OUT/'world-map-data.json').write_text(json.dumps(d,indent=2),encoding='utf-8')
# Keep the accepted decorative plate and lower geometry; rebuild only changed upper geometry.
NS='http://www.w3.org/2000/svg';ET.register_namespace('',NS)
root=ET.fromstring((BEFORE/'Qolossal_ConnectedWorld.svg').read_text(encoding='utf-8'));tag=lambda n:'{'+NS+'}'+n
find=lambda id:next(e for e in root.iter() if e.get('id')==id)
path=lambda points:'M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in points)
color=lambda rid:colors[int(rid[2:])-1]
surface=find('surface');surface.clear();surface.set('id','surface')
for band in bands:
 pts=band['points'];rid=band['region'];top=[[x,y+10] for x,y in pts];under=[[x,y+90] for x,y in reversed(pts)]
 ET.SubElement(surface,tag('path'),{'d':path(top+under)+' Z','fill':color(rid),'fill-opacity':'.12','stroke':'none'})
 for offset,opacity in [(10,'.75'),(90,'.25')]:ET.SubElement(surface,tag('path'),{'d':path([[x,y+offset] for x,y in pts]),'stroke':'#d6be8c','stroke-width':'2','stroke-opacity':opacity,'fill':'none'})
for e in d['edges']:
 if e['kind']=='surface':
  rid=old[e['from']]['region'];ET.SubElement(surface,tag('path'),{'d':path(e['points']),'class':'walkroom','stroke':color(rid)});ET.SubElement(surface,tag('path'),{'d':path(e['points']),'class':'walkfill','style':'stroke:#12212a;stroke-opacity:.92','stroke':color(rid)})
active={c['upperAreaId'] for c in d['caves']}
for n in d['nodes']:
 if n['type']!='surface' or n['id'] not in active:continue
 x,y,w,h=n['mapBounds'];k=min(8,h/5);g=ET.SubElement(surface,tag('g'),{'class':'node room','data-id':n['id']});ET.SubElement(g,tag('path'),{'d':f'M{x+k} {y} H{x+w-k} L{x+w} {y+k} V{y+h-k} L{x+w-k} {y+h} H{x+k} L{x} {y+h-k} V{y+k} Z','fill':color(n['region']),'fill-opacity':'.2','stroke':color(n['region']),'stroke-width':'3'})
passages=find('passages')
for e in list(passages):
 if e.get('class')=='scene-seam':passages.remove(e)
for e in d['edges']:
 if e['kind']=='scene-transition':ET.SubElement(passages,tag('path'),{'d':path(e['points']),'class':'scene-seam','style':'stroke-dasharray:none;stroke:#dec897;stroke-width:9;opacity:.9','data-from':e['from'],'data-to':e['to']})
g=find('cave-connections');g.clear();g.set('id','cave-connections')
for c in d['caves']:
 e=next(e for e in d['edges'] if e['id']==c['connectionId']);ET.SubElement(g,tag('path'),{'d':path(e['points']),'class':'cave-link','data-from':e['from'],'data-to':e['to'],'data-id':e['id']})
 for x,y in [c['mapUpper'],c['mapLower']]:
  m=ET.SubElement(g,tag('g'),{'class':'mouth node','data-id':c['lowerRoomId'],'data-cave':c['connectionId'],'transform':f'translate({x:.2f} {y:.2f})'});ET.SubElement(m,tag('circle'),{'r':'12'});ET.SubElement(m,tag('path'),{'d':'M-6 5 V0 Q-6-8 0-8 Q6-8 6 0 V5 M-9 6 H9'})
labels=find('labels')
for e in list(labels):
 n=nodes.get(e.get('data-id'))
 if n and n['type']=='surface':
  # Translate label alongside its numbered area, including the line to its point.
  delta=n['position'][1]-old[n['id']]['position'][1];e.set('transform',f'translate(0 {delta:.3f})')
markers=find('markers');markers.clear();markers.set('id','markers')
world=json.loads((ROOT/'Assets/MountainReliefAtlas/Resources/MRA/mra_world.json').read_text())
for r in world['regions']:
 for n in r['nodes']:
  if n.get('waymark'):
   x,y=nodes[n['id']]['position'];ET.SubElement(markers,tag('path'),{'d':f'M{x} {y-12} l10 12-10 12-10-12Z','fill':'#e7d5a4','stroke':'#14202c','stroke-width':'3'})
for parent in root.iter():
 for el in list(parent):
  if el.tag==tag('text') and el.text=='UNDERGROUND':parent.remove(el)
for el in root.iter(tag('text')):
 if el.text and 'Pale dashed seams:' in el.text:el.text='Continuous upper ridge: connected regions of the same landform'
(OUT/'Qolossal_ConnectedWorld.svg').write_text(ET.tostring(root,encoding='unicode'),encoding='utf-8')
print('Reassembled seven upper regions on approved contour; 89 rooms and 37 cave pairs retained.')

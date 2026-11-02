from pathlib import Path
import json,sys,math,shutil,hashlib
ROOT=Path('C:/UnityProjects/Qolossal'); WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal'); REVIEW=WORK/'DesignReview'; OUT=WORK/'Tools/UndergroundRelocation_20261030'; BEFORE=OUT/'Before'
sys.path.insert(0,str(WORK/'.tools/geometry'))
import shapely as S
from shapely.geometry import shape,box,Point,Polygon,LineString
from shapely.affinity import translate
from shapely.ops import unary_union
import numpy as np
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
paths=[ROOT/'Assets/Scenes/Game/Qolossal_Underground.unity',ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json',ROOT/'Assets/Scripts/World/Atlas/AtlasGraphic.cs',ROOT/'Assets/Scripts/World/Atlas/ConnectedAtlas.cs',ROOT/'Tools/Design/MainWorldLayout/topology-source.json',ROOT/'Tools/Design/MainWorldLayout/build_layout.py',ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json',REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json',REVIEW/'MainWorldLayout_20261030/world-map-data.json']
BEFORE.mkdir(exist_ok=True,parents=True)
receipt=[]
for p in paths:
 q=BEFORE/('Project' if p.is_relative_to(ROOT) else 'Artifacts')/p.relative_to(ROOT if p.is_relative_to(ROOT) else WORK);q.parent.mkdir(parents=True,exist_ok=True)
 if not q.exists():shutil.copy2(p,q)
 receipt.append(dict(source=str(p),backup=str(q),sha256=sha(q)))
(OUT/'backup.json').write_text(json.dumps(receipt,indent=2))
geo=read(BEFORE/'Artifacts/DesignReview/ActualUndergroundComparison_20261030/scene_geometry.json')
moves={'MR06_C01':[-205,-110],'PC11':[-91,-62.481],'PC12':[-75,-36.304]}
air=shape(read(REVIEW/'UndergroundRegional_20261023/air_geometry.json')); parts=list(S.get_parts(air));affected=[]
for id,(dx,dy) in moves.items():
 r=next(r for r in geo['rooms'] if r['id']==id);x,y,w,h=r['bounds'];i=next(i for i,p in enumerate(parts) if p.covers(Point(x+w/2,y+h/2)));p=parts[i]
 assert p.area<=w*h+1 and box(x-.1,y-.1,x+w+.1,y+h+.1).covers(p),(id,p.bounds)
 q=translate(p,dx,dy);parts[i]=q;affected.extend([p.buffer(24,quad_segs=6),q.buffer(24,quad_segs=6)])
base=unary_union(parts); mouths=[]
for c in geo['colliders']:
 if 'Embedded return |' not in c['path']:continue
 pts=c['paths'][0];p=Polygon(pts)
 # Keep the existing small floor clearance used by the carve operation.
 p=p.union(translate(p,0,-.12));dx,dy=moves.get(c['room'],[0,0]);mouths.append(translate(p,dx,dy))
cutouts=base.union(unary_union(mouths));solid=base.buffer(24,quad_segs=6).difference(cutouts)
src=read(REVIEW/'UndergroundTerrain_20261022/terrain_build_v2.json');rooms=src['rooms'];rmap={r['id']:r for r in rooms}
for r in rooms:
 dx,dy=moves.get(r['id'],[0,0]);r['x']+=dx;r['y']+=dy
groups={'MR01':0,'MR02':1,'MR03':2,'A':3,'C':4,'MR05':4,'D':5,'MR06':5,'E':6,'MR07':6,'H':7};themes=[]
for ti in range(8):
 shapes=[box(r['x'],r['y'],r['x']+r['w'],r['y']+r['h']) for r in rooms if groups[r['group']]==ti]
 for l in src['connections']:
  assert l['fromRoom'] not in moves and l['toRoom'] not in moves
  if groups[rmap[l['fromRoom']]['group']]==ti and groups[rmap[l['toRoom']]['group']]==ti:shapes.append(LineString([(p['x'],p['y']) for p in l['points']]).buffer(l['width']/2,cap_style=2,join_style=2))
 themes.append(unary_union(shapes))
area=unary_union(affected);x0,y0,x1,y1=area.bounds;chunks=[]
for cx in range(math.floor(x0/64),math.ceil(x1/64)):
 for cy in range(math.floor(y0/64),math.ceil(y1/64)):
  tile=box(cx*64,cy*64,(cx+1)*64,(cy+1)*64)
  if not area.intersects(tile):continue
  verts=[]
  for x in range(cx*64,(cx+1)*64,8):
   for y in range(cy*64,(cy+1)*64,8):
    cell=solid.intersection(box(x,y,x+8,y+8))
    if cell.is_empty:continue
    for t in S.get_parts(S.constrained_delaunay_triangles(cell)):verts.extend(list(t.exterior.coords)[:3])
  weights=[];shade=[]
  if verts:
   p=S.points(np.array(verts));dist=np.stack([S.distance(p,g) for g in themes],axis=1);delta=dist-dist.min(axis=1)[:,None];w=np.maximum(0,1-delta/7)**3;w/=w.sum(axis=1)[:,None];weights=np.round(w,5).tolist();shade=np.round(.76-.38*np.clip((S.distance(p,base)-1.2)/7,0,1),5).tolist()
  chunks.append(dict(name=f'Rock_{cx}_{cy}',v=verts,w=weights,s=shade))
(OUT/'backing.json').write_text(json.dumps(dict(sourceHash=geo['sha256'],moves=moves,chunks=chunks),separators=(',',':')))
print(json.dumps(dict(chunks=len(chunks),vertices=sum(len(c['v']) for c in chunks),solidAirOverlap=solid.intersection(cutouts).area,backupFiles=len(receipt))))

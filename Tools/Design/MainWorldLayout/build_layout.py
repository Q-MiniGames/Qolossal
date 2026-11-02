"""Build the canonical terrain-derived upper + underground review layout.
Per-component display translations preserve internal scene geometry.
No scene writes or runtime publication. Refuse stale scene snapshots.
"""
from pathlib import Path
import json, hashlib, math, copy, html, collections
ROOT=Path(__file__).resolve().parents[3]
REVIEW=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
OUT=REVIEW/'MainWorldLayout_20261030';OUT.mkdir(exist_ok=True)
SOURCE=ROOT/'Tools/Design/MainWorldLayout'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
geo=read(REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json')
inv=read(ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json')
old=read(SOURCE/'topology-source.json')
assert old['sourceUndergroundSceneHash']==inv['sceneHashes']['Qolossal_Underground'], 'Underground scene changed: refresh topology-source.json from current room bounds and passage endpoints before rebuilding.'
world=read(ROOT/'Assets/MountainReliefAtlas/Resources/MRA/mra_world.json')
for name,h in inv['sceneHashes'].items():
 assert sha(ROOT/'Assets/Scenes/Game'/f'{name}.unity')==h, f'Stale topology inventory: {name}. Refresh the scene inventory and passage geometry before regenerating.'
for s in geo['scenes']:
 assert sha(ROOT/s['path'])==s['sha256'],f'Stale terrain extraction: {s["scene"]}. Rerun ActualSlopeComparison_20261030/extract.cs before regenerating.'
# The upper comparison's approved translation convention: 16 Unity-unit scene seams.
SCALE=3.0;SEAM=16;LEFT=180;TOP=300
COLORS=['#b3d99b','#e5c786','#94d6ce','#dfc3a1','#a7c66a','#98c7e8','#d2b7ef']
def ground(ts,x):
 hits=[]
 for t in ts:
  for a,b in zip(t['surface'],t['surface'][1:]):
   if a[0]<=x<=b[0] and b[0]-a[0]>1e-6:hits.append(a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]))
 return max(hits) if hits else None
regs=[];cursor=0;elevation=0
for s,w,color in zip(geo['scenes'],world['regions'],COLORS):
 ts=[t for t in s['terrains'] if '/Terrain/Route ' in t['path'] and t['active'] and t['solid']];a=w['nodes'][0]['position']['x'];b=w['nodes'][-1]['position']['x'];y0=ground(ts,a);y1=ground(ts,b)
 regs.append(dict(id=w['id'],name=w['name'],scene=w['scene'],color=color,start=a,end=b,entryY=y0,exitY=y1,dx=cursor-a,dy=elevation-y0,terrain=ts,source=s,world=w))
 cursor+=b-a+SEAM;elevation+=y1-y0
YTOP=max(p[1]+r['dy'] for r in regs for t in r['terrain'] for p in t['surface'])
W=LEFT*2+(cursor-SEAM)*SCALE
transforms={r['scene']:dict(sx=SCALE,sy=SCALE,ox=LEFT+r['dx']*SCALE,oy=TOP+(YTOP-r['dy'])*SCALE,yFlip=True) for r in regs}
def convert(m,p):return [m['ox']+p[0]*m['sx'],m['oy']-p[1]*m['sy']]
def bounds(m,b):return [m['ox']+b[0]*m['sx'],m['oy']-(b[1]+b[3])*m['sy'],b[2]*m['sx'],b[3]*m['sy']]
def point(p):return dict(zip(('x','y'),p))
nodes=[];edges=[];contours=[];groups={};regionInfo=[]
for r in regs:
 m=transforms[r['scene']];left=convert(m,[r['start'],0])[0];right=convert(m,[r['end'],0])[0]
 # Clip shared terrain to the route anchors when assembling; keep the original points in source provenance.
 for t in r['source']['terrains']:
  if not(t['active'] and t['solid'] and t['colliderEnabled']):continue
  chunks=[];current=[]
  for a,b in zip(t['surface'],t['surface'][1:]):
   lo=max(min(a[0],b[0]),r['start']);hi=min(max(a[0],b[0]),r['end'])
   if lo>hi:continue
   if abs(b[0]-a[0])<1e-7:pts=[a,b]
   else:pts=[[x,a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0])] for x in ([lo,hi] if b[0]>=a[0] else [hi,lo])]
   if current and current[-1]!=pts[0]:chunks.append(current);current=[]
   if not current:current.append(pts[0])
   current.append(pts[1])
  if current:chunks.append(current)
  for ch in chunks:contours.append(dict(region=r['id'],source=t['path'],points=[convert(m,p) for p in ch],primary='/Terrain/Route ' in t['path']))
 for n,a in zip(r['world']['nodes'],sorted(r['source']['areas'],key=lambda a:a['id'])):
  assert n['id']==a['id'];y=ground(r['terrain'],n['position']['x']);p=convert(m,[n['position']['x'],y if y is not None else n['position']['y']])
  nodes.append(dict(id=n['id'],name=a['name'],type='surface',region=r['id'],scene=r['scene'],position=p,point=point(p),worldBounds=a['bounds'],mapBounds=bounds(m,a['bounds'])))
 for e in r['world']['edges']:
  # Topological route centerlines remain separate from the measured terrain contours.
  pts=[convert(m,[p['x'],p['y']]) for p in e['points']]
  edges.append(dict(id=e['id'],**{'from':e['source'],'to':e['target']},kind='surface',points=pts,requires=e.get('requires',[])))
 regionInfo.append(dict(id=r['id'],name=r['name'],color=r['color'],left=left,right=right,labelY=min(p[1] for c in contours if c['region']==r['id'] for p in c['points'])-100,netRise=r['exitY']-r['entryY']))
# Baseline scene registration, followed by user-approved rigid component placement.
ug=read(REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json')
assert ug['sha256']==inv['sceneHashes']['Qolossal_Underground']
ground=[c for c in ug['colliders'] if c['layer']=='Ground' and not c['dynamic']]
upoints=[p for c in ground for ps in c['paths'] for p in ps]
ugTop=max(p[1] for c in contours for p in c['points'])+230
um=dict(sx=SCALE,sy=SCALE,ox=LEFT-min(p[0] for p in upoints)*SCALE,oy=ugTop+max(p[1] for p in upoints)*SCALE,yFlip=True)
# Base cartographic compression before forward component placement.
surfaceSegments=[(a,b) for c in contours if c['primary'] for a,b in zip(c['points'],c['points'][1:])]
gaps=[]
for p in upoints:
 x,y=convert(um,p)
 hits=[a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]) for a,b in surfaceSegments if min(a[0],b[0])<=x<=max(a[0],b[0]) and abs(b[0]-a[0])>1e-7]
 if hits:gaps.append(y-max(hits))
clearance=220.0;removedSeparation=min(gaps)-clearance
um['oy']-=removedSeparation
transforms['Qolossal_Underground']=um
for n in old['nodes']:
 if n['type']!='underground':continue
 q=copy.deepcopy(n);q['mapBounds']=bounds(um,q['worldBounds']);x,y,w,h=q['mapBounds'];q['position']=[x+w/2,y+h/2];q['point']=point(q['position']);nodes.append(q)
from forward_placement import place
placements=place(nodes,old['edges'],inv,transforms,um,convert,bounds,surfaceSegments)
byid={n['id']:n for n in nodes}
def roompoint(id,p):return convert(transforms[byid[id].get('mappingId','Qolossal_Underground')],p)
for g in sorted({n['group'] for n in nodes if n['type']=='underground'}):
 ns=[n for n in nodes if n.get('group')==g];bs=[n['mapBounds'] for n in ns];xmin=min(b[0] for b in bs);ymin=min(b[1] for b in bs);xmax=max(b[0]+b[2] for b in bs);ymax=max(b[1]+b[3] for b in bs)
 groups[g]=dict(id=g,region=ns[0]['region'],bounds=[xmin,ymin,xmax-xmin,ymax-ymin])
undergroundContours=[]
for c in ground:
 for ps in c['paths']:
  pts=ps+([ps[0]] if c['closed'] else [])
  region=byid[c['room']]['region'] if c['room'] else 'shared'
  if not c['room']:
   # Shared wall loops can span several galleries. Attribute contiguous segments
   # by distance to room bounds so region focus never drops its exterior walls.
   # A continuous collider outline must never tear when nearby disconnected
   # caves acquire separate display offsets. Choose one component for the
   # whole outline, while retaining regional coloring inside the deep network.
   votes=collections.defaultdict(float)
   for a,b in zip(pts,pts[1:]):
    center=[(a[0]+b[0])/2,(a[1]+b[1])/2]
    def distance(n):
     x,y,w,h=n['worldBounds'];return max(x-center[0],0,center[0]-x-w)**2+max(y-center[1],0,center[1]-y-h)**2
    near=min((n for n in nodes if n['type']=='underground'),key=distance)
    votes[near['mappingId']]+=math.dist(a,b)
   outlineMapping=max(votes,key=votes.get)
   runs=[];current=[];last=None
   for a,b in zip(pts,pts[1:]):
    center=[(a[0]+b[0])/2,(a[1]+b[1])/2]
    def distance(n):
     x,y,w,h=n['worldBounds'];return max(x-center[0],0,center[0]-x-w)**2+max(y-center[1],0,center[1]-y-h)**2
    nearest=min((n for n in nodes if n.get('mappingId')==outlineMapping),key=distance)
    owner=(nearest['region'],nearest['mappingId'])
    if last!=owner and current:runs.append((last,current));current=[]
    if not current:current=[a]
    current.append(b);last=owner
   if current:runs.append((last,current))
   for (region,mid),run in runs:undergroundContours.append(dict(region=region,mappingId=mid,source=c['path'],room=None,closed=False,points=[convert(transforms[mid],p) for p in run]))
  else:undergroundContours.append(dict(region=region,source=c['path'],room=c['room'],closed=c['closed'],mappingId=byid[c['room']]['mappingId'],points=[roompoint(c['room'],p) for p in pts]))
W=max(W,max(p[0] for c in undergroundContours for p in c['points'])+180)
H=max(p[1] for c in undergroundContours for p in c['points'])+320

for e in old['edges']:
 if e['kind']=='passage':
  q=copy.deepcopy(e);a,b=q['from'],q['to'];wp=[[p['x'],p['y']] for p in e['worldPoints']]
  pts=[roompoint(a,p) for p in wp]
  q['points']=pts;edges.append(q)
 elif e['kind']=='scene-transition':
  q=copy.deepcopy(e);q['points']=[byid[q['from']]['position'],byid[q['to']]['position']];edges.append(q)
caves=[]
def segmentRect(a,b,r):
 x,y,w,h=r;lo,hi=0.,1.;dx,dy=b[0]-a[0],b[1]-a[1]
 for p,q in [(-dx,a[0]-x),(dx,x+w-a[0]),(-dy,a[1]-y),(dy,y+h-a[1])]:
  if abs(p)<1e-9:
   if q<0:return False
  else:
   t=q/p
   if p<0:lo=max(lo,t)
   else:hi=min(hi,t)
 return lo<=hi
upper=[c for c in inv['caves'] if not c['undergroundSide']];lower={c['connectionId']:c for c in inv['caves'] if c['undergroundSide']}
for c in upper:
 q=copy.deepcopy(c);q['lowerPosition']=lower[c['connectionId']]['entrancePosition'];up=convert(transforms[q['upperScene']],q['entrancePosition']);down=roompoint(q['lowerRoomId'],q['lowerPosition']);q['mapUpper']=up;q['mapLower']=down;q['upperPoint']=point(up);q['lowerPoint']=point(down);caves.append(q)
 # Illustrated tunnel route between the same paired access points. These curves
 # describe scene transitions on the map; they do not create playable corridors.
 dx,dy=down[0]-up[0],down[1]-up[1];seed=sum(ord(k) for k in q['connectionId']);bend=min(45,abs(dy)*.12)*(1 if seed%2 else -1)
 obstacles=[]
 for n in nodes:
  if n['type']=='underground' and n['id']!=q['lowerRoomId']:
   x,y,w,h=n['mapBounds'];obstacles.append([x-3,y-3,w+6,h+6])
 for bend in [bend,0,*[v for a in range(40,841,40) for v in [-a,a]]]:
  pts=[]
  for i in range(49):
   t=i/48;u=1-t;x=u*u*u*up[0]+3*u*u*t*(up[0]+dx*.12+bend)+3*u*t*t*(down[0]-dx*.12+bend)+t*t*t*down[0];y=u*u*u*up[1]+3*u*u*t*(up[1]+dy*.35)+3*u*t*t*(down[1]-dy*.35)+t*t*t*down[1];pts.append([x,y])
  if not any(segmentRect(a,b,r) for r in obstacles for a,b in zip(pts,pts[1:])):break
 else:raise AssertionError('No clear illustrated tunnel route for '+q['connectionId'])
 edges.append(dict(id=q['connectionId'],**{'from':q['upperAreaId'],'to':q['lowerRoomId']},kind='cave',points=pts,requirements=q['requirements']))
# Tunnels that cross in projection pass on separate illustrated depths.
# Cut a short gap in the lower drawing layer; never add a graph junction.
tunnels=[e for e in edges if e['kind']=='cave'];bridges=[]
def cross(a,b):return a[0]*b[1]-a[1]*b[0]
for i,e in enumerate(tunnels):
 ps=e['points'];lengths=[0.]
 for a,b in zip(ps,ps[1:]):lengths.append(lengths[-1]+math.dist(a,b))
 cuts=[]
 for later in tunnels[i+1:]:
  if byid[e['from']]['region']!=byid[later['from']]['region']:continue
  for j,(a,b) in enumerate(zip(ps,ps[1:])):
   r=[b[k]-a[k] for k in range(2)]
   for c,z in zip(later['points'],later['points'][1:]):
    s=[z[k]-c[k] for k in range(2)];den=cross(r,s)
    if abs(den)<1e-8:continue
    ca=[c[k]-a[k] for k in range(2)];t=cross(ca,s)/den;u=cross(ca,r)/den
    if 0<t<1 and 0<u<1:
     distance=lengths[j]+t*(lengths[j+1]-lengths[j]);cuts.append([max(0,distance-30),min(lengths[-1],distance+30)]);bridges.append(dict(under=e['id'],over=later['id'],point=[a[k]+r[k]*t for k in range(2)]))
 cuts.sort();merged=[]
 for a,b in cuts:
  if merged and a<=merged[-1][1]:merged[-1][1]=max(merged[-1][1],b)
  else:merged.append([a,b])
 ranges=[];start=0
 for a,b in merged:
  if a>start:ranges.append([start,a])
  start=b
 if start<lengths[-1]:ranges.append([start,lengths[-1]])
 def at(s):
  for j in range(len(ps)-1):
   if lengths[j]<=s<=lengths[j+1]:
    t=(s-lengths[j])/max(.000001,lengths[j+1]-lengths[j]);return [ps[j][k]+(ps[j+1][k]-ps[j][k])*t for k in range(2)]
  return ps[-1]
 e['tunnelSegments']=[[at(a),*[p for p,l in zip(ps,lengths) if a<l<b],at(b)] for a,b in ranges]
for e in edges:e['path']=[point(p) for p in e['points']]
for r in regionInfo:
 ns=[n for n in nodes if n['region']==r['id']];bs=[n['mapBounds'] for n in ns];r['focusBounds']=[min(b[0] for b in bs)-75,min(b[1] for b in bs)-140,max(b[0]+b[2] for b in bs)+75,max(b[1]+b[3] for b in bs)+80]
policy=dict(id='actual-terrain-upper-layout-20261030',approvedBy='User, 30 October 2026',authority='Playable terrain geometry; the illustrated reclining contour is superseded for map geometry.',upperScale=SCALE,sceneSeamUnits=SEAM,sceneAssembly='Translate scene-local entry/exit route anchors; 16-unit display gaps mark scene transitions. Local slopes have equal X/Y scale.',undergroundAssembly='One equal-X/Y affine transform for the complete actual underground scene, including every physical corridor. A common vertical display offset places it below the assembled upper scenes; cave links are transitions, not measured shafts.',futureChangeRule='A terrain or topology edit must refresh extraction, regenerate this canonical layout, then update and verify the Unity map before the edit is complete.',currentStage='Canonical scene-derived layout exported to Unity; prior unregistered terrain painting hidden until replacement artwork follows this geometry.',sceneHashes=inv['sceneHashes'])
data=dict(width=W,height=H,nodes=nodes,edges=edges,caves=caves,transforms=transforms,mappings=[dict(id=k,**v) for k,v in transforms.items()],regions=regionInfo,surfaceContours=contours,undergroundContours=undergroundContours,groups=groups,layout=policy,disclosure='All existing room IDs and gates retained. Cave links are scene transitions, not physical shafts. Layout review contains spoilers.')
data['presentation']=dict(noLabels=True,worldConnections=False,zoneConnections='illustrated recessed tunnels',minimumSurfaceClearance=70,placement='forward connected components',removedBlankSeparation=removedSeparation,sceneGeometryChanged=False)
data['tunnelBridges']=bridges
data['componentPlacements']=placements
policy['undergroundAssembly']='User-approved forward cave placement, 30 October 2026: rigid translation per physically connected underground component. Early networks shift right; isolated caves shift right/up. Equal X/Y scale preserves every internal slope and passage. Continuous deep galleries retain registration. Illustrated tunnels depict scene transitions, not newly built corridors.'
(OUT/'world-map-data.json').write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
(SOURCE/'layout-policy.json').write_text(json.dumps(policy,indent=2),encoding='utf-8')
# Layered editable vector master, with all text and connection markers separate from terrain.
def esc(t):return html.escape(str(t),quote=True)
def line(pts):return 'M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in pts)
def text(x,y,t,size=25,color='#d4dfdb',anchor='start',cl=''):return f'<text x="{x:.2f}" y="{y:.2f}" font-size="{size}" fill="{color}" text-anchor="{anchor}" class="{cl}">{esc(t)}</text>'
colors={r['id']:r['color'] for r in regs};colors['shared']='#ead4a1'
styles='''text{font-family:Segoe UI,Arial,sans-serif}.label text{paint-order:stroke;stroke:#0d1c24;stroke-width:7;stroke-linejoin:round}.node,.region-target{cursor:pointer}.node:hover,.region-target:hover{filter:brightness(1.5)}.cave-link{fill:none;stroke:#dfbc77;stroke-width:3;stroke-dasharray:12 10;opacity:.52}.passage{fill:none;stroke-width:8;stroke-linejoin:round}.scene-seam{fill:none;stroke:#dddcc2;stroke-width:4;stroke-dasharray:11 9}.mouth circle{fill:#10222b;stroke:#f1d69d;stroke-width:3}.mouth path{fill:none;stroke:#f1d69d;stroke-width:2.5}.faded{opacity:.10}.selected path,.selected rect{stroke:#fff1b9!important;stroke-width:6}.selected text{fill:#fff1b9}.selected.cave-link,.selected.passage{stroke:#fff1b9!important;opacity:1;stroke-width:7}.hide-links .cave-link{display:none}.hide-labels .label{display:none}'''
start=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W:.0f}" height="{H:.0f}" viewBox="0 0 {W:.0f} {H:.0f}" id="worldmap"><title>Qolossal — canonical actual terrain and underground layout</title><style>{styles}</style>'
bg=f'<rect width="{W}" height="{H}" fill="#0d1c24"/>'
head=text(110,110,'Q O L O S S A L',62,'#efdfb5')+text(110,160,'MAIN WORLD LAYOUT / ACTUAL TERRAIN',27,'#a7c0c8')+text(W-110,105,'84 upper areas · 89 underground rooms · 37 cave pairs',27,'#c1d0cc','end')+text(W-110,150,'Artwork foundation · 30 October 2026',23,'#8ca6b1','end')
terrain=[];labels=[];regionsvg=[]
for r in regs:
 rc=next(q for q in regionInfo if q['id']==r['id']);primary=[c for c in contours if c['region']==r['id'] and c['primary']]
 for c in primary:
  pts=c['points'];bottom=H-150;poly=pts+[[pts[-1][0],bottom],[pts[0][0],bottom]];terrain.append(f'<path d="{line(poly)} Z" fill="{r["color"]}" fill-opacity=".045"/>')
 for c in [c for c in contours if c['region']==r['id']]:terrain.append(f'<path d="{line(c["points"])}" fill="none" stroke="{r["color"]}" stroke-width="{6 if c["primary"] else 3}" stroke-linejoin="round"/>')
 x=rc['left'];y=rc['labelY'];regionsvg.append(f'<g class="region-target" data-region="{r["id"]}"><rect x="{x-10}" y="{y-50}" width="{rc["right"]-x+20}" height="80" rx="9" fill="#122731" stroke="{r["color"]}" stroke-opacity=".35"/>'+text(x+15,y,r['id']+' / '+r['name'],31,r['color'])+'</g>')
 for i,n in enumerate(n for n in nodes if n['type']=='surface' and n['region']==r['id']):
  x,y=n['position'];labels.append(f'<g class="node label" data-id="{n["id"]}"><title>{esc(n["id"]+" / "+n["name"])}</title><circle cx="{x}" cy="{y}" r="6" fill="{r["color"]}"/>'+text(x,y-24-(i%2)*27,n['id'].split('_')[-1],21,r['color'],'middle')+'</g>')
passages=[];cavelines=[];mouths=[]
for e in edges:
 d=line(e['points']);color=colors[byid[e['from']]['region']];attrs=f'data-from="{e["from"]}" data-to="{e["to"]}" data-id="{e["id"]}"'
 if e['kind']=='passage':passages.append(f'<path d="{d}" fill="none" stroke="#0d1c24" stroke-width="17"/><path d="{d}" class="passage" stroke="{color}" {attrs}/>')
 elif e['kind']=='scene-transition':passages.append(f'<path d="{d}" class="scene-seam" {attrs}/>')
 elif e['kind']=='cave':cavelines.append(f'<path d="{d}" class="cave-link" {attrs}/>')
for c in caves:
 for p in [c['mapUpper'],c['mapLower']]:
  x,y=p;mouths.append(f'<g class="mouth node" data-id="{c["lowerRoomId"]}" data-cave="{c["connectionId"]}" transform="translate({x:.2f} {y:.2f})"><title>{esc(c["upperAreaId"]+" ↔ "+c["lowerRoomId"])}</title><circle r="13"/><path d="M-7 6 V-1 Q-7-10 0-10 Q7-10 7-1 V6 M-10 7 H10"/></g>')
rooms=[];occupied=[]
def overlap(a,b):return a[0]<b[2] and a[2]>b[0] and a[1]<b[3] and a[3]>b[1]
for n in sorted((n for n in nodes if n['type']=='underground'),key=lambda n:-n['mapBounds'][2]*n['mapBounds'][3]):
 id=n['id'];x,y,w,h=n['mapBounds'];color=colors[n['region']];k=min(9,w/8,h/8);d=f'M{x+k} {y} H{x+w-k} L{x+w} {y+k} V{y+h-k} L{x+w-k} {y+h} H{x+k} L{x} {y+h-k} V{y+k} Z'
 rooms.append(f'<g class="node room" data-id="{id}"><title>{esc(id+" / "+n["name"])}</title><rect x="{x}" y="{y}" width="{w}" height="{h}" fill="transparent"/></g>')
 label=id.replace('_','-');fs=24 if len(id)>7 else 27;tw=len(label)*fs*.58;cx,cy=n['position'];candidates=[(cx,cy+8)] if w>tw+10 and h>40 else [];candidates.extend([(cx,y-17),(cx,y+h+30),(cx,y-50),(cx,y+h+65)])
 lx,ly=candidates[0]
 for xx,yy in candidates:
  b=(xx-tw/2-5,yy-fs-5,xx+tw/2+5,yy+8)
  if not any(overlap(b,o) for o in occupied):lx,ly=xx,yy;break
 occupied.append((lx-tw/2-5,ly-fs-5,lx+tw/2+5,ly+8));leader=f'<path d="M{cx} {cy} L{lx} {ly-12}" stroke="{color}" opacity=".45"/>' if ly<y or ly>y+h else ''
 labels.append(f'<g class="node label" data-id="{id}">'+leader+text(lx,ly,label,fs,color,'middle')+'</g>')
# Exact collider outlines and platforms replace the former schematic room boxes.
for c in undergroundContours:
 rooms.append(f'<path d="{line(c["points"])}" fill="none" stroke="{colors[c["region"]]}" stroke-width="2.4" stroke-linejoin="round"/>')
for g,group in []:
 x,y,w,h=group['bounds'];name={'A':'CISTERN GALLERIES / A01–A10','C':'ROOT GARDENS / C01–C10','D':'WIND GALLERIES / D01–D10','E':'MEMORY GALLERIES / E01–E10'}.get(g,g+' / CAVES & POCKETS');regionsvg.append(text(x,y-55,name,23,colors[group['region']]))
foot=text(110,H-180,'LAYOUT LOCK',27,'#efdfb5')+text(110,H-135,'Upper slopes come from playable terrain. Artwork follows this contour.',23)+text(110,H-95,'Solid links: existing passages · Gold dashed links: cave transitions, not shafts.',23)+text(W-110,H-135,'Region click: upper + underground · Room click: connections',23,'#a7c0c8','end')+text(W-110,H-95,'Terrain changes → regenerate this layout → update the Unity map',23,'#efdfb5','end')
svg=start+bg+head+'<g id="terrain">'+''.join(terrain)+'</g><g id="passages">'+''.join(passages)+'</g><g id="cave-connections">'+''.join(cavelines)+'</g><g id="rooms">'+''.join(rooms)+'</g><g id="cave-mouths">'+''.join(mouths)+'</g><g id="region-headings">'+''.join(regionsvg)+'</g><g id="labels">'+''.join(labels)+'</g>'+foot+'</svg>'
(OUT/'Qolossal_MainWorldLayout.svg').write_text(svg,encoding='utf-8')
# A clean geometry-only art underlay; labels and icons remain separate in the master.
underlay=start+bg+'<g id="terrain">'+''.join(terrain)+'</g><g id="passages">'+''.join(passages)+'</g><g id="rooms">'+''.join(rooms)+'</g></svg>'
(OUT/'Qolossal_ArtUnderlay.svg').write_text(underlay,encoding='utf-8')
# Validate topology, endpoint pairs and affine slope preservation before writing the receipt.
assert len(nodes)==173 and len(caves)==37
assert len([e for e in edges if e['kind']=='passage'])==63
assert {n['id'] for n in nodes}=={n['id'] for n in old['nodes']}
assert {(e['id'],e['from'],e['to'],e['kind']) for e in edges}=={(e['id'],e['from'],e['to'],e['kind']) for e in old['edges']}
retired={f'MR{r:02d}_C{c:02d}' for r in range(1,4) for c in range(1,6)};assert not retired & {c['lowerRoomId'] for c in caves}
for r in regs:assert transforms[r['scene']]['sx']==transforms[r['scene']]['sy']==3
for c in caves:
 assert c['lowerRoomId']==lower[c['connectionId']]['lowerRoomId'];assert c['upperAreaId']==lower[c['connectionId']]['upperAreaId']
for a in nodes:
 if a['type']!='underground':continue
 for b in nodes:
  if b['type']!='underground' or a['id']>=b['id']:continue
  x,y,w,h=a['mapBounds'];xx,yy,ww,hh=b['mapBounds'];assert not overlap([x,y,x+w,y+h],[xx,yy,xx+ww,yy+hh]),f'Overlapping rooms {a["id"]} {b["id"]}'
adj={n['id']:set() for n in nodes}
for e in edges:adj[e['from']].add(e['to']);adj[e['to']].add(e['from'])
seen=set();todo=['MR01_N00']
while todo:
 n=todo.pop()
 if n not in seen:seen.add(n);todo.extend(adj[n]-seen)
assert len(seen)==173
inputs=[REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json',REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json',ROOT/'Tools/Design/ConnectedWorldMap_20261030/current_scene_inventory.json',SOURCE/'topology-source.json',ROOT/'Assets/MountainReliefAtlas/Resources/MRA/mra_world.json']
qa=dict(topologyUnchanged=True,all173NodesConnectedIgnoringGates=True,all37CavePairsVerified=True,retiredC01toC05EntrancesAbsent=True,noRoomOverlap=True,upperXYScaleEqual=True,sceneHashes=inv['sceneHashes'],sourceHashes={str(p):sha(p) for p in inputs},layoutHash=sha(OUT/'world-map-data.json'),runtimeIntegrated=True,runtimeStatus='Exact scene-derived contours exported to Unity; live UI verification recorded in UndergroundRelocation_20261030.',supportedTargets=['Windows PC','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked'],notTested=['Player builds','Console hardware','Performance','Memory','Controller coverage'],changedScenes=[])
(OUT/'qa.json').write_text(json.dumps(qa,indent=2),encoding='utf-8')
print(f'Canonical map: {W:.0f} × {H:.0f}, {len(nodes)} nodes, {len(caves)} cave pairs; no room overlaps; sources current.')


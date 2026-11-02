from pathlib import Path
from collections import defaultdict,Counter
from html import escape
import json,math,re,hashlib
ROOT=Path(r'C:/UnityProjects/Qolossal');SRC=ROOT/'Tools/Design/ConnectedWorldMap_20261030';OUT=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/ConnectedWorldMap_20261030')
inv=json.loads((SRC/'current_scene_inventory.json').read_text());world=json.loads((ROOT/'Assets/MountainReliefAtlas/Resources/MRA/mra_world.json').read_text());geo=json.loads(Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/UndergroundRegional_20261023/regional_geometry.json').read_text())
regions=world['regions'];rooms={r['id']:dict(r) for r in geo['rooms']};areas={a['id']:a for a in inv['areas']}
for r in inv['rooms']:
 rooms[r['id']].update(name=r['name'],x=r['bounds'][0],y=r['bounds'][1],w=r['bounds'][2],h=r['bounds'][3])
caves={c['connectionId']:dict(c) for c in inv['caves'] if not c['undergroundSide']}
for c in inv['caves']:
 if c['undergroundSide']:caves[c['connectionId']]['lowerPosition']=c['entrancePosition']
activeSurfaceAreas={c['upperAreaId'] for c in caves.values()}
COLORS=['#a8cca2','#d6ba82','#afd6cb','#e4c89b','#a7c389','#aac9eb','#c6badd']
W,H=7000,2700;pitch=960;left=155
pos={};boxes={};mapping={};nodes=[];edges=[];labels=[];geom=[];cavelines=[];landmarks=[];surfaceRooms=[]
def line(points):return 'M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in points)
def text(x,y,t,size=24,fill='#cfdbc9',anchor='start',cls='',extra=''):
 return f'<text x="{x:.2f}" y="{y:.2f}" fill="{fill}" font-size="{size}" text-anchor="{anchor}" class="{cls}" {extra}>{escape(t)}</text>'
def regionof(room):
 g=room['group'];return {'A':3,'C':4,'D':5,'E':6,'H':4}.get(g,int(g[2:])-1 if g.startswith('MR') else 0)
# Each scene keeps its local shape. Scene-to-scene placement is a cartographic assembly.
for i,reg in enumerate(regions):
 x0=left+i*pitch;ns=reg['nodes'];minx=min(n['position']['x'] for n in ns);maxx=max(n['position']['x'] for n in ns);miny=min(n['position']['y'] for n in ns);sx=780/(maxx-minx);sy=min(sx,1.55)
 mapping[reg['scene']]={'sx':sx,'sy':sy,'ox':x0-minx*sx,'oy':660+miny*sy,'yFlip':True}
 for j,n in enumerate(ns):
  x=x0+(n['position']['x']-minx)*sx;y=660-(n['position']['y']-miny)*sy;pos[n['id']]=(x,y)
  ar=areas[n['id']];b=ar['bounds'];nodes.append({'id':n['id'],'name':ar['name'],'type':'surface','region':reg['id'],'position':[x,y],'worldBounds':b,'scene':reg['scene']})
  bx=x0+(b[0]-minx)*sx;by=660-(b[1]+b[3]-miny)*sy;bw=b[2]*sx;bh=b[3]*sy
  boxes[n['id']]=(bx,by,bw,bh)
  k=min(8,bh/5);d=f'M{bx+k} {by} H{bx+bw-k} L{bx+bw} {by+k} V{by+bh-k} L{bx+bw-k} {by+bh} H{bx+k} L{bx} {by+bh-k} V{by+k} Z'
  if n['id'] in activeSurfaceAreas:surfaceRooms.append(f'<g class="node room" data-id="{n["id"]}"><path d="{d}" fill="{COLORS[i]}" fill-opacity=".2" stroke="{COLORS[i]}" stroke-width="3"/></g>')
  labely=y+65 if j%2==0 else y-51
  labels.append(f'<g class="node label" data-id="{n["id"]}">'+text(x,labely,n['id'].replace('_','-'),20,'#e1e8dc','middle')+'</g>')
  if n.get('waymark'):landmarks.append(f'<path d="M{x} {y-12} l10 12-10 12-10-12Z" fill="#e7d5a4" stroke="#14202c" stroke-width="3"/>')
 for e in reg['edges']:
  pts=[(x0+(p['x']-minx)*sx,660-(p['y']-miny)*sy) for p in e['points']]
  d=line(pts);geom.append(f'<path d="{d}" class="walkroom" stroke="{COLORS[i]}"/><path d="{d}" class="walkfill" stroke="{COLORS[i]}"/>')
  edges.append({'id':e['id'],'from':e['source'],'to':e['target'],'kind':'surface','points':pts,'requires':e.get('requires',[])})
# Local numbered caves, then the four gallery networks under their access regions.
groups=defaultdict(list)
for r in rooms.values():groups[r['group']].append(r)
for g,rs in groups.items():
 if g=='H':continue
 ri=regionof(rs[0]);xmin=min(r['x'] for r in rs);xmax=max(r['x']+r['w'] for r in rs);ymax=max(r['y']+r['h'] for r in rs)
 scale=min(2.65,780/(xmax-xmin));top=930 if g.startswith('MR') else (1030 if g=='A' else 1660)
 ox=left+ri*pitch+(780-(xmax-xmin)*scale)/2-xmin*scale;oy=top+ymax*scale
 mapping['underground:'+g]={'sx':scale,'sy':scale,'ox':ox,'oy':oy,'yFlip':True}
 for r in rs:
  x=ox+r['x']*scale;y=oy-(r['y']+r['h'])*scale;w=r['w']*scale;h=r['h']*scale;boxes[r['id']]=(x,y,w,h);pos[r['id']]=(x+w/2,y+h/2)
  nodes.append({'id':r['id'],'name':r['name'],'type':'underground','region':f'MR{ri+1:02}','group':g,'position':list(pos[r['id']]),'worldBounds':[r['x'],r['y'],r['w'],r['h']],'mapBounds':[x,y,w,h],'scene':'Qolossal_Underground'})
r=rooms['HEART'];boxes['HEART']=(left+4.7*pitch,2420,230,115);pos['HEART']=(boxes['HEART'][0]+115,2477.5);nodes.append({'id':'HEART','name':r['name'],'type':'underground','region':'shared','group':'H','position':list(pos['HEART']),'mapBounds':list(boxes['HEART']),'worldBounds':[r['x'],r['y'],r['w'],r['h']],'scene':'Qolossal_Underground'})
def worldmap(roomid,p):
 r=rooms[roomid]
 if roomid=='HEART':return pos['HEART']
 m=mapping['underground:'+r['group']];return m['ox']+p[0]*m['sx'],m['oy']-p[1]*m['sy']
passages=[]
for e in geo['connections']:
 a,b=e['fromRoom'],e['toRoom'];ra,rb=rooms[a],rooms[b]
 if ra['group']==rb['group']:pts=[worldmap(a,(p['x'],p['y'])) for p in e['points']]
 else:
  ax,ay=worldmap(a,(e['points'][0]['x'],e['points'][0]['y']));bx,by=pos[b];rail=2330+'ACDE'.index(a[0])*19;pts=[(ax,ay),(ax,rail),(bx,rail),(bx,by)]
 passages.append(f'<path d="{line(pts)}" class="passage-outline"/><path d="{line(pts)}" class="passage" stroke="{COLORS[regionof(ra)]}" data-from="{a}" data-to="{b}"/>')
 edges.append({'id':e['id'],'from':a,'to':b,'kind':'passage','points':pts,'worldPoints':e['points']})
# Draw actual region-to-region connections as visibly distinct scene seams.
seams=[]
for e in world['links']:
 a,b=e['source'],e['target'];ax,ay=pos[a];bx,by=pos[b];mid=(ax+bx)/2;pts=[(ax,ay),(mid,ay),(mid,by),(bx,by)]
 seams.append(f'<path d="{line(pts)}" class="scene-seam" data-from="{a}" data-to="{b}"/>');edges.append({'id':e['id'],'from':a,'to':b,'kind':'scene-transition','points':pts})
# Surface cave mouths are placed from current scene transforms (including recent relocation work).
for j,(cid,c) in enumerate(sorted(caves.items())):
 a,b=c['upperAreaId'],c['lowerRoomId'];m=mapping[c['upperScene']];p=c['entrancePosition'];ax=m['ox']+p[0]*m['sx'];ay=m['oy']-p[1]*m['sy'];bx,by=worldmap(b,c['lowerPosition'])
 ri=int(a[2:4])-1;local=sorted(q['connectionId'] for q in caves.values() if q['upperScene']==c['upperScene']).index(cid)
 rail=770+local*15;pts=[(ax,ay),(ax,rail),(bx,rail),(bx,by)]
 edges.append({'id':cid,'from':a,'to':b,'kind':'cave','points':pts,'requirements':c['requirements']});c['mapUpper']=[ax,ay];c['mapLower']=[bx,by]
 cavelines.append(f'<path d="{line(pts)}" class="cave-link" data-from="{a}" data-to="{b}" data-id="{cid}"/>')
 for x,y in [(ax,ay),(bx,by)]:
  cavelines.append(f'<g class="mouth node" data-id="{b}" data-cave="{cid}" transform="translate({x:.2f} {y:.2f})"><circle r="12"/><path d="M-6 5 V0 Q-6-8 0-8 Q6-8 6 0 V5 M-9 6 H9"/></g>')
# Small chamfers and steps make room silhouettes read as excavated spaces, without inventing routes.
roomshapes=[];occupied=[]
for r in sorted(rooms.values(),key=lambda r:-(r['w']*r['h'])):
 id=r['id'];x,y,w,h=boxes[id];col=COLORS[regionof(r)] if id!='HEART' else '#e0cda3';k=min(8,w/8,h/8)
 d=f'M{x+k} {y} H{x+w-k} L{x+w} {y+k} V{y+h-k} L{x+w-k} {y+h} H{x+k} L{x} {y+h-k} V{y+k} Z'
 roomshapes.append(f'<g class="node room" data-id="{id}"><title>{escape(id+" · "+r["name"])}</title><path d="{d}" fill="{col}" fill-opacity=".24" stroke="{col}" stroke-width="3"/></g>')
 display=id.replace('_','-');fs=19 if len(id)>7 else 23;tw=len(display)*fs*.62;th=26;cx,cy=pos[id]
 candidates=[(cx,cy+6)] if w>tw+12 and h>40 else []
 candidates.extend([(cx,y-15),(cx,y+h+27),(x-tw/2-12,cy+6),(x+w+tw/2+12,cy+6),(cx,y-43),(cx,y+h+55)])
 def overlaps(a,b):return a[0]<b[2] and a[2]>b[0] and a[1]<b[3] and a[3]>b[1]
 chosen=candidates[0]
 for candidate in candidates:
  lx,ly=candidate;bb=(lx-tw/2-4,ly-th,lx+tw/2+4,ly+8)
  if not any(overlaps(bb,o) for o in occupied):chosen=candidate;break
 lx,ly=chosen;occupied.append((lx-tw/2-4,ly-th,lx+tw/2+4,ly+8))
 labels.append(f'<g class="node label" data-id="{id}">'+(f'<path d="M{cx:.2f},{cy:.2f} L{lx:.2f},{ly-10:.2f}" stroke="{col}" stroke-width="1" opacity=".5"/>' if abs(lx-cx)>1 or abs(ly-cy)>h/2 else '')+text(lx,ly,display,fs,col,'middle')+'</g>')
 if id=='MR01_C02':roomshapes.append(f'<rect x="{x-8}" y="{y-8}" width="{w+16}" height="{h+16}" rx="13" fill="none" stroke="#f1dd9e" stroke-width="2" stroke-dasharray="6 6"/>')
# Graph audit is unconditional on unlocks, not a claim of current-save reachability.
allids=set(pos);assert allids==set(areas)|set(rooms)
adj={k:set() for k in allids}
for e in edges:assert e['from'] in allids and e['to'] in allids;adj[e['from']].add(e['to']);adj[e['to']].add(e['from'])
seen=set();todo=['MR01_N00']
while todo:
 n=todo.pop()
 if n in seen:continue
 seen.add(n);todo.extend(adj[n]-seen)
assert len(seen)==173 and len(caves)==37
DATA={'width':W,'height':H,'nodes':nodes,'edges':edges,'caves':list(caves.values()),'transforms':mapping,'disclosure':'Complete world review. Display discovery and ability gates from the save in runtime UI.','layout':'Scene-local proportions retained within groups; groups assembled with surface above underground. Scene seams and cave transitions are not physical shafts.'}
(OUT/'world-map-data.json').write_text(json.dumps(DATA,indent=2))
styles='''text{font-family:Segoe UI,Arial,sans-serif}.walkroom{fill:none;stroke-width:17;stroke-linejoin:round;stroke-linecap:round}.walkfill{fill:none;stroke-width:11;stroke-opacity:.26;stroke-linejoin:round;stroke-linecap:round}.passage-outline{fill:none;stroke:#09121f;stroke-width:18;stroke-linejoin:round}.passage{fill:none;stroke-width:9;stroke-opacity:.7;stroke-linejoin:round}.scene-seam{fill:none;stroke:#d4e0ce;stroke-width:4;stroke-dasharray:9 7;opacity:.8}.cave-link{fill:none;stroke:#d5b96b;stroke-width:2.8;stroke-dasharray:7 6;stroke-opacity:.4}.mouth circle{fill:#19222b;stroke:#e4c87e;stroke-width:2}.mouth path{fill:none;stroke:#e4c87e;stroke-width:2}.label text{paint-order:stroke;stroke:#0d1725;stroke-width:5;stroke-linejoin:round}.node{cursor:pointer}.node:hover{filter:brightness(1.6)}.faded{opacity:.10}.selected path,.selected rect{stroke:#fff1b9!important;stroke-width:5}.selected text{fill:#fff1b9}.selected.cave-link,.selected.scene-seam,.selected.passage{stroke:#fff1b9!important;stroke-width:6;stroke-opacity:1}.hide-links .cave-link{display:none}.hide-labels .label{display:none}'''
header=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}" id="worldmap"><title>Qolossal — surface and underground atlas</title><style>{styles}</style>'
bg=f'<rect width="{W}" height="{H}" fill="#0c1522"/><defs><radialGradient id="mist"><stop stop-color="#1b3341" stop-opacity=".75"/><stop offset="1" stop-color="#0c1522" stop-opacity="0"/></radialGradient></defs><ellipse cx="3500" cy="1260" rx="3400" ry="1250" fill="url(#mist)"/>'
ornaments=text(120,105,'Q O L O S S A L',64,'#eee2bd')+text(120,155,'SURFACE & UNDERGROUND ATLAS',26,'#9fb1b7')+text(W-130,98,'84 upper areas  ·  89 underground rooms  ·  37 cave connections',25,'#b9c8c6','end')+text(W-130,145,'Complete layout · 29 October 2026',22,'#82979d','end')
for i,r in enumerate(regions):
 x=left+i*pitch;ornaments+=text(x,232,r['id']+'  /  '+r['name'],32,COLORS[i])+f'<path d="M{x} 254 H{x+780}" stroke="{COLORS[i]}" stroke-opacity=".35"/>'
 ornaments+=text(x,907,'UNDERGROUND',17,COLORS[i])
 if i>=3:ornaments+=text(x,1000 if i==3 else 1618,{'MR04':'A01–A10  /  Cistern galleries','MR05':'C01–C10  /  Root gardens','MR06':'D01–D10  /  Wind galleries','MR07':'E01–E10  /  Memory galleries'}[r['id']],24,COLORS[i])
ornaments+=text(120,2350,'HOW TO READ THE ATLAS',27,'#dcc99c')+text(120,2400,'Solid rooms and passages: continuous underground space',23)+text(120,2440,'Gold cave symbols + dashed links: paired surface entrances',23)+text(120,2480,'Pale dashed seams: travel between upper-ground scenes',23)+text(120,2520,'Select any area or room to trace its connections and requirements.',23)+text(120,2600,'Scene-local shapes retained; distances between scene groups are compressed for the map.',20,'#82979d')
ornaments+=text(pos['HEART'][0],2590,rooms['HEART']['name'],26,'#e0cda3','middle')
# Upper ribbons have a dark inset to avoid looking like thick neon roads.
base=header+bg+ornaments+'<g id="surface">'+''.join(geom+surfaceRooms)+'</g><g id="passages">'+''.join(passages+seams)+'</g><g id="cave-connections">'+''.join(cavelines)+'</g><g id="rooms">'+''.join(roomshapes)+'</g><g id="markers">'+''.join(landmarks)+'</g>'
# Draw region routes with colored edge and dark center tint.
base=base.replace('class="walkfill" stroke=', 'class="walkfill" style="stroke:#12212a;stroke-opacity:.92" stroke=')
svg=base+'<g id="labels">'+''.join(labels)+'</g></svg>'
(OUT/'Qolossal_ConnectedWorld.svg').write_text(svg,encoding='utf-8')
(OUT/'Qolossal_ConnectedWorld_NoRoomLabels.svg').write_text(base+'</svg>',encoding='utf-8')
(OUT/'Qolossal_ConnectedWorld_Geometry.svg').write_text(base.replace(ornaments,'')+'</svg>',encoding='utf-8')
validation={'areas':len(areas),'rooms':len(rooms),'cavePairs':len(caves),'uniqueIds':len(allids),'connectedIgnoringLocks':len(seen),'edges':dict(Counter(e['kind'] for e in edges)),'sceneFilesChanged':False,'source':'Current scene metadata and cave transforms; regional gallery passage polylines; current MRA surface route graph','latestSummitC06':caves['CAVE_MR07_C06'],'surfaceAboveUnderground':max(p[1] for id,p in pos.items() if id in areas)<min(b[1] for id,b in boxes.items() if id in rooms),'runtimeChartIntegrated':False}
assert validation['surfaceAboveUnderground'];(OUT/'validation.json').write_text(json.dumps(validation,indent=2))
(SRC/'map-data.json').write_text(json.dumps(DATA,indent=2))
print(json.dumps({k:v for k,v in validation.items() if k!='latestSummitC06'},indent=2))

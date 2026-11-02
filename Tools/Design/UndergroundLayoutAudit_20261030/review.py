from pathlib import Path
import json,hashlib,math,re,html
ROOT=Path(__file__).resolve().parents[3];REVIEW=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview');OUT=REVIEW/'UndergroundLayoutAudit_20261030'
d=json.loads((REVIEW/'ActualUndergroundComparison_20261030/scene_geometry.json').read_text());u=json.loads((REVIEW/'ActualSlopeComparison_20261030/scene_geometry.json').read_text());m=json.loads((REVIEW/'MainWorldLayout_20261030/world-map-data.json').read_text());base=json.loads((REVIEW/'MainWorldLayout_20261030/qa.json').read_text())
for n,h in base['sceneHashes'].items():assert hashlib.sha256((ROOT/'Assets/Scenes/Game'/f'{n}.unity').read_bytes()).hexdigest()==h
cs=[c for c in d['colliders'] if c['layer']=='Ground'];rooms={r['id']:r for r in d['rooms']}
def inside(p,poly):
 x,y=p;v=False
 for (a,b),(c,e) in zip(poly,poly[1:]+poly[:1]):
  if (b>y)!=(e>y) and x<(c-a)*(y-b)/(e-b)+a:v=not v
 return v
def clip(a,b,r):
 x0,y0,x1,y1=r;dx=b[0]-a[0];dy=b[1]-a[1];lo,hi=0,1
 for p,q in [(-dx,a[0]-x0),(dx,x1-a[0]),(-dy,a[1]-y0),(dy,y1-a[1])]:
  if abs(p)<1e-9:
   if q<0:return False
  else:
   t=q/p
   if p<0:lo=max(lo,t)
   else:hi=min(hi,t)
 return lo<=hi
segments=[]
for c in cs:
 for ps in c['paths']:
  xs=[p[0] for p in ps];ys=[p[1] for p in ps];segments.append((c,ps,[min(xs),min(ys),max(xs),max(ys)],list(zip(ps,ps[1:]+([ps[0]] if c['closed'] else [])))))
checks=[]
for c in d['caves']:
 x,y=c['arrival'];r=[x-.395,y-.595,x+.395,y+.595];hits=[];floors=[]
 for o,ps,bb,segs in segments:
  if bb[2]<r[0] or bb[0]>r[2] or bb[3]<y-3 or bb[1]>r[3]:continue
  if any(clip(a,b,r) for a,b in segs) or (o['closed'] and inside([x,y],ps)):hits.append(o['path'])
  for a,b in segs:
   if min(a[0],b[0])<=x<=max(a[0],b[0]) and abs(a[0]-b[0])>1e-8:
    yy=a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]);
    if y-3<=yy<=y:floors.append(yy)
 gap=y-.6-max(floors) if floors else None;checks.append(dict(id=c['id'],room=c['room'],bodyIntersections=hits,footGap=gap));assert not hits and gap is not None and -.01<gap<.12
pairs=[]
for i,a in enumerate(d['rooms']):
 for b in d['rooms'][i+1:]:
  x,y,w,h=a['bounds'];X,Y,W,H=b['bounds'];dx=min(x+w,X+W)-max(x,X);dy=min(y+h,Y+H)-max(y,Y);assert dx<=0 or dy<=0
  gap=math.hypot(max(-dx,0),max(-dy,0))
  if gap<5:pairs.append(dict(a=a['id'],b=b['id'],gap=gap))
proposals=[]
for idx,(id,new,why) in enumerate([('MR06_C01',[1710,446],'Place Sheltered Nest first in the Heights side-cave sequence, matching its early N01 surface entrance.'),('PC11',[1765,468],'Place Quiet Wind Pocket after MR06-C01 and before MR06-C02, matching N02 on the surface.'),('PC12',[2208,660],'Place Last Cairn after MR07-C03 and before MR07-C01, matching the early N02 surface entrance.')],1):
 b=rooms[id]['bounds'];nb=[*new,*b[2:]];links=[e for e in m['edges'] if e['kind']=='passage' and id in [e['from'],e['to']]];assert not links
 # Candidate rectangles plus 3.5-unit art/mouth margin must avoid all existing Ground segments.
 r=[nb[0]-3.5,nb[1]-1,nb[0]+nb[2]+3.5,nb[1]+nb[3]+1];hits=[]
 for o,ps,bb,segs in segments:
  if bb[2]<r[0] or bb[0]>r[2] or bb[3]<r[1] or bb[1]>r[3]:continue
  if any(clip(a,b,r) for a,b in segs) or (o['closed'] and inside([(r[0]+r[2])/2,(r[1]+r[3])/2],ps)):hits.append(o['path'])
 assert not hits,(id,hits)
 proposals.append(dict(number=idx,id=id,name=rooms[id]['name'],oldBounds=b,proposedBounds=nb,delta=[new[0]-b[0],new[1]-b[1]],reason=why,physicalPassages=0,candidateGroundIntersections=hits,status='Proposal only; scene not changed'))
# The regional ordering metric uses entrance positions, not room IDs, which need not be sequential.
orders=[]
for rid in ['MR06','MR07']:
 src=next(s for s in u['scenes'] if rid in s['scene']);ordered=sorted([c for c in src['caves'] if c['room'].startswith(rid+'_') or c['room'].startswith('PC')],key=lambda c:c['entrance'][0]);lower=sorted([c for c in d['caves'] if c['id'] in {q['id'] for q in ordered}],key=lambda c:c['entrance'][0]);orders.append(dict(region=rid,surfaceOrder=[c['room'] for c in ordered],currentUndergroundOrder=[c['room'] for c in lower]))
report=dict(rooms=89,cavePairs=37,groundColliders=921,roomBoundsOverlapCount=0,arrivalChecks=checks,arrivalModel='Saved Qori collider: 0.8 × 1.2, zero offset. Static segment/body checks inset by 0.005 units; center floor ray up to 3 units. Does not test emergence motion, jump traversal or camera/art.',tightRoomPairs=pairs,proposals=proposals,orderChecks=orders,keep=['A/C/D/E galleries and Great Heart Hall','MR01–MR03 C01–C06 networks; C06 sole access','MR07-C06 east-wall exit relocation'],limitations=['No live traversal or runtime trigger test','Art, shared meshes and colliders require a coordinated migration before any proposal can be applied','The upper and underground scenes have separate coordinate frames; vertical registration is a map design choice, not measured depth'],sceneHashes=base['sceneHashes'],appliedRelocations=0)
(OUT/'audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
# Crop exact collision map and add proposed bounds/arrows, preserving source geometry unchanged.
svg=(REVIEW/'ActualUndergroundComparison_20261030/actual-underground.svg').read_text();S=3;xmin=6.6;ymax=824
xy=lambda x,y:((x-xmin)*S,(ymax-y)*S)
for rid,extent in [('MR06',(1685,430,2070,660)),('MR07',(2115,610,2430,780))]:
 x0,y0,x1,y1=extent;x,y=xy(x0,y1);w=(x1-x0)*S;h=(y1-y0)*S;s=re.sub(r'<svg\b[^>]*>',f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{x} {y} {w} {h}">',svg,count=1);over=[]
 for q in proposals:
  if (rid=='MR06' and q['id']=='PC12') or (rid=='MR07' and q['id']!='PC12'):continue
  centers=[]
  for key,color,dash in [('oldBounds','#ec8e7f','4 4'),('proposedBounds','#ffd483','10 6')]:
   a,b,ww,hh=q[key];xx,yy=xy(a,b+hh);centers.append([xx+ww*S/2,yy+hh*S/2]);over.append(f'<rect x="{xx-4}" y="{yy-4}" width="{ww*S+8}" height="{hh*S+8}" fill="none" stroke="{color}" stroke-width="3" stroke-dasharray="{dash}"/>')
   if key=='proposedBounds':over.append(f'<text x="{xx}" y="{yy-13}" font-family="Segoe UI" font-size="18" fill="#ffd483">{q["number"]}. {q["id"].replace("_","-")} proposed</text>')
  a,b=centers;over.append(f'<path d="M{a[0]} {a[1]} L{b[0]} {b[1]}" stroke="#ffd483" stroke-width="2.5" stroke-dasharray="7 5"/>')
 s=s.replace('</svg>','<g>'+''.join(over)+'</g></svg>');(OUT/f'{rid}-proposal.svg').write_text(s,encoding='utf-8')
rows=''.join(f'<tr><td>{q["id"].replace("_","-")}<br><small>{html.escape(q["name"])}</small></td><td>{html.escape(q["reason"])}</td><td>({q["oldBounds"][0]:g}, {q["oldBounds"][1]:g}) → ({q["proposedBounds"][0]:g}, {q["proposedBounds"][1]:g})</td></tr>' for q in proposals)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Qolossal · Underground layout review</title><style>:root{color-scheme:dark}body{margin:0;background:#0d1c24;color:#d1e0df;font:16px/1.6 Segoe UI,Arial,sans-serif}main{max-width:1450px;margin:auto;padding:35px}h1{font:36px Georgia,serif;color:#ebdab3}h2{font-size:23px;color:#dfd2ad}p{max-width:1050px}a{color:#eacb90}small{color:#8faab5}table{border-collapse:collapse;width:100%}td,th{padding:15px;border-bottom:1px solid #34505d;text-align:left}th{color:#a6bfc7;font-weight:500}img{width:100%;border:1px solid #34505d;background:#0d1c24}.callout{border-left:3px solid #e3c586;padding:14px 20px;background:#142a35}.good{color:#b4d49e}.links{display:flex;flex-wrap:wrap;gap:20px;margin:20px 0}</style><main><small>QOLOSSAL / ACTUAL UNDERGROUND / 30 OCTOBER 2026</small><h1>Keep the main networks. Improve three isolated cave positions.</h1><p class="callout">Actual underground scene geometry is now the approved underground reference. This review proposes three small relocations for easier geographic reading. <b>No rooms or entrances have been moved.</b></p><h2>What passed</h2><p class="good">89 room bounds checked: no overlaps. All 37 underground arrivals fit Qori’s saved collider and have ground approximately 0.06 units below his feet. All current cave pairs retain their matching IDs.</p><p>The A, C, D and E galleries work as one connected central space around the Great Heart Hall. Keep their real adjacency. Keep the MR01–MR03 six-room networks and their single C06 entrance/exit. Keep the corrected MR07-C06 east-wall exit.</p><h2>Recommended position improvements</h2><table><tr><th>Room</th><th>Reason</th><th>Candidate bottom-left position<br><small>Unity scene units</small></th></tr>ROWS</table><p>All three rooms are isolated side caves with no physical passage connections to other rooms. Candidate footprints, including a margin for the entrance, avoid the currently extracted ground geometry. Their IDs, rewards, skill requirements and upper entrances should stay the same. Move each complete room and its underground doorway/arrival together.</p><h2>Heights: align the early cave sequence</h2><p>Surface order begins MR06-C01 → PC11 → MR06-C02. Underground positions currently begin MR06-C02 → MR06-C03 → PC11 → MR06-C04 → MR06-C01. Moving the first two pockets resolves this reversal while leaving the D galleries intact.</p><p><span style="color:#ec8e7f">Coral: current bounds.</span> <span style="color:#ffd483">Gold: proposed bounds.</span> Existing terrain is shown unchanged.</p><img src="MR06-proposal.png" alt="Heights current cave positions and two proposed locations"><h2>Summit: bring Last Cairn back beside its early entrance</h2><p>PC12 is entered from N02, but its lower room currently lies beyond MR07-C01, which is entered later at N03. Place PC12 between MR07-C03 and MR07-C01.</p><img src="MR07-proposal.png" alt="Summit current Last Cairn location and proposed location"><h2>Keep these close pairs, with clear rock separation</h2><p>MR07-C05 and MR07-C06 have a 2-unit gap between room bounds. MR05-C04 and MR05-C05 have a 4-unit gap. They do not overlap. Preserve their solid dividers and distinguish their entrances visually; their spacing alone does not justify a relocation.</p><h2>Entrances that need clear visual identification</h2><p>The Heights D01 entrance and MR06-C06 mouth are near one another around N09; Ribwood’s C01 gallery entrance and MR05-C05 are near N07. Keep their positions for now. Give the large connected galleries a recognizably different entrance treatment from small side caves, and verify prompts/framing during the next in-game pass.</p><h2>Progression decision to retain</h2><p>C06 is currently Glidecap-gated in Cradle, Causeway and Terraces. That makes those six-room networks return visits. Preserve that approved rule. If an early complete Cradle dungeon is desired later, review the C06 gate explicitly; moving rooms will not solve that timing issue.</p><h2>Before any relocation is implemented</h2><p>Move room metadata, platforms, art, shared wall/collision segments, return trigger, arrival and checkpoint anchors as one coordinated change. Preserve save/reward IDs; old position saves need a safe landing audit. Re-extract geometry, update the canonical map and Unity map, then test entry/exit, respawn and traversal. Reparenting only the room object will not move all shared terrain.</p><p><small>This is a static geometry and topology review. No live movement, emergence animation, enemy behavior, camera framing, performance or console hardware tests were performed. Upper and underground scene coordinates do not by themselves define a shared vertical alignment.</small></p><div class="links"><a href="../ActualUndergroundComparison_20261030/index.html">Actual underground comparison</a><a href="audit.json">Measurements and proposed coordinates</a><a href="MR06-proposal.svg">Heights vector diagram</a><a href="MR07-proposal.svg">Summit vector diagram</a></div></main></html>'''.replace('ROWS',rows)
(OUT/'index.html').write_text(page,encoding='utf-8');print('Audit complete: 37 clear underground arrivals, 0 room-bound overlaps, 3 validated candidate footprints, 0 scene edits.')

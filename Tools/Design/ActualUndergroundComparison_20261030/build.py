from pathlib import Path
import json,hashlib,html,math,re
ROOT=Path(__file__).resolve().parents[3];OUT=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/ActualUndergroundComparison_20261030');REF=OUT.parent/'MainWorldLayout_20261030'
d=json.loads((OUT/'scene_geometry.json').read_text(encoding='utf-8-sig'));ref=json.loads((REF/'world-map-data.json').read_text());assert hashlib.sha256((ROOT/d['path']).read_bytes()).hexdigest()==d['sha256']
meta={n['id']:n for n in ref['nodes'] if n['type']=='underground'};assert {r['id'] for r in d['rooms']}==set(meta)
cs=[c for c in d['colliders'] if c['layer']=='Ground'];ps=[p for c in cs for line in c['paths'] for p in line];xmin=min(p[0] for p in ps)-25;xmax=max(p[0] for p in ps)+25;ymin=min(p[1] for p in ps)-35;ymax=max(p[1] for p in ps)+65
S=3;W=(xmax-xmin)*S;H=(ymax-ymin)*S
colors={r['id']:r['color'] for r in ref['regions']};colors['shared']='#e6d0a2'
def xy(p):return [(p[0]-xmin)*S,(ymax-p[1])*S]
def path(ps):return 'M'+' L'.join(f'{x:.3f},{y:.3f}' for x,y in ps)
def esc(s):return html.escape(str(s),quote=True)
def text(x,y,t,fs=24,col='#d2dedc',anchor='start'):return f'<text x="{x:.2f}" y="{y:.2f}" font-size="{fs}" fill="{col}" text-anchor="{anchor}">{esc(t)}</text>'
style='text{font-family:Segoe UI,Arial,sans-serif}.labels text{paint-order:stroke;stroke:#0d1c24;stroke-width:7;stroke-linejoin:round}.room-node{cursor:pointer}.room-node:hover{filter:brightness(1.5)}.bounds{fill:none;stroke:#9dc4d1;stroke-width:1;stroke-dasharray:6 6;opacity:.3;display:none}.show-bounds .bounds{display:block}.hide-labels .labels{display:none}.selected .bounds{display:block;stroke:#ffdb81;stroke-width:5;opacity:1}.selected text{fill:#ffdb81}.mouth{fill:#0d1c24;stroke:#f5d38b;stroke-width:2.5}'
start=f'<svg xmlns="http://www.w3.org/2000/svg" id="actualmap" width="{W:.0f}" height="{H:.0f}" viewBox="0 0 {W:.0f} {H:.0f}"><style>{style}</style><rect x="-10000" y="-10000" width="30000" height="30000" fill="#0d1c24"/>'
grid=[]
for x in range(math.ceil(xmin/100)*100,math.floor(xmax/100)*100+1,100):
 a,b=xy([x,ymin]),xy([x,ymax]);grid.append(f'<path d="{path([a,b])}" stroke="#21353f" stroke-width="1"/>'+text(a[0]+4,H-12,str(x),19,'#75909c'))
for y in range(math.ceil(ymin/100)*100,math.floor(ymax/100)*100+1,100):
 a,b=xy([xmin,y]),xy([xmax,y]);grid.append(f'<path d="{path([a,b])}" stroke="#21353f" stroke-width="1"/>'+text(6,a[1]-8,str(y),19,'#75909c'))
geometry=[]
for c in cs:
 color=colors[meta[c['room']]['region']] if c['room'] in meta else '#9bbbc4'
 for pts in c['paths']:
  geometry.append(f'<path d="{path([xy(p) for p in pts])}{" Z" if c["closed"] else ""}" fill="{color if c["closed"] else "none"}" fill-opacity=".16" stroke="{color}" stroke-width="{1.4 if c["closed"] else 2.1}" stroke-linejoin="round"><title>{esc(c["path"])}</title></path>')
labels=[];roomdata=[]
for r in d['rooms']:
 id=r['id'];x,y,w,h=r['bounds'];p=xy([x,y+h]);b=[*p,w*S,h*S];m=meta[id];color=colors[m['region']];cx=p[0]+w*S/2;ly=p[1]-15;display=id.replace('_','-');fs=21 if len(id)>7 else 24
 labels.append(f'<g class="room-node labels" data-id="{id}"><rect class="bounds" x="{p[0]}" y="{p[1]}" width="{w*S}" height="{h*S}"/><rect x="{cx-len(display)*fs*.3}" y="{ly-fs}" width="{len(display)*fs*.6}" height="{fs+8}" fill="transparent"/>'+text(cx,ly,display,fs,color,'middle')+'</g>')
 roomdata.append(dict(**r,group=m['group'],owner=m['region'],actualBounds=b,referenceBounds=m['mapBounds'],color=color))
markers=[]
for c in d['caves']:
 x,y=xy(c['entrance']);markers.append(f'<g class="room-node" data-id="{c["room"]}"><title>{esc(c["room"]+" ↔ "+c["area"])}</title><circle class="mouth" cx="{x}" cy="{y}" r="9"/><path d="M{x-4} {y+4} V{y-2} Q{x} {y-8} {x+4} {y-2} V{y+4}" fill="none" stroke="#f5d38b" stroke-width="2"/></g>')
header=text(50,50,'ACTUAL UNDERGROUND / ONE SCENE COORDINATE SYSTEM',30,'#e6d3a2')+text(50,90,'89 rooms · 37 exits · 921 ground colliders · equal X/Y scale · Unity units',22,'#a7bbc2')
svg=start+''.join(grid)+header+'<g id="collision">'+''.join(geometry)+'</g>'+''.join(labels+markers)+'</svg>'
(OUT/'actual-underground.svg').write_text(svg,encoding='utf-8')
# The reference retains the current diagram positions, but hides the upper layer for a fair underground-only comparison.
reference=(REF/'Qolossal_MainWorldLayout.svg').read_text();reference=reference.replace('id="worldmap"','id="referencemap"').replace('<style>','<style>#terrain,#region-headings,#cave-connections,#cave-mouths{display:none} .scene-seam{display:none} ')
# Retain only underground label/room groups; shared passages are deliberately visible.
import xml.etree.ElementTree as ET
root=ET.fromstring(reference);ns='{http://www.w3.org/2000/svg}'
for parent in root.iter():
 for child in list(parent):
  id=child.get('data-id')
  if (id and id not in meta and child.tag==ns+'g') or (child.tag==ns+'text' and parent==root):parent.remove(child)
bs=[n['mapBounds'] for n in meta.values()];rb=[min(b[0] for b in bs)-80,min(b[1] for b in bs)-100,max(b[0]+b[2] for b in bs)+80,max(b[1]+b[3] for b in bs)+90];rb=[rb[0],rb[1],rb[2]-rb[0],rb[3]-rb[1]];root.set('viewBox',' '.join(map(str,rb)));ET.register_namespace('', 'http://www.w3.org/2000/svg');reference=ET.tostring(root,encoding='unicode');(OUT/'current-diagram.svg').write_text(reference,encoding='utf-8')
payload=dict(rooms=roomdata,caves=d['caves'],actualView=[0,0,W,H],referenceView=rb,colliderCount=len(cs),sceneHash=d['sha256'])
(OUT/'comparison-data.json').write_text(json.dumps(payload,indent=2),encoding='utf-8')
# A standalone comparison plate with explicit fit/scale disclosure.
def embed(s,x,y,w,h):return re.sub(r'<svg\b[^>]*>',lambda m:re.sub(r'\s(?:id|width|height)="[^"]*"','',m.group(0))[:-1]+f' x="{x}" y="{y}" width="{w}" height="{h}">',s,count=1)
plate='<svg xmlns="http://www.w3.org/2000/svg" width="2400" height="2200" viewBox="0 0 2400 2200"><rect width="2400" height="2200" fill="#0d1c24"/>'+text(65,65,'UNDERGROUND / ACTUAL SCENE VS CURRENT DIAGRAM',38,'#ecddb7')+text(65,110,'Actual view preserves all world positions, slopes and distances. The current diagram moves networks beneath their upper regions.',23)+text(65,165,'01  ACTUAL UNITY SCENE',29,'#b8dba8')+embed(svg,40,185,2320,940)+text(65,1180,'02  CURRENT ARRANGED UNDERGROUND DIAGRAM',29,'#e7c588')+embed(reference,40,1200,2320,900)+text(65,2150,'Panels fitted independently, with equal X/Y scale within each. Gold circles mark exits to upper areas; no gameplay geometry changed.',22)+'</svg>'
(OUT/'comparison.svg').write_text(plate,encoding='utf-8')
print('Actual bounds',xmin,xmax,ymin,ymax,'colliders',len(cs),'rooms',len(roomdata))

"""Label-free cartographic artwork foundation; geometry comes from the canonical data."""
from pathlib import Path
import json,math,html
ROOT=Path(__file__).resolve().parents[3];WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal');O=WORK/'DesignReview/MainWorldLayout_20261030';REVIEW=WORK/'Tools/ForwardCaveMap_20261030'
d=json.loads((O/'world-map-data.json').read_text(encoding='utf-8'));W,H=d['width'],d['height'];colors={r['id']:r['color'] for r in d['regions']};colors['shared']='#ceb886';nodes={n['id']:n for n in d['nodes']}
def path(ps):return 'M'+' L'.join(f'{x:.2f},{y:.2f}' for x,y in ps)
def points(ps):return ' '.join(f'{x:.2f},{y:.2f}' for x,y in ps)
styles='''.zone-only{display:none}.zoomed .zone-only{display:inline}.outside{display:none!important}.node{cursor:pointer}.mouth path{fill:none;stroke:#e3ca87;stroke-width:5;stroke-linejoin:round}.mouth circle{fill:#0e201e;stroke:#bb985b;stroke-width:3}.selected{filter:brightness(1.6)}.cave-link{fill:none;stroke:#091713;stroke-width:26;stroke-linecap:round;stroke-linejoin:round}.tunnel-rock{fill:none;stroke:#65583b;stroke-width:18;stroke-linecap:round;stroke-linejoin:round}.tunnel-inner{fill:none;stroke:#102421;stroke-width:9;stroke-linecap:round;stroke-linejoin:round}.tunnel-edge{fill:none;stroke:#b99c62;stroke-width:1.6;stroke-dasharray:13 19;opacity:.65}.terrain{fill:none;stroke-linejoin:round;stroke-linecap:round}'''
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" id="worldmap" width="{W}" height="{H}" viewBox="0 0 {W} {H}"><style>{styles}</style><defs><linearGradient id="earth" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#223a32"/><stop offset="1" stop-color="#102320"/></linearGradient><pattern id="strata" width="280" height="180" patternUnits="userSpaceOnUse"><path d="M-30 40 Q60 10 140 42 T310 35 M-10 130 Q80 155 170 120 T310 122" fill="none" stroke="#83916a" stroke-width="2" opacity=".10"/><path d="M40 58 l18 6 m130 28 l13 -4 m-80 65 l11 3" stroke="#bda77b" stroke-width="2" opacity=".17"/></pattern></defs>',f'<rect width="{W}" height="{H}" fill="#091b1b"/>']
allSurface=sorted([p for c in d['surfaceContours'] if c['primary'] for p in c['points']],key=lambda p:p[0]);land=allSurface+[[allSurface[-1][0],H-65],[allSurface[0][0],H-65]]
svg.append(f'<path d="{path(land)} Z" fill="url(#earth)"/><path d="{path(land)} Z" fill="url(#strata)"/>')
for r in d['regions']:
 cs=[c for c in d['surfaceContours'] if c['region']==r['id'] and c['primary']];p=[p for c in cs for p in c['points']];p.sort(key=lambda p:p[0]);poly=p+[[p[-1][0],H-65],[p[0][0],H-65]]
 svg.append(f'<g data-region="{r["id"]}">')
 for c in cs:svg.append(f'<path class="terrain" d="{path(c["points"])}" stroke="#061614" stroke-width="18"/><path class="terrain" d="{path(c["points"])}" stroke="{r["color"]}" stroke-width="6"/>')
 svg.append('</g>')
# Illustrated connection routes are revealed only within a selected zone.
for e in d['edges']:
 if e['kind']!='cave':continue
 region=nodes[e['from']]['region'];p=' '.join(path(ps) for ps in e['tunnelSegments']);svg.append(f'<g class="zone-only tunnel" data-region="{region}" data-cave="{e["id"]}"><path class="cave-link" data-from="{e["from"]}" data-to="{e["to"]}" data-id="{e["id"]}" d="{p}"/><path class="tunnel-rock" d="{p}"/><path class="tunnel-inner" d="{p}"/><path class="tunnel-edge" d="{p}" transform="translate(-5,-2)"/></g>')
for c in d['undergroundContours']:
 cls='terrain'+(' zone-only' if c['room'] else '');color=colors[c['region']]
 svg.append(f'<path data-region="{c["region"]}" class="{cls}" d="{path(c["points"])}" fill="none" stroke="{color}" stroke-width="3.5" opacity=".9"/>')
for n in d['nodes']:
 x,y,w,h=n['mapBounds'];svg.append(f'<rect class="node hit" data-id="{n["id"]}" data-region="{n["region"]}" x="{x}" y="{y}" width="{w}" height="{h}" fill="transparent"/>')
for c in d['caves']:
 region=nodes[c['upperAreaId']]['region']
 for p in [c['mapUpper'],c['mapLower']]:
  x,y=p;svg.append(f'<g class="mouth node" data-id="{c["lowerRoomId"]}" data-region="{region}" data-cave="{c["connectionId"]}" transform="translate({x} {y})"><circle r="12"/><path d="M-7 7 V-1 Q-7-10 0-10 Q7-10 7-1 V7 M-10 8 H10"/></g>')
svg.append('</svg>');svg=''.join(svg)
(O/'Qolossal_PlayerMap.svg').write_text(svg,encoding='utf-8');(O/'Qolossal_MainWorldLayout.svg').replace(O/'Qolossal_TechnicalLayout.svg');(O/'Qolossal_MainWorldLayout.svg').write_text(svg,encoding='utf-8')
buttons=[]
for r in d['regions']:
 p=[p for c in d['surfaceContours'] if c['region']==r['id'] and c['primary'] for p in c['points']];x0=min(p[0] for p in p);x1=max(p[0] for p in p);y0=min(q[1] for q in p);y1=max(q[1] for q in p);scale=min(160/(x1-x0),38/max(y1-y0,1));p=[[(x-(x0+x1)/2)*scale+90,(y-(y0+y1)/2)*scale+25] for x,y in p]
 buttons.append(f'<button class="zone" data-zone="{r["id"]}" aria-label="Explore this landscape"><svg viewBox="0 0 180 50"><path d="{path(p)}" fill="none" stroke="{r["color"]}" stroke-width="2"/></svg></button>')
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Qolossal · Map artwork study</title><style>:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#091b1b;color:#ddcfaa;font:15px Segoe UI,sans-serif}header{height:65px;display:flex;align-items:center;gap:12px;padding:12px 24px;border-bottom:1px solid #385047}header b{letter-spacing:.22em;margin-right:auto}button{border:1px solid #4e6150;background:#162e28;color:#e6d8af;border-radius:5px;padding:9px 16px;cursor:pointer}button:hover,button.active{background:#365043;border-color:#c6ad73}#stage{position:absolute;inset:65px 0 112px;touch-action:none}#stage>svg{width:100%;height:100%;display:block;cursor:grab}#stage>svg:active{cursor:grabbing}nav{position:absolute;bottom:40px;left:4%;right:4%;height:65px;display:flex;gap:10px}.zone{flex:1;padding:2px;min-width:0}.zone svg{width:100%;height:100%}footer{position:absolute;bottom:0;height:35px;width:100%;text-align:center;color:#94ada1;font-size:13px}a{color:#d8bd83}#hint{position:absolute;top:80px;left:25px;background:#0c201de8;padding:9px 14px;border-radius:5px;font-size:13px;pointer-events:none}#mode{color:#9bb0a3;font-size:13px}@media(max-width:700px){header{padding:8px;gap:5px}header b{font-size:11px}button{padding:7px}#mode{display:none}nav{left:8px;right:8px;gap:4px}footer{font-size:10px}}</style><header><b>QOLOSSAL</b><span id="mode">The whole landscape</span><button id="home">Whole map</button><button id="minus" aria-label="Zoom out">−</button><button id="plus" aria-label="Zoom in">+</button></header><div id="stage">__SVG__</div><div id="hint">Choose a landscape to reveal its cave passages.</div><nav>__BUTTONS__</nav><footer>Click a landscape · Scroll to zoom · Drag to explore · <a href="Qolossal_PlayerMap.svg">Artwork outline</a></footer><script>
const DATA=__DATA__,svg=document.querySelector('#worldmap'),stage=document.querySelector('#stage');let box=[0,0,DATA.width,DATA.height],region=null,drag=null,moved=false;
function draw(){svg.setAttribute('viewBox',box.join(' '))}function fitBounds(x,y,w,h){const ratio=stage.clientWidth/stage.clientHeight;if(w/h<ratio){const nw=h*ratio;x-=(nw-w)/2;w=nw}else{const nh=w/ratio;y-=(nh-h)/2;h=nh}box=[x,y,w,h];draw()}
function focus(id){region=id;svg.classList.toggle('zoomed',!!id);svg.querySelectorAll('[data-region]').forEach(el=>el.classList.toggle('outside',!!id&&el.dataset.region!==id));document.querySelectorAll('[data-zone]').forEach(b=>b.classList.toggle('active',b.dataset.zone===id));document.querySelector('#mode').textContent=id?'Surface & depths':'The whole landscape';document.querySelector('#hint').textContent=id?'Recessed tunnels trace the way between cave mouths.':'Choose a landscape to reveal its cave passages.';if(!id){fitBounds(0,0,DATA.width,DATA.height);return}const ns=DATA.nodes.filter(n=>n.region===id),ps=ns.flatMap(n=>[[n.mapBounds[0],n.mapBounds[1]],[n.mapBounds[0]+n.mapBounds[2],n.mapBounds[1]+n.mapBounds[3]]]);for(const e of DATA.edges.filter(e=>e.kind==='cave'&&DATA.nodes.find(n=>n.id===e.from).region===id))ps.push(...e.points);const xs=ps.map(p=>p[0]),ys=ps.map(p=>p[1]);const x=Math.min(...xs)-90,y=Math.min(...ys)-100;fitBounds(x,y,Math.max(...xs)-x+90,Math.max(...ys)-y+100)}
function zoom(f,p){p=p||[box[0]+box[2]/2,box[1]+box[3]/2];const w=Math.max(180,Math.min(DATA.width*1.2,box[2]*f)),s=w/box[2];box=[p[0]+(box[0]-p[0])*s,p[1]+(box[1]-p[1])*s,w,box[3]*s];draw()}
function point(e){const p=svg.createSVGPoint();p.x=e.clientX;p.y=e.clientY;const q=p.matrixTransform(svg.getScreenCTM().inverse());return[q.x,q.y]}
document.querySelectorAll('[data-zone]').forEach(b=>b.onclick=()=>focus(b.dataset.zone));document.querySelector('#home').onclick=()=>focus(null);document.querySelector('#plus').onclick=()=>zoom(1/1.3);document.querySelector('#minus').onclick=()=>zoom(1.3);
svg.onwheel=e=>{e.preventDefault();zoom(e.deltaY>0?1.15:1/1.15,point(e))};svg.onpointerdown=e=>{drag={point:point(e),box:box.slice()};moved=false;svg.setPointerCapture(e.pointerId)};svg.onpointermove=e=>{if(!drag)return;const p=point(e),dx=p[0]-drag.point[0],dy=p[1]-drag.point[1];if(Math.abs(dx)+Math.abs(dy)>4)moved=true;box[0]-=dx;box[1]-=dy;draw()};svg.onpointerup=e=>{if(!moved){const target=document.elementFromPoint(e.clientX,e.clientY)?.closest('[data-region]');if(target&&target.dataset.region!=='shared'){if(!region)focus(target.dataset.region);else{svg.querySelectorAll('.selected').forEach(el=>el.classList.remove('selected'));if(target.dataset.cave)svg.querySelectorAll('[data-cave="'+target.dataset.cave+'"]').forEach(el=>el.classList.add('selected'))}}}drag=null};svg.onpointercancel=()=>drag=null;focus(null);
</script></html>'''
page=page.replace('__SVG__',svg).replace('__BUTTONS__',''.join(buttons)).replace('__DATA__',json.dumps(d,separators=(',',':')).replace('</','<\\/'))
(O/'index.html').write_text(page,encoding='utf-8');REVIEW.mkdir(exist_ok=True);(REVIEW/'index.html').write_text(page.replace('href="Qolossal_PlayerMap.svg"','href="../../DesignReview/MainWorldLayout_20261030/Qolossal_PlayerMap.svg"'),encoding='utf-8')
# Export focused SVGs with the same rule as the interactive view.
for rid in ['MR01','MR06','MR07']:
 import xml.etree.ElementTree as ET
 ET.register_namespace('','http://www.w3.org/2000/svg');tree=ET.fromstring(svg);tree.set('class','zoomed');ps=[p for n in d['nodes'] if n['region']==rid for p in [[n['mapBounds'][0],n['mapBounds'][1]],[n['mapBounds'][0]+n['mapBounds'][2],n['mapBounds'][1]+n['mapBounds'][3]]]]
 ps.extend(p for e in d['edges'] if e['kind']=='cave' and nodes[e['from']]['region']==rid for p in e['points']);x=min(p[0] for p in ps)-100;y=min(p[1] for p in ps)-100;w=max(p[0] for p in ps)-x+100;h=max(p[1] for p in ps)-y+100;tree.set('viewBox',f'{x} {y} {w} {h}');tree.set('width',str(w));tree.set('height',str(h))
 for el in tree.iter():
  if el.get('data-region') and el.get('data-region')!=rid:el.set('class',el.get('class','')+' outside')
 (REVIEW/f'{rid}.svg').write_text(ET.tostring(tree,encoding='unicode'),encoding='utf-8')
print('Built label-free overview, landscape selectors and focused tunnel artwork views.')


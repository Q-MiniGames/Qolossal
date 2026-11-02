import json, math, hashlib, html
from pathlib import Path
ROOT=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview')
OUT=ROOT/'ActualSlopeComparison_20261030'
geo=json.loads((OUT/'scene_geometry.json').read_text(encoding='utf-8-sig'))
atlas=json.loads((ROOT/'PaintedWorldAtlas_20261030/world-map-data.json').read_text(encoding='utf-8-sig'))
world=json.loads(Path('Assets/MountainReliefAtlas/Resources/MRA/mra_world.json').read_text(encoding='utf-8-sig'))
def esc(x):return html.escape(str(x),quote=True)
def fmt(x):return f'{x:.1f}'.rstrip('0').rstrip('.')
def path(points):return 'M'+' L'.join(f'{x:.3f},{y:.3f}' for x,y in points)
def ground(ts,x):
 hits=[]
 for t in ts:
  for a,b in zip(t['surface'],t['surface'][1:]):
   if min(a[0],b[0])<=x<=max(a[0],b[0]) and abs(b[0]-a[0])>1e-6:hits.append(a[1]+(b[1]-a[1])*(x-a[0])/(b[0]-a[0]))
 return max(hits) if hits else None
regions=[];cursor=0;level=0
for s,w in zip(geo['scenes'],world['regions']):
 main=[t for t in s['terrains'] if '/Terrain/Route ' in t['path'] and t['active'] and t['solid']]
 start=w['nodes'][0]['position']['x'];end=w['nodes'][-1]['position']['x'];y0=ground(main,start);y1=ground(main,end)
 assert y0 is not None and y1 is not None
 ns=sorted([n for n in atlas['nodes'] if n['type']=='surface' and n['region']==w['id']],key=lambda n:n['id'])
 factor=(end-start)/(ns[-1]['position'][0]-ns[0]['position'][0])
 reference=[[start+(n['position'][0]-ns[0]['position'][0])*factor,y0-(n['position'][1]-ns[0]['position'][1])*factor] for n in ns]
 pts=[p for t in main for p in t['surface'] if start<=p[0]<=end]
 r=dict(id=w['id'],name=w['name'],scene=s['scene'],start=start,end=end,entryHeight=y0,exitHeight=y1,rise=y1-y0,span=end-start,minimum=min(p[1] for p in pts),maximum=max(p[1] for p in pts),offset=[cursor-start,level-y0],reference=reference,referenceRise=reference[-1][1]-y0,sceneData=s,nodes=w['nodes'])
 regions.append(r);cursor+=end-start+16;level+=y1-y0
width=cursor-16
# Only translate actual regions. A 16-unit dashed display seam separates scene-local routes.
# Current illustrative profile receives one uniform XY scale in the whole-map view.
allnodes=[n for n in atlas['nodes'] if n['type']=='surface'];allnodes.sort(key=lambda n:n['id']);ax0,ay0=allnodes[0]['position'];ax1=allnodes[-1]['position'][0];af=width/(ax1-ax0)
whole_ref=[[(n['position'][0]-ax0)*af,-(n['position'][1]-ay0)*af] for n in allnodes]
COLORS=['#b3d99b','#e5c786','#94d6ce','#dfc3a1','#a7c66a','#98c7e8','#d2b7ef']
def svg_chart(selection,overview=False):
 rs=regions if selection=='all' else [next(r for r in regions if r['id']==selection)]
 if selection=='all': xmin=-20;xmax=width+20;ymin=-75;ymax=level+100;ref=whole_ref
 else:
  r=rs[0];xmin=r['start']-18;xmax=r['end']+18;ref=r['reference'];vals=[p[1] for t in r['sceneData']['terrains'] if t['active'] and t['solid'] for p in t['surface']]+[p[1] for p in ref];ymin=min(vals)-35;ymax=max(vals)+65
 W=1760 if overview else 1500;H=720 if overview else 660;pad=65;k=min((W-pad*2)/(xmax-xmin),(H-pad*2)/(ymax-ymin));left=pad+(W-pad*2-(xmax-xmin)*k)/2;top=pad+(H-pad*2-(ymax-ymin)*k)/2
 def xy(p):return [left+(p[0]-xmin)*k,top+(ymax-p[1])*k]
 items=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" role="img" aria-label="Actual scene terrain compared with the illustrated atlas"><rect width="{W}" height="{H}" fill="#101e26"/>', '<style>text{font-family:Segoe UI,Arial,sans-serif} .label{paint-order:stroke;stroke:#101e26;stroke-width:5;stroke-linejoin:round} .collision{display:none} .surface{fill:none;stroke-linejoin:round;stroke-linecap:round}</style>']
 step=100 if selection=='all' else 25
 for y in range(math.ceil(ymin/step)*step,math.floor(ymax/step)*step+1,step):
  a,b=xy([xmin,y]),xy([xmax,y]);items.append(f'<path d="{path([a,b])}" stroke="#28404b" stroke-width="1"/><text x="{a[0]-8}" y="{a[1]+4}" text-anchor="end" fill="#829ba7" font-size="13">{y}</text>')
 for x in range(math.ceil(xmin/step)*step,math.floor(xmax/step)*step+1,step):
  a,b=xy([x,ymin]),xy([x,ymax]);items.append(f'<path d="{path([a,b])}" stroke="#203640" stroke-width="1"/>')
 items.append(f'<path class="reference" d="{path([xy(p) for p in ref])}" fill="none" stroke="#ffb963" stroke-width="3" stroke-dasharray="9 7" opacity="0.9"/>')
 for r in rs:
  s=r['sceneData'];off=r['offset'] if selection=='all' else [0,0];color=COLORS[int(r['id'][-1])-1]
  def trans(p):return xy([p[0]+off[0],p[1]+off[1]])
  if selection=='all':
   clipx=trans([r['start'],0])[0];clipw=(r['end']-r['start'])*k;items.append(f'<defs><clipPath id="clip-{r["id"]}"><rect x="{clipx}" y="0" width="{clipw}" height="{H}"/></clipPath></defs>')
   items.append(f'<g clip-path="url(#clip-{r["id"]})">')
  for c in s['colliders']:
   if c['path'].endswith('/West end') or c['path'].endswith('/East end'):continue
   for pts in c['paths']:
    items.append(f'<path class="collision" d="{path([trans(p) for p in pts])}{" Z" if c["closed"] else ""}" fill="{color}" fill-opacity="0.05" stroke="{color}" stroke-opacity="0.35" stroke-width="0.8"><title>{esc(c["path"])}</title></path>')
  for t in s['terrains']:
   if not(t['active'] and t['solid'] and t['colliderEnabled']):continue
   pts=t['surface'];items.append(f'<path class="surface" d="{path([trans(p) for p in pts])}" stroke="{color}" stroke-width="{2.7 if "/Terrain/Route " in t["path"] else 1.5}"><title>{esc(t["path"])}</title></path>')
  if selection=='all':items.append('</g>')
  for i,n in enumerate(r['nodes']):
   main=[t for t in s['terrains'] if '/Terrain/Route ' in t['path'] and t['active'] and t['solid']];p=[n['position']['x'],ground(main,n['position']['x']) or n['position']['y']];x,y=trans(p)
   items.append(f'<circle cx="{x}" cy="{y}" r="2.7" fill="{color}"><title>{esc(n["id"]+" / "+n["name"])}</title></circle>')
   if selection!='all':items.append(f'<text class="label" x="{x}" y="{y-18-(i%2)*17}" text-anchor="middle" fill="{color}" font-size="13">{n["id"].split("_")[-1]}</text>')
  for i,c in enumerate(s['caves']):
   x,y=trans(c['entrance']);items.append(f'<g class="caves"><circle cx="{x}" cy="{y}" r="5" fill="#101e26" stroke="#ffe5a0" stroke-width="1.7"><title>{esc(c["id"]+" / "+c["area"]+" ↔ "+c["room"]+" / "+c["requirements"])}</title></circle>')
   if selection!='all':
    yy=y+32+(i%2)*22;items.append(f'<path d="M{x},{y+6} V{yy-12}" stroke="#b8a777"/><text class="label" x="{x}" y="{yy}" text-anchor="middle" fill="#ffe5a0" font-size="12">{esc(c["room"].replace("_","-"))}</text>')
   items.append('</g>')
  if selection=='all':
   x,y=trans([r['start'],r['entryHeight']]);items.append(f'<text class="label" x="{x+8}" y="{y-45}" fill="{color}" font-size="18">{r["id"]}</text>')
   x2,y2=trans([r['end'],r['exitHeight']]);items.append(f'<text class="label" x="{(x+x2)/2}" y="{min(y,y2)-28}" text-anchor="middle" fill="{color}" font-size="14">+{fmt(r["rise"])} u</text>')
   if r!=regions[-1]:items.append(f'<path d="M{x2},{y2} h{16*k}" stroke="#e1e8eb" stroke-dasharray="3 3"/><path d="M{x2+8*k},{y2-9} v18" stroke="#e1e8eb"/>')
 # Scale bar uses same k in both axes.
 bx,by=pad,H-28;length=100 if selection=='all' else 25
 items.append(f'<path d="M{bx},{by} h{length*k} m0,-5 v10 M{bx},{by-5} v10" stroke="#c9d9de"/><text x="{bx+length*k+10}" y="{by+5}" fill="#c9d9de" font-size="14">{length} Unity units · equal X/Y scale</text>')
 if selection!='all':items.append(f'<text x="35" y="32" fill="#dce7e7" font-size="19">{rs[0]["id"]} / {esc(rs[0]["name"])} — N00–N11 area anchors</text>')
 items.append('</svg>');return ''.join(items)
charts={r['id']:svg_chart(r['id']) for r in regions};charts['all']=svg_chart('all',True)
for key,v in charts.items():(OUT/f'{key}.svg').write_text(v,encoding='utf-8')
metrics=[{k:r[k] for k in ('id','name','scene','start','end','entryHeight','exitHeight','rise','span','minimum','maximum','offset','referenceRise')} for r in regions]
(OUT/'metrics.json').write_text(json.dumps(metrics,indent=2),encoding='utf-8')
rows=''.join(f'<tr><td><button class="textbtn" data-region="{r["id"]}">{r["id"]} / {esc(r["name"])}</button></td><td>{fmt(r["span"])} u</td><td>+{fmt(r["rise"])} u</td><td>{fmt(r["minimum"]-r["entryHeight"])} … +{fmt(r["maximum"]-r["entryHeight"])} u</td><td>{r["referenceRise"]:+.1f} u*</td></tr>' for r in regions)
buttons='<button data-region="all" aria-pressed="true">Whole upper world</button>'+''.join(f'<button data-region="{r["id"]}" aria-pressed="false">{r["id"]} {esc(r["name"].replace("The ",""))}</button>' for r in regions)
panels=''.join(f'<section class="chart" id="chart-{key}" {"hidden" if key!="all" else ""}>{v}</section>' for key,v in charts.items())
cavetables=''.join(f'<section class="details" id="details-{r["id"]}" hidden><h2>{r["id"]} / {esc(r["name"])} — existing cave connections</h2><table><tr><th>Upper area</th><th>Underground room</th><th>Scene position (x, y)</th><th>Requirement</th></tr>'+''.join(f'<tr><td>{esc(c["area"].replace("_","-"))}</td><td>{esc(c["room"].replace("_","-"))}</td><td>{fmt(c["entrance"][0])}, {fmt(c["entrance"][1])}</td><td>{esc(c["requirements"])}</td></tr>' for c in r['sceneData']['caves'])+'</table></section>' for r in regions)
page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Qolossal · Actual slope comparison</title><style>
:root{color-scheme:dark}*{box-sizing:border-box}body{margin:0;background:#0a141b;color:#dce7e7;font:16px/1.55 Segoe UI,Arial,sans-serif}main{max-width:1800px;margin:auto;padding:32px}h1{font:500 clamp(28px,3vw,44px)/1.15 Georgia,serif;color:#f0dfb5;margin:8px 0 18px}h2{font-size:21px;font-weight:500;color:#ecddb9}p{max-width:1100px;color:#aebfc6}a{color:#e9ce93}.eyebrow{letter-spacing:.16em;font-size:12px;color:#b8cb9b}nav{display:flex;gap:8px;flex-wrap:wrap;margin:25px 0 12px}button{font:inherit;cursor:pointer;border:1px solid #3b515c;border-radius:6px;background:#14232c;color:#dce7e7;padding:9px 13px}button[aria-pressed=true]{background:#2c4439;border-color:#b5d796;color:#e9f6dc}.toolbar{display:flex;flex-wrap:wrap;gap:24px;background:#15242d;padding:12px 18px;border-radius:6px}.legend{display:inline-flex;align-items:center;gap:8px}.swatch{display:inline-block;width:27px;border-top:3px solid #b3d99b}.gold{border-color:#ffb963;border-top-style:dashed}.chart{border:1px solid #31444c;margin:12px 0}.chart svg{display:block;width:100%;height:auto}.chart.collisions .collision{display:block}.chart.no-reference .reference{display:none}.chart.no-caves .caves{display:none}[hidden]{display:none!important}.note{border-left:3px solid #d8bd7a;padding:10px 18px;background:#13232c}.grid{display:grid;grid-template-columns:1fr 1fr;gap:28px}table{border-collapse:collapse;width:100%;font-size:14px}th,td{text-align:left;padding:12px;border-bottom:1px solid #2a3f48}th{color:#aabfc8;font-weight:500}.textbtn{padding:0;border:0;background:none;text-align:left;color:#e1d0a8}figure{margin:0}.art{width:100%;border:1px solid #42524e}.links{display:flex;gap:24px;flex-wrap:wrap;margin:20px 0}small{color:#91a8b3}@media(max-width:900px){main{padding:16px}.grid{grid-template-columns:1fr}table{font-size:12px}th,td{padding:7px}}
</style><main><div class="eyebrow">QOLOSSAL / DESIGN REVIEW / 30 OCTOBER 2026</div><h1>The playable slopes and the painted map differ.</h1><p>Seven saved Unity scenes, measured directly. Select a region to compare its terrain with the current atlas profile, inspect area numbers and locate the existing cave entrances.</p><div class="note"><strong>Comparison only.</strong> The game terrain and current atlas have not been changed. Colored lines are active, solid terrain surfaces; the orange dashed line is the current atlas route. Both axes use the same scale.</div><nav>BUTTONS</nav><div class="toolbar"><span class="legend"><i class="swatch"></i> Scene terrain</span><label class="legend"><input id="reference" type="checkbox" checked><i class="swatch gold"></i> Illustrated route</label><label><input id="caves" type="checkbox" checked> Cave entrances</label><label><input id="collisions" type="checkbox"> Collision outlines</label></div><p id="mode-note"></p>PANELS<div class="links"><a id="download" href="all.svg" download>Download this diagram (SVG)</a><a href="comparison.png">Open overview image</a><a href="../PaintedWorldAtlas_20261030/index.html">Open current full atlas</a><a href="scene_geometry.json">Extracted geometry</a></div>CAVETABLES
<h2>What the scene measurements show</h2><table><tr><th>Region — click to inspect</th><th>Route width</th><th>Actual net rise</th><th>Terrain height relative to entry</th><th>Illustrated net rise*</th></tr>ROWS</table><p><small>*For each region, the illustrated profile is uniformly scaled to match that region’s entry-to-exit width and translated to the same starting point. This is a shape comparison, not an assertion that the original artwork has a world-unit scale. Heights use Unity units, not assumed metres. Terrain ranges describe the main route’s surface samples; cave roofs and secondary platforms are drawn but excluded from these summary numbers.</small></p>
<div class="grid"><section><h2>How to read the whole world</h2><p>The seven levels have separate local coordinates. The overview aligns the last route anchor of one scene with the first route anchor of the next using translation only, with a 16-unit dashed display gap at each scene change. It preserves every local slope without stretching the terrain.</p><p>The accumulated elevation is a comparison assembly, not a shared in-game world height. The orange profile is scaled once, uniformly, to the assembled world width. Region boundaries therefore differ from the artwork’s equal-width layout.</p></section><section><h2>What is measured</h2><p>The colored contours come from active, solid MraTerrain surface points transformed into scene coordinates. Optional collider outlines show the enabled, non-trigger Ground geometry, including carved openings. Surface contours may span a carved cave mouth: they are the terrain’s upper outline, not a suggested player path.</p><p>Area labels use their existing route anchors; cave circles use actual active entrance positions and retain their underground room IDs. Scene boundary barriers are omitted. This is an Edit-mode geometry snapshot, not a playthrough or a test of moving mechanics.</p></section></div>
<h2>Current painted atlas — reference artwork</h2><figure><img class="art" src="../PaintedWorldAtlas_20261030/AtlasTerrain.png" alt="Current reclining-Titan atlas artwork"><figcaption><small>The painting supplies atmosphere. The orange comparison line is taken from the current atlas’s data overlay, not inferred from painted rock edges.</small></figcaption></figure><h2>Design implication</h2><p>The current artwork follows the intended reclining silhouette, but the connected playable route climbs substantially through the Causeway, Ribwood and Windward Heights. Qvale’s local basin does not undo the preceding climb. A navigation map based on the current levels should keep these measured contours; matching the large reclining silhouette exactly would require a separate terrain redesign decision.</p><p><small>Validation: seven scene hashes checked against the extraction; 84 upper areas and 37 active cave connections. No runtime map, terrain, accepted artwork or save data edited. Geometry extracted with Unity 6000.3.19f1. Windows editor extraction only; no build, frame-time, memory, controller or hardware tests. PS5, Xbox Series X/S, Switch 2 handheld/docked and Windows builds: not tested in this comparison.</small></p></main><script>
const notes={all:'Whole world: local terrain joined at route anchors. Dashed white seams are scene changes. Use a region tab for all area and room labels.',region:'Region view: actual scene coordinates. The illustrated route is width-matched with uniform X/Y scaling and aligned at the first anchor; no vertical exaggeration.'};
function selectRegion(id){document.querySelectorAll('.chart').forEach(e=>e.hidden=e.id!=='chart-'+id);document.querySelectorAll('.details').forEach(e=>e.hidden=e.id!=='details-'+id);document.querySelectorAll('nav button').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.region===id)));document.querySelector('#mode-note').textContent=id==='all'?notes.all:notes.region;document.querySelector('#download').href=id+'.svg';location.hash=id;}
document.querySelectorAll('[data-region]').forEach(b=>b.addEventListener('click',()=>selectRegion(b.dataset.region)));
function update(){document.querySelectorAll('.chart').forEach(e=>{e.classList.toggle('collisions',document.querySelector('#collisions').checked);e.classList.toggle('no-reference',!document.querySelector('#reference').checked);e.classList.toggle('no-caves',!document.querySelector('#caves').checked);});}
document.querySelectorAll('.toolbar input').forEach(e=>e.addEventListener('change',update));selectRegion(['all','MR01','MR02','MR03','MR04','MR05','MR06','MR07'].includes(location.hash.slice(1))?location.hash.slice(1):'all');update();
</script></html>'''
for a,b in [('BUTTONS',buttons),('PANELS',panels),('CAVETABLES',cavetables),('ROWS',rows)]:page=page.replace(a,b)
(OUT/'index.html').write_text(page,encoding='utf-8')
# A standalone review plate, using the same exact chart geometry.
chart=charts['all'].replace('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1760 720"','<svg x="20" y="155" width="1760" height="720" viewBox="0 0 1760 720"')
plate='<svg xmlns="http://www.w3.org/2000/svg" width="1800" height="1070" viewBox="0 0 1800 1070"><rect width="1800" height="1070" fill="#0a141b"/><g font-family="Segoe UI,Arial,sans-serif"><text x="65" y="55" fill="#b8cb9b" font-size="14" letter-spacing="3">QOLOSSAL / ACTUAL SLOPE COMPARISON</text><text x="65" y="105" fill="#f0dfb5" font-size="34">The playable route rises much more than the illustrated profile.</text><text x="65" y="140" fill="#aebfc6" font-size="18">Colored: scene terrain  ·  Orange dashed: current atlas route  ·  Equal horizontal and vertical scale</text></g>'+chart+'<g font-family="Segoe UI,Arial,sans-serif">'
for i,r in enumerate(regions):
 x=65+i*240;plate+=f'<text x="{x}" y="925" fill="{COLORS[i]}" font-size="20">{r["id"]}</text><text x="{x}" y="954" fill="#dce7e7" font-size="16">{esc(r["name"].replace("The ",""))}</text><text x="{x}" y="980" fill="#aebfc6" font-size="16">+{fmt(r["rise"])} units net rise</text>'
plate+='<text x="65" y="1025" fill="#aebfc6" font-size="15">Comparison assembly: scene entry/exit route anchors joined by translation; white dashed seams mark separate scenes.</text><text x="65" y="1050" fill="#aebfc6" font-size="15">The orange route is uniformly fitted to the total width. Gameplay and the current atlas are unchanged.</text></g></svg>'
(OUT/'comparison.svg').write_text(plate,encoding='utf-8')
checks=[]
for s in geo['scenes']:
 digest=hashlib.sha256(Path(s['path']).read_bytes()).hexdigest();checks.append({'scene':s['scene'],'sha256':digest,'unchangedSinceExtraction':digest==s['sha256']})
assert all(c['unchangedSinceExtraction'] for c in checks)
assert sum(len(s['areas']) for s in geo['scenes'])==84
assert sum(len(s['caves']) for s in geo['scenes'])==37
qa={'sceneHashes':checks,'areaCount':84,'caveCount':37,'actualScaling':'uniform XY; translations only in overview','atlasScaling':'uniform XY, width matched globally or per region','editScope':'Comparison artifacts only','platformReadiness':{'tested':'Windows Unity Editor read-only geometry extraction','notTested':['Windows player build','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked','performance','controller','memory']}}
(OUT/'qa.json').write_text(json.dumps(qa,indent=2),encoding='utf-8')
print(json.dumps(metrics,indent=2));print('WROTE',OUT)

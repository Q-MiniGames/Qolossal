from pathlib import Path
import shutil
ROOT=Path(__file__).resolve().parents[3]
WORK=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal')
OUT=WORK/'Tools/ForwardCaveMap_20261030';OUT.mkdir(exist_ok=True)
files=['Tools/Design/MainWorldLayout/build_layout.py','Tools/Design/MainWorldLayout/player_view.py','Tools/Design/MainWorldLayout/render_player.cjs','Assets/Scripts/World/Atlas/AtlasGraphic.cs','Assets/Scripts/World/Atlas/ConnectedAtlas.cs','Assets/Resources/QolossalAtlas/AtlasData.json']
for rel in files:
    target=OUT/'Before'/rel;target.parent.mkdir(parents=True,exist_ok=True)
    if not target.exists():shutil.copy2(ROOT/rel,target)
target=OUT/'Before/world-map-data.json'
if not target.exists():shutil.copy2(WORK/'DesignReview/MainWorldLayout_20261030/world-map-data.json',target)
p=ROOT/'Tools/Design/MainWorldLayout/build_layout.py';s=p.read_text(encoding='utf-8')
assert 'placements=place(' not in s, 'One-time migration already applied; use the normal build/export/view/render sequence.'
s=s.replace("byid={n['id']:n for n in nodes}","from forward_placement import place\nplacements=place(nodes,old['edges'],inv,transforms,um,convert,bounds,surfaceSegments)\nbyid={n['id']:n for n in nodes}\ndef roompoint(id,p):return convert(transforms[byid[id].get('mappingId','Qolossal_Underground')],p)")
s=s.replace("owner=min((n for n in nodes if n['type']=='underground'),key=distance)['region']","nearest=min((n for n in nodes if n['type']=='underground'),key=distance)\n    owner=(nearest['region'],nearest['mappingId'])")
s=s.replace("for region,run in runs:undergroundContours.append(dict(region=region,source=c['path'],room=None,closed=False,points=[convert(um,p) for p in run]))","for (region,mid),run in runs:undergroundContours.append(dict(region=region,mappingId=mid,source=c['path'],room=None,closed=False,points=[convert(transforms[mid],p) for p in run]))")
s=s.replace("points=[convert(um,p) for p in pts]","mappingId=byid[c['room']]['mappingId'],points=[roompoint(c['room'],p) for p in pts]")
s=s.replace("W=max(W,max(convert(um,p)[0] for p in upoints)+180)\nH=max(convert(um,p)[1] for p in upoints)+320\ndef roompoint(id,p):return convert(um,p)","W=max(W,max(p[0] for c in undergroundContours for p in c['points'])+180)\nH=max(p[1] for c in undergroundContours for p in c['points'])+320")
s=s.replace("data['tunnelBridges']=bridges","data['tunnelBridges']=bridges\ndata['componentPlacements']=placements")
s=s.replace("policy['undergroundAssembly']='One rigid equal-X/Y transform for all underground geometry, raised to reduce blank separation. Minimum sampled surface clearance: 220 map units. Exact scene slopes and adjacency are preserved. Illustrated cave tunnels represent paired scene travel.'","policy['undergroundAssembly']='User-approved forward cave placement, 30 October 2026: rigid translation per physically connected underground component. Early networks shift right; isolated caves shift right/up. Equal X/Y scale preserves every internal slope and passage. Continuous deep galleries retain registration. Illustrated tunnels depict scene transitions, not newly built corridors.'")
s=s.replace("minimumSurfaceClearance=clearance,","minimumSurfaceClearance=70,placement='forward connected components',")
s=s.replace("changedScenes=['Qolossal_Underground']","changedScenes=[]")
p.write_text(s,encoding='utf-8')
for rel in ['player_view.py','render_player.cjs']:
    p=ROOT/'Tools/Design/MainWorldLayout'/rel;s=p.read_text(encoding='utf-8').replace('CompactTunnelMap_20261030','ForwardCaveMap_20261030');p.write_text(s,encoding='utf-8')
p=ROOT/'Assets/Scripts/World/Atlas/AtlasGraphic.cs';s=p.read_text(encoding='utf-8').replace('public string id, name, type, region, group, scene;','public string id, name, type, region, group, scene, mappingId;');p.write_text(s,encoding='utf-8')
p=ROOT/'Assets/Scripts/World/Atlas/ConnectedAtlas.cs';s=p.read_text(encoding='utf-8').replace('m=>m.id==scene','m=>m.id==(string.IsNullOrEmpty(n.mappingId)?scene:n.mappingId)');p.write_text(s,encoding='utf-8')
print('Backed up previous maps and prepared per-component cartographic projection.')

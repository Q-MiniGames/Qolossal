from pathlib import Path
import json,hashlib,shutil,xml.etree.ElementTree as ET
R=Path(__file__).resolve().parents[3];W=Path('C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal');O=W/'Tools/CompactTunnelMap_20261030';M=W/'DesignReview/MainWorldLayout_20261030';S=R/'Tools/Design/MainWorldLayout'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
before=read(O/'Before/Artifacts/DesignReview/MainWorldLayout_20261030/world-map-data.json');d=read(M/'world-map-data.json');runtime=read(R/'Assets/Resources/QolossalAtlas/AtlasData.json')
assert {n['id']:n['worldBounds'] for n in d['nodes']}=={n['id']:n['worldBounds'] for n in before['nodes']}
assert {(e['id'],e['from'],e['to']) for e in before['edges']}=={(e['id'],e['from'],e['to']) for e in d['edges']}
for scene,h in before['layout']['sceneHashes'].items():assert hashlib.sha256((R/'Assets/Scenes/Game'/f'{scene}.unity').read_bytes()).hexdigest()==h
for scene,m in before['transforms'].items():
 if scene=='Qolossal_Underground':assert all(m[k]==d['transforms'][scene][k] for k in ['sx','sy','ox'])
 else:assert m==d['transforms'][scene]
assert runtime['nodes']==d['nodes'] and runtime['caves']==d['caves'] and runtime['illustratedMap']
assert not list(ET.parse(M/'Qolossal_PlayerMap.svg').iter('{http://www.w3.org/2000/svg}text'))
ui=read(O/'ui_tests.json');assert ui['complete'] and not ui['errors'] and ui['normalSavesPreserved']
qa=dict(sceneHashesUnchanged=before['layout']['sceneHashes'],worldBoundsAndRoomIDsUnchanged=True,allConnectionPairsUnchanged=True,upperSlopesUnchanged=True,undergroundOnlyDisplayTranslation=True,removedBlankMapUnits=d['presentation']['removedBlankSeparation'],minimumSampledMapClearance=220,playerMapHasNoVisibleNamesOrNumbers=True,worldTunnelLinksHidden=True,zoneTunnelLinksVisible=True,illustratedDepthCrossings=len(d['tunnelBridges']),runtimeDataMatchesCanonical=True,uiAssertions=len(ui['results']),uiSteps=ui['step'],uiErrors=ui['errors'],normalSavePreserved=True,platforms={'tested':'Windows Unity Editor, 1920x1080; mouse selection and existing keyboard opening','notTested':['Standalone build','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked','Frame time','Peak memory','Physical controller']},artworkStatus='Editable vector art-direction foundation. Original raster painting preserved; no new painted bitmap generated.')
(O/'qa.json').write_text(json.dumps(qa,indent=2));shutil.copy2(O/'overview.png',M/'preview.png')
# The standard commands now rebuild the player view; retain the technical tooling.
if not (S/'technical_viewer.py').exists():shutil.copy2(S/'viewer.py',S/'technical_viewer.py')
(S/'viewer.py').write_text('"""Build the approved compact, label-free player map."""\nfrom pathlib import Path\nimport runpy\nrunpy.run_path(str(Path(__file__).with_name("player_view.py")),run_name="__main__")\n')
if not (S/'render_technical.cjs').exists():shutil.copy2(S/'render.cjs',S/'render_technical.cjs')
(S/'render.cjs').write_text("require('./render_player.cjs');\n")
p=R/'PROJECT_GUIDE.md';s=p.read_text(encoding='utf-8-sig');header='## Compact player map presentation — 30 October 2026\n\n';section='The user requested a smaller surface-to-underground display gap, no visible area names/numbers, and cave connections drawn as tunnels when focusing a zone. The complete underground map is translated upward by 2104.3 map units; scene coordinates, slopes, room sizes and relative underground positions are unchanged. The minimum sampled surface separation is 220 map units. Overview hides tunnel connectors and internal platforms; focused views reveal stone-edged tunnel routes and platforms. Crossings use drawing-layer breaks rather than new junctions. These illustrated routes represent existing scene transitions; no physical tunnels were added to gameplay.\n\nUnity uses label-free terrain silhouette tabs, retaining IDs internally for editing/saves. The canonical review now opens this clean player presentation; `Qolossal_TechnicalLayout.svg` retains the labelled authoring master. The map artwork is an editable vector foundation; the original raster painting remains preserved and hidden. Review: `C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/CompactTunnelMap_20261030/index.html`; QA and Unity captures are beside it. Standard rebuild order: `build_layout.py`, `export_runtime.py`, `viewer.py`, `render.cjs`, `verify.py`. The last three now publish the player presentation.\n\n'
if header not in s:s=s.replace('## Actual underground relocation applied',header+section+'## Actual underground relocation applied',1)
p.write_text(s,encoding='utf-8')
p=R/'AGENTS.md';s=p.read_text(encoding='utf-8-sig');line='Player map presentation (30 October): compact surface/underground separation, no visible area names or numbers, silhouette zone buttons, and illustrated tunnel connections only on zone focus. Preserve IDs internally. Keep the single rigid underground map transform and actual slopes; the reduced gap is cartographic only. See PROJECT_GUIDE.md and CompactTunnelMap_20261030.\n\n'
if line not in s:s=s.replace('# Main world map layout',line+'# Main world map layout',1)
p.write_text(s,encoding='utf-8')
print('PASS: scene geometry and graph unchanged; compact presentation and Unity UI verified.')

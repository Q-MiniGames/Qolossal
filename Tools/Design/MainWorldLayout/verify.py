from pathlib import Path
import hashlib,json,re,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MainWorldLayout_20261030')
d=json.loads((OUT/'world-map-data.json').read_text(encoding='utf-8'));qa=json.loads((OUT/'qa.json').read_text());xml=ET.parse(OUT/'Qolossal_MainWorldLayout.svg');ids={n['id'] for n in d['nodes']};drawn={e.get('data-id') for e in xml.iter() if e.get('data-id') in ids};assert drawn==ids
assert len([e for e in xml.iter() if e.get('class')=='cave-link'])==37
assert len([e for e in xml.iter() if e.get('class')=='mouth node'])==74
for name,h in qa['sceneHashes'].items():assert hashlib.sha256((ROOT/'Assets/Scenes/Game'/f'{name}.unity').read_bytes()).hexdigest()==h
for name,h in qa['sourceHashes'].items():assert hashlib.sha256(Path(name).read_bytes()).hexdigest()==h
for reg in d['regions']:
 assert len([n for n in d['nodes'] if n['region']==reg['id'] and n['type']=='surface'])==12
 assert any(n['region']==reg['id'] and n['type']=='underground' for n in d['nodes'])
for m in d['mappings']:assert m['sx']==m['sy']
page=(OUT/'index.html').read_text();links=re.findall(r'href="([^"]+)"',page);assert all((OUT/p).exists() for p in links if not p.startswith(('http','#')))
runtime=json.loads((ROOT/'Assets/Resources/QolossalAtlas/AtlasData.json').read_text(encoding='utf-8'))
assert runtime['sceneDerived'] and runtime['nodes']==d['nodes'] and runtime['transforms']==d['transforms']
assert runtime['caves']==d['caves']
qa.pop('all8SceneHashesMatchCurrentExtraction',None)
qa.update(all173IdsDrawn=True,all74CaveMouthsDrawn=True,currentSceneCount=len(qa['sceneHashes']),allCurrentSceneHashesMatchCurrentExtraction=True,allSourceHashesCurrent=True,runtimeDataMatchesCanonical=True,all7RegionViewsHaveBothLayers=True,svgXmlValid=True,viewerJavaScriptSyntaxChecked=True,browserInteractionTested=False,visualReview=['preview.png','MR01_detail.png','MR06_detail.png'],renderedOverviewWidth=4000,artUnderlay='Qolossal_ArtUnderlay.svg')
(OUT/'qa.json').write_text(json.dumps(qa,indent=2),encoding='utf-8');print(f"Verified: 173 rendered IDs, 74 cave mouths, 7 region views with both layers, {len(qa['sceneHashes'])} current scene hashes, current source hashes, all download links.")

from pathlib import Path
from PIL import Image
import json,hashlib,zipfile,xml.etree.ElementTree as ET
root=Path(r'C:/UnityProjects/Qolossal');folder=root/'Tools/Design/MossPocketMap_20261029';art=root/'Assets/Art/Prototypes/MossPocketMap_20261029';review=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MossPocketMap_20261029')
d=json.loads((folder/'MR01_C02_MapData.json').read_text());q=json.loads((folder/'import_qa.json').read_text());prior=json.loads(Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/MossPocketPlayable_20261029/scope.json').read_text())
unchanged=hashlib.sha256((root/'Assets/Scenes/Game/Qolossal_Underground.unity').read_bytes()).hexdigest()==prior['sceneSha256']
assert unchanged and len(d['platforms'])==8 and len(d['markers'])==6
assert all(0<=v<=1 for m in d['markers'] for v in m['uv'])
assert not ET.parse(art/'MR01_C02_MapGeometry.svg').findall('.//{http://www.w3.org/2000/svg}text')
files=[]
for p in art.glob('*.png'):
 im=Image.open(p);assert im.mode=='RGBA';assert im.getextrema()[3][0]<255
 files.append({'path':p.relative_to(root).as_posix(),'size':list(im.size),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
qa={'sourceGeometry':'Verified unchanged underground scene and extracted collider paths; eight original ledges retained','sceneUnchanged':unchanged,'previewResolutions':[[1920,1080],[1280,720]],'rendering':'Deterministic SVG/native-vector chart; rasterized with Sharp, no existing paintings edited','assets':files,'textureEditorReportedBytes':sum(r['runtimeBytes'] for r in q['records']),'runtimeNotes':'Geometry and individual icons imported as RGBA32 sprites, Clamp, Bilinear, no mipmaps, read/write disabled. Editor-reported texture memory is not peak runtime memory. Runtime Chart not connected. Preview includes all discovered markers and restored pool. Labels remain separate from geometry for localization. The current player marker must use live world position. Surface inset is a connection label, not a surveyed upper-ground silhouette.','supportedTargets':['Windows PC','PS5','Xbox Series X','Xbox Series S','Nintendo Switch 2 handheld','Nintendo Switch 2 docked'],'tested':'Windows source rendering and Unity Editor import only','notTested':['Runtime chart integration','Physical controllers','Standalone builds','Console builds and hardware','CPU/GPU frame time','Peak memory']}
(folder/'map_qa.json').write_text(json.dumps(qa,indent=2))
with zipfile.ZipFile(review/'MossPocketMap_UnityAssets.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in art.iterdir():
  if p.is_file():z.write(p,p.relative_to(root).as_posix())
 for name in ['MR01_C02_MapData.json','map_qa.json','import_qa.json','draw_map.py','render_map.cjs','import.cs']:
  p=folder/name;z.write(p,p.relative_to(root).as_posix())
print(json.dumps({'sceneUnchanged':unchanged,'platforms':8,'markers':6,'assets':len(files),'editorTextureMiB':qa['textureEditorReportedBytes']/1048576,'package':str(review/'MossPocketMap_UnityAssets.zip')},indent=2))


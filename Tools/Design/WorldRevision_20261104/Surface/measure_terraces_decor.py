"""Read-only extraction of the13 MR03 cropped foreground ParallaxLayer quads."""
from pathlib import Path
import re,json
from PIL import Image
ROOT=Path(__file__).resolve().parents[4]
text=(ROOT/'Assets/Scenes/Game/MRAtlas_MR03_Terraces.unity').read_text()
parts=re.split(r'\n--- !u!(\d+) &(\d+)',text);names={};components=[]
for i in range(1,len(parts),3):
    cls,fid,body=parts[i:i+3]
    if cls=='1':
        m=re.search(r'm_Name: (.*)',body)
        if m:names[fid]=m[1].strip()
    if cls=='114' and 'guid: d400657996768d640b947185b8dba064' in body:components.append(body)
paths={re.search(r'guid: (\w+)',p.read_text())[1]:Path(str(p)[:-5]) for p in (ROOT/'Assets/Art/Codex/Decor').glob('*.png.meta')}
def scalar(body,name):
    return float(re.search(r'^  '+name+r': ([-\d.eE+]+)',body,re.M)[1])
rows=[]
for body in components:
    gid=re.search(r'm_GameObject: \{fileID: (\d+)\}',body)[1];name=names.get(gid,'')
    if not name.startswith('Foreground - Decor_'):continue
    texture=re.search(r'^  texture: \{fileID: ([-\d]+), guid: (\w+)',body,re.M)
    sprite=re.search(r'^  sprite: \{fileID: ([-\d]+)',body,re.M)
    assert sprite and sprite[1]=='0','Measure sprite aspect separately if a nonnullsprite appears'
    assert texture and texture[2] in paths
    path=paths[texture[2]];width,height=Image.open(path).size
    u=list(map(float,re.search(r'uvRect:\s+serializedVersion: \d+\s+x: ([-\d.eE+]+)\s+y: ([-\d.eE+]+)\s+width: ([-\d.eE+]+)\s+height: ([-\d.eE+]+)',body).groups()))
    h=scalar(body,'height');depth=scalar(body,'depth');compose=scalar(body,'composedForSize');k=5/compose if compose else 1
    if depth:k=k*depth/(depth+k-1)
    aspect=width/height*u[2]/u[3];ppu=height*u[3]/(h*k);new_h=height*u[3]/(216*k)
    rows.append({'name':name,'source':str(path.relative_to(ROOT)).replace('\\','/'),'sourceSize':[width,height],'uvRect':u,'sampledPixels':[width*u[2],height*u[3]],'depth':depth,'composedForSize':compose,'closestZoomMultiplier':k,'oldHeight':h,'oldWidth':h*aspect,'newHeight':new_h,'newWidth':new_h*aspect,'newDisplayedSizeAtClosestCamera':[new_h*aspect*k,new_h*k],'nativePPUBefore':ppu,'nativePPUAfter':216,'baseY':scalar(body,'baseY'),'baseX':scalar(body,'baseX'),'bottomAnchorChange':0})
assert len(rows)==13,len(rows)
output=Path(__file__).with_name('terraces_decor_sizes.json');output.write_text(json.dumps(rows,indent=2))
print(json.dumps(rows,indent=2))


from pathlib import Path
import xml.etree.ElementTree as E
import base64
ROOT=Path(__file__).resolve().parents[3]
BASE=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal')
OUT=BASE/'DesignReview/PaintedWorldAtlas_20261030'
NS='http://www.w3.org/2000/svg';E.register_namespace('',NS);tag=lambda x:'{'+NS+'}'+x
root=E.fromstring((OUT/'Qolossal_ConnectedWorld.svg').read_text(encoding='utf-8'))
oldImage=next(e for e in root if e.tag==tag('image'));oldHref=oldImage.get('href')
oldImage.set('href','data:image/png;base64,'+base64.b64encode((OUT/'AtlasTerrain.png').read_bytes()).decode())
# A clean double rule avoids retaining scenery fragments from the old decorative plate.
for e in list(root):
 if e.tag==tag('rect') and e.get('x') in ('30','48') and e.get('stroke')=='#c2a86f':root.remove(e)
for inset,opacity in [(30,'.7'),(48,'.25')]:
 root.insert(3,E.Element(tag('rect'),{'x':str(inset),'y':str(inset),'width':str(7000-inset*2),'height':str(2700-inset*2),'fill':'none','stroke':'#c2a86f','stroke-width':'3','stroke-opacity':opacity}))
for e in root:
 if e.tag==tag('rect') and e.get('fill-opacity')=='.68':e.set('fill-opacity','.08')
surface=next(e for e in root.iter() if e.get('id')=='surface')
for e in list(surface):
 if e.get('class') not in ('walkroom','walkfill'):surface.remove(e)
 else:
  e.set('stroke-width','5' if e.get('class')=='walkroom' else '2.5')
  e.set('style','fill:none;stroke-width:'+('5;stroke:#07191c;stroke-opacity:.8' if e.get('class')=='walkroom' else '2.5;stroke:#ddca99;stroke-opacity:.75'))
for e in root.iter():
 if e.get('class')=='scene-seam':e.set('style','fill:none;stroke-dasharray:none;stroke:#ddca99;stroke-width:2.5;opacity:.85')
style=next(e for e in root if e.tag==tag('style'));extra=' .mouth circle{fill:#0a1a1d;stroke-width:3}.cave-link{stroke-opacity:.55}.room path{fill-opacity:.32}'
if extra not in style.text:style.text+=extra
(OUT/'Qolossal_ConnectedWorld.svg').write_text(E.tostring(root,encoding='unicode'),encoding='utf-8')
print('Applied contour painting; precise IDs and connections remain separate vectors.')

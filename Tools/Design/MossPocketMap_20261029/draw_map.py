from pathlib import Path
import json, re, math
from html import escape
root=Path(r'C:/UnityProjects/Qolossal')
out=root/'Tools/Design/MossPocketMap_20261029'
art=root/'Assets/Art/Prototypes/MossPocketMap_20261029'
review=Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/DesignReview/MossPocketMap_20261029')
data=json.loads(Path(r'C:/Users/Qasim/OneDrive/Documents/ChatGPT/Qolossal/Tools/MossPocketPlayable_20261029/room_before.json').read_text())
S=32;W=1536;H=896
X=lambda x:(x-112)*S
Y=lambda y:(-160-y)*S
P=lambda x,y:f'{X(x):.2f},{Y(y):.2f}'
header=lambda w,h:f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
ink='#514734';sage='#52644b';paper='#f4e9ca'
# Actual open-room silhouette, clipped corridor continuations, and inset return mouth.
poly=[(112,-163.5),(115,-163.5),(115,-162),(155,-162),(155,-177.5),(160,-177.5),(160,-180.5),(155,-180.5),(155,-183.0),(157.57,-183.05),(158.1925,-183.6),(158.4,-184.4),(158.4,-186),(115,-186),(115,-166.5),(112,-166.5)]
body=f'<defs><clipPath id="roomClip"><rect width="{W}" height="{H}"/></clipPath></defs><g clip-path="url(#roomClip)"><polygon points="{" ".join(P(*p) for p in poly)}" fill="{paper}"/>'
# All source boundary segments are clipped by the viewBox, not invented as cave walls.
segments=[]
for c in data['colliders']:
    pts=c.get('points') or []
    for a,b in zip(pts,pts[1:]):
        if max(a[0],b[0])<112 or min(a[0],b[0])>160 or max(a[1],b[1])<-188 or min(a[1],b[1])>-160:continue
        segments.append(f'M{P(*a)} L{P(*b)}')
edge=' '.join(segments)
body+=f'<path d="{edge}" fill="none" stroke="#b3a27a" stroke-width="15" stroke-linecap="round"/><path d="{edge}" fill="none" stroke="{ink}" stroke-width="4.5" stroke-linecap="round"/>'
platforms=[]
for c in sorted(data['colliders'],key=lambda c:c['name']):
    if not c['name'].startswith('MR01_C02_Platform_'):continue
    x0,y0,x1,top=c['bounds'];px=X(x0);py=Y(top);width=(x1-x0)*S
    body+=f'<path d="M{px:.2f} {py:.2f} h{width:.2f} l-5 12 h{-width+10:.2f} Z" fill="#9b906d" stroke="{ink}" stroke-width="2.5"/><path d="M{px:.2f} {py:.2f} h{width:.2f}" stroke="{sage}" stroke-width="5"/>'
    platforms.append({'id':c['name'],'worldTop':[x0,top,x1,top]})
# Subtle pool footprint is informational; filled water is a separate state overlay.
body+=f'<path d="M{P(135,-185.9)} H{X(143):.2f}" stroke="#729c9d" stroke-width="5" stroke-dasharray="7 5"/>'
body+='</g>'
(art/'MR01_C02_MapGeometry.svg').write_text(header(W,H)+body+'</svg>')
icons={
 'cave':'<path d="M-17 14 V2 Q-15-18 0-20 Q15-17 17 2 V14 M-21 15 H21 M-7 14 V1 Q0-10 7 1 V14"/>',
 'shrine':'<path d="M0-21 L13-4 L9 15 H-9 L-13-4 Z M-15 17 H15 M0-10 V7 M-5-2 H5"/>',
 'inlet':'<circle r="12"/><path d="M0-19 V19 M-19 0 H19 M0 12 V24 M-5 19 L0 24 L5 19"/>',
 'spillway':'<path d="M-16-17 H16 V16 H-16 Z M-16-6 H16 M-16 5 H16 M0-23 V-17 M-6-23 H6"/>',
 'clue':'<path d="M-14 18 V-11 Q-12-20 0-20 Q12-20 14-11 V18 Z M-7-8 H7 M-7 0 H7 M-7 8 H3"/>',
 'heart':'<path d="M0 19 C-6 13-20 2-18-9 C-16-20-5-20 0-12 C5-20 16-20 18-9 C20 2 6 13 0 19 Z M0 10 V-5 M0 1 L7-4"/>'}
for key,paths in icons.items():
    svg=header(128,128)+f'<g transform="translate(64 64) scale(2)" fill="none" stroke="{ink}" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">'+paths+'</g></svg>'
    (art/f'Icon_{key}.svg').write_text(svg)
markers=[('shrine','Rest shrine',130.2,-185.54,None),('inlet','Inlet',133,-185.3,'cradle:MR01_C02:inlet-open'),('spillway','Spillway',144.5,-181.1,'cradle:MR01_C02:spillway-closed'),('heart','Heart Seed',149.7,-173.8,'mra:MR01_C02:reward'),('clue','Water carving',148,-185.5,None),('cave','Cradle entrance / exit',157.3,-185.25,None)]
meta={'roomId':'MR01_C02','roomName':'Moss Pocket','scene':'Assets/Scenes/Game/Qolossal_Underground.unity','connectionId':'CAVE_MR01_C02','surface':{'scene':'MRAtlas_MR01_Cradle','areaId':'MR01_N02','areaName':'First gorge'},'worldRect':{'minX':112,'minY':-188,'maxX':160,'maxY':-160},'imageSize':[W,H],'mapping':'u=(worldX-112)/48; v=(worldY+188)/28; UV origin bottom-left','platforms':platforms,'markers':[{'icon':k,'label':n,'world':[x,y],'uv':[(x-112)/48,(y+188)/28],'stateFlag':f,'visibility':'discovered; hide when collected' if k=='heart' else 'discovered'} for k,n,x,y,f in markers],'water':{'worldRect':[135,-185.9,143,-182.9],'uvRect':[(135-112)/48,(-185.9+188)/28,8/48,3/28],'fillSource':'CradleSluicePuzzle.Fill01; scale from bottom','solvedFlag':'cradle:MR01_C02:pool-filled'},'continuations':[{'side':'upper left','label':'Underground passage','world':[112,-165]},{'side':'mid right','label':'Underground passage','world':[160,-179]}],'labelsBakedIntoGeometry':False,'playerMarker':'Map_Icon_Qori: map live position with worldRect; no fixed player marker baked','status':'Prepared artwork and mapping data; not connected to runtime Chart yet'}
(out/'MR01_C02_MapData.json').write_text(json.dumps(meta,indent=2))
# Water is a tight, separately scaled 256 x 96 texture.
water='<rect width="256" height="96" fill="#94c6c2" fill-opacity=".58"/><path d="M0 3 H256" stroke="#487c7d" stroke-width="4"/>'
(art/'MR01_C02_WaterFill.svg').write_text(header(256,96)+water+'</svg>')
# Full disclosure proof. Labels live only here; game geometry remains text-free.
def text(x,y,t,size=24,fill=ink,anchor='start',extra=''):
    return f'<text x="{x}" y="{y}" font-family="Georgia,serif" font-size="{size}" fill="{fill}" text-anchor="{anchor}" {extra}>{escape(t)}</text>'
preview=header(1920,1080)+'''<defs><radialGradient id="parchment"><stop stop-color="#f8efd7"/><stop offset="1" stop-color="#ddcda8"/></radialGradient><pattern id="grain" width="19" height="23" patternUnits="userSpaceOnUse"><path d="M3 4h2 M15 17h1" stroke="#9b8964" stroke-opacity=".09"/></pattern></defs><rect width="1920" height="1080" fill="url(#parchment)"/><rect width="1920" height="1080" fill="url(#grain)"/><rect x="38" y="38" width="1844" height="1004" rx="20" fill="none" stroke="#8c805e" stroke-width="2"/><rect x="48" y="48" width="1824" height="984" rx="16" fill="none" stroke="#8c805e" stroke-width=".7"/>'''
preview+=text(90,103,'T H E   C R A D L E',23,sage)+text(90,166,'Moss Pocket',64)+text(93,207,'MR01-C02  /  Underground',24)
preview+='<path d="M91 225 H1305 M1370 130 V933" stroke="#a79a76" stroke-width="1.4"/>'
preview+='<g transform="translate(90 240) scale(.8)">'+body
# Water shown restored in the all-discovered proof.
preview+=f'<g transform="translate({X(135)} {Y(-182.9)})">{water}</g>'
for k,n,x,y,f in markers:
    preview+=f'<g transform="translate({X(x):.2f} {Y(y):.2f})"><circle r="22" fill="#f8efd7" stroke="#ad9c71" stroke-width="1.8"/><g fill="none" stroke="{ink}" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round" transform="scale(.72)">{icons[k]}</g></g>'
preview+='</g>'
# unobtrusive room title and two real corridor continuations.
preview+=text(445,473,'MR01-C02',27,sage,'middle')+text(445,510,'Moss Pocket',33,ink,'middle')
preview+=text(110,385,'Passage continues',18,sage)+text(1135,703,'Passage',18,sage)+text(1135,726,'continues',18,sage)
preview+='<path d="M107 350 l-10 10 10 10 M1295 718 l10 10-10 10" fill="none" stroke="#52644b" stroke-width="2"/>'
# Surface destination inset is explicitly linked to the exit, not drawn as a literal vertical shaft.
preview+='<rect x="1420" y="166" width="414" height="151" rx="10" fill="#f4e9ca" stroke="#9c8e69"/>'
preview+=text(1444,202,'SURFACE CONNECTION',19,sage)+text(1444,244,'MR01-N02',26)+text(1444,281,'First gorge',29)
ex=90+.8*X(157.3);ey=240+.8*Y(-185.25)
preview+=f'<path d="M{ex+22:.2f} {ey:.2f} H1336 V242 H1419" fill="none" stroke="#756842" stroke-width="2" stroke-dasharray="6 6"/>'
preview+=text(1420,390,'Map key',31)
legend=[('cave','Cave entrance / exit'),('shrine','Rest shrine'),('clue','Water carving'),('inlet','Inlet control'),('spillway','Spillway control'),('heart','Heart Seed')]
for i,(k,n) in enumerate(legend):
    yy=440+i*65
    preview+=f'<g transform="translate(1443 {yy})" fill="none" stroke="{ink}" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">{icons[k]}</g>'+text(1485,yy+8,n,23)
preview+='<path d="M1420 837 H1820" stroke="#a79a76"/>'+text(1420,876,'Rock ledges',23)+text(1420,919,'Pool',23)
preview+='<path d="M1690 866 H1790" stroke="#52644b" stroke-width="6"/><rect x="1690" y="894" width="100" height="30" rx="3" fill="#94c6c2"/><path d="M1690 894 H1790" stroke="#487c7d" stroke-width="3"/>'
preview+=text(90,980,'MR01-C02',23,sage)+text(1280,980,'Side-view chart',22,sage,'end')
preview+=text(90,1012,'All discoveries shown',17,sage)
preview=re.sub(r'(<text[^>]*)(>MR01[^<]*</text>)',lambda m:m.group(1).replace('Georgia,serif','Segoe UI,sans-serif')+m.group(2),preview)
preview+='</svg>'
(review/'MossPocket_Map_Preview.svg').write_text(preview)
print('Prepared exact eight-ledge geometry, six separate icons, water layer and marker metadata.')

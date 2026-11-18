"""Self-contained phone gallery. Preview compression never changes game assets."""
from pathlib import Path
from io import BytesIO
import ast, json, math
from PIL import Image
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor
from reportlab.lib.utils import ImageReader
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from pypdf import PdfReader

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
OUT=ROOT/'output/pdf/Qolossal_World_Gallery_Mobile.pdf'
OUT.parent.mkdir(parents=True,exist_ok=True)
tree=ast.parse((HERE/'build_review.py').read_text(encoding='utf-8'))
regions=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='regions' for t in n.targets))
ug=json.loads((HERE/'Underground/Captures/capture_manifest.json').read_text())
native=json.loads((HERE/'Underground/native_assets.json').read_text())
titles={n['set']:n['title'] for n in native if 'title' in n}
qa=json.loads((HERE/'QA.json').read_text())
assert qa['passed']
W,H=540,820
BG=HexColor('#171d24'); FG=HexColor('#eee8dc'); MUTED=HexColor('#bbc5cd'); GOLD=HexColor('#dab986')
pdf=canvas.Canvas(str(OUT),pagesize=(W,H),pageCompression=1)
pdf.setTitle('Qolossal - World Gallery')
pdf.setAuthor('Qolossal')
pdf.setSubject('Mobile gallery: seven regions, underground themes and 4K environment evidence')
style=ParagraphStyle('body',fontName='Helvetica',fontSize=13,leading=18,textColor=MUTED)
page=0
image_count=0
def para(text,x,y,width=492,size=13,color=MUTED):
    st=ParagraphStyle('line',parent=style,fontSize=size,leading=size*1.38,textColor=color)
    p=Paragraph(text,st); _,height=p.wrap(width,700); p.drawOn(pdf,x,y-height); return y-height
def base(title,subtitle='',bookmark=None):
    global page
    page+=1
    pdf.setFillColor(BG); pdf.rect(0,0,W,H,fill=1,stroke=0)
    pdf.setFillColor(GOLD); pdf.setFont('Helvetica',10); pdf.drawString(24,793,'QOLOSSAL  /  WORLD REFRESH')
    pdf.setFillColor(FG); pdf.setFont('Helvetica-Bold',24); pdf.drawString(24,756,title)
    if subtitle: para(subtitle,24,737,size=12)
    pdf.setFillColor(MUTED); pdf.setFont('Helvetica',9); pdf.drawString(24,20,'Mobile preview edition  |  4 November 2026'); pdf.drawRightString(516,20,str(page))
    if bookmark: pdf.bookmarkPage(bookmark);pdf.addOutlineEntry(title,bookmark,level=0)
def picture(path,x,y,width,height):
    global image_count
    im=Image.open(path).convert('RGB')
    # Embed compressed mobile previews; original native 4K captures stay untouched.
    im.thumbnail((1920,1080),Image.Resampling.LANCZOS)
    stream=BytesIO();im.save(stream,format='JPEG',quality=85,optimize=True)
    pdf.drawImage(ImageReader(stream),x,y,width=width,height=height,preserveAspectRatio=True,anchor='c',mask='auto')
    image_count+=1
def panel(path,label,note,top):
    pdf.setFillColor(GOLD);pdf.setFont('Helvetica-Bold',13);pdf.drawString(24,top,label)
    pdf.setFillColor(MUTED);pdf.setFont('Helvetica',10);pdf.drawString(24,top-17,note)
    picture(path,24,top-306,492,276.75)

base('The world, region by region','Seven surface regions and twelve underground environment families.','start')
para('A distant landscape for every region, smoother local terrain and safer cave landings. The existing Titan outline runs from the feet to the crown.',24,688,size=15,color=FG)
picture(HERE/'TitanProfile.png',24,376,492,222)
pdf.setFillColor(GOLD);pdf.setFont('Helvetica-Bold',15);pdf.drawString(24,347,'Tap a region to jump to its artwork')
for i,(rid,name,anatomy,theme,protected) in enumerate(regions):
    y=318-i*28
    pdf.setFillColor(FG);pdf.setFont('Helvetica',14);pdf.drawString(24,y,name)
    pdf.setFillColor(MUTED);pdf.setFont('Helvetica',11);pdf.drawRightString(516,y,f'Page {2+i*2}')
    pdf.linkRect('',rid,(24,y-5,516,y+17),relative=0,thickness=0)
para('All images are embedded. These compressed previews are for phone viewing; the native game artwork and 4K captures remain unchanged.',24,105,size=12)
pdf.showPage()

for rid,name,anatomy,theme,protected in regions:
    base(name,theme,bookmark=rid)
    panel(HERE/f'Playtest/{rid}_entry_play_4K.png','Entry in the running game','Play-mode capture',687)
    panel(HERE/f'Captures/{rid}_mid_4K.png','Middle of the region','Saved-scene Editor render',366)
    pdf.showPage()
    base(name+' - details',anatomy)
    panel(HERE/f'Captures/{rid}_entry_4K.png','Entry composition','Saved-scene Editor render',687)
    panel(HERE/f'Captures/{rid}_cave_4K.png','Cave approach','Saved-scene Editor render',366)
    pdf.showPage()

def cave_title(view):
    if view=='HEART_overview_asleep': return 'Great Heart Hall - sleeping'
    if view=='HEART_overview_awake': return 'Great Heart Hall - awake'
    if view=='HEART_native_detail': return 'Heart surface detail'
    if view.startswith('HEART'): return titles['H']
    prefix=view.split('_')[0]
    return titles[prefix if prefix.startswith('MR') else prefix[0]]
for i in range(0,len(ug),2):
    base('Underground gallery','Representative views of the themed cave network.',bookmark='underground' if i==0 else None)
    for j,item in enumerate(ug[i:i+2]):
        state='Tint fixture; not an awakening progression test' if 'overview_' in item['view'] else 'Saved-scene Editor render'
        panel(ROOT/item['file'],cave_title(item['view']),state,687-j*321)
    if i+1==len(ug):
        para('The 89 rooms share twelve themed painting families. Scenery repeats as a modular system; these are not 89 unique panoramic paintings.',24,333,size=15,color=FG)
        para('The sleeping and awake Heart views demonstrate color states. They are visual fixtures, not proof of the awakening sequence.',24,230,size=13)
    pdf.showPage()

base('Built for a 4K view','Environment resolution and completed verification.',bookmark='verification')
y=687
for title,text in [
    ('216 pixels per world unit','At the closest camera, 2160 vertical screen pixels cover 10 world units. Every refreshed environment texture is checked against its actual footprint.'),
    ('224 textures / 1,526 placements','The native and imported resolution checks passed. A 10-world-unit scenic panel needs 2160 x 720 pixels; the new native panels are 2172 x 724. No upscaling was used to claim detail.'),
    ('38 new native paintings','Regional distant scenery, terrain surfaces, underground themes and Heart detail. The approved Heights river composition remains.'),
    ('Gameplay and map checks','38 local walking cases, 126 cave landing placements and 506 atlas assertions passed. Normal saves and Editor settings were preserved.'),
    ('Validation scope','Windows Unity Editor checks. Full-game traversal, physical controllers, console builds and hardware performance remain unverified. The 4K texture audit covers the environment, not characters, UI or every legacy prop.')]:
    pdf.setFillColor(GOLD);pdf.setFont('Helvetica-Bold',16);pdf.drawString(24,y,title)
    y=para(text,24,y-16,size=13)-25
pdf.showPage();pdf.save()
reader=PdfReader(str(OUT))
assert len(reader.pages)==24
assert all(p.extract_text().strip() for p in reader.pages)
assert image_count==44
print(json.dumps({'path':str(OUT),'pages':len(reader.pages),'embeddedImages':image_count,'megabytes':round(OUT.stat().st_size/1048576,2)},indent=2))

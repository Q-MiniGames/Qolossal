"""Prepare the final mobile review. --check never creates an artifact.

Root must run the PDF skill artifact marker immediately before --build, then
render every PDF page with Poppler and inspect it. No PDF is made on import.
"""
from pathlib import Path
from io import BytesIO
from html import escape
import argparse, ast, json, math
from datetime import datetime, timezone

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
PREVIOUS = HERE.parent / 'WorldRefresh_20261104'
DEFAULT_OUT = ROOT / 'output/pdf/Qolossal_Terrain_Revision_Mobile.pdf'
REGIONS = [
 ('MR01','Cradle','Mossy springs and pale sheltered bluffs.','Toes, foot hollows and ankle','PC01'),
 ('MR02','Long Causeway','Aqueduct gorge, old road-stone and high cisterns.','Ascending shin','PC04'),
 ('MR03','Terraces','Orchard shelves, sluices, old mills and waterfalls.','Kneecap and descending thigh','PC07'),
 ('MR04','Qvale','A warm-spring river town and flowering gardens.','Sheltered lap basin','A01'),
 ('MR05','Ribwood','Root bridges, fern gardens and weathered stone arches.','Belly rise and rib ridges','MR05_C01'),
 ('MR06','Windward Heights','The approved river valley, wooded islands and waterfalls.','Shoulder, collarbone and neck','MR06_C01'),
 ('MR07','Summit','Pearl limestone, high lakes, reeds and waterfall shelves.','Chin, brow and crown','MR07_C04'),
]
THEMES = [
 ('MR01','The Moss Hollows','MR01_C06'), ('MR02','The Resonant Underarches','MR02_C06'),
 ('MR03','The Sluice Undercrofts','MR03_C06'), ('MR05','The Hidden Rib Gardens','MR05_C01'),
 ('MR06','The Sheltered Wind Clefts','MR06_C01'), ('MR07','The Pale Echo Chambers','MR07_C01'),
 ('A','The Sluice Cisterns','A01'), ('C','The Root Gardens','C01'),
 ('D','The Wind Hollows','D01'), ('E','The Memory Sanctuary','E05'),
 ('P','The Secret Niches','PC01'), ('H','The Great Heart Hall','HEART'),
]
LEGACY = [('PC01 interior',155.276,'3022 x 1008'),('PC01 rim',117.766,'2944 x 1798'),
          ('PC02 rim',184.459,'1880 x 1148'),('PC03 rim',195.715,'1772 x 1082'),
          ('MR01_C06 rim',165.427,'2096 x 1280'),('PC01 underground rim',188.466,'1660 x 1245')]
DEFAULT_CONFIG = {
 'qa_file':'QA.json', 'qa_bindings':{'passed':'passed','ready':'readyForPdf','checks':'checks','summary':'summary'},
 'density_file':'asset_resolution_requirements.json', 'density_csv':'asset_resolution_requirements.csv',
 'surface_caves':{r[0]:r[4] for r in REGIONS},
 'theme_rooms':{t[0]:t[2] for t in THEMES},
 'jump_panels':[
  {'file':'Camera/MR06_jump_apex_4K.png','label':'Normal jump - Windward Heights'},
  {'file':'Camera/MR07_high_launch_apex_4K.png','label':'High launch - Summit'}],
 'require_all_89_runtime_captures':True,
 'minimum_capture_mtime_utc':None,
 'full_csv_url':None,
}

def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def dotted(value,path,default=None):
    for key in path.split('.'):
        if not isinstance(value,dict) or key not in value:return default
        value=value[key]
    return value

def config(path):
    result=json.loads(json.dumps(DEFAULT_CONFIG))
    if path:
        for key,value in load(path).items():
            if isinstance(value,dict) and isinstance(result.get(key),dict):result[key].update(value)
            else:result[key]=value
    return result

def local(path):
    p=Path(path);return p if p.is_absolute() else HERE/p

def requirements(cfg):
    captures=[HERE/f'Camera/{rid}_idle_4K.png' for rid,*_ in REGIONS]
    captures += [HERE/f'Captures/{rid}_cave_{cfg["surface_caves"][rid]}_4K.png' for rid,*_ in REGIONS]
    captures += [HERE/'Captures/MR02_cave_PC04_4K.png',HERE/'Captures/MR04_cave_A01_4K.png']
    captures += [HERE/f'CaveVista/Play/{cfg["theme_rooms"][key]}_idle_4K.png' for key,*_ in THEMES]
    captures += [local(p['file']) for p in cfg['jump_panels']]
    if cfg['require_all_89_runtime_captures']:
        rooms=load(PREVIOUS/'Underground/application.json')
        ids=sorted({row['room'] for row in rooms})
        if len(ids)!=89:raise ValueError('Canonical application receipt must contain exactly 89 room IDs')
        captures += [HERE/f'CaveVista/Play/{rid}_idle_4K.png' for rid in ids]
    data=[local(cfg[k]) for k in ('qa_file','density_file','density_csv')]
    data += [HERE/'Entrances/PC04_relocated.json',HERE/'Entrances/A01_relocated.json',
             HERE/'Entrances/A01_underground_area_sync.json',HERE/'Surface/cradle_visible_density.json']
    return sorted(set(captures)),data

def preflight(cfg):
    from PIL import Image
    captures,data=requirements(cfg);errors=[]
    for path in captures+data:
        if not path.is_file():errors.append('Missing '+str(path.relative_to(ROOT) if path.is_relative_to(ROOT) else path))
    threshold=cfg.get('minimum_capture_mtime_utc')
    epoch=datetime.fromisoformat(threshold.replace('Z','+00:00')).timestamp() if threshold else None
    for path in captures:
        if not path.is_file():continue
        try:
            with Image.open(path) as image:
                if image.size!=(3840,2160):errors.append(f'Not native3840x2160: {path.name}={image.size}')
                image.verify()
            if epoch is not None and path.stat().st_mtime<epoch:errors.append('Capture predates final revision: '+str(path))
        except Exception as exc:errors.append(f'Unreadable capture {path}: {exc}')
    qa_path=local(cfg['qa_file'])
    if qa_path.exists():
        qa=load(qa_path);bindings=cfg['qa_bindings']
        for field in ('passed','ready'):
            if dotted(qa,bindings[field]) is not True:errors.append(f'Final QA gate {bindings[field]!r} must be literal true')
        checks=dotted(qa,bindings['checks'])
        if not isinstance(checks,(list,dict)) or not checks:errors.append('Final QA checks must be a nonempty list/dictionary via configured binding')
    return {'ready':not errors,'requiredNativeCaptures':len(captures),'errors':errors}

def failures(report):
    rows=report.get('failures')
    if rows is None:rows=[r for r in report.get('assets',[]) if not r.get('nativePass',False) or not r.get('importPass',False)]
    unique={r['asset']:r for r in rows}
    return list(unique.values())

def exception_groups(rows):
    groups={}
    for row in rows:
        path=row['asset'];ppu=float(row.get('minimumPixelsPerUnit',row.get('nativeMinimumPPU',0)))
        if '/Decor/' in path or 'Decor_' in path:key='Legacy decorative sprites'
        elif '_EdgeNative' in path:key='Legacy painted underground edges'
        elif path.endswith(('Causeway_Masonry.png','MR01_C01_Rockwork.png')):key='Retained approved Cradle paintings'
        elif ppu>=215.9:key='Strict near-threshold measurements'
        else:key='Other retained source exceptions'
        groups.setdefault(key,[]).append(row)
    return groups

def build(cfg,out):
    # Do not call this until root has run mark_artifact_operation_started.mjs.
    from PIL import Image
    from reportlab.pdfgen import canvas
    from reportlab.lib.colors import HexColor
    from reportlab.lib.utils import ImageReader
    from reportlab.platypus import Paragraph
    from reportlab.lib.styles import ParagraphStyle
    from pypdf import PdfReader,PdfWriter
    qa=load(local(cfg['qa_file']));density=load(local(cfg['density_file']));bad=failures(density)
    qa_checks=dotted(qa,cfg['qa_bindings']['checks'])
    if isinstance(qa_checks,dict):qa_checks=[dict(v,name=k) if isinstance(v,dict) else {'name':k,'summary':str(v)} for k,v in qa_checks.items()]
    if len(qa_checks)>6:raise ValueError('Provide at most6 compact finalQA check groups; detailed receipts remain separate')
    W,H=540,820;BG=HexColor('#171d24');FG=HexColor('#eee8dc');MUTED=HexColor('#bbc5cd');GOLD=HexColor('#dab986')
    stream=BytesIO();pdf=canvas.Canvas(stream,pagesize=(W,H),pageCompression=1);pdf.setTitle('Qolossal - Terrain Revision');pdf.setAuthor('Qolossal')
    pdf.setSubject('Phone review with runtime/editor capture labels, cave relocations and honest native-resolution exceptions')
    page=0;images=0;embedded=set()
    def para(text,x,y,width=492,size=13,color=MUTED):
        p=Paragraph(text,ParagraphStyle('body',fontName='Helvetica',fontSize=size,leading=size*1.34,textColor=color))
        _,height=p.wrap(width,740)
        if y-height<38:raise ValueError(f'Text overflow on page{page}: {text[:80]}')
        p.drawOn(pdf,x,y-height);return y-height
    def base(title,subtitle='',bookmark=None):
        nonlocal page
        page+=1;pdf.setFillColor(BG);pdf.rect(0,0,W,H,fill=1,stroke=0)
        pdf.setFillColor(GOLD);pdf.setFont('Helvetica',10);pdf.drawString(24,793,'QOLOSSAL  /  TERRAIN REVISION')
        pdf.setFillColor(FG);pdf.setFont('Helvetica-Bold',23);pdf.drawString(24,757,title)
        if subtitle:para(subtitle,24,739,size=12)
        pdf.setFillColor(MUTED);pdf.setFont('Helvetica',9);pdf.drawString(24,20,'Mobile review  |  Native source files remain unchanged');pdf.drawRightString(516,20,str(page))
        if bookmark:pdf.bookmarkPage(bookmark);pdf.addOutlineEntry(title,bookmark,level=0)
        if page>1:pdf.linkRect('Contents','cover',(24,10,350,32),relative=0,thickness=0)
    def picture(path,x,y,width=492,height=276.75):
        nonlocal images
        im=Image.open(path).convert('RGB');im.thumbnail((1920,1080),Image.Resampling.LANCZOS)
        image_stream=BytesIO();im.save(image_stream,format='JPEG',quality=87,optimize=True)
        pdf.drawImage(ImageReader(image_stream),x,y,width=width,height=height,preserveAspectRatio=True,anchor='c',mask='auto');images+=1;embedded.add(str(path))
    def panel(path,label,source,top):
        para(escape(label),24,top,size=13,color=GOLD);para(escape(source),24,top-21,size=10)
        picture(path,24,top-314)
    def link(text,dest,x,y,width=492,size=13):
        para(escape(text),x,y,width,size,FG);pdf.linkRect('',dest,(x,y-20,x+width,y+3),relative=0,thickness=0)
    def table(headers,rows,y,widths,size=11,row_h=31):
        x=24
        for title,w in zip(headers,widths):para(escape(str(title)),x,y,w-7,size,GOLD);x+=w
        y-=row_h
        for row in rows:
            x=24
            for text,w in zip(row,widths):para(escape(str(text)),x,y,w-7,size,FG);x+=w
            y-=row_h
        return y
    base('Terrain and cave revision','Seven surface regions, restored painted cave depth and connected distant scenery.','cover')
    para('The Titan anatomy remains the world-scale route. Local slopes and corners are refined while cave mouths attach to real terrain.',24,693,size=15,color=FG)
    picture(HERE/'Camera/MR06_idle_4K.png',24,350)
    para('Tap a region to open its page',24,342,size=14,color=GOLD)
    for i,(rid,name,*_) in enumerate(REGIONS):link(f'{name}  -  page {2+i}',rid,24,314-i*27)
    link('Cave themes  -  page 10','caves',24,115,240,12);link('Jump camera  -  page 16','jump',280,115,236,12)
    link('4K evidence  -  page 17','resolution',24,87,240,12);link('QA and limits  -  page 18','limits',280,87,236,12)
    pdf.showPage()
    for rid,name,theme,body,_ in REGIONS:
        base(name,theme+' Anatomy: '+body+'.',rid)
        panel(HERE/f'Camera/{rid}_idle_4K.png','View in the running game','Runtime capture - native 3840 x 2160',687)
        panel(HERE/f'Captures/{rid}_cave_{cfg["surface_caves"][rid]}_4K.png','Painted cave and terrain joins','Saved-scene Editor render - native 3840 x 2160',366)
        pdf.showPage()
    base('Two entrances, real cliffs','Original connection IDs and destinations are retained.','relocations')
    panel(HERE/'Captures/MR02_cave_PC04_4K.png','Causeway: PC04 at the natural wall','Editor render - artificial mound removed; entrance mirrored inward',687)
    panel(HERE/'Captures/MR04_cave_A01_4K.png','Qvale: A01 at the western mountainside','Editor render - entrance moved from town; slope meets the approach',366)
    pdf.showPage()
    for index in range(0,len(THEMES),2):
        base('Underground depth','Two of twelve theme families; shared painting pieces form each room view.','caves' if index==0 else 'caves'+str(index//2))
        for j,(key,title,_) in enumerate(THEMES[index:index+2]):
            room=cfg['theme_rooms'][key];panel(HERE/f'CaveVista/Play/{room}_idle_4K.png',title,'Runtime checkpoint view - native 3840 x 2160',687-j*321)
        pdf.showPage()
    base('Jump camera in play','Real player movement and launch path; not a full traversal test.','jump')
    for j,item in enumerate(cfg['jump_panels']):panel(local(item['file']),item['label'],'Runtime apex capture - native 3840 x 2160',687-j*321)
    pdf.showPage()
    base('Resolution at a 4K view','2160 screen pixels / 10 world units = 216 pixels per world unit.','resolution')
    rows=[('Far surface scene','2172 x724','10 x3.333','2160 x720'),
          ('Top, maximum1:1grade','2172 x724','6.033 x2.011','2109 x703'),
          ('Native surface Fill','1254 x1254','3.483 x3.483','753 x753'),
          ('Causeway UG Fill','1254 x1254','4 x4','864 x864'),
          ('Cave center / vault','1774 x887','8.213 x4.106','1774 x887'),
          ('Cave side reach','1024 x1536','4.741 x7.111','1024 x1536'),
          ('Cave outer field','1254 x1254','5.806 x5.806','1254 x1254')]
    y=table(['New source role','Native pixels','World footprint','Required pixels'],rows,687,[151,112,112,117],10.5,40)
    y=para('The top calculation includes worst-case shear on a 1:1 slope: 360 / 1.618 = 222.49 native PPU. Source dimensions are measured, not upscaled. Cave scenery uses several pieces; no single bitmap spans the whole view.',24,y-6,size=12)-17
    y=para(f'Final audit: {density.get("assetCount",len(density.get("assets",[])))} assets / {density.get("placements","unreported")} placements. {len(bad)} source-level exceptions remain in the supplied report. This is not a universal native-4K pass.',24,y,size=12,color=FG)-12
    for title,items in exception_groups(bad).items():
        minimum=min(float(r.get('minimumPixelsPerUnit',r.get('nativeMinimumPPU',0))) for r in items)
        y=para(f'{escape(title)}: {len(items)} sources; minimum {minimum:.3f} PPU.',24,y,size=11)-4
    y=para('Full per-asset CSV and JSON are attached to this PDF. Six retained painting placements are itemized on the next page. The separate procedural roof mask also passes its measured requirement.',24,y-6,size=11)-6
    para('Open the full per-asset CSV companion',24,y,size=11,color=GOLD)
    pdf.linkURL(cfg.get('full_csv_url') or local(cfg['density_csv']).as_uri(),(24,y-16,350,y+3),relative=0,thickness=0)
    pdf.showPage()
    base('Checks and retained limits','Approved art is preserved; exceptions remain visible in the report.','limits')
    y=para('Six approved painting placements remain at their original footprint below native 216 PPU. Their appearance is preserved in this revision.',24,687,size=12,color=FG)-12
    y=table(['Approved placement','Native PPU','Required whole source'],[(n,f'{ppu:.3f}',req) for n,ppu,req in LEGACY],y,[180,100,212],11,27)
    y=para('Sources: Causeway_Masonry (2172 x724), Cradle Rockwork (1605 x980), and ReturnRim (1448 x1086). No source pixels were enlarged to manufacture a pass.',24,y-4,size=11)-15
    for check in qa_checks:
        name=check.get('name',check.get('label','QA check'));status='PASS' if check.get('passed') is True else 'NOT VERIFIED' if check.get('passed') is None else 'FAIL'
        detail=check.get('summary',check.get('scope','Recorded in final QA receipt.'))
        y=para(f'<b>{escape(str(name))}: {status}.</b> {escape(str(detail))}',24,y,size=11)-8
    para('Scope: Windows Unity Editor captures and bounded fixtures. Full-game traversal, physical controllers, console builds and hardware performance are unverified. No 60 FPS or console-readiness claim; Xbox Series S and Switch 2 handheld/docked still need hardware validation. PDF images are compressed previews; native captures remain unchanged.',24,y-4,size=10.5)
    pdf.showPage();pdf.save()
    reader=PdfReader(BytesIO(stream.getvalue()));assert len(reader.pages)==18
    assert all(p.extract_text().strip() for p in reader.pages)
    writer=PdfWriter();writer.clone_document_from_reader(reader)
    for filename,path in [('Qolossal_Terrain_Resolution.csv',local(cfg['density_csv'])),('Qolossal_Terrain_Resolution.json',local(cfg['density_file'])),('Qolossal_Final_QA.json',local(cfg['qa_file']))]:writer.add_attachment(filename,path.read_bytes())
    out.parent.mkdir(parents=True,exist_ok=True)
    with out.open('wb') as f:writer.write(f)
    checked=PdfReader(str(out));assert len(checked.pages)==18 and len(checked.attachments)==3
    return {'path':str(out),'pages':18,'embeddedImagePanels':images,'distinctImageFiles':len(embedded),'densityExceptions':len(bad),'megabytes':round(out.stat().st_size/1048576,2),'renderVerificationRequired':True}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config',type=Path);parser.add_argument('--output',type=Path,default=DEFAULT_OUT)
    mode=parser.add_mutually_exclusive_group(required=True);mode.add_argument('--check',action='store_true');mode.add_argument('--print-config',action='store_true');mode.add_argument('--build',action='store_true')
    args=parser.parse_args();cfg=config(args.config)
    if args.print_config:print(json.dumps(cfg,indent=2));return
    result=preflight(cfg)
    if args.check:print(json.dumps(result,indent=2));return
    if not result['ready']:raise SystemExit('Final PDF is gated:\n'+'\n'.join(result['errors']))
    print(json.dumps(build(cfg,args.output),indent=2))

if __name__=='__main__':main()

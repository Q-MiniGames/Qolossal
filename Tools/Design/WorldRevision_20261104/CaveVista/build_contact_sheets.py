"""Create labeled review sheets from 4K captures; original evidence stays unchanged.
Usage: python build_contact_sheets.py <capture_directory> [--glob *.png] [--columns 3]
Sheets contain at most12 thumbnails and a manifest linking exact SHA256 originals.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import argparse, hashlib, json, math
p=argparse.ArgumentParser();p.add_argument('directory',type=Path);p.add_argument('--glob',default='*.png');p.add_argument('--columns',type=int,default=3)
a=p.parse_args();files=sorted(f for f in a.directory.glob(a.glob) if not f.name.startswith('contact_'))
out=a.directory/'ReviewSheets';out.mkdir(exist_ok=True)
records=[]
for start in range(0,len(files),12):
    group=files[start:start+12];w,h=640,360;label=30
    sheet=Image.new('RGB',(a.columns*w,math.ceil(len(group)/a.columns)*(h+label)),(22,23,26));draw=ImageDraw.Draw(sheet)
    for i,f in enumerate(group):
        with Image.open(f) as im:
            sourceSize=im.size;preview=im.convert('RGB');preview.thumbnail((w,h),Image.Resampling.LANCZOS)
        x=(i%a.columns)*w;y=(i//a.columns)*(h+label)
        sheet.paste(preview,(x,y+label));draw.text((x+8,y+8),f.stem,fill='white')
        records.append(dict(file=str(f.resolve()),width=sourceSize[0],height=sourceSize[1],sha256=hashlib.sha256(f.read_bytes()).hexdigest(),sheet=f'contact_{start//12+1:02}.jpg'))
    sheet.save(out/f'contact_{start//12+1:02}.jpg',quality=94)
(out/'manifest.json').write_text(json.dumps(dict(captures=len(files),sheets=math.ceil(len(files)/12),originalsUnmodified=True,rows=records),indent=2),encoding='utf-8')
print(json.dumps(dict(captures=len(files),sheets=math.ceil(len(files)/12),output=str(out))))

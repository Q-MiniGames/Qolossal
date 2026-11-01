"""Fetches Ludo sprite sheets (6 x 6 webp, the same frames as "Download All Frames") and cuts them into frames
under Tools/IncomingArt/Ludo/Underground/<clip folder>/frames/frame_NNN.png. Used when the browser blocks downloads.
Not art: download and slicing. Clips are matched to LUDO_PROMPTS_UNDERGROUND_ENEMIES.md by the start of the prompt.
Usage: python sheets_underground.py "<prompt start>|<sheet hash>" ...
"""
import os, sys, urllib.request, io
from PIL import Image
from fetch_underground import clips, OUT

table = clips()
for arg in sys.argv[1:]:
    start, sheet = arg.split('|')
    match = [(cid, f) for cid, (f, p) in table.items() if p.startswith(start.strip())]
    if len(match) != 1: print('no single clip for', start); continue
    cid, folder = match[0]
    url = 'https://storage.googleapis.com/ludo-assets/%s.webp' % sheet.strip()
    data = urllib.request.urlopen(url, timeout=60).read()
    im = Image.open(io.BytesIO(data)).convert('RGBA')
    w, h = im.size[0] // 6, im.size[1] // 6
    target = os.path.join(OUT, folder, 'frames'); os.makedirs(target, exist_ok=True)
    for i in range(36):
        im.crop(((i % 6) * w, (i // 6) * h, (i % 6 + 1) * w, (i // 6 + 1) * h)).save(os.path.join(target, 'frame_%03d.png' % i))
    open(os.path.join(OUT, folder, 'sheet_url.txt'), 'w').write(url + '\n')
    print(cid, folder, '%dx%d frames' % (w, h))

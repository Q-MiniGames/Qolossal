"""Fetches Ludo's 6 x 6 sprite sheets of the residents' idles into Tools/IncomingArt/Ludo/Residents/<Rig>/frames, and
reports how far each clip drifts from its first frame, which is how much Ludo redrew the approved figure.
Usage: python sheets_residents.py Rig=<sheet hash> ...
"""
import io, os, sys, urllib.request
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '..', '..', 'IncomingArt', 'Ludo', 'Residents'))

for arg in sys.argv[1:]:
    rig, key = arg.split('=')
    url = 'https://storage.googleapis.com/ludo-assets/%s.webp' % key
    sheet = Image.open(io.BytesIO(urllib.request.urlopen(url, timeout=60).read())).convert('RGBA')
    w, h = sheet.size[0] // 6, sheet.size[1] // 6
    target = os.path.join(OUT, rig, 'frames'); os.makedirs(target, exist_ok=True)
    open(os.path.join(OUT, rig, 'sheet_url.txt'), 'w').write(url + '\n')
    frames = []
    for i in range(36):
        f = sheet.crop(((i % 6) * w, (i // 6) * h, (i % 6 + 1) * w, (i // 6 + 1) * h))
        f.save(os.path.join(target, 'frame_%03d.png' % i))
        frames.append(np.asarray(f, dtype=np.float32))
    stack = np.stack(frames)
    solid = stack[0][:, :, 3] > 128
    drift = [float(np.abs(stack[i][:, :, :3] - stack[0][:, :, :3]).mean(axis=2)[solid].mean()) for i in range(36)]
    print('%s: %dx%d; change from first frame: mean %.1f, worst %.1f of 255 (frame %d); last vs first %.1f' % (rig, w, h, np.mean(drift), max(drift), int(np.argmax(drift)), drift[-1]))
    strip = Image.new('RGB', (w * 9 // 2, h // 2), (80, 90, 105))
    for k, i in enumerate((0, 4, 8, 12, 16, 20, 24, 28, 35)):
        im = Image.fromarray(frames[i].astype(np.uint8), 'RGBA').resize((w // 2, h // 2), Image.LANCZOS)
        strip.paste(im, (k * (w // 2), 0), im)
    strip.save(os.path.join(OUT, rig + '_strip.jpg'), quality=88)

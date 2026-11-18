"""Fetches Ludo's sprite sheets for the ambient clips (6 x 6 webp, transparent) and the butterfly's still, and cuts the
sheets into frames under Tools/IncomingArt/Ludo/Ambient/<name>/frames. Not art: download and slicing.
"""
import io, os, urllib.request
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '..', '..', 'IncomingArt', 'Ludo', 'Ambient'))
BASE = 'https://storage.googleapis.com/ludo-assets/%s.webp'
SHEETS = {'Splash': '68be58009814b01d09bdd48ea4881d18', 'Bird': 'c7041c775e24dfbfa3d702e585e2462d'}
STILLS = {'Butterfly_Still': '45d08d58535c20cc5f149823a21ff574'}


def fetch(key):
    return Image.open(io.BytesIO(urllib.request.urlopen(BASE % key, timeout=60).read())).convert('RGBA')


for name, key in SHEETS.items():
    sheet = fetch(key)
    w, h = sheet.size[0] // 6, sheet.size[1] // 6
    target = os.path.join(OUT, name, 'frames'); os.makedirs(target, exist_ok=True)
    for i in range(36):
        sheet.crop(((i % 6) * w, (i // 6) * h, (i % 6 + 1) * w, (i // 6 + 1) * h)).save(os.path.join(target, 'frame_%03d.png' % i))
    print(name, sheet.size, '%dx%d' % (w, h))
for name, key in STILLS.items():
    fetch(key).save(os.path.join(OUT, name + '.png'))
    print(name)

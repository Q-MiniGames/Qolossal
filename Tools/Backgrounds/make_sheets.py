"""Labelled contact sheets of the background-audit captures (Tools/Backgrounds/proposal/current_*.jpg).
  python Tools/Backgrounds/make_sheets.py <capture folder>"""
import glob, os, sys
from PIL import Image, ImageDraw, ImageFont

SRC = sys.argv[1] if len(sys.argv) > 1 else r"C:\_temp\qp_cap\bg_audit"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "proposal")
os.makedirs(OUT, exist_ok=True)
ORDER = ["low", "mid", "high", "climb", "west_edge", "east_edge", "vista0", "vista1", "overlook_seated", "encounter"]
try:
    FONT = ImageFont.truetype("arial.ttf", 22); SMALL = ImageFont.truetype("arial.ttf", 16)
except OSError:
    FONT = SMALL = ImageFont.load_default()


def label(im, text, sub=""):
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, im.width, 34], fill=(20, 18, 16))
    d.text((8, 5), text, fill=(255, 240, 210), font=FONT)
    if sub:
        d.text((im.width - d.textlength(sub, font=SMALL) - 8, 9), sub, fill=(200, 200, 190), font=SMALL)
    return im


def sheet(region, kind):
    W = 640
    tiles = []
    for spot in ORDER:
        path = os.path.join(SRC, f"{region}_{spot}_{kind}.png")
        if not os.path.exists(path):
            continue
        im = Image.open(path).convert("RGB").resize((W, 360), Image.LANCZOS)
        tiles.append(label(im, f"{region} {spot}", "game view" if kind == "1080" else "background layers only"))
    if not tiles:
        return None
    cols = 3; rows = (len(tiles) + cols - 1) // cols
    s = Image.new("RGB", (W * cols + 8 * (cols - 1), 360 * rows + 8 * (rows - 1)), (245, 242, 235))
    for i, t in enumerate(tiles):
        s.paste(t, ((i % cols) * (W + 8), (i // cols) * (360 + 8)))
    name = os.path.join(OUT, f"current_{region}_{'game' if kind == '1080' else 'bgonly'}.jpg")
    s.save(name, quality=86)
    return name


regions = sorted({os.path.basename(p).split("_")[0] for p in glob.glob(os.path.join(SRC, "MR0*_*.png"))})
for r in regions:
    for k in ("1080", "bgonly_1080"):
        print(sheet(r, k))

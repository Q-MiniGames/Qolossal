# Puts Qori's feet back into the Ludo seated frames (Assets/ListeningRetreat/Art/LudoSeated/Retreat_QoriSeated_001..108).
# The bench was erased from Ludo's bench clips by colour, and the brown feet went with it (the user, 18 Oct:
# "Qori's feet should appear"). For each frame, the matching source frame (redo2, redo3, redo4: 36 frames each) is
# registered onto the seated frame by the ears and head (scale and offset from the figure's top part), and the
# brown pixels below Ludo's bench seat, between its legs, are copied in. Everything else in the frame is kept.
#   python restore_seated_feet.py [--check]      --check writes a contact sheet and changes nothing
# The originals are backed up once to Tools/ListeningRetreat/QA/LudoSeated_before_feet/.
import os, shutil, sys, json
import numpy as np
from scipy import ndimage
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(os.path.dirname(HERE))
SEATED = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", "LudoSeated")
SRC = os.path.join(ROOT, "Tools", "IncomingArt", "Ludo", "Qori")
CLIPS = ["Ludo-Qori-Bench (redo2)", "Ludo-Qori-Bench (redo3)", "Ludo-Qori-Bench (redo4)"]
BACKUP = os.path.join(HERE, "QA", "LudoSeated_before_feet")
check = "--check" in sys.argv
PAD = 70

def brown(a):
    r, g, b, al = [a[:, :, i].astype(int) for i in range(4)]
    return (al > 128) & (r - g > 25) & (g - b > 8) & (r < 190)

def bench_of(a):
    """Ludo's bench in a source frame: the seat's lowest row and the inner edges of its two legs."""
    br = brown(a); h, w = br.shape
    rows = br.sum(axis=1)
    seat = [y for y in range(h // 2, h) if rows[y] > .6 * (br.any(axis=0).sum())]
    seat_bottom = max(seat) if seat else int(h * .82)
    cols = br[h - 12:h - 2].any(axis=0)             # near the frame bottom only the legs remain
    xs = np.where(cols)[0]; mid = w // 2
    left = xs[xs < mid].max() if (xs < mid).any() else 0
    right = xs[xs > mid].min() if (xs > mid).any() else w - 1
    return seat_bottom, left, right

def top_box(al, frac=.42):
    """Bounding box of the figure's top part (ears and head), which the bench never touched."""
    ys = np.where((al > 128).any(axis=1))[0]; y0 = ys.min()
    y1 = y0 + int(frac * (ys.max() - y0))
    xs = np.where((al[y0:y1] > 128).any(axis=0))[0]
    return y0, y1, xs.min(), xs.max()

def fit(src, dst):
    """Scale and offset taking source pixels to seated-frame pixels, from the ears' width and the figure's top."""
    sy0, sy1, sx0, sx1 = top_box(src[:, :, 3]); dy0, dy1, dx0, dx1 = top_box(dst[:, :, 3])
    s = (dx1 - dx0) / max(1, (sx1 - sx0))
    return s, dx0 - sx0 * s, dy0 - sy0 * s

files = sorted(f for f in os.listdir(SEATED) if f.startswith("Retreat_QoriSeated_") and f.endswith(".png"))
assert len(files) == 108, len(files)
if not check and not os.path.isdir(BACKUP):
    os.makedirs(BACKUP); [shutil.copy2(os.path.join(SEATED, f), BACKUP) for f in files]
report, sheet = [], []
for k, f in enumerate(files):
    clip, i = CLIPS[k // 36], k % 36
    src = np.asarray(Image.open(os.path.join(SRC, clip, "frame_%03d.png" % i)).convert("RGBA"))
    base = os.path.join(BACKUP if os.path.isdir(BACKUP) else SEATED, f)
    dst = np.asarray(Image.open(base).convert("RGBA")).copy()
    # The feet hang below the old 970 px canvas: add PAD transparent rows at the bottom (the top-left origin and
    # every figure row stay put; Retreat_QoriSeated_info.json gets the new canvas height).
    dst = np.concatenate([dst, np.zeros((PAD, dst.shape[1], 4), dst.dtype)], axis=0)
    # One transform per clip, from its first frame: Ludo's camera is still within a clip, but the ears lift in the
    # breeze, which would throw a per-frame fit.
    if i == 0: clip_fit = fit(src, dst)
    s, ox, oy = clip_fit
    seat_bottom, left, right = bench_of(src)
    feet = brown(src); feet[:seat_bottom + 1] = False; feet[:, :left + 3] = False; feet[:, right - 2:] = False
    # Keep only the two feet: sizeable pieces near the bench's middle, not slivers of the bench legs.
    lab, n = ndimage.label(feet); mid = (left + right) / 2   # between the bench legs (the head turns; the feet stay)
    keep = np.zeros(n + 1, bool)
    for j, sl in enumerate(ndimage.find_objects(lab), 1):
        ys, xs = sl; cx = (xs.start + xs.stop) / 2; area = (lab[sl] == j).sum()
        keep[j] = area > 150 and abs(cx - mid) < .22 * (right - left) and (xs.stop - xs.start) > 12
    feet = keep[lab]
    piece = np.where(feet[:, :, None], src, 0).astype(np.uint8)
    piece = Image.fromarray(piece, "RGBA")
    piece = piece.resize((round(piece.width * s), round(piece.height * s)), Image.LANCZOS)
    out = Image.fromarray(dst, "RGBA")
    layer = Image.new("RGBA", out.size); layer.paste(piece, (round(ox), round(oy)))
    feet_px = int((np.asarray(layer)[:, :, 3] > 128).sum())
    out = Image.alpha_composite(layer, out)            # the cloak stays in front of the shins
    report.append({"frame": f, "clip": clip, "source": i, "scale": round(s, 4), "offset": [round(ox, 1), round(oy, 1)], "feetPx": feet_px})
    if k % 9 == 0 or k in (19, 23): sheet.append(out.crop((400, 600, 950, 970 + PAD)))   # the lower body, at 100%
    if not check: out.save(os.path.join(SEATED, f))
# Contact sheet of the lower bodies, five to a row, on sky blue.
w, h = sheet[0].size; cols = 5; rows = (len(sheet) + cols - 1) // cols
cs = Image.new("RGBA", (w * cols, h * rows), (120, 150, 200, 255))
for n, im in enumerate(sheet): cs.alpha_composite(im, ((n % cols) * w, (n // cols) * h))
os.makedirs(os.path.join(HERE, "QA"), exist_ok=True)
cs.convert("RGB").resize((cs.width // 2, cs.height // 2)).save(os.path.join(HERE, "QA", "LudoSeated_feet_sheet.jpg"), quality=88)
json.dump(report, open(os.path.join(HERE, "QA", "LudoSeated_feet_report.json"), "w"), indent=1)
if not check:
    ip = os.path.join(SEATED, "Retreat_QoriSeated_info.json"); info = json.load(open(ip))
    info["canvas"] = [info["canvas"][0], 970 + PAD]; info["feet"] = "restored by Tools/ListeningRetreat/restore_seated_feet.py (18 Oct)"
    json.dump(info, open(ip, "w", newline="\n"), indent=1)
fp = [r["feetPx"] for r in report]
print("frames", len(report), "feet px min/max", min(fp), max(fp), "scales", sorted({r["scale"] for r in report})[:3], "...", "(check only)" if check else "(written)")

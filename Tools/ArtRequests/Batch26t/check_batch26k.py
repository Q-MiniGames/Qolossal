"""Checks Batch 26k's head and blink (the face at 45 degrees) against Batch 26j.

  python check_batch26k.py <Batch26j scene folder> <new scene folder>

Only Qori_Head and Qori_Blink change. The face must read as a face, not an eyeball: a cream face that
is clearly bigger than the eye, an eye of the rig's size (dark, no white ring) inside it, and the
blink covering just the eye. Exit code: the number of failures.
"""
import glob, hashlib, json, os, sys
import numpy as np
from PIL import Image

OLD, NEW = sys.argv[1], sys.argv[2]
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest() if os.path.exists(p) else None

name = os.path.basename(glob.glob(os.path.join(OLD, "Retreat_*_Registration.json"))[0])
pre = name.replace("Registration.json", "")
test(sha(os.path.join(OLD, name)) == sha(os.path.join(NEW, name)), "registration unchanged")
for L in json.load(open(os.path.join(OLD, name)))["layers"]:
    if L["name"] in ("Qori_Head", "Qori_Blink"): continue
    test(sha(os.path.join(OLD, L["file"])) == sha(os.path.join(NEW, L["file"])), f"{L['name']} unchanged")

h = np.array(Image.open(os.path.join(NEW, "Layers", pre + "Qori_Head.png")).convert("RGBA")).astype(int)
ho = np.array(Image.open(os.path.join(OLD, "Layers", pre + "Qori_Head.png")).convert("RGBA"))[:, :, 3] > 128
b = np.array(Image.open(os.path.join(NEW, "Layers", pre + "Qori_Blink.png")).convert("RGBA")).astype(int)
a = h[:, :, 3] > 128
iou = (a & ho).sum() / max(1, (a | ho).sum())
test(iou >= .95, f"head outline kept (IoU {iou:.3f})")
ys, _ = np.nonzero(a); head_h = ys.max() - ys.min()
mx, mn = h[:, :, :3].max(axis=2), h[:, :, :3].min(axis=2)
eye = (b[:, :, 3] > 128) & a                              # the blink marks where the eye is
cream = a & (mx > 185) & ((mx - mn) < 70) & ~eye          # the face around it
ey, ex = np.nonzero(eye)
eye_h = (ey.max() - ey.min() + 1) if len(ey) else 0
test(.18 <= eye_h / head_h <= .30, f"eye about the rig's size: {eye_h} px tall, {eye_h / head_h:.2f} of the head (0.18-0.30; the rig's is 0.23, 26j's 0.35)")
test(cream.sum() >= 2.5 * eye.sum(), f"a face around the eye: cream {cream.sum()} px, at least 2.5x the eye's {eye.sum()} px")
under = h[eye]
test(len(under) > 0 and (under[:, :3].max(axis=1) < 120).mean() >= .7, f"the eye is dark, like the rig's, with no white ring ({(under[:, :3].max(axis=1) < 120).mean() if len(under) else 0:.0%} of it dark, at least 70%)")
if len(ey):
    ring = np.zeros_like(a); y0, y1, x0, x1 = ey.min(), ey.max(), ex.min(), ex.max()
    ring[max(0, y0 - 4):y1 + 5, max(0, x0 - 4):x1 + 5] = True; ring &= ~eye & a
    test((cream & ring).sum() >= .6 * ring.sum(), f"cream face on every side of the eye ({(cream & ring).sum()} of {ring.sum()} px round it)")
lid = b[eye]
test(len(lid) > 0 and lid[:, :3].max(axis=1).mean() >= 150, "the blink is a cream lid")
print(f"{fails} failed"); sys.exit(fails)

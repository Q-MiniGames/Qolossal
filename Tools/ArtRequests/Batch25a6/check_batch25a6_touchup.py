"""Extra checks for the Batch 25a-6 touch-up, run after check_batch25a5.py passes.

  python check_batch25a6_touchup.py <Batch25a5 Layers folder> <Batch25a6 Layers folder>

Tests: only BenchFront and Qori_Head changed; the backrest's top edge is waved (it rises and falls
by at least 15 px along its length after removing the straight perspective slope); the cheek is a
softer, wider curve (cream 9-16% of the head, at least 26 px wide at its widest row).
"""
import hashlib, os, sys
import numpy as np
from PIL import Image

OLD, NEW = sys.argv[1], sys.argv[2]
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()

for f in sorted(os.listdir(OLD)):
    if not f.endswith(".png"): continue
    same = os.path.exists(os.path.join(NEW, f)) and sha(os.path.join(OLD, f)) == sha(os.path.join(NEW, f))
    changed_ok = f.endswith(("BenchFront.png", "Qori_Head.png"))
    test(same or changed_ok, f"{f} {'unchanged' if same else 'changed'}" + ("" if same or changed_ok else " (must be byte-identical)"))

# The backrest's top edge: the highest opaque row per column across the backrest's middle 80%.
a = np.array(Image.open(os.path.join(NEW, "Retreat_OverlookTerrace_BenchFront.png")))[:, :, 3] > 128
cols = np.where(a.any(axis=0))[0]
x0, x1 = cols.min(), cols.max()
xs = np.arange(int(x0 + .1 * (x1 - x0)), int(x1 - .1 * (x1 - x0)))
top = np.array([np.argmax(a[:, x]) for x in xs], float)
# Leave out the posts (much higher than the plank) and remove the straight slope.
keep = top > np.percentile(top, 25)
fit = np.polyfit(xs[keep], top[keep], 1)
resid = top[keep] - np.polyval(fit, xs[keep])
amp = np.percentile(resid, 95) - np.percentile(resid, 5)
test(amp >= 15, f"backrest top edge is waved ({amp:.0f} px of rise and fall along it, >= 15; the straight 25a-5 plank measures 7)")

h = np.array(Image.open(os.path.join(NEW, "Retreat_OverlookTerrace_Qori_Head.png")).convert("RGBA")).astype(int)
ha = h[:, :, 3] > 128
mx, mn = h[:, :, :3].max(axis=2), h[:, :, :3].min(axis=2)
cream = ha & (mx > 195) & ((mx - mn) < 60)
share = cream.sum() / ha.sum()
test(.09 <= share <= .16, f"cheek is 9-16% of the head ({share:.0%})")
widest = cream.sum(axis=1).max()
test(widest >= 26, f"cheek at least 26 px wide at its widest ({widest} px; 25a-5's crescent is 20)")
print(f"{fails} failed"); sys.exit(fails)

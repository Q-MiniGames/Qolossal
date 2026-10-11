"""Checks Codex's Batch 25a-7 (from 25a-4/25a-5, plus ear widths) layers against the projected bench blockout and the Qori pose guide.

  python check_batch25a7.py <input folder> <delivery Layers folder>

The Layers folder holds Retreat_OverlookTerrace_BenchBack/BenchFront/Qori_Body/Qori_Head/Qori_EarFar/
Qori_EarNear.png. Every test prints PASS or FAIL; the exit code is the number of failures.
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw

IN, LAY = sys.argv[1], sys.argv[2]
P = json.load(open(os.path.join(IN, "Placement.json")))
W, H = P["canvas"]
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1

def load(name):
    path = os.path.join(LAY, "Retreat_OverlookTerrace_" + name + ".png")
    if not os.path.exists(path):
        test(False, f"{name} delivered"); return np.zeros((H, W, 4), int)
    im = Image.open(path)
    test(im.size == (W, H) and im.mode == "RGBA", f"{name} is {W}x{H} RGBA (got {im.size} {im.mode})")
    return np.array(im.convert("RGBA")).astype(int)
def guide(name): return np.array(Image.open(os.path.join(IN, name + ".png")))[:, :, 3] > 0
def iou(a, b): return (a & b).sum() / max(1, (a | b).sum())
def centroid(m):
    ys, xs = np.nonzero(m); return (xs.mean(), ys.mean()) if len(xs) else (-1e9, -1e9)

back, front = load("BenchBack"), load("BenchFront")
body, head, earf, earn = load("Qori_Body"), load("Qori_Head"), load("Qori_EarFar"), load("Qori_EarNear")
A = lambda a: a[:, :, 3] > 128

# The bench follows the projected blockout.
gb, gf = guide("Bench_Blockout_Back"), guide("Bench_Blockout_Front")
ua, ub = A(back) | A(front), gb | gf
test(iou(ua, ub) >= .75, f"bench silhouette follows the blockout (IoU {iou(ua, ub):.2f} >= 0.75)")
test(iou(A(front), gf) >= .6, f"backrest and back posts (BenchFront) follow their blockout (IoU {iou(A(front), gf):.2f} >= 0.60)")
test(iou(A(back), gb) >= .6, f"seat and front legs (BenchBack) follow their blockout (IoU {iou(A(back), gb):.2f} >= 0.60)")
inside = (ua & ub).sum() / max(1, ua.sum())
test(inside >= .8, f"at least 80% of the painted bench lies inside the blockout ({inside:.0%})")
# The seat is painted solid under Qori.
m = Image.new("L", (W, H), 0); ImageDraw.Draw(m).polygon([tuple(p) for p in P["seatTopUnderQori"]], fill=255)
seat = np.array(m) > 0
cov = (back[:, :, 3][seat] >= 200).mean()
test(cov >= .9, f"seat solid under Qori in BenchBack ({cov:.0%} of the seat patch)")
# Feet on the paving.
def lowest_near(a, x, y):
    x0, x1 = int(x) - 45, int(x) + 45
    rows = np.where((a[:, x0:x1, 3] > 128).any(axis=1))[0]
    rows = rows[rows < y + 40]
    return rows.max() if len(rows) else -1
for (x, y), layer, what in [(P["groundUnderBackPosts"][0], front, "near back post"), (P["groundUnderBackPosts"][1], front, "far back post"),
                            (P["groundUnderFrontLegs"][0], back, "near front leg"), (P["groundUnderFrontLegs"][1], back, "far front leg")]:
    lo = lowest_near(layer, x, y)
    test(abs(lo - y) <= 12, f"{what}'s root feet on the paving at y {y:.0f} (lowest {lo})")

# Qori follows the pose guide.
gq = guide("Qori_Guide_Body") | guide("Qori_Guide_Head") | guide("Qori_Guide_EarNear") | guide("Qori_Guide_EarFar")
uq = A(body) | A(head) | A(earf) | A(earn)
visible = lambda mask: mask & ~A(front)
test(iou(visible(uq), visible(gq)) >= .5, f"Qori's visible silhouette follows the guide (IoU {iou(visible(uq), visible(gq)):.2f} >= 0.50)")
hc, gc = centroid(A(head)), centroid(guide("Qori_Guide_Head"))
test(np.hypot(hc[0] - gc[0], hc[1] - gc[1]) <= 25, f"head centred on the guide ({hc[0]:.0f},{hc[1]:.0f} vs {gc[0]:.0f},{gc[1]:.0f})")
def height(mask): ys = np.nonzero(mask)[0]; return ys.max() - ys.min() if len(ys) else 0
hr = height(A(head)) / max(1, height(guide("Qori_Guide_Head")))
test(.8 <= hr <= 1.25, f"head size matches the rig ({hr:.2f} of the guide height)")
# Lost profile: some cream cheek on the river side, mostly leaf crest.
rgb = head[:, :, :3]; mx, mn = rgb.max(axis=2), rgb.min(axis=2)
cream = A(head) & (mx > 195) & ((mx - mn) < 60)
share = cream.sum() / max(1, A(head).sum())
test(.03 <= share <= .30, f"a lost profile: cream cheek is 3-30% of the head ({share:.0%})")
cc = centroid(cream)
test(cc[0] > hc[0] + 5, f"the cheek is on the river (right) side of the head (cheek x {cc[0]:.0f}, head x {hc[0]:.0f})")
test((A(head) & A(front)).sum() <= .05 * A(head).sum(), "the backrest does not cover his head")
for name, ear in (("EarNear", earn), ("EarFar", earf)):
    ec = centroid(A(ear)); tip = P["earTips"][name]
    ys, xs = np.nonzero(A(ear))
    far_pt = (xs[np.argmin(ys)], ys.min()) if len(ys) else (-1e9, -1e9)
    test(ec[1] < hc[1] and ec[0] < hc[0] + 10, f"{name} rises from the head and sweeps back toward the camera (centroid {ec[0]:.0f},{ec[1]:.0f})")
    test(np.hypot(far_pt[0] - tip[0], far_pt[1] - tip[1]) <= 70, f"{name} tip near the guide's ({far_pt[0]},{far_pt[1]} vs {tip[0]:.0f},{tip[1]:.0f})")

# Both ears full leaves, not slivers: mean width (area / length) at least 80% of the guide's, and the
# two ears within 30% of each other.
def mean_width(mask):
    ys, xs = np.nonzero(mask)
    if len(xs) < 10: return 0.0
    pts = np.stack([xs, ys], 1).astype(float); pts -= pts.mean(0)
    axis = np.linalg.svd(pts, full_matrices=False)[2][0]
    t = pts @ axis
    return mask.sum() / max(1.0, t.max() - t.min())
wn, wf = mean_width(A(earn)), mean_width(A(earf))
gn, gf2 = mean_width(guide("Qori_Guide_EarNear")), mean_width(guide("Qori_Guide_EarFar"))
test(wn >= .8 * gn, f"EarNear is a full leaf (mean width {wn:.1f} px, guide {gn:.1f})")
test(wf >= .8 * gf2, f"EarFar is a full leaf, not a sliver (mean width {wf:.1f} px, guide {gf2:.1f})")
test(min(wn, wf) >= .7 * max(wn, wf), f"the ears match each other in width ({wf:.1f} vs {wn:.1f} px)")

# UI zones and edges.
allm = ua | uq
test(allm[:, 1920:].sum() == 0 and allm[1296:, :].sum() == 0, "nothing in the right quarter or bottom 10%")
for name, a in (("BenchBack", back), ("BenchFront", front), ("Qori_Body", body), ("Qori_Head", head)):
    edge = (a[:, :, 3] > 0) & (a[:, :, 3] < 64)
    dark = (a[:, :, :3].max(axis=2) < 30) & edge
    test(dark.sum() <= edge.sum() * .02 + 10, f"{name}: no dark halo ({dark.sum()} dark of {edge.sum()} soft edge px)")
print(f"{fails} failed")
sys.exit(fails)

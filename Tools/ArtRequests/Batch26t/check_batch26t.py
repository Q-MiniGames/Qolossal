"""Checks Codex's Batch 25a-8 (from 25a-4 to 25a-7, plus ear widths, feet and the tree trunk) layers against the projected bench blockout and the Qori pose guide.

  python check_batch25a8.py <input folder> <delivery Layers folder>

The Layers folder holds Retreat_OverlookTerrace_BenchBack/BenchFront/Qori_Body/Qori_Head/Qori_EarFar/
Qori_EarNear.png. Every test prints PASS or FAIL; the exit code is the number of failures.
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw

IN, LAY = sys.argv[1], sys.argv[2]
PREFIX = sys.argv[3] if len(sys.argv) > 3 else "Retreat_OverlookTerrace_"
P = json.load(open(os.path.join(IN, "Placement.json")))
W, H = P["canvas"]
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1

def load(name):
    path = os.path.join(LAY, PREFIX + name + ".png")
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
# The face: as much cream cheek as the guide shows for this angle (none when he faces away), on its side.
gh = np.array(Image.open(os.path.join(IN, "Qori_Guide_Head.png"))).astype(int)
gcream = (gh[:, :, 3] > 0) & (gh[:, :, :3].max(axis=2) > 195) & ((gh[:, :, :3].max(axis=2) - gh[:, :, :3].min(axis=2)) < 60)
gshare = gcream.sum() / max(1, (gh[:, :, 3] > 0).sum())
rgb = head[:, :, :3]; mx, mn = rgb.max(axis=2), rgb.min(axis=2)
cream = A(head) & (mx > 195) & ((mx - mn) < 60)
share = cream.sum() / max(1, A(head).sum())
if gshare >= .02:
    lo, hi = max(.03, gshare * .5), min(.45, gshare * 2 + .05)
    test(lo <= share <= hi, f"the face shows as the guide does: cream cheek {share:.0%} of the head ({lo:.0%}-{hi:.0%}; guide {gshare:.0%})")
    cc, gc2 = centroid(cream), centroid(gcream)
    test(abs(cc[0] - gc2[0]) <= 30, f"the cheek is on the guide's side of the head (cheek x {cc[0]:.0f}, guide's {gc2[0]:.0f})")
else:
    test(share <= .03, f"he faces away: no cream cheek showing ({share:.0%} of the head; the guide shows none)")
test((A(head) & A(front)).sum() <= .05 * A(head).sum(), "the backrest does not cover his head")
for name, ear in (("EarNear", earn), ("EarFar", earf)):
    ec = centroid(A(ear)); tip = P["earTips"][name]
    ys, xs = np.nonzero(A(ear))
    far_pt = (xs[np.argmin(ys)], ys.min()) if len(ys) else (-1e9, -1e9)
    gec = centroid(guide("Qori_Guide_" + name))
    test(ec[1] < hc[1] and np.hypot(ec[0] - gec[0], ec[1] - gec[1]) <= 30, f"{name} rises from the head as the guide's does (centroid {ec[0]:.0f},{ec[1]:.0f}; guide's {gec[0]:.0f},{gec[1]:.0f})")
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

# His feet show: bark-coloured paint in Qori_Body at both foot points, not hidden by the bench.
for k, (fx, fy) in enumerate(P["qoriFeet"]):
    x0, y0 = int(fx) - 9, int(fy) - 9
    patch = body[y0:y0 + 18, x0:x0 + 18]
    painted = (patch[:, :, 3] > 128).mean()
    hiddenb = (A(back) | A(front))[y0:y0 + 18, x0:x0 + 18].mean()
    test(painted >= .5 and hiddenb <= .3, f"foot {k + 1} painted and visible at ({fx:.0f},{fy:.0f}) ({painted:.0%} painted, {hiddenb:.0%} under the bench)")

# His legs hang beyond the seat's front edge, so the seat (nearer the camera) hides them until they
# come out below it: below that edge, nothing of Qori_Body may be painted over the seat in BenchBack.
if "seatFrontEdgeLine" in P:
    ex, ey = np.array(P["seatFrontEdgeLine"]).T
    yy, xx = np.mgrid[0:H, 0:W]
    edge_y = np.interp(xx, ex[::-1] if ex[0] > ex[-1] else ex, ey[::-1] if ex[0] > ex[-1] else ey)
    below = yy > edge_y + 4
    # Bark (brown, not leaf green): his cloak may rest on the seat top, his shins may not cross it.
    bark = (body[:, :, 0] > body[:, :, 1] + 10) & (body[:, :, 0] > body[:, :, 2] + 20) & (body[:, :, :3].max(axis=2) < 200)   # brown, not the cream tunic
    over = A(body) & bark & below & (back[:, :, 3] > 200)
    test(over.sum() <= 60, f"no legs painted over the seat: hidden behind it below its front edge ({over.sum()} bark px over the seat)")

    # From behind, nothing of his legs shows at seat level between the backrest and the seat: no
    # folded side-view knee or foot left over from the 25a-6 profile body (the user: "the old feet
    # still showing"). His only feet are the hanging ones below the seat.
    gap = A(body) & bark & (yy > edge_y - 80) & (yy < edge_y + 4)
    test(gap.sum() <= 40, f"no leftover knee or foot at seat level ({gap.sum()} bark px just above the seat's front edge)")

# The olive trunk is one continuous trunk from the branches down into its planter: no gap and no
# sudden change of width (25a-7 joined a separate stump to the old trunk at y 690, leaving a gap).
tree_path = os.path.join(LAY, PREFIX + "Tree.png")
if os.path.exists(tree_path):
    t = np.array(Image.open(tree_path).convert("RGBA"))[:, :, 3] > 128
    widths = []
    for y in range(560, 941, 4):
        xs = np.nonzero(t[y, :240])[0]
        widths.append(xs.max() - xs.min() + 1 if len(xs) else 0)
    gaps = sum(1 for w in widths if w < 20)
    jump = max(abs(a - b) for a, b in zip(widths, widths[1:]))
    test(gaps == 0, f"tree trunk unbroken from y 560 to 940 ({gaps} rows without trunk)")
    test(jump <= 16, f"tree trunk width changes smoothly (largest step {jump} px over 4 rows, <= 16)")
else: print("SKIP tree trunk tests (no Tree layer in this scene)")

# UI zones and edges.
allm = ua | uq
test(allm[:, 1920:].sum() == 0 and allm[1296:, :].sum() == 0, "nothing in the right quarter or bottom 10%")
for name, a in (("BenchBack", back), ("BenchFront", front), ("Qori_Body", body), ("Qori_Head", head)):
    edge = (a[:, :, 3] > 0) & (a[:, :, 3] < 64)
    dark = (a[:, :, :3].max(axis=2) < 30) & edge
    test(dark.sum() <= edge.sum() * .02 + 10, f"{name}: no dark halo ({dark.sum()} dark of {edge.sum()} soft edge px)")
print(f"{fails} failed")
sys.exit(fails)

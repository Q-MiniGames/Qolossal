"""Checks one Batch 26 listening-retreat scene against the fixed camera, bench and Qori of the
approved terrace overlook (Batch 25a-11).

  python check_batch26.py <Batch26_Input> <scene folder> [<actor reference scene folder>]

With a third folder (Batch 26h: the approved terrace with Qori's neck and eye), the actors are compared
with that scene's layers instead of the 25a-11 originals in Batch26_Input/Actors.

The scene folder holds Retreat_<Scene>_Registration.json, its Layers/ and Retreat_<Scene>_Composite.png.
Every test prints PASS or FAIL; the exit code is the number of failures.
"""
import glob, json, os, sys
import numpy as np
from PIL import Image

IN, SCENE = sys.argv[1], sys.argv[2]
REF = sys.argv[3] if len(sys.argv) > 3 else None
P = json.load(open(os.path.join(IN, "Placement.json")))
W, H = 2560, 1440
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1

regs = glob.glob(os.path.join(SCENE, "Retreat_*_Registration.json"))
test(len(regs) == 1, f"one Retreat_<Scene>_Registration.json ({len(regs)} found)")
if not regs: sys.exit(1)
reg = json.load(open(regs[0])); prefix = os.path.basename(regs[0]).replace("Registration.json", "")
test(prefix != "Retreat_OverlookTerrace_" and prefix.count("_") == 2, f"a new scene prefix ({prefix})")
layers = reg["layers"]; names = [L["name"] for L in layers]
def img(L):
    p = os.path.join(SCENE, L["file"])
    return np.array(Image.open(p).convert("RGBA")) if os.path.exists(p) else None

# Every listed file exists, is named with the prefix, and has the registered size.
for L in layers:
    p = os.path.join(SCENE, L["file"])
    ok = os.path.exists(p) and os.path.basename(p).startswith(prefix)
    size = Image.open(p).size if os.path.exists(p) else None
    test(ok and list(size) == list(L["canvas"]), f"{L['name']}: {os.path.basename(L['file'])} {size} matches its registered canvas {L['canvas']}")

# The bench and Qori are the approved ones: same names, order, alpha and pivots (colours may be
# relit for the scene's light).
ACTORS = ["BenchShadow", "BenchBack", "Qori_Body", "Qori_Head", "Qori_EarFar", "Qori_EarNear", "BenchFront"]
idx = [names.index(n) if n in names else -1 for n in ACTORS]
test(all(i >= 0 for i in idx) and idx == sorted(idx), f"actor layers present, in order {ACTORS}")
if REF:
    ref_reg = glob.glob(os.path.join(REF, "Retreat_*_Registration.json"))[0]
    ref = {L["name"]: L for L in json.load(open(ref_reg))["layers"]}
    def ref_path(n): return os.path.join(REF, ref[n]["file"])
else:
    ref = {L["name"]: L for L in json.load(open(os.path.join(IN, "Actors", "Retreat_OverlookTerrace_Registration.json")))["layers"]}
    def ref_path(n): return os.path.join(IN, "Actors", "Retreat_OverlookTerrace_" + n + ".png")
for n in ACTORS:
    if n not in names: continue
    L = layers[names.index(n)]; a = img(L)
    r = np.array(Image.open(ref_path(n)).convert("RGBA"))
    test(a is not None and a.shape == r.shape and np.array_equal(a[:, :, 3], r[:, :, 3]), f"{n}: alpha identical to the approved layer")
    test(L.get("pivot_px") == ref[n].get("pivot_px") and L.get("parent") == ref[n].get("parent"), f"{n}: pivot and parent as approved")

# Ground under the four root feet: some layer drawn before BenchShadow is opaque there.
first_actor = min(i for i in idx if i >= 0) if any(i >= 0 for i in idx) else len(layers)
env = np.zeros((H, W), bool)
for L in layers[:first_actor]:
    a = img(L)
    if a is None: continue
    ox, oy = (L.get("offset") or [0, 0]); ch, cw = a.shape[:2]
    m = np.zeros((H, W), bool)
    xs, ys = max(0, int(ox)), max(0, int(oy)); xe, ye = min(W, int(ox) + cw), min(H, int(oy) + ch)
    m[ys:ye, xs:xe] = a[ys - int(oy):ye - int(oy), xs - int(ox):xe - int(ox), 3] > 250
    if L["name"] != "Sky": env |= m
for (x, y) in P["groundUnderBackPosts"] + P["groundUnderFrontLegs"]:
    patch = env[int(y) - 6:int(y) + 7, int(x) - 6:int(x) + 7]
    test(patch.mean() >= .95, f"solid ground under the root foot at ({x:.0f},{y:.0f}) ({patch.mean():.0%} opaque, not sky)")

# Nothing drawn after BenchFront covers Qori or the bench.
actor_mask = np.zeros((H, W), bool)
for n in ACTORS[1:]:
    if n in names:
        a = img(layers[names.index(n)]); actor_mask |= a[:, :, 3] > 128
last_actor = names.index("BenchFront") if "BenchFront" in names else len(layers)
for L in layers[last_actor + 1:]:
    if L.get("wrap") == "vertical": continue   # weather is clipped by its rectangles
    a = img(L)
    if a is None: continue
    ox, oy = (L.get("offset") or [0, 0]); ch, cw = a.shape[:2]
    xs, ys = max(0, int(ox)), max(0, int(oy)); xe, ye = min(W, int(ox) + cw), min(H, int(oy) + ch)
    over = (a[ys - int(oy):ye - int(oy), xs - int(ox):xe - int(ox), 3] > 64) & actor_mask[ys:ye, xs:xe]
    test(over.sum() <= 20, f"{L['name']} keeps off Qori and the bench ({over.sum()} px over them)")

# Wrapping layers tile; weather keeps out of the actors and the UI.
for L in layers:
    a = img(L)
    if a is None: continue
    if L.get("wrap") == "horizontal":
        test(np.abs(a[:, 0].astype(int) - a[:, -1].astype(int)).max() <= 2, f"{L['name']} tiles left to right")
    if L.get("wrap") == "vertical":
        test(np.abs(a[0].astype(int) - a[-1].astype(int)).max() <= 2, f"{L['name']} tiles top to bottom")
        ex = L.get("exclude_rects", [])
        test(any(r[0] <= 300 and r[1] <= 620 and r[2] >= 1030 and r[3] >= 1190 for r in ex) and L.get("clip_rect") == [0, 0, 1920, 1296],
             f"{L['name']} is clipped to the view and excludes the bench and Qori")

# The composite is the stack, and its UI zones are calm.
comp_path = os.path.join(SCENE, prefix + "Composite.png")
if os.path.exists(comp_path):
    c = np.array(Image.open(comp_path).convert("RGB")).astype(float)
    lum = c @ np.array([.299, .587, .114])
    def busy(zone):
        z = lum[zone[1]:zone[3], zone[0]:zone[2]]
        return float(np.abs(np.diff(z, axis=0)).mean() + np.abs(np.diff(z, axis=1)).mean())
    limit = P.get("uiBusyLimit", 9.0)
    for nm, zone in (("right quarter", [1920, 0, 2560, 1440]), ("bottom 10%", [0, 1296, 2560, 1440])):
        b = busy(zone)
        test(b <= limit, f"{nm} is calm (detail {b:.1f}, at most {limit:.1f}; the approved terrace measures {P.get('uiBusyRef', {}).get(nm, '?')})")
else:
    test(False, f"{prefix}Composite.png delivered")
print(f"{fails} failed"); sys.exit(fails)

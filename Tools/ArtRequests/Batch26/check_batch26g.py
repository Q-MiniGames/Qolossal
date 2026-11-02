"""Checks one Batch 26g scene (the stone slab under the bench removed) against its passed Batch 26 delivery.

  python check_batch26g.py <passed scene folder> <new scene folder>

Only the Ground layer may change, and only inside the slab region x 180-1200, y 1000-1260; every other
file listed in the registration, and the registration itself, stays byte-identical. Run check_batch26.py
on the new folder as well. Every test prints PASS or FAIL; the exit code is the number of failures.
"""
import glob, hashlib, json, os, sys
import numpy as np
from PIL import Image

OLD, NEW = sys.argv[1], sys.argv[2]
X0, Y0, X1, Y1 = 180, 1000, 1200, 1260
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest() if os.path.exists(p) else None

reg_old = glob.glob(os.path.join(OLD, "Retreat_*_Registration.json"))[0]
reg_new = os.path.join(NEW, os.path.basename(reg_old))
test(sha(reg_old) == sha(reg_new), "registration unchanged")
for L in json.load(open(reg_old))["layers"]:
    a, b = os.path.join(OLD, L["file"]), os.path.join(NEW, L["file"])
    if L["name"] != "Ground":
        test(sha(a) == sha(b), f"{L['name']} unchanged")
        continue
    ga = np.array(Image.open(a).convert("RGBA")).astype(int); gb = np.array(Image.open(b).convert("RGBA")).astype(int)
    diff = np.abs(ga - gb).max(axis=2) > 0
    outside = diff.copy(); outside[Y0:Y1, X0:X1] = False
    test(outside.sum() == 0, f"Ground changed only inside x {X0}-{X1}, y {Y0}-{Y1} ({outside.sum()} px changed outside)")
    test(diff[Y0:Y1, X0:X1].mean() > .05, f"Ground repainted where the slab was ({diff[Y0:Y1, X0:X1].mean():.0%} of the region changed)")
    test((gb[Y0:Y1, X0:X1, 3] == 255).all(), "Ground stays opaque in the region")
print(f"{fails} failed"); sys.exit(fails)

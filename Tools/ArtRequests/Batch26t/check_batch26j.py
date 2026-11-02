"""Checks one Batch 26j scene (the bench and Qori at 45 degrees) against its Batch 26h scene: only the
bench, Qori, the blink and the floor layer (Ground, or Terrace on the overlook) may change; the floor
only in the band around the old and new bench (x 150-1300, y 820-1296).

  python check_batch26j.py <Batch26h scene folder> <new scene folder>

Run check_batch26t.py on the new Layers folder too. Exit code: the number of failures.
"""
import glob, hashlib, json, os, sys
import numpy as np
from PIL import Image

OLD, NEW = sys.argv[1], sys.argv[2]
MAY_CHANGE = {"BenchShadow", "BenchBack", "BenchFront", "Qori_Body", "Qori_Head", "Qori_Blink", "Qori_EarFar", "Qori_EarNear"}
X0, Y0, X1, Y1 = 150, 820, 1300, 1296
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest() if os.path.exists(p) else None

name = os.path.basename(glob.glob(os.path.join(OLD, "Retreat_*_Registration.json"))[0])
old = json.load(open(os.path.join(OLD, name)))["layers"]
new = json.load(open(os.path.join(NEW, name)))["layers"]
test([L["name"] for L in new] == [L["name"] for L in old], "same layers, same order (Qori_Blink after Qori_Head)")
for L in old:
    if L["name"] in MAY_CHANGE: continue
    a, b = os.path.join(OLD, L["file"]), os.path.join(NEW, L["file"])
    if L["name"] in ("Ground", "Terrace"):
        ga = np.array(Image.open(a).convert("RGBA")).astype(int); gb = np.array(Image.open(b).convert("RGBA")).astype(int)
        diff = np.abs(ga - gb).max(axis=2) > 0
        out = diff.copy(); out[Y0:Y1, X0:X1] = False
        test(out.sum() == 0, f"{L['name']} changed only around the bench ({out.sum()} px changed outside x {X0}-{X1}, y {Y0}-{Y1})")
        continue
    test(sha(a) == sha(b), f"{L['name']} unchanged")
blink = next((L for L in new if L["name"] == "Qori_Blink"), None)
test(blink is not None and blink.get("parent") == "Qori_Head", "Qori_Blink present, parent Qori_Head")
print(f"{fails} failed"); sys.exit(fails)

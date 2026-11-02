"""Checks one Batch 26h scene (Qori's neck and the edge of his eye) against its passed delivery.

  python check_batch26h.py <passed scene folder> <new scene folder>

Only Qori_Body and Qori_Head change, and a Qori_Blink layer is added right after Qori_Head (parent
Qori_Head, full canvas at 0,0); everything else stays byte-identical. Also run check_batch26.py with the
new OverlookTerrace folder as the actor reference, so every scene shares the same body and head shapes.
Every test prints PASS or FAIL; the exit code is the number of failures.
"""
import glob, hashlib, json, os, sys
import numpy as np
from PIL import Image

OLD, NEW = sys.argv[1], sys.argv[2]
MIN_EYE = int(sys.argv[3]) if len(sys.argv) > 3 else 40   # 26i: 200 (the 26h eye, 61 px, read as a crack)
fails = 0
def test(ok, what):
    global fails
    print(("PASS " if ok else "FAIL ") + what); fails += 0 if ok else 1
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest() if os.path.exists(p) else None
def rgba(p): return np.array(Image.open(p).convert("RGBA")).astype(int)

reg_name = os.path.basename(glob.glob(os.path.join(OLD, "Retreat_*_Registration.json"))[0])
old = json.load(open(os.path.join(OLD, reg_name)))["layers"]
new = json.load(open(os.path.join(NEW, reg_name)))["layers"]
on, nn = [L["name"] for L in old], [L["name"] for L in new]
test(nn == on[:on.index("Qori_Head") + 1] + ["Qori_Blink"] + on[on.index("Qori_Head") + 1:], "layer order: the old one with Qori_Blink right after Qori_Head")
blink = next((L for L in new if L["name"] == "Qori_Blink"), None)
test(blink is not None and blink.get("parent") == "Qori_Head" and list(blink.get("canvas", [])) == [2560, 1440] and list(blink.get("offset") or [0, 0]) == [0, 0],
     "Qori_Blink: parent Qori_Head, 2560x1440 at (0,0)")
for L in old:
    a, b = os.path.join(OLD, L["file"]), os.path.join(NEW, L["file"])
    if L["name"] in ("Qori_Body", "Qori_Head"): continue
    test(sha(a) == sha(b), f"{L['name']} unchanged")
    Ln = next(x for x in new if x["name"] == L["name"])
    test({k: v for k, v in Ln.items()} == {k: v for k, v in L.items()}, f"{L['name']} registration unchanged")

pre = "Retreat_" + reg_name.split("_")[1] + "_"
head_old, head = rgba(os.path.join(OLD, "Layers", pre + "Qori_Head.png")), rgba(os.path.join(NEW, "Layers", pre + "Qori_Head.png"))
body = rgba(os.path.join(NEW, "Layers", pre + "Qori_Body.png"))
ho, hn = head_old[:, :, 3] > 128, head[:, :, 3] > 128
iou = (ho & hn).sum() / max(1, (ho | hn).sum())
test(iou >= .97, f"head outline kept (IoU {iou:.3f} with the passed head)")

# The neck: no pinch between the crest and the shoulders.
op = (head[:, :, 3] > 128) | (body[:, :, 3] > 128)
widths = [op[y, 700:840].sum() for y in range(784, 813, 2)]
test(min(widths) >= 40, f"a neck: Qori at least 40 px wide from y 784 to 812 (narrowest {min(widths)} px; the passed one pinches to 29)")
neck = body[788:806, 745:790]; nm = neck[:, :, 3] > 128
bark = nm & (neck[:, :, 0] > neck[:, :, 1] + 8) & (neck[:, :, 0] > neck[:, :, 2] + 15) & (neck[:, :, :3].max(axis=2) < 200)
test(bark.sum() >= 150, f"the neck is bark brown in Qori_Body ({bark.sum()} bark px at x 745-790, y 788-806; the passed body has 68)")

# The edge of the eye, on the cheek, and a blink that covers it.
bl = rgba(os.path.join(NEW, "Layers", pre + "Qori_Blink.png")) if blink else np.zeros_like(head)
bm = bl[:, :, 3] > 128
box = np.zeros_like(bm); box[715:795, 770:830] = True
covered = bm & box
test(MIN_EYE <= covered.sum() <= 700, f"Qori_Blink covers an eye-sized area on the cheek ({covered.sum()} px; at least {MIN_EYE}, at most 700)")
test((bm & ~box).sum() <= 20, f"Qori_Blink stays on the cheek ({(bm & ~box).sum()} px outside x 770-830, y 715-795)")
under = head[covered]
test(len(under) > 0 and under[:, :3].max(axis=1).mean() <= 110, f"under the blink the head shows a dark eye (mean brightness {under[:, :3].max(axis=1).mean() if len(under) else -1:.0f}, at most 110)")
lid = bl[covered]
test(len(lid) > 0 and lid[:, :3].max(axis=1).mean() >= 150, f"the blink is the cheek's cream, a closed lid (mean brightness {lid[:, :3].max(axis=1).mean() if len(lid) else -1:.0f})")
print(f"{fails} failed"); sys.exit(fails)

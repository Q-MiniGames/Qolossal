# Crops every layer of one imported listening-retreat scene to its painted area (plus a 2 px margin for
# filtering), in place, and rewrites the scene's registration with the new canvas sizes and offsets. The
# pixels that remain are unchanged and the scene looks the same; only empty transparent space goes, so
# each layer's texture shrinks (the terrace from about 245 MB of raw RGBA to about 50 MB). Wrapping
# layers keep their wrapping axis whole (clouds and mist their width, the weather tile its height).
# Pivots are in view-canvas pixels and do not move. The receipt records each crop.
#   python trim_retreat_layers.py <scene id>        (OverlookTerrace, CradleMeadow, ..., OverlookTerrace4K)
import glob, hashlib, json, os, sys
from PIL import Image
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
scene = sys.argv[1]
ART = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art") if scene == "OverlookTerrace" else os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", scene)
prefix = "Retreat_" + scene + "_"
rp = os.path.join(ART, prefix + "Registration.json"); reg = json.load(open(rp))
cp = os.path.join(ART, prefix + "RECEIPT.json"); receipt = json.load(open(cp))
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
before = after = 0; crops = []
for L in reg["layers"]:
    path = os.path.join(ART, os.path.basename(L["file"]))
    im = Image.open(path).convert("RGBA"); w, h = im.size
    before += w * h * 4
    bb = im.getchannel("A").getbbox()
    if bb is None: after += w * h * 4; continue
    x0, y0, x1, y1 = max(0, bb[0] - 2), max(0, bb[1] - 2), min(w, bb[2] + 2), min(h, bb[3] + 2)
    if L.get("wrap") == "horizontal": x0, x1 = 0, w
    if L.get("wrap") == "vertical": y0, y1 = 0, h
    # Block compression (DXT/BC, ASTC) needs sides that are multiples of 4, or Unity stores the
    # texture uncompressed: grow the crop to the next multiple of 4, inside the canvas where possible.
    def grow(a0, a1, full):
        need = (-(a1 - a0)) % 4
        a1 = min(full, a1 + need); need = (-(a1 - a0)) % 4; a0 = max(0, a0 - need)
        return a0, a1
    x0, x1 = grow(x0, x1, w); y0, y1 = grow(y0, y1, h)
    if (x1 - x0) % 4 or (y1 - y0) % 4:
        # The whole canvas side is not a multiple of 4 (the 4K far valley is 3936 wide, fine; odd
        # foreground canvases are not): pad with transparent pixels instead.
        pw, ph = (x1 - x0) + (-(x1 - x0)) % 4, (y1 - y0) + (-(y1 - y0)) % 4
        pad = Image.new("RGBA", (pw, ph), (0, 0, 0, 0)); pad.paste(im.crop((x0, y0, x1, y1)), (0, 0))
        pad.save(path); ox, oy = (L.get("offset") or [0, 0])
        L["canvas"] = [pw, ph]; L["offset"] = [ox + x0, oy + y0]
        crops.append({"asset": os.path.basename(path), "crop": [x0, y0, x1, y1], "padTo": [pw, ph], "from": [w, h], "sha256": sha(path)})
        after += pw * ph * 4; continue
    if (x0, y0, x1, y1) != (0, 0, w, h):
        im.crop((x0, y0, x1, y1)).save(path)
        ox, oy = (L.get("offset") or [0, 0])
        L["canvas"] = [x1 - x0, y1 - y0]; L["offset"] = [ox + x0, oy + y0]
        crops.append({"asset": os.path.basename(path), "crop": [x0, y0, x1, y1], "from": [w, h], "sha256": sha(path)})
    after += (x1 - x0) * (y1 - y0) * 4
json.dump(reg, open(rp, "w"), indent=2)
receipt["trimmed"] = crops; json.dump(receipt, open(cp, "w"), indent=2)
print(f"{scene}: trimmed {len(crops)} layers, raw RGBA {before / 2**20:.0f} -> {after / 2**20:.0f} MiB")

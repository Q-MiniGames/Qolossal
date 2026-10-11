# Puts a reviewed terrace-overlook batch into Assets/ListeningRetreat/Art in place of what is there:
#   python copy_retreat_batch.py <Batch folder> <review file>   (25a-6: Review 59; 25a-7: Review 60)
# Every file is checked against the delivery's MANIFEST.json, the
# weather clip mask is derived from the registration's clip and exclude rectangles, PNG metas get
# deterministic GUIDs from png_meta_template.txt (single sprite, internalID 21300000), and a receipt
# is written. Run write_metas.py afterwards for any new folders or scripts.
import glob, hashlib, json, os, shutil, sys
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
SRC = os.path.join(r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full", sys.argv[1])
ART = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art")
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
manifest = {f["path"]: f["sha256"] for f in json.load(open(os.path.join(SRC, "MANIFEST.json")))["files"]}
reg = json.load(open(os.path.join(SRC, "Retreat_OverlookTerrace_Registration.json")))
files = [L["file"] for L in reg["layers"]] + ["Retreat_OverlookTerrace_Registration.json"]
for rel in files:
    if manifest.get(rel) != sha(os.path.join(SRC, rel)): sys.exit("HASH MISMATCH " + rel)
# The previous set goes; existing .meta files stay (GUIDs and import settings are kept by name).
for p in glob.glob(os.path.join(ART, "Retreat_*")):
    if not p.endswith(".meta"): os.remove(p)
template = open(os.path.join(HERE, "png_meta_template.txt")).read()
def meta(path):
    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    g = hashlib.md5(("qolossal-retreat:" + rel).encode()).hexdigest()
    body = template.replace("{GUID}", g) if path.endswith(".png") else \
        f"fileFormatVersion: 2\nguid: {g}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if not os.path.exists(path + ".meta"): open(path + ".meta", "w", newline="\n").write(body)
receipt = {"source": "Codex_v2_full/" + sys.argv[1], "review": "Tools/IncomingArt/Codex_v2/" + sys.argv[2], "files": []}
for rel in files:
    dst = os.path.join(ART, os.path.basename(rel)); shutil.copyfile(os.path.join(SRC, rel), dst)
    if sha(dst) != manifest[rel]: sys.exit("copy changed " + rel)
    meta(dst); receipt["files"].append({"source": rel, "asset": os.path.basename(rel), "sha256": manifest[rel]})
# The weather motes show only inside the clip rectangle and outside the actor rectangle.
w = next(L for L in reg["layers"] if L["name"] == "Weather")
W, H = reg["view_canvas"]
m = Image.new("L", (W, H), 0); d = ImageDraw.Draw(m)
d.rectangle(tuple(w["clip_rect"]), fill=255)
for r in w.get("exclude_rects", []): d.rectangle(tuple(r), fill=0)
mp = os.path.join(ART, "Retreat_OverlookTerrace_WeatherMask.png"); m.save(mp); meta(mp)
receipt["derived"] = [{"asset": "Retreat_OverlookTerrace_WeatherMask.png", "from": "Weather clip_rect and exclude_rects"}]
json.dump(receipt, open(os.path.join(ART, "..", "RECEIPT.json"), "w"), indent=2)
print(f"copied {len(files)} files (hashes match), derived the weather mask")

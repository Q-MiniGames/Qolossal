# Puts one reviewed listening-retreat scene into Assets/ListeningRetreat/Art, in place of what that
# scene had there:
#   python copy_retreat_batch.py <Batch folder> <review file> [--actors-from <Batch folder>] [--as <scene id>]
# --as files a delivery under another scene id (the 4K terrace test: --as OverlookTerrace4K), renaming
# its files to that prefix. Afterwards run trim_retreat_layers.py on the scene's folder.
# (25a-6 to 25a-11: the overlook terrace, Reviews 59-64; Batch 26a-26f: six more scenes, Reviews 65-67).
# The overlook terrace lives in Art/ itself; every other scene in Art/<Scene>/. With --actors-from, the
# seven bench and Qori layers come from that batch's Retreat_OverlookTerrace_ files instead (alpha is
# identical; used for 26e-2, whose cool relight fought its warm light, Review 67).
# Every file is checked against its delivery's MANIFEST.json; the weather clip mask is derived from the
# registration's clip and exclude rectangles; PNG metas get deterministic GUIDs from
# png_meta_template.txt (single sprite, internalID 21300000); existing metas are kept; a receipt is
# written per scene. Run write_metas.py afterwards for any new folders or scripts.
import glob, hashlib, json, os, shutil, sys
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
DELIVERIES = r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full"
ACTORS = ("BenchShadow", "BenchBack", "Qori_Body", "Qori_Head", "Qori_Blink", "Qori_EarFar", "Qori_EarNear", "BenchFront")
batch, review = sys.argv[1], sys.argv[2]
actors_from = sys.argv[sys.argv.index("--actors-from") + 1] if "--actors-from" in sys.argv else None
SRC = os.path.join(DELIVERIES, batch)
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
def manifest_of(folder): return {f["path"]: f["sha256"] for f in json.load(open(os.path.join(folder, "MANIFEST.json")))["files"]}
manifest = manifest_of(SRC)
reg_name = os.path.basename(glob.glob(os.path.join(SRC, "Retreat_*_Registration.json"))[0])
src_prefix = reg_name.replace("Registration.json", "")
scene = sys.argv[sys.argv.index("--as") + 1] if "--as" in sys.argv else src_prefix[len("Retreat_"):-1]
prefix = "Retreat_" + scene + "_"
ART = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art") if scene == "OverlookTerrace" else \
      os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", scene)
os.makedirs(ART, exist_ok=True)
reg = json.load(open(os.path.join(SRC, reg_name)))
files = [L["file"] for L in reg["layers"]] + [reg_name]
for rel in files:
    if manifest.get(rel) != sha(os.path.join(SRC, rel)): sys.exit("HASH MISMATCH " + rel)
# This scene's previous files go; existing .meta files stay (GUIDs and import settings are kept by name).
for p in glob.glob(os.path.join(ART, prefix + "*")):
    if not p.endswith(".meta"): os.remove(p)
template = open(os.path.join(HERE, "png_meta_template.txt")).read()
def meta(path):
    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    g = hashlib.md5(("qolossal-retreat:" + rel).encode()).hexdigest()
    body = template.replace("{GUID}", g) if path.endswith(".png") else \
        f"fileFormatVersion: 2\nguid: {g}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    if not os.path.exists(path + ".meta"): open(path + ".meta", "w", newline="\n").write(body)
receipt = {"source": "Codex_v2_full/" + batch, "review": "Tools/IncomingArt/Codex_v2/" + review, "files": []}
alt = None
if actors_from:
    alt_src = os.path.join(DELIVERIES, actors_from); alt = manifest_of(alt_src); receipt["actorsFrom"] = "Codex_v2_full/" + actors_from
for rel in files:
    name = os.path.basename(rel).replace(src_prefix, prefix, 1); dst = os.path.join(ART, name)
    layer = name[len(prefix):-4] if name.endswith(".png") else None
    if alt is not None and layer in ACTORS:
        src_rel = "Layers/Retreat_OverlookTerrace_" + layer + ".png"; src = os.path.join(alt_src, src_rel)
        if alt.get(src_rel) != sha(src): sys.exit("HASH MISMATCH " + actors_from + "/" + src_rel)
        # Same alpha as the delivered layer, or the scene's registration would not fit it.
        import numpy as np
        if not np.array_equal(np.array(Image.open(src).convert("RGBA"))[:, :, 3], np.array(Image.open(os.path.join(SRC, rel)).convert("RGBA"))[:, :, 3]):
            sys.exit("alpha differs for " + layer)
        shutil.copyfile(src, dst); want = alt[src_rel]; source = actors_from + "/" + src_rel
    else:
        shutil.copyfile(os.path.join(SRC, rel), dst); want = manifest[rel]; source = rel
    if sha(dst) != want: sys.exit("copy changed " + name)
    meta(dst); receipt["files"].append({"source": source, "asset": name, "sha256": want})
# The weather motes show only inside the clip rectangle and outside the actor rectangle.
w = next(L for L in reg["layers"] if L["name"] == "Weather")
W, H = reg["view_canvas"]
# At a quarter of the view's size (its edges are straight, so nothing is lost); the builder imports
# masks at a quarter of the pixels per unit.
m = Image.new("L", (W // 4, H // 4), 0); d = ImageDraw.Draw(m)
d.rectangle(tuple(v / 4 for v in w["clip_rect"]), fill=255)
for r in w.get("exclude_rects", []): d.rectangle(tuple(v / 4 for v in r), fill=0)
mp = os.path.join(ART, prefix + "WeatherMask.png"); m.save(mp); meta(mp)
receipt["derived"] = [{"asset": prefix + "WeatherMask.png", "from": "Weather clip_rect and exclude_rects"}]
if prefix != src_prefix:
    # The registration names its files with the delivery's prefix: point it at the renamed ones.
    rp = os.path.join(ART, prefix + "Registration.json"); r2 = json.load(open(rp))
    for L in r2["layers"]: L["file"] = L["file"].replace(src_prefix, prefix)
    json.dump(r2, open(rp, "w"), indent=2); receipt["renamedFrom"] = src_prefix
json.dump(receipt, open(os.path.join(ART, prefix + "RECEIPT.json"), "w"), indent=2)
print(f"{scene}: copied {len(files)} files (hashes match){' with actors from ' + actors_from if actors_from else ''}, derived the weather mask")

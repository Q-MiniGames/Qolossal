# Puts one place's new listening-bench scenery (Codex Retreat v01 + Fixes v02 + v02b; Claude reviews 79, 81, 83)
# into Assets/ListeningRetreat/Art/v2/<Scene>/, beside the old art, which stays untouched:
#   python copy_retreat_v2.py <Scene>            e.g. OverlookTerrace
# Reads the scene's newest registration (v02b, else v02) from the Codex workspace, checks every layer's SHA-256,
# copies the visible layers as Retreat_<Scene>_<UnityName>.png and writes Retreat_<Scene>_Registration.json in the
# shape ListeningRetreatBuilder reads (view_canvas 3840 x 2160, offsets top-left, back to front), plus a receipt.
# Run write_metas.py afterwards for deterministic GUIDs. ListeningRetreatBuilder.UseV2 lists the places that use it.
import hashlib, json, os, shutil, sys
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
WORK = r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal"
FIXES = os.path.join(WORK, "Art", "Codex_v2_full")
scene = sys.argv[1]
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()

reg_path = next(p for p in (os.path.join(FIXES, "Retreat_Fixes_v02b", scene, "Registration_v02b.json"),
                            os.path.join(FIXES, "Retreat_Fixes_v02", scene, "Registration_v02.json")) if os.path.exists(p))
reg = json.load(open(reg_path))

# Codex layer names -> the names the builder and ListeningRetreat know (Far/Mid drift, Clouds wraps,
# LanternGlow flickers, Lantern sways on the tree, Foreground_* sways from its base, Weather scrolls).
RENAME = {"01_Sky": "Sky", "01_Backdrop": "Sky", "02_Clouds": "Clouds", "03_Mountains": "Far", "04_Valley": "Mid",
          "02_MiddleDistance": "Mid", "05_Terrace": "Terrace", "03_Ground": "Ground", "06_Tree": "Tree", "04_Tree": "Tree",
          "07_Lantern": "Lantern", "05_Lantern": "Lantern", "08_Bench": "Bench", "05_Bench": "Bench",
          "09_FlowerPot": "FlowerPot", "05_FlowerPot": "FlowerPot", "10_ForegroundFoliage": "Foreground_01",
          "06_ForegroundPlants": "Foreground_01", "07_Rain_Tile": "Weather"}
def unity_name(n): return RENAME.get(n, n.split("_", 1)[1] if n[:2].isdigit() and "_" in n else n)

# The bench (18-19 Oct, the user on the overlook terrace: Qori's feet should show, the bench "back similar to the old
# one", then "the bench location is wrong" and "on the left side more"). Every place keeps its old angled bench from
# the live art (Art/[<id>/]Retreat_<id>_BenchShadow/BenchBack/BenchFront, painted on the 2560 canvas, enlarged 1.5x),
# with the Ludo Qori between BenchBack and BenchFront where that place's painted Qori sat. The new Codex bench and its
# shadow are left out. The old shadow is darkened 1.7x to read on the brighter new ground. The bench is moved so its
# front posts' feet (bottom centre of BenchFront) stand at BENCH_FEET; by default where the Codex bench's feet stand.
# The two overlooks share a composition, and there the bench stands on the open paving just right of the new tree
# planter (the user's pick on the terrace).
# 19 Oct, the user: "remove the bench shadow from all": no BenchShadow (old or new) in any place.
# Overlook Night's wall stands about 130 px lower in the frame than the terrace's, so there the bench comes forward
# (its far legs had stood on the wall) and is drawn 6% larger, as nearer things are (BENCH_SCALE).
OLD_BENCH = [("BenchBack", 1.0), ("BenchFront", 1.0)]
BENCH_FEET = {"OverlookTerrace": [1327.5, 1759], "OverlookNight": [1327.5, 1889]}
BENCH_SCALE = {"OverlookNight": 1.06}

dst_dir = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", "v2", scene)
os.makedirs(dst_dir, exist_ok=True)
prefix = "Retreat_" + scene + "_"
layers, receipt = [], {"scene": scene, "registration": os.path.relpath(reg_path, WORK).replace("\\", "/"),
                       "reviews": ["CLAUDE_REVIEW_79", "CLAUDE_REVIEW_81", "CLAUDE_REVIEW_83"], "files": []}
names = {L["name"]: unity_name(L["name"]) for L in reg["layers"]}
old_art = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art") + ("" if scene == "OverlookTerrace" else os.sep + scene)
old_reg = {L["name"]: L for L in json.load(open(os.path.join(old_art, prefix + "Registration.json")))["layers"]}

def opaque_box(path, offset, k=1.0):
    a = np.asarray(Image.open(path).convert("RGBA"))[:, :, 3]
    ys = np.where((a > 128).any(axis=1))[0]; xs = np.where((a > 128).any(axis=0))[0]
    return (offset[0] + xs.min()) * k, (offset[1] + ys.min()) * k, (offset[0] + xs.max()) * k, (offset[1] + ys.max()) * k

# 19 Oct, the user: scale the bench down "to give the scenery more room and make [it] look bigger". All benches (and
# Qori with them) at 75%, about their front feet, which stay put: bench and Qori now span about 28% of the screen's
# height (37% before), inside the quarter-to-a-third agreed for the retreats.
BENCH_SIZE = 0.75
K = 1.5 * BENCH_SCALE.get(scene, 1.0) * BENCH_SIZE   # old 2560 px art onto this 3840 canvas, times the bench scales

def bench_move():
    """Where the old bench goes: its front feet (BenchFront bottom centre, at 4K) onto BENCH_FEET or the Codex bench's feet."""
    x0, _, x1, y1 = opaque_box(os.path.join(old_art, prefix + "BenchFront.png"), old_reg["BenchFront"]["offset"], K)
    if scene in BENCH_FEET: tx, ty = BENCH_FEET[scene]
    else:
        L = next(L for L in reg["layers"] if names[L["name"]] == "Bench")
        bx0, _, bx1, by1 = opaque_box(os.path.join(WORK, L["file"]), L["offset"])
        tx, ty = (bx0 + bx1) / 2, by1
    return [tx - (x0 + x1) / 2, ty - y1]

def old_bench(move):
    """The live place's bench layers, enlarged K x into this folder, with their registration entries."""
    out = []
    for n, dark in OLD_BENCH:
        src = os.path.join(old_art, prefix + n + ".png")
        im = Image.open(src).convert("RGBA"); im = im.resize((round(im.width * K), round(im.height * K)), Image.LANCZOS)
        if dark != 1.0:
            a = np.asarray(im).copy(); a[:, :, 3] = np.clip(a[:, :, 3].astype(float) * dark, 0, 255).astype(np.uint8); im = Image.fromarray(a)
        im.save(os.path.join(dst_dir, prefix + n + ".png"))
        receipt["files"].append({"layer": n, "from": os.path.relpath(src, ROOT).replace("\\", "/"), "sha256": sha(src),
                                 "derived": "enlarged %.2fx" % K + (", alpha x%.1f" % dark if dark != 1.0 else "")})
        o = old_reg[n]["offset"]
        out.append({"name": n, "file": "Layers/" + prefix + n + ".png", "canvas": [im.width, im.height],
                    "offset": [round(o[0] * K + move[0], 1), round(o[1] * K + move[1], 1)]})
    return out

def qori_seat(move):
    """The painted Qori's extent in the old place (body centre, feet row, head-to-feet height), at 4K, moved with the bench."""
    qx0 = qy0 = 1e9; qy1 = -1e9; cx = None
    for n, L in old_reg.items():
        if not n.startswith("Qori_") or n == "Qori_Blink": continue
        o, c = L["offset"], L["canvas"]
        qy0 = min(qy0, o[1]); qy1 = max(qy1, o[1] + c[1])
        if n == "Qori_Body": cx = o[0] + c[0] / 2
    return {"anchor_px": [round(cx * K + move[0], 1), round(qy1 * K + move[1], 1)],
            "figure_height_px": round((qy1 - qy0) * K, 1), "insert_before": "BenchFront"}

move = bench_move()
for L in reg["layers"]:
    src = os.path.join(WORK, L["file"])
    if sha(src) != L["sha256"]: sys.exit("HASH MISMATCH " + L["file"])
    if not L.get("visible", True): continue
    n = names[L["name"]]
    if n in ("Bench", "BenchShadow"):
        if n == "Bench": layers.extend(old_bench(move))
        continue
    dst = os.path.join(dst_dir, prefix + n + ".png")
    shutil.copyfile(src, dst)
    if sha(dst) != L["sha256"]: sys.exit("COPY MISMATCH " + dst)
    receipt["files"].append({"layer": n, "codexName": L["name"], "from": L["file"], "sha256": L["sha256"]})
    e = {"name": n, "file": "Layers/" + prefix + n + ".png", "canvas": L["canvas"], "offset": L["offset"]}
    if L.get("pivot"): e["pivot_px"] = L["pivot"]
    if n == "Tree" and L.get("pivot"): e["motion_degrees"] = [-.2, .2]   # the planter rides on it (Review 79)
    if n == "Lantern": e["parent"] = "Tree"; e["motion_degrees"] = [-4, 4]
    if n == "LanternGlow":
        e["opacity_range"] = L.get("suggested_opacity_range", [.65, 1])
        if names.get(L.get("parent")) == "Lantern": e["parent"] = "Lantern"
        else: e.pop("pivot_px", None)                                    # a lamp on a fixed post: the glow only flickers
    if n.startswith("Foreground"):
        e.pop("pivot_px", None); e["base_pivot_canvas_px"] = L.get("pivot") or [0, 2160]; e["motion_degrees"] = [-3, 3]
    if n.startswith("PropShadow") and "LanternGlow" not in n: e.pop("pivot_px", None)
    wp = L.get("wrap_period")
    if wp and wp["axis"] == "x": e["wrap"] = "horizontal"; e["period_px"] = wp["pixels"]
    if wp and wp["axis"] == "y":
        # The rain tile scrolls down inside a mask; this one lets it show over the whole view (quarter size, white).
        e["wrap"] = "vertical"; e["period_px"] = wp["pixels"]
        Image.new("L", (960, 540), 255).save(os.path.join(dst_dir, prefix + n + "Mask.png"))
    if L.get("drift_margin") and L["drift_margin"][0] > 0: e["parallax_x_range_px"] = [-L["drift_margin"][0], L["drift_margin"][0]]
    # Large layers that were enlarged from ~2000 px generations import at 2048: the same detail, a quarter of the memory.
    g = L.get("generated_size")
    if L.get("enlarged") and g and max(L["canvas"]) > 2048 and max(g) <= 2200: e["import_max_px"] = 2048
    layers.append(e)
out = {"schema": "qolossal-retreat-terrace/3", "view_canvas": [3840, 2160], "coordinates": "top-left pixels",
       "alpha": "straight", "source": receipt["registration"], "layers": layers, "qori_seat": qori_seat(move),
       "bench_move_px": move}
json.dump(out, open(os.path.join(dst_dir, prefix + "Registration.json"), "w", newline="\n"), indent=1)
json.dump(receipt, open(os.path.join(dst_dir, prefix + "RECEIPT.json"), "w", newline="\n"), indent=1)
print("copied", len(receipt["files"]), "layers to", os.path.relpath(dst_dir, ROOT), "| bench move", [round(v, 1) for v in move], "| qori seat:", out["qori_seat"])

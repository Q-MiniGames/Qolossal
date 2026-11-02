# The 26h eye read as a crack on the cheek (the user, 12 Oct). Until it is repainted, each place uses its
# previous approved Qori_Head (no eye; same alpha as the 26h head) and the builder leaves out Qori_Blink
# (ListeningRetreatBuilder.UseEye = false). The neck (Qori_Body) from 26h stays.
#   python restore_heads_no_eye.py
import hashlib, json, os, shutil, sys
import numpy as np
from PIL import Image
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
D = r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full"
SOURCES = {"OverlookTerrace": "Batch25a11", "OverlookNight": "Batch26f", "CradleMeadow": "Batch26g/CradleMeadow",
           "TerracesRain": "Batch26g/TerracesRain", "CausewayDusk": "Batch26g/CausewayDusk", "RibwoodGlade": "Batch26g/RibwoodGlade",
           "HeightsClouds": "Batch25a11"}   # the game uses the terrace's actor colours for the Heights
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
for scene, batch in SOURCES.items():
    src_name = "Retreat_" + ("OverlookTerrace" if batch == "Batch25a11" else scene) + "_Qori_Head.png"
    src = os.path.join(D, batch, "Layers", src_name)
    man = {f["path"]: f["sha256"] for f in json.load(open(os.path.join(D, batch, "MANIFEST.json")))["files"]}
    if man.get("Layers/" + src_name) != sha(src): sys.exit("HASH MISMATCH " + src)
    art = os.path.join(ROOT, "Assets", "ListeningRetreat", "Art") if scene == "OverlookTerrace" else os.path.join(ROOT, "Assets", "ListeningRetreat", "Art", scene)
    dst = os.path.join(art, "Retreat_" + scene + "_Qori_Head.png")
    if not np.array_equal(np.array(Image.open(src).convert("RGBA"))[:, :, 3], np.array(Image.open(dst).convert("RGBA"))[:, :, 3]): sys.exit("alpha differs " + scene)
    shutil.copyfile(src, dst)
    rp = os.path.join(art, "Retreat_" + scene + "_RECEIPT.json"); r = json.load(open(rp))
    for f in r["files"]:
        if f["asset"] == os.path.basename(dst): f["source"] = batch + "/Layers/" + src_name; f["sha256"] = sha(dst); f["note"] = "restored: no eye until 26h's eye is repainted"
    json.dump(r, open(rp, "w"), indent=2)
    print(scene, "head restored from", batch)

# Copies the reviewed Batch 24d listening-retreat art (CLAUDE_REVIEW_54) into Assets/ListeningRetreat/Art,
# checking every file against the delivery's MANIFEST.json, and writes a receipt.
import hashlib, json, os, shutil, sys
SRC = r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch24d"
DST = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "ListeningRetreat", "Art")
FILES = [f"Layers/Retreat_RainWindow_{n}.png" for n in
         ("Sky", "FarValley", "NearValley", "Mist", "RainGlass", "RainStreaks", "Room", "LanternGlow", "Bench",
          "Qori_Body", "Qori_EarFar", "Qori_EarNear", "Qori_Head")]
EXTRA = {"Sources/Prepared/WindowMask.png": "Retreat_RainWindow_WindowMask.png",
         "Sources/Prepared/RainStreaks_RepeatTile.png": "Retreat_RainWindow_RainTile.png",
         "Retreat_RainWindow_Registration.json": "Retreat_RainWindow_Registration.json"}
def sha(p): return hashlib.sha256(open(p, "rb").read()).hexdigest()
manifest = {f["path"]: f["sha256"] for f in json.load(open(os.path.join(SRC, "MANIFEST.json")))["files"]}
os.makedirs(DST, exist_ok=True)
receipt = {"source": "Codex_v2_full/Batch24d", "review": "Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_54.md", "files": []}
pairs = [(f, os.path.basename(f)) for f in FILES] + list(EXTRA.items())
for rel, name in pairs:
    src = os.path.join(SRC, rel)
    want = manifest.get(rel)
    got = sha(src)
    if want != got: sys.exit(f"HASH MISMATCH {rel}: manifest {want} file {got}")
    shutil.copyfile(src, os.path.join(DST, name))
    if sha(os.path.join(DST, name)) != got: sys.exit("copy changed " + name)
    receipt["files"].append({"source": rel, "asset": name, "sha256": got})
json.dump(receipt, open(os.path.join(DST, "..", "RECEIPT.json"), "w"), indent=2)
print(f"copied {len(pairs)} files, all hashes match")

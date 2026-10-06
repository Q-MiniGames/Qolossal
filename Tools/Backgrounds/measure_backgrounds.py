"""Measures every background painting: source size, alpha coverage, skyline rows, edge wrap, and its
Unity import settings (from the .meta). Read-only. Writes Tools/Backgrounds/BACKGROUND_SOURCES.json.

  python Tools/Backgrounds/measure_backgrounds.py
"""
import glob, json, os, re
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Tools", "Backgrounds", "BACKGROUND_SOURCES.json")
FOLDERS = ["Assets/Art/Codex/Backgrounds", "Assets/Art/Backgrounds", "Assets/Resources/WorldBackground"]


def meta_settings(path):
    text = open(path + ".meta", encoding="utf-8", errors="ignore").read()
    def get(key):
        m = re.search(r"^\s*" + key + r":\s*(.+)$", text, re.M)
        return m.group(1).strip() if m else None
    plats = []
    for block in re.findall(r"- serializedVersion: \d+\n\s+buildTarget: (\w+)\n\s+maxTextureSize: (\d+)\n(?:.*\n){0,4}?\s+textureFormat: (-?\d+)\n\s+textureCompression: (\d+)\n\s+compressionQuality: (\d+)\n\s+crunchedCompression: (\d)\n\s+allowsAlphaSplitting: \d\n\s+overridden: (\d)", text):
        plats.append({"target": block[0], "max": int(block[1]), "format": int(block[2]), "compression": int(block[3]), "overridden": block[6] == "1"})
    return {"ppu": get("spritePixelsToUnits"), "filter": get("filterMode"), "mips": get("enableMipMap"),
            "type": get("textureType"), "wrapU": get("wrapU"), "maxDefault": next((p["max"] for p in plats if p["target"] == "DefaultTexturePlatform"), None),
            "compressionDefault": next((p["compression"] for p in plats if p["target"] == "DefaultTexturePlatform"), None),
            "npot": get("nPOTScale"), "platforms": [p for p in plats if p["overridden"]]}


def analyse(path):
    im = Image.open(path)
    w, h = im.size
    a = np.array(im.convert("RGBA"))
    alpha = a[:, :, 3]
    opaque = float((alpha > 127).mean())
    tops = []
    for x in range(0, w, max(1, w // 64)):
        col = np.nonzero(alpha[:, x] > 127)[0]
        tops.append(int(col[0]) if len(col) else h)
    edge = float(np.abs(a[:, 0, :3].astype(int) - a[:, -1, :3].astype(int)).mean())
    # Detail: mean absolute luminance gradient (a softness proxy), on opaque pixels.
    lum = a[:, :, :3].astype(float) @ [0.299, 0.587, 0.114]
    gx = np.abs(np.diff(lum, axis=1))[:, :] * (alpha[:, 1:] > 127)
    detail = float(gx.sum() / max(1, (alpha[:, 1:] > 127).sum()))
    return {"w": w, "h": h, "mode": im.mode, "opaque_share": round(opaque, 3), "skyline_rows_min_med_max": [min(tops), int(np.median(tops)), max(tops)],
            "edge_wrap_diff": round(edge, 2), "detail_gradient": round(detail, 2), "bytes": os.path.getsize(path)}


rows = []
for folder in FOLDERS:
    for p in sorted(glob.glob(os.path.join(ROOT, folder, "*.png"))):
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        r = {"path": rel, "name": os.path.splitext(os.path.basename(p))[0]}
        r.update(analyse(p))
        r["import"] = meta_settings(p)
        rows.append(r)
json.dump(rows, open(OUT, "w", encoding="utf-8"), indent=1)
for r in rows:
    i = r["import"]
    print(f"{r['name']:<34} {r['w']:>5}x{r['h']:<5} opaque {r['opaque_share']:.2f} sky {r['skyline_rows_min_med_max']} wrap {r['edge_wrap_diff']:>6} detail {r['detail_gradient']:>5}  ppu {i['ppu']} filt {i['filter']} mips {i['mips']} max {i['maxDefault']} comp {i['compressionDefault']} npot {i['npot']} ovr {[(x['target'], x['max']) for x in i['platforms']]}")

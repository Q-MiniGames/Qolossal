"""Seam proofs for every repeating accepted image the Mountain Relief Atlas prototype reuses.

Backgrounds and top strips (horizontal repeat): a 3x horizontal repeat. Fills (both axes): a 4x2
repeat. Each proof is written next to a JSON line with the measured opposite-edge RGBA difference
(mean over pixels where either edge is visible). Source bytes are only read.
Usage: python seam_proofs.py <out_folder>
"""
import json, os, sys
import numpy as np
from PIL import Image

P = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ART = os.path.join(P, "Assets", "Art", "Codex")
H_REPEAT = ["Backgrounds/BG_%s_%s" % (a, l) for a in ["A0", "A1", "A2", "A3", "A4", "A5", "A6"] for l in ["Mid", "Near"]] + \
    ["Backgrounds/BG_Qvale_Far", "Backgrounds/BG_Qvale_Mid", "Backgrounds/BG_Qvale_Near"] + \
    ["Backgrounds/BG_Sky_%s" % a for a in ["A0", "A1", "A2", "A3", "A4", "A5", "Terraces"]] + \
    ["Terrain/Body/Body_Top_Strip", "Terrain/Body/Causeway/Causeway_Top_Strip", "Terrain/Body/Terraces/Terraces_Top_Strip", "Terrain/Body/Cave_Edge_Strip",
     "Terrain/Body/Causeway/Causeway_Cave_Edge_Strip"] + ["Terrain/%s_Ground_Top" % a for a in ["A2", "A3", "A4", "A6"]]
FILLS = ["Terrain/Body/Body_Fill", "Terrain/Body/Causeway/Causeway_Fill", "Terrain/Body/Terraces/Terraces_Fill", "Terrain/Body/Cave_Fill",
         "Terrain/Body/Causeway/Causeway_Cave_Fill"] + ["Terrain/%s_Ground_Fill" % a for a in ["A2", "A3", "A4", "A6"]]


def edge_diff(a, axis):
    first, last = (a[:, 0], a[:, -1]) if axis == "x" else (a[0], a[-1])
    vis = (first[:, 3] > 8) | (last[:, 3] > 8)
    return float(np.abs(first.astype(int) - last.astype(int))[vis].mean()) if vis.any() else 0.0


def main(out):
    os.makedirs(out, exist_ok=True)
    rows = []
    for rel, kind in [(r, "3x") for r in H_REPEAT] + [(r, "4x2") for r in FILLS]:
        path = os.path.join(ART, rel + ".png")
        if not os.path.exists(path): rows.append({"file": rel, "missing": True}); continue
        im = Image.open(path).convert("RGBA"); a = np.asarray(im)
        nx, ny = (3, 1) if kind == "3x" else (4, 2)
        w, h = im.size
        sheet = Image.new("RGBA", (w * nx, h * ny), (128, 128, 128, 255))
        for j in range(ny):
            for i in range(nx): sheet.alpha_composite(im, (i * w, j * h))
        sheet.thumbnail((2400, 1200))
        name = rel.replace("/", "_") + "_" + kind + ".png"
        sheet.convert("RGB").save(os.path.join(out, name))
        row = {"file": rel, "proof": name, "edge_diff_x": round(edge_diff(a, "x"), 2)}
        if kind == "4x2": row["edge_diff_y"] = round(edge_diff(a, "y"), 2)
        rows.append(row)
    json.dump(rows, open(os.path.join(out, "SEAM_PROOFS.json"), "w"), indent=1)
    for r in rows: print(r)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(P, "Tools", "MountainReliefAtlas", "SeamProofs"))

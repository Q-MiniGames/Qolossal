"""Measure a terrain kit's pixel landmarks (the values TerrainKit needs) from its PNGs.

Rules follow the TerrainKit tooltips; all values are sprite pixels, x from the left edge
and y from the top edge. Writes Assets/Art/Codex/Terrain/<AREA>_TerrainKit.landmarks.json,
which the kit builder reads.

  python measure_terrain_kit.py A1 [A2 ...]      # measure and write
  python measure_terrain_kit.py --check A0       # print only (A0's are hand-set in TerrainKit)
"""
import json
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
TERRAIN = os.path.join(PROJECT, "Assets", "Art", "Codex", "Terrain")
SOLID = 128  # alpha counted as painted rock
# Walk lines sit a few px into the painted moss so feet sink into it rather than hover
# (calibrated on the hand-set A0 values: floating platforms 36/46/47, slope +8). The ledge
# corner gets none: from A1 on its walk line matches Ground_Top's exactly (Review 04).
FLOAT_SINK, SLOPE_SINK = 4, 8


def alpha(area, piece):
    with Image.open(os.path.join(TERRAIN, f"{area}_{piece}.png")) as im:
        return np.asarray(im.convert("RGBA"))[..., 3].astype(float)


def first_full_row(a, coverage=.97):
    rows = np.where((a > SOLID).mean(1) >= coverage)[0]
    return int(rows[0])


def last_full_row(a, coverage=.97):
    rows = np.where((a > SOLID).mean(1) >= coverage)[0]
    return int(rows[-1])


def right_face(a, rows=None):
    """5th percentile of each row's rightmost painted pixel (the innermost point of the outline)."""
    xs = [np.where(a[y] > SOLID)[0].max() for y in (rows if rows is not None else range(a.shape[0])) if (a[y] > SOLID).any()]
    return float(np.percentile(xs, 5))


def column_top(a, x):
    col = np.where(a[:, x] > SOLID)[0]
    return int(col[0]) if len(col) else None


def slope_surface(a, step=64, start=40):
    """Moss top: per-column first painted row, median over 80 px, SLOPE_SINK px into the moss,
    Gaussian smoothed (sigma 70 px), never more than 12 px above the painted art, and level
    after the crest (it never drops again, whatever the art does past the top)."""
    h, w = a.shape
    tops = np.array([column_top(a, x) if column_top(a, x) is not None else h for x in range(w)], float)
    half = 40
    med = np.array([np.median(tops[max(0, x - half):x + half]) for x in range(w)]) + SLOPE_SINK
    xs = np.arange(w)
    sigma = 70.0
    kernel = np.exp(-0.5 * (np.arange(-3 * int(sigma), 3 * int(sigma) + 1) / sigma) ** 2)
    kernel /= kernel.sum()
    padded = np.pad(med, 3 * int(sigma), mode="edge")
    smooth = np.convolve(padded, kernel, mode="valid")
    smooth = np.maximum(smooth, tops - 12)
    smooth = np.minimum.accumulate(smooth)   # y from the top: never lower than any point before it
    end = w - 44
    pts = [[start, h]]
    for x in list(range(start + step, end, step)) + [end]:
        pts.append([x, int(round(smooth[x]))])
    pts.append([end, h])
    return pts


def platform_line(a):
    """Walk line (first row mostly painted) and the usable span along that row."""
    cov = (a > SOLID).mean(1)
    width = (a > SOLID).any(0).sum()
    rows = np.where((a > SOLID).sum(1) >= .85 * width)[0]
    y = int(rows[0])
    band = a[y:y + 12] > SOLID
    cols = np.where(band.mean(0) > .9)[0]
    return int(cols[0]), int(cols[-1]), y


def measure(area):
    top = alpha(area, "Ground_Top")
    wall = alpha(area, "Wall_Side")
    ceiling = alpha(area, "Ceiling_Under")
    corner_top = alpha(area, "Corner_Outer_TopRight")
    corner_bottom = alpha(area, "Corner_Outer_BottomRight")
    ct_walk = first_full_row(corner_top[:, :int(corner_top.shape[1] * .6)])
    # Underside of the under-corner: last row that is mostly rock in a band just inside its face
    # (its far side may be notched or feathered, as in A1).
    face_guess = int(np.median([np.where(r > SOLID)[0].max() for r in corner_bottom if (r > SOLID).any()]))
    cb_under = last_full_row(corner_bottom[:, max(0, face_guess - 220):face_guess - 20], .7)
    one = alpha(area, "Platform_OneWay")
    ox0, ox1, oy = platform_line(one)
    floats = {}
    for size in ("Small", "Medium", "Large"):
        x0, x1, y = platform_line(alpha(area, "Platform_Floating_" + size[0]))
        floats[size] = [x0, x1, y + FLOAT_SINK]
    return {
        "area": area,
        "groundTopWalkLine": first_full_row(top),
        "wallSideFace": right_face(wall),
        "ceilingUnderside": last_full_row(ceiling),
        "cornerOuterTopRightLedge": [right_face(corner_top, range(ct_walk + 40, corner_top.shape[0])), ct_walk],
        "cornerOuterBottomRightEdge": [right_face(corner_bottom, range(int(cb_under * .3), cb_under)), cb_under],
        "slopeSurface": slope_surface(alpha(area, "Slope_30")),
        "oneWayWalkLine": oy,
        "oneWaySpan": [ox0, ox1],
        "floatingSmallLine": floats["Small"],
        "floatingMediumLine": floats["Medium"],
        "floatingLargeLine": floats["Large"],
    }


def main(argv):
    check = argv[:1] == ["--check"]
    for area in argv[1:] if check else argv:
        m = measure(area)
        if check:
            print(json.dumps(m))
            continue
        path = os.path.join(TERRAIN, f"{area}_TerrainKit.landmarks.json")
        with open(path, "w", encoding="utf-8") as f:
            json.dump(m, f, indent=1)
        print(f"{area}: wrote {os.path.relpath(path, PROJECT)}")


if __name__ == "__main__":
    main(sys.argv[1:] or ["--check", "A0"])

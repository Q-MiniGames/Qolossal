"""Draw every region's built route end to end, to scale: one zoomed-out map of the whole game.

Usage: python Tools/Design/whole_levels_map.py
Reads mra_world.json and mra_route.json (the data the Unity builder uses), joins each region's
entry to the previous region's exit, and writes QOLOSSAL_WHOLE_LEVELS_MAP_DESIGNER_SPOILERS.png
next to this script. MR08 (after the reveal) is drawn inside the body, heading back down to the heart.
"""
import json
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
MRA = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA")
OUT = os.path.join(HERE, "QOLOSSAL_WHOLE_LEVELS_MAP_DESIGNER_SPOILERS.png")

SS = 2            # supersampling
PX = 1.45         # pixels per unit (1 u = 1 m)
MARGIN_L, MARGIN_R, TOP, BOTTOM = 70, 90, 250, 260
BAND = [(214, 186, 140), (203, 190, 150), (186, 196, 132), (232, 178, 110),
        (168, 196, 150), (176, 192, 206), (222, 206, 176)]
GROUND = (196, 182, 150)
EDGE = (88, 110, 58)


def font(size, bold=False):
    name = "segoeuib.ttf" if bold else "segoeui.ttf"
    try:
        return ImageFont.truetype(os.path.join(os.environ.get("WINDIR", r"C:\Windows"), "Fonts", name), size * SS)
    except OSError:
        return ImageFont.load_default()


def main():
    world = json.load(open(os.path.join(MRA, "mra_world.json"), encoding="utf-8"))
    route = {r["id"]: r for r in json.load(open(os.path.join(MRA, "mra_route.json"), encoding="utf-8"))["regions"]}
    regions = [r for r in world["regions"] if r["id"] != "MR08"]
    chambers = world["chambers"]

    # Offsets: each region's entry sits on the previous region's exit. Each region is clipped to
    # entry..exit so the arrival and exit pads don't overlap their neighbours.
    offsets, ox, oy = {}, 0.0, 0.0
    for r in regions:
        nodes = {n["id"]: n["position"] for n in r["nodes"]}
        e, x = nodes[r["entry"]], nodes[r["exit"]]
        for p in route[r["id"]]["pieces"]:
            p["points"] = [q for q in p["points"] if e["x"] <= q["x"] <= x["x"]]
        offsets[r["id"]] = (ox - e["x"], oy - e["y"])
        ox, oy = ox - e["x"] + x["x"], oy - e["y"] + x["y"]
    xs, ys = [], []
    for r in regions:
        dx, dy = offsets[r["id"]]
        for p in route[r["id"]]["pieces"]:
            for q in p["points"]:
                xs.append(q["x"] + dx)
                ys.append(q["y"] + dy)
    minx, maxx, miny, maxy = min(xs), max(xs), min(ys), max(ys)
    W = int(MARGIN_L + MARGIN_R + (maxx - minx) * PX)
    H = int(TOP + BOTTOM + (maxy - miny) * PX)
    base = H - BOTTOM + 40

    def P(x, y):
        return ((MARGIN_L + (x - minx) * PX) * SS, (H - BOTTOM - (y - miny) * PX) * SS)

    im = Image.new("RGB", (W * SS, H * SS), (244, 232, 206))
    d = ImageDraw.Draw(im, "RGBA")
    # sky gradient
    for yy in range(0, H * SS, 4):
        t = yy / (H * SS)
        d.rectangle([0, yy, W * SS, yy + 4], fill=(int(246 - 18 * t), int(230 - 14 * t), int(200 - 10 * t)))

    # region bands
    bounds = {}
    for i, r in enumerate(regions):
        dx, dy = offsets[r["id"]]
        nodes = {n["id"]: n["position"] for n in r["nodes"]}
        x0, x1 = nodes[r["entry"]]["x"] + dx, nodes[r["exit"]]["x"] + dx
        bounds[r["id"]] = (x0, x1)
        a, b = P(x0, maxy + 60)[0], P(x1, 0)[0]
        d.rectangle([a, TOP * SS * 0.55, b, base * SS], fill=BAND[i] + (40,))
        d.line([a, TOP * SS * 0.55, a, base * SS], fill=(120, 100, 70, 120), width=SS)

    # terrain: fill under each piece, then the walk line
    for r in regions:
        dx, dy = offsets[r["id"]]
        for p in route[r["id"]]["pieces"]:
            pts = [P(q["x"] + dx, q["y"] + dy) for q in p["points"]]
            if len(pts) < 2:
                continue
            poly = pts + [(pts[-1][0], base * SS), (pts[0][0], base * SS)]
            d.polygon(poly, fill=GROUND)
    for r in regions:
        dx, dy = offsets[r["id"]]
        for p in route[r["id"]]["pieces"]:
            pts = [P(q["x"] + dx, q["y"] + dy) for q in p["points"]]
            if len(pts) >= 2:
                d.line(pts, fill=EDGE, width=4 * SS, joint="curve")

    # MR08, after the reveal: mirrored so it runs back left and down inside the body
    mr8 = next(r for r in world["regions"] if r["id"] == "MR08")
    n8 = {n["id"]: n["position"] for n in mr8["nodes"]}
    e8 = n8[mr8["entry"]]
    sx, sy = ox - 25, oy - 20
    for p in route["MR08"]["pieces"]:
        pts = [P(sx - (q["x"] - e8["x"]), sy + (q["y"] - e8["y"]) - 30) for q in p["points"]]
        d.line(pts, fill=(130, 50, 30, 170), width=3 * SS, joint="curve")
    endx = sx - (max(q["x"] for q in route["MR08"]["pieces"][0]["points"]) - e8["x"])
    hx, hy = P(endx, sy + (n8[mr8["exit"]]["y"] - e8["y"]) - 30)
    d.ellipse([hx - 14 * SS, hy - 14 * SS, hx + 14 * SS, hy + 14 * SS], fill=(181, 70, 46), outline=(110, 30, 20), width=2 * SS)
    f13, f15, f17, f20, f34 = font(15), font(19), font(21, True), font(24, True), font(40, True)
    d.text((hx - 60 * SS, hy + 26 * SS), "MR08 The Descent (after the reveal):\ninside, down to the heart", font=f15, fill=(110, 30, 20), anchor="la")

    # caves, drift levels, Qvale homes, knots and waymarks
    for r in regions:
        dx, dy = offsets[r["id"]]
        nodes = {n["id"]: n for n in r["nodes"]}
        doors = {dd["chamber"]: dd for dd in route[r["id"]]["doors"]}
        for c in (c for c in chambers if c["region"] == r["id"]):
            if c["id"] in doors:
                cx, cy = doors[c["id"]]["x"], doors[c["id"]]["y"]
            else:
                pos = nodes[c["entranceNode"]]["position"]
                cx, cy = pos["x"], pos["y"]
            x, y = P(cx + dx, cy + dy)
            if c["type"] == "drift-level":
                # a cluster of floating stones above the launch point
                for k, (ddx, ddy, w) in enumerate([(-30, 70, 22), (-5, 95, 18), (22, 118, 24), (0, 145, 20), (30, 165, 26)]):
                    a, b = P(cx + dx + ddx, cy + dy + ddy)
                    d.rounded_rectangle([a - w * SS, b - 4 * SS, a + w * SS, b + 4 * SS], radius=4 * SS, fill=(95, 160, 78), outline=(47, 95, 40), width=2 * SS)
                    d.polygon([(a - (w - 5) * SS, b + 4 * SS), (a, b + 12 * SS), (a + (w - 5) * SS, b + 4 * SS)], fill=(155, 143, 114))
                d.line([x, y - 6 * SS, *P(cx + dx - 30, cy + dy + 66)], fill=(47, 95, 40), width=2 * SS)
                lx, ly = P(cx + dx, cy + dy + 180)
                d.text((lx, ly), c["name"], font=f17, fill=(40, 85, 34), anchor="ms")
            elif c["presentation"] in ("house-instance", "in-place-cutaway"):
                d.polygon([(x - 8 * SS, y), (x - 8 * SS, y - 12 * SS), (x, y - 20 * SS), (x + 8 * SS, y - 12 * SS), (x + 8 * SS, y)], fill=(231, 162, 74), outline=(122, 78, 29))
            else:
                y += 14 * SS
                d.pieslice([x - 9 * SS, y - 9 * SS, x + 9 * SS, y + 9 * SS], 180, 360, fill=(200, 50, 42), outline=(110, 23, 18), width=2 * SS)
        for n in r["nodes"]:
            x, y = P(n["position"]["x"] + dx, n["position"]["y"] + dy)
            if n.get("knot"):
                d.regular_polygon((x, y - 26 * SS, 10 * SS), 4, fill=(240, 196, 70), outline=(120, 80, 10))
            elif n.get("waymark"):
                d.rectangle([x - 3 * SS, y - 26 * SS, x + 3 * SS, y - 6 * SS], fill=(47, 107, 138))
                d.ellipse([x - 6 * SS, y - 32 * SS, x + 6 * SS, y - 20 * SS], fill=(120, 190, 220), outline=(47, 107, 138), width=SS)

    # start
    x, y = P(offsets["MR01"][0] + 12, offsets["MR01"][1] + 22)
    d.text((x, y - 40 * SS), "START", font=f17, fill=(47, 79, 95), anchor="ls")

    # region labels
    for i, r in enumerate(regions):
        x0, x1 = bounds[r["id"]]
        cx = (P(x0, 0)[0] + P(x1, 0)[0]) / 2
        y = (base + 40 + (i % 2) * 70) * SS
        d.text((cx, y), f'{r["id"]} {r["name"]}', font=f20, fill=(43, 36, 24), anchor="ma")
        d.text((cx, y + 28 * SS), r.get("body", ""), font=f15, fill=(75, 64, 48), anchor="ma")

    # title, legend, scale bar
    d.text((40 * SS, 34 * SS), "Qolossal: every level, end to end, to scale", font=f34, fill=(43, 36, 24))
    d.text((40 * SS, 84 * SS), "Built route data (mra_route.json, reclining layout). DESIGNER SPOILERS: never shown in game.", font=f15, fill=(110, 23, 18))
    lx, ly = 40 * SS, 120 * SS
    d.line([lx, ly, lx + 40 * SS, ly], fill=EDGE, width=4 * SS)
    d.text((lx + 52 * SS, ly), "walk line (the ground Qori walks)", font=f15, fill=(43, 36, 24), anchor="lm")
    d.pieslice([lx + 11 * SS, ly + 22 * SS, lx + 29 * SS, ly + 40 * SS], 180, 360, fill=(200, 50, 42), outline=(110, 23, 18))
    d.text((lx + 52 * SS, ly + 28 * SS), "cave / side chamber", font=f15, fill=(43, 36, 24), anchor="lm")
    d.rounded_rectangle([lx, ly + 50 * SS, lx + 40 * SS, ly + 58 * SS], radius=4 * SS, fill=(95, 160, 78), outline=(47, 95, 40))
    d.text((lx + 52 * SS, ly + 54 * SS), "floating-platform level (optional)", font=f15, fill=(43, 36, 24), anchor="lm")
    lx2 = lx + 360 * SS
    d.regular_polygon((lx2 + 20 * SS, ly, 10 * SS), 4, fill=(240, 196, 70), outline=(120, 80, 10))
    d.text((lx2 + 52 * SS, ly), "knot", font=f15, fill=(43, 36, 24), anchor="lm")
    d.ellipse([lx2 + 14 * SS, ly + 22 * SS, lx2 + 26 * SS, ly + 34 * SS], fill=(120, 190, 220), outline=(47, 107, 138))
    d.text((lx2 + 52 * SS, ly + 28 * SS), "Waymark", font=f15, fill=(43, 36, 24), anchor="lm")
    d.polygon([(lx2 + 12 * SS, ly + 62 * SS), (lx2 + 12 * SS, ly + 50 * SS), (lx2 + 20 * SS, ly + 42 * SS), (lx2 + 28 * SS, ly + 50 * SS), (lx2 + 28 * SS, ly + 62 * SS)], fill=(231, 162, 74), outline=(122, 78, 29))
    d.text((lx2 + 52 * SS, ly + 54 * SS), "Qvale home", font=f15, fill=(43, 36, 24), anchor="lm")
    sx0, sy0 = (W - MARGIN_R - 100 * PX - 20) * SS, 120 * SS
    d.line([sx0, sy0, sx0 + 100 * PX * SS, sy0], fill=(43, 36, 24), width=3 * SS)
    for t in (0, 50, 100):
        d.line([sx0 + t * PX * SS, sy0 - 6 * SS, sx0 + t * PX * SS, sy0 + 6 * SS], fill=(43, 36, 24), width=2 * SS)
    d.text((sx0 + 50 * PX * SS, sy0 + 12 * SS), "100 m", font=f15, fill=(43, 36, 24), anchor="ma")

    im = im.resize((W, H), Image.LANCZOS)
    im.save(OUT, optimize=True)
    total = sum(r["size"]["x"] for r in regions)
    print(OUT, im.size, "route x span", round(maxx - minx), "height span", round(maxy - miny), "sum of region widths", total)


if __name__ == "__main__":
    main()

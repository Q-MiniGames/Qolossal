"""The reclining titan route (Tools/Design/QOLOSSAL_RECLINING_TITAN_PROPOSAL.md, approved 8 Oct 2026).

Applied to the converted world data (mra_world.json) after convert_spec.py, so a re-conversion of the
design package keeps it. It changes only what the reclining body needs:

  region-to-body  each region and chamber gets a designer-only `body` place (never shown in game:
                  the titan stays a mystery until the Summit reveal);
  MR03            the knee crest stays at the Terraces knot (the mill on the kneecap); the last
                  three beats now run DOWN the thigh into Qvale (a long descent, region 45 u longer);
  MR05            the belly: an open, gentle slope for the first three beats, then the rib climb as
                  before (MR05 keeps its length, the rest is 18 u lower);
  MR07            the face: the Prow (a long bare ridge, the nose) after the pale steps; the rest
                  of the Summit in its old order, 30 u further on;
  MR06            the neck dip: after the shoulder the route falls into a sheltered dip, the glide
                  span crosses it, and the region ends low, where the Summit's climb (the chin) begins;
  P1-P4           four optional floating-platform levels (drift stones on the warm air), each its own
                  scene entered from a launch point on the main route: over the shin (MR02), over
                  Qvale (MR03's town overlook), over the chest (MR05) and up the face (MR07).

Beats are moved to absolute positions, so the step is idempotent. Edges touching a moved beat get new
surface points (stairs as the package drew them, or a straight walk), and everything placed relative
to a moved beat (cave mouths, return spots, encounter centres, modules, region links) moves with it.
The Chart's relief pages and chart UVs are unchanged (they are review-only drawings: see the handoff).

Usage: python reclining.py   (rewrites Assets/MountainReliefAtlas/Resources/MRA/mra_world.json)
"""
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
WORLD = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "mra_world.json")

LAYOUT = "reclining-titan-2026-10-08"

# Designer-only body places (spoilers): never shown to players.
BODY = {
    "MR01": "feet: toe tips, top of the foot, ankle",
    "MR02": "shin",
    "MR03": "knee (Mill Ridge on the kneecap), then down the thigh",
    "MR04": "lap",
    "MR05": "belly, then between the ribs",
    "MR06": "collarbone, shoulder, then the neck dip",
    "MR07": "face: chin, mouth, nose, closed eye, brow, forehead",
    "MR08": "inside: throat down to the heart",
}
CHAMBER_BODY = {
    "MR01_C01": "between the toes", "MR01_C02": "between the toes", "MR01_C03": "under a toenail",
    "MR01_C04": "the ankle hollow", "MR01_C05": "the heel", "MR01_C06": "the heel",
    "MR02_C01": "under the shin bone's edge", "MR03_C04": "inside the kneecap",
    "MR03_C06": "the Under-Arch, under the raised knee", "MR05_C03": "the Navel Well",
    "MR05_C05": "over the heart", "MR06_C01": "the armpit hollow", "MR06_C02": "the collarbone hollow",
    "MR07_C01": "in the lashes", "MR07_C02": "the mouth corner", "MR07_C03": "behind the teeth",
    "MR07_C05": "the ear",
}

MOVES = {
    # MR03: crest at the knot (258, 158); then down the thigh.
    "MR03_N09": (300.0, 140.0), "MR03_N10": (350.0, 112.0), "MR03_N11": (400.0, 92.0),
    # MR05: the belly's open slope, then the ribs 18 u lower than before.
    "MR05_N01": (48.0, 26.0), "MR05_N02": (82.0, 38.0), "MR05_N03": (118.0, 52.0),
    "MR05_N04": (153.0, 76.0), "MR05_N05": (187.0, 98.0), "MR05_N06": (222.0, 121.0),
    "MR05_N07": (256.0, 140.0), "MR05_N08": (288.0, 152.0), "MR05_N09": (321.0, 175.0),
    "MR05_N10": (362.0, 190.0), "MR05_N11": (402.0, 194.0),
    # MR06: over the shoulder, down into the neck (the exit is the glide span's moved beat).
    "MR06_N09": (350.0, 232.0), "MR06_N10": (390.0, 224.0), "MR06_N11": (440.0, 202.0),
    # MR07, the face: after the pale steps (the lips) a long bare ridge, the Prow (the nose), then the
    # reeds (lashes), the lake and its seam (the closed eye), the brow, the knot (forehead) and the
    # reveal at the crown, in their old order, each 30 u further on.
    **{f"MR07_N{i:02d}": (x + 30.0, y) for i, (x, y) in enumerate(
        [(101.0, 82.0), (133.0, 99.0), (164.0, 112.0), (195.0, 134.0), (222.0, 151.0), (248.0, 163.0),
         (275.0, 169.0), (301.0, 172.0), (325.0, 148.0)], start=3)},
}
REGION_SIZE = {"MR03": {"size": (425.0, 210.0), "cameraMax": (437.0, 228.0)},
               "MR07": {"size": (380.0, 200.0), "cameraMax": (392.0, 218.0)}}

# The floating-platform levels: placeholder layouts in accepted art (each kit's floating and moving
# platforms). Platforms are [left, top, width, depth]; movers [left, top, dx, dy] (3 u wide);
# crumbles [left, top, width, depth]; rests are where a fall returns Qori to.
DRIFT = [
    {
        "id": "MR02_P1", "name": "Stepping Stones", "region": "MR02", "node": "MR02_N09", "prerequisite": "",
        "reward": "song-shell", "body": "drift stones over the shin", "size": (96.0, 30.0),
        "objective": "Stones drift on the warm air. Hop from one to the next; the thread reaches the top shelf.",
        "platforms": [(0, 2, 8, 4), (10.5, 3.2, 3.5, .8), (16, 4.4, 3, .8), (21.5, 5.4, 3, .8), (27, 6.2, 4, 1),
                      (44, 7.2, 4, 1), (61.5, 10.2, 5, 1), (74.5, 14.4, 4, 1), (82, 19, 10, 1.2)],
        "movers": [(33, 6.2, 6, 0), (69, 10.2, 0, 4)],
        "crumbles": [(50.5, 8.2, 3, .6), (56, 9.2, 3, .6)],
        "anchors": [(80.5, 21.4)], "pods": [],
        "rests": [(4, 3), (29, 7.2), (63.5, 11.2), (76.5, 15.4)], "rewardAt": (88, 20),
    },
    {
        "id": "MR03_P2", "name": "Lantern Drift", "region": "MR03", "node": "MR03_N10", "prerequisite": "bloomfall",
        "reward": "heart-seed", "body": "drift stones over Qvale, between knee and belly", "size": (86.0, 28.0),
        "objective": "Seed pods hang in the drift. Strike down on them to bounce higher.",
        "platforms": [(0, 2, 8, 4), (11, 3.4, 3, .8), (19.5, 6, 3, .8), (32, 10, 4, 1), (50, 11, 3, .8),
                      (64, 15, 4, 1), (76, 19.2, 8, 1.2)],
        "movers": [(38.5, 10, 6, 0), (70.5, 15, 0, 4)],
        "crumbles": [(55.5, 12, 3, .6)],
        "anchors": [], "pods": [(16, 3.5), (24.5, 6.2), (28.5, 7.8), (60.5, 12.4)],
        "rests": [(4, 3), (34, 11), (66, 16)], "rewardAt": (80, 20.2),
    },
    {
        "id": "MR05_P3", "name": "The Swell Isles", "region": "MR05", "node": "MR05_N09", "prerequisite": "wind-leaf",
        "reward": "rare-find", "body": "drift stones over the chest (they rise and fall with the swell)", "size": (96.0, 24.0),
        "objective": "The isles rise and fall slowly. Dash across the wide gaps.",
        "platforms": [(0, 2, 8, 4), (13, 2.6, 3.5, .8), (30, 4, 3.5, .8), (46, 6, 4, 1), (64, 7.5, 3.5, .8),
                      (86, 9.5, 8, 1.2)],
        "movers": [(21.5, 3, 0, 1.4), (55.5, 6.4, 0, 1.4), (72.5, 8.5, 6, 0)],
        "crumbles": [(38.5, 5, 3, .6)],
        "anchors": [], "pods": [],
        "rests": [(4, 3), (48, 7), (66, 8.5)], "rewardAt": (90, 10.5),
    },
    {
        "id": "MR07_P4", "name": "Updraft Stair", "region": "MR07", "node": "MR07_N04", "prerequisite": "glidecap",
        "reward": "lore-stone", "body": "drift stones rising beside the face (the air comes in long slow gusts)", "size": (94.0, 30.0),
        "objective": "Ride the rising stones, then glide across to the next.",
        "platforms": [(0, 2, 8, 4), (22, 6, 4, 1), (40, 10, 4, 1), (52, 12, 4, 1), (70, 16, 4, 1), (84.5, 21, 8, 1.2)],
        "movers": [(9.5, 2, 0, 5), (28, 6, 0, 5), (58, 12, 0, 5), (76, 16, 0, 5)],
        "crumbles": [(46.5, 11, 3, .6)],
        "anchors": [], "pods": [],
        "rests": [(4, 3), (24, 7), (54, 13), (72, 17)], "rewardAt": (88, 22),
    },
]


def v2(x, y):
    return {"x": round(float(x), 3), "y": round(float(y), 3)}


def v4(t):
    return {"x": float(t[0]), "y": float(t[1]), "z": float(t[2]), "w": float(t[3])}


def stairs(a, b, rise=1.2):
    """The package's stone steps from a to b: a tread, then a riser, each riser at most `rise`."""
    dy = b[1] - a[1]
    n = max(1, int(math.ceil(abs(dy) / rise - 1e-4)))
    pts, y = [v2(*a)], a[1]
    for i in range(1, n + 1):
        x = a[0] + (b[0] - a[0]) * i / n
        pts.append(v2(x, y))
        y = a[1] + dy * i / n
        pts.append(v2(x, y))
    return pts


def lerp_delta(x, ax, adx, bx, bdx):
    t = 0.0 if abs(bx - ax) < 1e-6 else min(1.0, max(0.0, (x - ax) / (bx - ax)))
    return adx + (bdx - adx) * t


def apply(world):
    world["layout"] = LAYOUT
    nodes = {n["id"]: (r, n) for r in world["regions"] for n in r["nodes"]}
    deltas = {}
    for nid, (x, y) in MOVES.items():
        r, n = nodes[nid]
        dx, dy = x - n["position"]["x"], y - n["position"]["y"]
        deltas[nid] = (dx, dy)
        off = (n["spawn"]["x"] - n["position"]["x"], n["spawn"]["y"] - n["position"]["y"])
        n["position"] = v2(x, y)
        n["spawn"] = v2(x + off[0], y + off[1])

    for r in world["regions"]:
        r["body"] = BODY[r["id"]]
        if r["id"] in REGION_SIZE:
            for k, v in REGION_SIZE[r["id"]].items():
                r[k] = v2(*v)
        pos = {n["id"]: (n["position"]["x"], n["position"]["y"]) for n in r["nodes"]}
        for e in r["edges"]:
            da, db = deltas.get(e["source"]), deltas.get(e["target"])
            if da is None and db is None:
                continue
            da, db = da or (0.0, 0.0), db or (0.0, 0.0)
            a, b = pos[e["source"]], pos[e["target"]]
            if any(abs(c) > 1e-6 for c in da + db):
                e["points"] = stairs(a, b) if e["surfaceKind"] == "stone-steps" else [v2(*a), v2(*b)]
            m = e["module"]
            # Things placed along the edge keep their place between its (old) ends.
            a0, b0 = a[0] - da[0], b[0] - db[0]
            def shift(p):
                return v2(p["x"] + lerp_delta(p["x"], a0, da[0], b0, db[0]), p["y"] + lerp_delta(p["x"], a0, da[1], b0, db[1]))
            if m.get("template") and "position" in m:
                m["position"] = shift(m["position"])
            for enc in world["encounters"]:
                if enc["edge"] == e["id"]:
                    enc["center"] = shift(enc["center"])

    for c in world["chambers"]:
        if c["id"] in CHAMBER_BODY:
            c["body"] = CHAMBER_BODY[c["id"]]
        d = deltas.get(c["entranceNode"])
        if d:
            for k in ("entrance", "returnSpawn"):
                c[k] = v2(c[k]["x"] + d[0], c[k]["y"] + d[1])
    for l in world["links"]:
        if l["target"] in deltas:
            d = deltas[l["target"]]
            l["arrival"] = v2(l["arrival"]["x"] + d[0], l["arrival"]["y"] + d[1])
        if l["source"] in deltas:
            d = deltas[l["source"]]
            l["reverseArrival"] = v2(l["reverseArrival"]["x"] + d[0], l["reverseArrival"]["y"] + d[1])

    # The floating-platform levels, as side chambers of type drift-level (replaced if present).
    world["chambers"] = [c for c in world["chambers"] if c.get("type") != "drift-level"]
    for p in DRIFT:
        r, n = nodes[p["node"]]
        x, y = n["position"]["x"], n["position"]["y"]
        uv = n["chartUv"]
        world["chambers"].append({
            "id": p["id"], "name": p["name"], "region": p["region"], "type": "drift-level",
            "reward": p["reward"], "rewardId": f"mra:{p['id']}:reward", "entranceNode": p["node"],
            "entrance": v2(x, y), "prerequisite": p["prerequisite"], "presentation": "drift-level",
            "scene": "MRAtlas_" + p["id"], "playerSpawn": v2(3.0, 3.0), "returnId": f"mra:{p['id']}:return",
            "returnSpawn": v2(x, y + 1.0),
            "chartUv": v2(uv["x"], min(0.98, uv["y"] + 0.05)), "size": v2(*p["size"]),
            "objective": p["objective"], "solution": "Placeholder layout (accepted floating and moving platform art) until the drift stone kit.",
            "platforms": [v4(t) for t in p["platforms"]], "walls": [], "gates": [], "water": [], "hazards": [],
            "catchFloor": [], "switches": [], "anchors": [v2(*a) for a in p["anchors"]], "pods": [v2(*a) for a in p["pods"]],
            "plate": [], "crate": [], "lever": [], "resident": [], "rewardAt": [v2(*p["rewardAt"])],
            "movers": [v4(t) for t in p["movers"]], "crumbles": [v4(t) for t in p["crumbles"]],
            "rests": [v2(*a) for a in p["rests"]], "body": p["body"],
        })
        if p["id"] not in r["chamberIds"]:
            r["chamberIds"].append(p["id"])
    return world


def main():
    with open(WORLD, encoding="utf-8") as f:
        world = json.load(f)
    apply(world)
    with open(WORLD, "w", encoding="utf-8", newline="\n") as f:
        json.dump(world, f, indent=1, ensure_ascii=False)
        f.write("\n")
    print(f"reclining layout applied: {len(MOVES)} beats moved, {len(DRIFT)} drift levels -> {os.path.relpath(WORLD, PROJECT)}")


if __name__ == "__main__":
    main()

"""Design the Mountain Relief Atlas walk lines: broad terraces, ramps, jumps and climbs.

The atlas package gives each region 12 beats (positions and safe-pad widths) and the edges between
them, with surface points that were mostly tiny generated stairs (about 1 u treads, 1.2 u risers). This
tool keeps the beats where the package puts them (the Chart's correspondence is measured there) and
designs the ground between them. It writes Assets/MountainReliefAtlas/Resources/MRA/mra_route.json,
which the Unity builder (MraWorldBuilder) reads, plus a side-view plot per region for review.

Design rules, checked against the live Player prefab's numbers (PlayerMovement and Player.prefab):
  jump apex  12^2 / (2 * 9.81 * 3) = 2.45 u; the collider is about 0.8 x 1.2 u;
  ledge grab (no relic): a ledge up to about 3.4 u above the takeoff is caught by a jump;
  walkable slope: rise/run up to 1.2 (we keep ramps at or under 0.85 so the top strip reads well);
  Climbing Moss (relic, MR01 N06): wall slide and wall jumps, so a clingable wall of any height
  can be climbed by jumping straight up it.
  beat pads    at least 6 u wide (a chamber door's beat: 11 u, with the cave set into the wall after it);
  terraces     treads of about 4-7 u; combat shelves 11 u, kept away from beats, doors and modules;
  features     'step' up to 1.9 u (a plain jump), 'ledge' 1.9-3.2 u (a jump and a ledge grab),
               'climb' 3.2-6.5 u (Climbing Moss, only once Qori has it), 'ramp' a walkable slope;
  gentle edges (under ~0.4) become one continuous eased slope with a crest into each pad;
  every non-vertical corner is rounded, so a slope meets a terrace in a continuous crest.
Each connector picks a style (walk, jumps, climb, mixed) from a short rotation so ascents differ.
The modules keep the geometry the play test validated (quake crossing, thread lift, sluice, glide span).

Deterministic: the same inputs give byte-identical output.

Usage: python design_route.py [--plots]
"""
import hashlib
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
WORLD = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "mra_world.json")
OUT = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "mra_route.json")
PLOTS = os.path.join(HERE, "QA", "route_design")

MOVED = {"MR06_N11": (440.0, 270.0)}       # the glide span's exit beat (see the handoff)
GLIDE_GAP, GLIDE_DROP, LIFT_HEIGHT = 18.0, 3.0, 5.0

PAD_MIN = 6.0                              # a beat's pad
DOOR_PAD_LEFT, DOOR_PAD_RIGHT = 3.0, 8.5   # a door beat's pad around the beat
# Resting places that need open, level ground: (left of the beat, right of the beat). Qvale's
# listening tree is a peaceful overlook: the bench and tree stand on a broad shelf, the climb on
# beyond it (user's decision, 4 Oct 2026).
RESTING = {"MR04_N09": (4.0, 15.0)}
SHELF = 11.0                               # a combat shelf
TREAD_MIN, TREAD_TARGET, TREAD_MAX = 4.0, 5.5, 7.5
RAMP_SLOPE = 0.8
GENTLE = 0.40                              # at or under this gradient: one eased slope
KINDS = {                                  # wall rise range, nominal rise
    "step": (0.7, 1.9, 1.5),
    "ledge": (1.9, 3.2, 2.7),
    "climb": (3.2, 7.0, 4.6),
}
# Each style: the walls in turn, and the treads' slopes in turn (0 = a level terrace to rest on).
STYLES = {
    "walk": (["step", "step", "ledge", "step"], [0.45, 0.0, 0.5, 0.35, 0.0, 0.55]),
    "terraces": (["ledge", "step", "ledge", "ledge"], [0.0, 0.25, 0.0, 0.0, 0.3]),
    "climb": (["climb", "ledge", "climb", "step"], [0.0, 0.4, 0.0, 0.3, 0.0]),
    "mixed": (["ledge", "climb", "step", "ledge"], [0.35, 0.0, 0.5, 0.0, 0.2]),
}
TREAD_RHYTHM = [1.0, 1.22, 0.86, 1.1, 0.8, 1.15, 0.92]
TREAD_SLOPE_MAX = 0.6


def r3(v):
    return round(v + 0.0, 3)


def stable(text):
    return int(hashlib.sha256(text.encode()).hexdigest()[:8], 16)


# ---------------------------------------------------------------------------------------- shapes

def eased(x0, y0, x1, y1, step=1.0):
    """One continuous slope with level ends (smoothstep): a crest into each neighbour."""
    n = max(2, int(math.ceil((x1 - x0) / step)))
    pts = []
    for i in range(n + 1):
        t = i / n
        s = t * t * (3 - 2 * t)
        pts.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * s))
    return pts


def plan_terraces(L, A, style_order, moss, first=None):
    """An ascent of A over L (relative, from (0,0) to (L,A)): k walls and k+1 treads (the two end
    treads are half-width aprons beside the pads). Treads may be gentle walkable slopes.
    Returns (points, features, note) or None."""
    best = None
    for style in style_order:
        walls, slopes = STYLES[style]
        if not moss:
            walls = [k if k != "climb" else "ledge" for k in walls]
        for k in range(1, 30):
            kinds = [walls[i % len(walls)] for i in range(k)]
            if first:
                kinds[0] = first
            weights = [0.5] + [TREAD_RHYTHM[i % len(TREAD_RHYTHM)] for i in range(k - 1)] + [0.5]
            widths = [L * wt / sum(weights) for wt in weights]
            tslopes = [slopes[i % len(slopes)] for i in range(k + 1)]
            tslopes[0] = tslopes[-1] = 0.0                     # level beside the pads
            tread_rise = sum(wd * sl for wd, sl in zip(widths, tslopes))
            wall_total = A - tread_rise
            if wall_total < 0:
                # Too gentle for these walls: scale the tread slopes down instead.
                if tread_rise <= 0:
                    continue
                tslopes = [sl * A / tread_rise for sl in tslopes]
                tread_rise, wall_total = A, 0.0
            nominal = [KINDS[kd][2] for kd in kinds]
            rises = [n * wall_total / sum(nominal) for n in nominal]
            ok = all(KINDS[kd][0] * 0.85 <= r <= KINDS[kd][1] for kd, r in zip(kinds, rises)) or wall_total == 0.0
            if not ok:
                continue
            w = L / k
            score = abs(w - TREAD_TARGET) * 0.6
            if w < TREAD_MIN:
                score += 10 + (TREAD_MIN - w) * 10
            if w > TREAD_MAX:
                score += 2 + (w - TREAD_MAX)
            score += style_order.index(style) * 0.6
            score += sum(0.4 for kd in kinds if kd == "climb") / max(1, k) * 2   # climbs are selective
            if best is None or score < best[0]:
                best = (score, kinds, rises, widths, tslopes, style, w)
    if best is None:
        return None
    score, kinds, rises, widths, tslopes, style, w = best
    pts, feats = [(0.0, 0.0)], []
    x = y = 0.0
    for i, (kd, r) in enumerate(zip(kinds, rises)):
        x += widths[i]
        y += widths[i] * tslopes[i]
        pts.append((x, y))
        if r > 1e-3:
            feats.append({"kind": kd, "x": x, "y": y, "rise": r, "run": 0.0})
            y += r
            pts.append((x, y))
    x += widths[-1]
    pts.append((L, A))
    yy = 0.0
    for i, sl in enumerate(tslopes):
        if sl > 0.12:
            feats.append({"kind": "ramp", "x": sum(widths[:i]), "y": yy, "rise": widths[i] * sl, "run": widths[i]})
        yy += widths[i] * sl + (rises[i] if i < len(rises) else 0.0)
    return pts, feats, {"style": style, "tread": w, "score": score}


def short_run(L, A, moss):
    """A run too short for terraces: a steep walkable scramble, a scramble with one wall in it, or
    one wall with level ground either side, whichever fits first."""
    if A / L <= 0.9:
        return [(0.0, 0.0), (L, A)], [{"kind": "ramp", "x": 0.0, "y": 0.0, "rise": A, "run": L}], {"style": "scramble", "tread": L, "score": 5.0}
    top = 9.0 if moss else 3.2
    for kind in ("step", "ledge", "climb"):
        lo, hi, _ = KINDS[kind]
        hi = top if kind == "climb" else hi
        wall = A - 0.75 * L
        if lo <= wall <= hi and (kind != "climb" or moss):
            half, y_half = L * 0.5, 0.75 * L * 0.5
            return ([(0.0, 0.0), (half, y_half), (half, y_half + wall), (L, A)],
                    [{"kind": "ramp", "x": 0.0, "y": 0.0, "rise": y_half, "run": half},
                     {"kind": kind, "x": half, "y": y_half, "rise": wall, "run": 0.0},
                     {"kind": "ramp", "x": half, "y": y_half + wall, "rise": y_half, "run": half}],
                    {"style": "scramble+" + kind, "tread": half, "score": 6.0})
    if A <= top:
        return ([(0.0, 0.0), (L * 0.5, 0.0), (L * 0.5, A), (L, A)],
                [{"kind": classify(A, moss), "x": L * 0.5, "y": 0.0, "rise": A, "run": 0.0}],
                {"style": "one wall", "tread": L * 0.5, "score": 7.0})
    return None


def fallback(L, A, moss):
    """No style fits (a short run): the fewest walls that do, evenly spaced."""
    top = 6.0 if moss else 3.2
    n = max(1, int(math.ceil(A / top - 1e-6)))
    h = A / n
    pts, feats, x, y = [(0.0, 0.0)], [], 0.0, 0.0
    w = L / (n + 1)
    for i in range(n):
        x += w if i == 0 else w
        pts.append((x, y))
        feats.append({"kind": classify(h, moss), "x": x, "y": y, "rise": h, "run": 0.0})
        y += h
        pts.append((x, y))
    pts.append((L, A))
    return pts, feats, {"style": "fallback", "tread": w, "score": 99.0}


def connector(x0, y0, x1, y1, ctx, first=None):
    """Ground from (x0, y0) to (x1, y1), exclusive of the start point. Returns (points, features, note)."""
    L, H = x1 - x0, y1 - y0
    if L < 0.05:
        return [(x1, y1)], [{"kind": classify(abs(H), ctx["moss"]), "x": x0, "y": min(y0, y1), "rise": abs(H), "run": 0.0}], {"style": "riser"}
    A = abs(H)
    if A < 1e-3:
        return [(x1, y1)], [], {"style": "level"}
    if A / L <= GENTLE and not first:
        return eased(x0, y0, x1, y1)[1:], [{"kind": "slope", "x": x0, "y": min(y0, y1), "rise": A, "run": L}], {"style": "slope", "grade": A / L}
    order = ctx["styles"]
    plan = plan_terraces(L, A, order, ctx["moss"], first if H > 0 else None)
    if plan is None or plan[2]["tread"] < TREAD_MIN:
        plan = short_run(L, A, ctx["moss"]) or plan or fallback(L, A, ctx["moss"])
    rel, feats, note = plan
    if H > 0:
        pts = [(x0 + px, y0 + py) for px, py in rel]
        for fe in feats:
            fe["x"] += x0
            fe["y"] += y0
    else:
        # A descent: the mirror image of an ascent planned from the right.
        pts = [(x0 + (L - px), y1 + py) for px, py in reversed(rel)]
        for fe in feats:
            fe["x"] = x0 + (L - fe["x"] - fe["run"])
            fe["y"] = y1 + fe["y"]
            fe["descent"] = True
    return pts[1:], feats, note


def classify(h, moss):
    if h <= 1.9:
        return "step"
    if h <= 3.2:
        return "ledge"
    return "climb" if moss else "too-high"



def fillet(points, radius=1.1, samples=5):
    """Rounds every corner between two non-vertical segments (a slope into a tread: a crest)."""
    out = [points[0]]
    for i in range(1, len(points) - 1):
        a, b, c = points[i - 1], points[i], points[i + 1]
        ab, bc = (b[0] - a[0], b[1] - a[1]), (c[0] - b[0], c[1] - b[1])
        if abs(ab[0]) < 1e-4 or abs(bc[0]) < 1e-4:
            out.append(b)
            continue
        la, lc = math.hypot(*ab), math.hypot(*bc)
        sa, sc = ab[1] / ab[0], bc[1] / bc[0]
        if abs(math.atan(sa) - math.atan(sc)) < math.radians(3):
            out.append(b)
            continue
        r = min(radius, la * 0.45, lc * 0.45)
        p0 = (b[0] - ab[0] / la * r, b[1] - ab[1] / la * r)
        p2 = (b[0] + bc[0] / lc * r, b[1] + bc[1] / lc * r)
        for s in range(samples + 1):
            t = s / samples
            out.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * b[0] + t * t * p2[0],
                        (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * b[1] + t * t * p2[1]))
    out.append(points[-1])
    return out


def merge_narrow(points, moss_x, keep=(), min_tread=3.7):
    """A tread narrower than `min_tread` between two risers going the same way is folded into one
    taller wall (within what can be climbed there), so no terrace is a sliver."""
    pts = list(points)
    changed = True
    while changed:
        changed = False
        risers = [i for i in range(len(pts) - 1) if abs(pts[i + 1][0] - pts[i][0]) < 1e-4 and abs(pts[i + 1][1] - pts[i][1]) > 1e-3]
        for a, b in zip(risers, risers[1:]):
            x1, x2 = pts[a][0], pts[b][0]
            up1, up2 = pts[a + 1][1] > pts[a][1], pts[b + 1][1] > pts[b][1]
            if x2 - x1 >= min_tread or up1 != up2:
                continue
            top = (8.5 if up1 else 9.5) if x1 > moss_x else 3.2
            h = abs(pts[b + 1][1] - pts[a][1])
            if h > top:
                continue
            if up1 and any(abs(x1 - k) < 1e-3 for k in keep):   # a cave's wall stays put: one wall there
                new = [(x1, pts[a][1]), (x1, pts[b + 1][1])]
            elif up1:   # the lower tread runs on to x2, then one wall
                new = [(x2, pts[a][1]), (x2, pts[b + 1][1])]
            else:     # going down: one drop at x1, the lower tread from there
                new = [(x1, pts[a][1]), (x1, pts[b + 1][1])]
            pts = pts[:a] + new + pts[b + 2:]
            changed = True
            break
    return pts


def clean(points):
    pts = []
    for p in points:
        if not pts or (abs(pts[-1][0] - p[0]) > 1e-4 or abs(pts[-1][1] - p[1]) > 1e-4):
            pts.append(p)
    i = len(pts) - 2
    while i >= 1:
        a, b, c = pts[i - 1], pts[i], pts[i + 1]
        cross = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
        dot = (b[0] - a[0]) * (c[0] - b[0]) + (b[1] - a[1]) * (c[1] - b[1])
        if abs(cross) < 1e-5 and dot > 0:
            pts.pop(i)
        i -= 1
    return pts


# ---------------------------------------------------------------------------------------- regions

def pos(n):
    return MOVED.get(n["id"], (n["position"]["x"], n["position"]["y"]))


def design_region(world, r):
    nodes = {n["id"]: n for n in r["nodes"]}
    order = [n["id"] for n in r["nodes"]]
    chambers = [c for c in world["chambers"] if c["region"] == r["id"] and c["presentation"] != "in-place-interior" and r["id"] != "MR04"]
    door_nodes = {c["entranceNode"]: c for c in chambers}
    moss_x = pos(nodes["MR01_N06"])[0] if r["id"] == "MR01" else -1e9
    encounters = {e["edge"]: e for e in world["encounters"] if e["region"] == r["id"]}

    # Spans: each beat's pad, then per edge either a module's fixed ground or connectors.
    def pad(nid):
        n = nodes[nid]
        x, y = pos(n)
        if nid in RESTING:
            return x - RESTING[nid][0], x + RESTING[nid][1], y
        if nid in door_nodes:
            return x - DOOR_PAD_LEFT, x + DOOR_PAD_RIGHT, y
        hw = max(n["padWidth"], PAD_MIN) * 0.5
        return x - hw, x + hw, y

    pieces = [{"points": [], "clingable": True, "label": None}]
    feats, notes, shelves, doors = [], [], [], []

    def add(pts):
        cur = pieces[-1]["points"]
        for p in pts:
            if not cur or abs(cur[-1][0] - p[0]) > 1e-4 or abs(cur[-1][1] - p[1]) > 1e-4:
                cur.append((p[0], p[1]))

    def gap(clingable=True, label=None):
        pieces.append({"points": [], "clingable": clingable, "label": label})

    def ctx_at(x, eid):
        rot = ["walk", "terraces", "climb", "mixed"]
        start = stable(eid) % len(rot)
        styles = rot[start:] + rot[:start]
        if r["id"] in ("MR04",):
            styles = ["walk", "terraces"]
        return {"moss": x > moss_x, "styles": styles}

    def connect(x0, y0, x1, y1, eid, first=None):
        pts, fs, note = connector(x0, y0, x1, y1, ctx_at(x0, eid), first)
        note.update({"edge": eid, "x0": r3(x0), "x1": r3(x1), "rise": r3(y1 - y0)})
        notes.append(note)
        for fe in fs:
            fe["edge"] = eid
        feats.extend(fs)
        add(pts)

    first = order[0]
    fx0, fx1, fy = pad(first)
    add([(r["cameraMin"]["x"], fy), (fx1, fy)])
    for e in r["edges"]:
        a, b = nodes[e["source"]], nodes[e["target"]]
        ax0, ax1, ay = pad(a["id"])
        bx0, bx1, by = pad(b["id"])
        m = e["module"]
        template = m.get("template", "")
        if r["id"] == "MR06" and e["id"] == "MR06_E06":
            template = ""
        if r["id"] == "MR06" and e["id"] == "MR06_E10":
            template = "glide-drop"
        door_first = None
        if a["id"] in door_nodes and by > ay + 2.5:
            door_first = "ledge" if by - ay < 6 else ("climb" if ax1 > moss_x else "ledge")
        if template == "root-gate-with-fallen-crossing":
            lip = m["position"]["x"] - m["bridgeWidth"] * 0.5
            far = lip + m["bridgeWidth"]
            ap, bp = pos(a), pos(b)
            y_pad = ap[1] + (bp[1] - ap[1]) * (lip - 3 - ap[0]) / (bp[0] - ap[0])
            y_far = ap[1] + (bp[1] - ap[1]) * (far - ap[0]) / (bp[0] - ap[0])
            connect(ax1, ay, max(ax1, lip - 5.0), y_pad, e["id"])
            add([(lip, y_pad)])
            gap(True, "past the ravine")
            add([(far, y_far), (far + 3.0, y_far)])
            connect(far + 3.0, y_far, bx0, by, e["id"])
            feats.append({"kind": "module", "name": template, "x": lip, "y": y_pad, "edge": e["id"]})
        elif template == "flat-thread-gap":
            lip = m["position"]["x"] + 0.5
            pad_x = lip - 7.0
            ledge_end = lip + m["landingWidth"]
            total = by - ay - LIFT_HEIGHT
            left_run, right_run = pad_x - ax1, bx0 - ledge_end
            y_l = ay + total * left_run / (left_run + right_run)
            y_u = y_l + LIFT_HEIGHT
            connect(ax1, ay, pad_x, y_l, e["id"])
            add([(lip - 2.0, y_l), (lip - 2.0, y_l + 1.2), (lip, y_l + 1.2)])
            gap(False, "thread lift wall")
            add([(lip, y_u), (ledge_end, y_u)])
            gap()
            add([(ledge_end, y_u)])
            connect(ledge_end, y_u, bx0, by, e["id"])
            feats.append({"kind": "module", "name": template, "x": lip, "y": y_l, "edge": e["id"]})
        elif template == "sluice-root-gate":
            x0, x1 = m["position"]["x"] - 4.0, m["position"]["x"] + 4.0
            y_p = ay + (by - ay) * (x0 - ax1) / ((x0 - ax1) + (bx0 - x1))
            connect(ax1, ay, x0, y_p, e["id"])
            add([(x1, y_p)])
            connect(x1, y_p, bx0, by, e["id"])
            feats.append({"kind": "module", "name": template, "x": x0, "y": y_p, "edge": e["id"]})
        elif template == "glide-drop":
            ap = pos(a)
            launch, landing = ap[0] + 4.0, ap[0] + 4.0 + GLIDE_GAP
            land_end = landing + 6.0
            add([(launch, ap[1])])
            gap(False, "glide landing")
            add([(landing, ap[1] - GLIDE_DROP), (land_end, ap[1] - GLIDE_DROP)])
            gap()
            add([(land_end, ap[1] - GLIDE_DROP)])
            connect(land_end, ap[1] - GLIDE_DROP, bx0, by, e["id"])
            feats.append({"kind": "module", "name": template, "x": launch, "y": ap[1], "edge": e["id"]})
        elif e["id"] in encounters:
            enc = encounters[e["id"]]
            # The shelf keeps at least a tread of ground to each side (no fight on a pad's edge).
            room = bx0 - ax1
            margin = max(4.0, min(8.0, (room - SHELF) * 0.3))
            width = max(8.0, min(SHELF, room - 2 * margin - (6.0 if a["id"] in door_nodes else 0.0)))
            lead = margin + (6.0 if a["id"] in door_nodes else 0.0)   # past a cave's mouth (and its wall's roof tread)
            tail = margin if room - lead - width >= margin else max(1.0, room - lead - width)   # a short edge: closer to the next pad, never the cave
            cx = min(max(enc["center"]["x"], ax1 + lead + width * 0.5), bx0 - tail - width * 0.5)
            s0, s1 = cx - width * 0.5, cx + width * 0.5
            ys = ay + (by - ay) * (s0 - ax1) / ((s0 - ax1) + (bx0 - s1))
            start_y = ay
            if door_first:
                h0 = min(ys - ay - 0.5, 4.2 if ax1 > moss_x else 3.0)
                if h0 >= 1.9:
                    add([(ax1, ay), (ax1, ay + h0)])
                    feats.append({"kind": classify(h0, ax1 > moss_x), "x": ax1, "y": ay, "rise": h0, "run": 0.0, "edge": e["id"]})
                    start_y = ay + h0
            roof = ax1 + 4.6 if start_y != ay and s0 - ax1 > 10.0 else ax1
            add([(roof, start_y)])
            connect(roof, start_y, s0, ys, e["id"])
            add([(s1, ys)])
            connect(s1, ys, bx0, by, e["id"])
            shelves.append({"encounter": enc["id"], "x0": r3(s0), "x1": r3(s1), "y": r3(ys)})
        elif door_first:
            # The cave's wall: it rises right at the end of the door beat's pad, so the mouth can be
            # set into it; the rest of the edge is designed from its top.
            h0 = min(by - ay - 1.0, 4.2 if ax1 > moss_x else 3.0)
            add([(ax1, ay), (ax1, ay + h0)])
            feats.append({"kind": classify(h0, ax1 > moss_x), "x": ax1, "y": ay, "rise": h0, "run": 0.0, "edge": e["id"]})
            # A full tread on the cave's roof before the climb goes on.
            roof = ax1 + 4.6 if bx0 - ax1 > 12.0 else ax1
            add([(roof, ay + h0)])
            connect(roof, ay + h0, bx0, by, e["id"])
        else:
            connect(ax1, ay, bx0, by, e["id"])
        add([(bx1, by)])
    last = order[-1]
    lx0, lx1, ly = pad(last)
    add([(r["cameraMax"]["x"], ly)])

    # Doors: the cave set into the wall after its beat's pad when the ground rises there; else free.

    out_pieces = []
    for p in pieces:
        pts = clean(p["points"])
        if len(pts) < 2:
            continue
        if p["clingable"]:
            keep = [pad(nid)[1] for nid in door_nodes]
            pts = clean(fillet(merge_narrow(pts, moss_x, keep)))
        out_pieces.append({"label": p["label"] or "", "clingable": p["clingable"], "points": [{"x": r3(x), "y": r3(y)} for x, y in pts]})
    # Doors: the cave set into the wall that rises at the end of its beat's pad (at least 2.4 u, so
    # the mouth's art stands against rock); else standing free on the pad.
    for nid, c in door_nodes.items():
        x0, x1, y = pad(nid)
        top = None
        for p in out_pieces:
            q = [(v["x"], v["y"]) for v in p["points"]]
            for (ax, ay_), (bx, by_) in zip(q, q[1:]):
                if abs(ax - x1) < 1e-3 and abs(bx - x1) < 1e-3 and abs(ay_ - y) < 1e-3 and by_ - ay_ >= 2.4:
                    top = by_
        mouth = x1 - 1.6 if top is not None else pos(nodes[nid])[0] + 5.5
        doors.append({"chamber": c["id"], "x": r3(mouth), "y": r3(y), "embedded": top is not None,
                      "wallX": r3(x1) if top is not None else 0.0, "wallTop": r3(top) if top is not None else 0.0})
    return {
        "id": r["id"],
        "pieces": out_pieces,
        "doors": sorted(doors, key=lambda d: d["chamber"]),
        "shelves": shelves,
        "features": [{"kind": f["kind"], "edge": f["edge"], "x": r3(f["x"]), "y": r3(f["y"]),
                      "rise": r3(f.get("rise", 0.0)), "run": r3(f.get("run", 0.0)), "descent": bool(f.get("descent", False)),
                      "name": f.get("name", "")} for f in feats],
        "connectors": notes,
    }


# ---------------------------------------------------------------------------------------- audit

def audit(region):
    """Tread widths, riser heights and slopes on the walk line."""
    risers, treads, slopes = [], [], []
    for p in region["pieces"]:
        pts = [(q["x"], q["y"]) for q in p["points"]]
        run_start = pts[0][0]
        for a, b in zip(pts, pts[1:]):
            dx, dy = b[0] - a[0], b[1] - a[1]
            if abs(dx) < 1e-4:
                risers.append(abs(dy))
                treads.append(a[0] - run_start)
                run_start = a[0]
            elif abs(dy / dx) > 0.12:
                slopes.append(abs(dy / dx))
        treads.append(pts[-1][0] - run_start)
    kinds = {}
    for f in region["features"]:
        kinds[f["kind"]] = kinds.get(f["kind"], 0) + 1
    narrow = [t for t in treads if t < TREAD_MIN - 0.3]
    return {
        "risers": len(risers), "max_riser": r3(max(risers, default=0.0)),
        "risers_over_3_2": sum(1 for h in risers if h > 3.2 + 1e-3),
        "min_tread": r3(min(treads, default=0.0)), "narrow_treads": len(narrow),
        "max_slope": r3(max(slopes, default=0.0)), "feature_kinds": kinds,
    }


def plot(region, world_region, path):
    from PIL import Image, ImageDraw
    xs = [q["x"] for p in region["pieces"] for q in p["points"]]
    ys = [q["y"] for p in region["pieces"] for q in p["points"]]
    x0, x1, y0, y1 = min(xs), max(xs), min(ys) - 4, max(ys) + 8
    s = 8.0
    W, H = int((x1 - x0) * s) + 40, int((y1 - y0) * s) + 40
    im = Image.new("RGB", (W, H), (246, 238, 224))
    d = ImageDraw.Draw(im)
    X = lambda x: 20 + (x - x0) * s
    Y = lambda y: H - 20 - (y - y0) * s
    for gx in range(int(x0) // 10 * 10, int(x1) + 10, 10):
        d.line([(X(gx), 0), (X(gx), H)], fill=(232, 222, 205))
    colors = {"step": (60, 120, 200), "ledge": (230, 140, 30), "climb": (200, 40, 40), "ramp": (60, 160, 60), "slope": (120, 120, 120), "module": (150, 60, 170)}
    for p in region["pieces"]:
        pts = [(X(q["x"]), Y(q["y"])) for q in p["points"]]
        poly = pts + [(pts[-1][0], H), (pts[0][0], H)]
        d.polygon(poly, fill=(214, 196, 160) if p["clingable"] else (190, 160, 150))
        d.line(pts, fill=(70, 110, 40), width=3)
    for f in region["features"]:
        c = colors.get(f["kind"], (0, 0, 0))
        d.ellipse([X(f["x"]) - 3, Y(f["y"]) - 3, X(f["x"]) + 3, Y(f["y"]) + 3], fill=c)
    for sh in region["shelves"]:
        d.line([(X(sh["x0"]), Y(sh["y"]) - 6), (X(sh["x1"]), Y(sh["y"]) - 6)], fill=(200, 0, 0), width=3)
    for dr in region["doors"]:
        d.rectangle([X(dr["x"]) - 10, Y(dr["y"]) - 18, X(dr["x"]) + 10, Y(dr["y"])], outline=(40, 40, 40), width=2)
    for n in world_region["nodes"]:
        x, y = pos(n)
        d.ellipse([X(x) - 4, Y(y) - 14, X(x) + 4, Y(y) - 6], fill=(30, 30, 30))
        d.text((X(x) - 12, Y(y) - 28), n["id"][-3:], fill=(0, 0, 0))
    im.save(path)


def main():
    with open(WORLD, encoding="utf-8") as f:
        world = json.load(f)
    regions = [design_region(world, r) for r in world["regions"]]
    for reg in regions:
        reg["audit"] = audit(reg)
    out = {"schema": 1, "generatedBy": "design_route.py", "worldSha256": hashlib.sha256(open(WORLD, "rb").read()).hexdigest(), "regions": regions}
    text = json.dumps(out, indent=1, sort_keys=False)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(text + "\n")
    for reg in regions:
        print(reg["id"], json.dumps(reg["audit"]))
    if "--plots" in sys.argv:
        os.makedirs(PLOTS, exist_ok=True)
        for reg, wr in zip(regions, world["regions"]):
            plot(reg, wr, os.path.join(PLOTS, reg["id"] + "_side.png"))


if __name__ == "__main__":
    main()

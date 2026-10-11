"""Builds the Batch 25a-7 inputs (11 Oct, the user: "the main focus on the view"): from 25a-5's
layout, the camera moves back to 9.6 u and up to eye height 2.3 u with the same lens, pitch and
horizon, so the 25a-6 sky and valley stay valid while the bench drops to about 29% of the width.
Adds the parapet wall and a paving grid on the bench's own axes (Wall_Blockout, Paving_Grid_Guide),
and turns both ear guides to face the camera (25a-6's far ear was edge-on, a sliver).

  python make_batch25a7_input.py <sit capture folder> <Batch25a6 folder> <output folder>
"""
import json, math, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw

CAP, B25A3, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
W, H = 2560, 1440
BETA, PITCH = math.radians(18), math.radians(7)
# 25a-7: the camera moves back (and a little up) with the same lens, pitch and horizon as 25a-5, so the
# sky and the distant valley paintings stay valid while the bench, Qori and the terrace get smaller.
HORIZON_Y = 660.0
EYE_HEIGHT, CAM_BACK, CAM_RIGHT = float(os.environ.get("R_EYE", 2.3)), float(os.environ.get("R_BACK", 9.6)), 0.35
FOCAL_25A5 = 300.0 * (6.5 * math.cos(PITCH) + (1.7 - (0.75 + .07 * 1.35)) * math.sin(PITCH))

F = np.array([math.sin(BETA), 0, math.cos(BETA)])        # bench front: into the scene, to the right
L = np.array([-math.cos(BETA), 0, math.sin(BETA)])       # along the bench, near end (right) to far end (left)
UP = np.array([0, 1.0, 0])

SEAT_TOP, SEAT_THICK, SEAT_DEPTH = .75, .12, .50
BACK_BOTTOM, BACK_TOP, POST_TOP, LENGTH = .95, 1.40, 1.47, 3.19
QW = 1.10                                                 # Qori's place along the bench, from the near end
QS = 1.35                                                 # Qori drawn 1.35x his game size, so his head and shoulders clear the backrest
HIP3 = .26 * F + (SEAT_TOP + .07 * QS) * UP + QW * L
HIP_SCREEN_X = float(os.environ.get("R_X", 760.0))

# A pinhole camera behind Qori and a little to his right, eye level just above his head, looking a
# little down, with the horizon where the 25a-3 background has it.
ZC = np.array([0, -math.sin(PITCH), math.cos(PITCH)]); YC = np.array([0, math.cos(PITCH), math.sin(PITCH)]); XC = np.array([1.0, 0, 0])
CAM = np.array([HIP3[0] + CAM_RIGHT, EYE_HEIGHT, HIP3[2] - CAM_BACK])
_zq = (HIP3 - CAM) @ ZC
FOCAL = FOCAL_25A5
S = FOCAL / _zq                                           # px per unit at Qori's depth
CY = HORIZON_Y + FOCAL * math.tan(PITCH)
CX = HIP_SCREEN_X - FOCAL * ((HIP3 - CAM) @ XC) / _zq
VIEW = ZC

def depth(p): return (np.asarray(p, float) - CAM) @ ZC
def proj(p):
    d = np.asarray(p, float) - CAM
    z = d @ ZC
    return np.array([CX + FOCAL * (d @ XC) / z, CY - FOCAL * (d @ YC) / z])

def B(u, v, w): return u * F + v * UP + w * L

def box(u0, u1, v0, v1, w0, w1):
    """Visible faces of a box in bench coordinates, as (polygon, shade, depth)."""
    c = {(a, b, d): B(u, v, w) for a, u in enumerate((u0, u1)) for b, v in enumerate((v0, v1)) for d, w in enumerate((w0, w1))}
    faces = [((0, None, None), -F), ((1, None, None), F), ((None, 0, None), -UP), ((None, 1, None), UP), ((None, None, 0), -L), ((None, None, 1), L)]
    out = []
    for fix, n in faces:
        pts = [c[k] for k in c if all(f is None or k[i] == f for i, f in enumerate(fix))]
        ctr = sum(pts) / 4
        if n @ (ctr - CAM) >= 0: continue   # facing away from the camera
        ordered = sorted(pts, key=lambda p: math.atan2(*(np.linalg.svd(np.array(pts) - ctr)[2][:2] @ (p - ctr))[::-1]))
        shade = .55 + .45 * max(0, n @ np.array([.5, .7, -.5]) / 1.0)
        out.append(([proj(p) for p in ordered], shade, depth(ctr)))
    return out

WOOD = np.array([214, 182, 132]); ROOT = np.array([140, 100, 62])
def part(boxes, col): return [(poly, col * shade, d) for b in boxes for poly, shade, d in box(*b)]

front_parts, back_parts = [], []
for w in (.04, LENGTH - .04):
    front_parts += part([(-.07, .07, 0, POST_TOP, w - .07, w + .07)], ROOT)                    # back post
    front_parts += part([(-.2, .14, 0, .12, w - .16, w + .16)], ROOT)                           # its root feet
    back_parts += part([(SEAT_DEPTH - .13, SEAT_DEPTH - .03, 0, SEAT_TOP - SEAT_THICK, w - .05, w + .05)], ROOT)   # front leg
    back_parts += part([(SEAT_DEPTH - .2, SEAT_DEPTH + .06, 0, .1, w - .12, w + .12)], ROOT)    # its root feet
front_parts += part([(-.035, .035, BACK_BOTTOM, BACK_TOP, -.02, LENGTH + .02)], WOOD * .92)    # backrest
back_parts += part([(-.03, SEAT_DEPTH + .03, SEAT_TOP - SEAT_THICK, SEAT_TOP, -.05, LENGTH + .05)], WOOD)   # seat

def render(polys):
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    for poly, col, _ in sorted(polys, key=lambda t: -t[2]):
        d.polygon([tuple(map(round, p)) for p in poly], fill=tuple(int(c) for c in np.clip(col, 0, 255)) + (255,))
    return im

bench_back, bench_front = render(back_parts), render(front_parts)

# Qori guide: ellipsoids for the body, a head whose visible face side is cream, two leaf ears.
FACE = math.radians(32)                                   # head turned toward the river, so a sliver of cheek shows
HF = np.array([math.sin(BETA + FACE), 0, math.cos(BETA + FACE)]); HL = np.cross(UP, HF)
def ellipsoid(c, r_l, r_up, r_f, axes=(L, UP, F), n=40):
    pts = []
    for i in range(n):
        for j in range(1, n):
            a, b = 2 * math.pi * i / n, math.pi * j / n
            nrm = math.sin(b) * math.cos(a) * axes[0] + math.cos(b) * axes[1] + math.sin(b) * math.sin(a) * axes[2]
            p = c + r_l * math.sin(b) * math.cos(a) * axes[0] + r_up * math.cos(b) * axes[1] + r_f * math.sin(b) * math.sin(a) * axes[2]
            pts.append((p, nrm))
    return pts
def hull(points):
    pts = sorted(set(map(tuple, np.round(points, 1))))
    def cross(o, a, b): return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, hi = [], []
    for p in pts:
        while len(lo) >= 2 and cross(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(hi) >= 2 and cross(hi[-2], hi[-1], p) <= 0: hi.pop()
        hi.append(p)
    return lo[:-1] + hi[:-1]

def layer(): return Image.new("RGBA", (W, H), (0, 0, 0, 0))
GREEN, CREAM, BARK = (120, 150, 80, 255), (240, 228, 205, 255), (120, 90, 60, 255)
body = layer(); db = ImageDraw.Draw(body)
for c, r, col in [(HIP3 + QS * (.2 * UP - .14 * F), (.2, .33, .09), GREEN),     # cloak falling behind him onto the seat
                  (HIP3 + QS * .27 * UP, (.14, .26, .11), (225, 214, 190, 255)),   # leaf tunic
                  (HIP3 + QS * .5 * UP, (.19, .1, .15), GREEN),               # mantle
                  (HIP3 + QS * (.15 * F + .02 * UP), (.09, .07, .2), BARK)]:   # thighs forward over the seat
    db.polygon(hull([proj(p) for p, _ in ellipsoid(c, *(QS * np.array(r)))]), fill=col)
head = layer(); dh = ImageDraw.Draw(head)
HEAD_C = HIP3 + QS * (.75 * UP + .03 * HF)
hp = ellipsoid(HEAD_C, QS * .17, QS * .155, QS * .165, axes=(HL, UP, HF), n=48)
dh.polygon(hull([proj(p) for p, _ in hp]), fill=GREEN)
for p, nrm in hp:   # the face: forward hemisphere, where the camera can see it
    if nrm @ HF > .35 and nrm @ VIEW < -.05 and nrm @ UP < .75:
        x, y = proj(p); dh.ellipse((x - 3, y - 3, x + 3, y + 3), fill=CREAM)
ears = {}
for name, side in (("EarNear", -1), ("EarFar", 1)):
    base = HEAD_C + QS * (.1 * UP - .06 * HF + side * .07 * HL)
    tip = base + QS * (-.42 * HF + .3 * UP + side * .12 * HL)
    # Both leaves turned so their green backs face the camera (25a-6's far ear was edge-on, a sliver).
    mid = (base + tip) / 2; wdir = np.cross(tip - base, mid - CAM); wdir = wdir / np.linalg.norm(wdir) * .07 * QS
    im = layer(); ImageDraw.Draw(im).polygon([tuple(proj(base)), tuple(proj(mid + wdir)), tuple(proj(tip)), tuple(proj(mid - wdir))], fill=(110, 140, 72, 255))
    ears[name] = (im, proj(base), proj(tip))

for n, im in (("Bench_Blockout_Back", bench_back), ("Bench_Blockout_Front", bench_front), ("Qori_Guide_Body", body), ("Qori_Guide_Head", head),
              ("Qori_Guide_EarNear", ears["EarNear"][0]), ("Qori_Guide_EarFar", ears["EarFar"][0])):
    im.save(os.path.join(OUT, n + ".png"))
for old in ("Qori_Sit_Rig_Placed.png", "Qori_Sit_Rig_Blink_Placed.png"):
    if os.path.exists(os.path.join(OUT, old)): os.remove(os.path.join(OUT, old))

# Preview over the 25a-3 background (its bench and Qori left out).
# The terrace, as geometry: the parapet wall parallel to the bench (its front face 0.45 u beyond the
# seat's front, 0.8 u high, 0.4 u thick, its top face showing), and square paving slabs 0.75 u wide
# laid on the bench's own axes, so every joint runs parallel or square to the wall and the bench.
WALL_U, WALL_H, WALL_T, TILE = SEAT_DEPTH + .45, .8, .4, .75
W0, W1 = -14.0, LENGTH + 14.0
NEAR = .5   # keep geometry in front of the camera: clip to this depth before projecting
def near_clip(pts3):
    out = []
    for i in range(len(pts3)):
        a, b = np.asarray(pts3[i], float), np.asarray(pts3[(i + 1) % len(pts3)], float)
        da, db = depth(a) - NEAR, depth(b) - NEAR
        if da >= 0: out.append(a)
        if (da >= 0) != (db >= 0): out.append(a + (b - a) * (da / (da - db)))
    return out
def clip_poly(pts3): return [tuple(map(float, proj(p))) for p in near_clip(pts3)]
def seg(a, b):
    a, b = np.asarray(a, float), np.asarray(b, float)
    da, db = depth(a) - NEAR, depth(b) - NEAR
    if da < 0 and db < 0: return None
    if da < 0: a = a + (b - a) * (da / (da - db))
    if db < 0: b = b + (a - b) * (db / (db - da))
    return [tuple(proj(a)), tuple(proj(b))]
wall = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dw = ImageDraw.Draw(wall)
dw.polygon(clip_poly([B(WALL_U, 0, W0), B(WALL_U, 0, W1), B(WALL_U, WALL_H, W1), B(WALL_U, WALL_H, W0)]), fill=(214, 196, 160, 255))
dw.polygon(clip_poly([B(WALL_U, WALL_H, W0), B(WALL_U, WALL_H, W1), B(WALL_U + WALL_T, WALL_H, W1), B(WALL_U + WALL_T, WALL_H, W0)]), fill=(236, 222, 190, 255))
wall.save(os.path.join(OUT, "Wall_Blockout.png"))
grid = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dg = ImageDraw.Draw(grid)
U_NEAR = -14.0   # far enough toward the camera to reach the bottom of the frame
for k in range(int(math.floor(W0 / TILE)), int(math.ceil(W1 / TILE)) + 1):   # joints running toward the wall
    w = k * TILE + .2
    sg = seg(B(WALL_U, 0, w), B(U_NEAR, 0, w))
    if sg: dg.line(sg, fill=(120, 96, 70, 255), width=3)
u = WALL_U
while u > U_NEAR:                                                              # joints parallel to the wall
    sg = seg(B(u, 0, W0), B(u, 0, W1))
    if sg: dg.line(sg, fill=(120, 96, 70, 255), width=3)
    u -= TILE
grid.save(os.path.join(OUT, "Paving_Grid_Guide.png"))

# Preview over the 25a-6 sky, valley, tree and lantern (its terrace, bench and Qori left out).
reg = json.load(open(os.path.join(B25A3, "Retreat_OverlookTerrace_Registration.json")))
bg = Image.new("RGBA", (W, H), (0, 0, 0, 255))
for Lr in reg["layers"]:
    if Lr["name"] in ("Sky", "Clouds", "Far", "Mid", "Mist", "Tree", "Lantern", "LanternGlow"):
        im = Image.open(os.path.join(B25A3, Lr["file"])).convert("RGBA")
        tmp = layer(); tmp.paste(im, tuple(Lr["offset"]), im); bg.alpha_composite(tmp)
bg.save(os.path.join(OUT, "Background_25a6_Kept.png"))
paving = Image.new("RGBA", (W, H), (0, 0, 0, 0)); dp = ImageDraw.Draw(paving)
dp.polygon(clip_poly([B(WALL_U, 0, W0), B(WALL_U, 0, W1), B(U_NEAR, 0, W1), B(U_NEAR, 0, W0)]), fill=(232, 210, 172, 255))
pv = bg.copy()
tree_and_lantern = Image.new("RGBA", (W, H), (0, 0, 0, 0))
for Lr in reg["layers"]:
    if Lr["name"] in ("Tree", "Lantern", "LanternGlow"):
        im = Image.open(os.path.join(B25A3, Lr["file"])).convert("RGBA"); tree_and_lantern.alpha_composite(im)
for im in (paving, grid, wall, bench_back, body, head, ears["EarFar"][0], ears["EarNear"][0], bench_front, tree_and_lantern): pv.alpha_composite(im)
d = ImageDraw.Draw(pv)
d.rectangle((1920, 0, W - 1, H - 1), outline=(220, 60, 60, 255), width=5); d.rectangle((0, 1296, W - 1, H - 1), outline=(220, 60, 60, 255), width=5)
pv.convert("RGB").save(os.path.join(OUT, "Template_Preview.png"))

def P(p): return [round(float(v), 1) for v in proj(p)]
def lowest_corner(u0, u1, w0, w1):
    pts = [proj(B(u, 0, w)) for u in (u0, u1) for w in (w0, w1)]
    x, y = max(pts, key=lambda q: q[1]); return [round(float(x), 1), round(float(y), 1)]
seat_patch = [P(B(u, SEAT_TOP, w)) for u, w in ((.02, QW - .18), (.45, QW - .18), (.45, QW + .18), (.02, QW + .18))]
placement = {
    "pixelsPerUnitAtQori": S, "canvas": [W, H], "coordinates": "top-left origin, y down",
    "camera": {"benchFrontTurnDegrees": 18, "lookDownDegrees": 7, "focalPx": round(FOCAL, 1), "eyeHeight_u": EYE_HEIGHT, "behindQori_u": CAM_BACK, "rightOfQori_u": CAM_RIGHT, "horizonY": HORIZON_Y, "pinhole": True},
    "qoriHip": P(HIP3), "qoriHeadCentre": P(HEAD_C), "qoriPlaceAlongBench_u": QW, "qoriScaleVsGame": QS,
    "parapetBaseLine": [P(B(WALL_U, 0, w)) for w in np.linspace(W0, W1, 15)], "parapetTopLine": [P(B(WALL_U, WALL_H, w)) for w in np.linspace(W0, W1, 15)],
    "tileSize_u": TILE, "benchWidthFraction": round(float((max(proj(B(u, v, w))[0] for u in (-.2, SEAT_DEPTH + .1) for v in (0, POST_TOP) for w in (-.2, LENGTH + .2)) - min(proj(B(u, v, w))[0] for u in (-.2, SEAT_DEPTH + .1) for v in (0, POST_TOP) for w in (-.2, LENGTH + .2))) / W), 3),
    "earBases": {k: [round(float(v), 1) for v in e[1]] for k, e in ears.items()},
    "earTips": {k: [round(float(v), 1) for v in e[2]] for k, e in ears.items()},
    "seatTopUnderQori": seat_patch,
    # The lowest visible point of each root foot (its nearest bottom corner), where it meets the paving.
    "groundUnderBackPosts": [lowest_corner(-.2, .14, w - .16, w + .16) for w in (.04, LENGTH - .04)],
    "groundUnderFrontLegs": [lowest_corner(SEAT_DEPTH - .2, SEAT_DEPTH + .06, w - .12, w + .12) for w in (.04, LENGTH - .04)],
    "bench_units": {"seatTop": SEAT_TOP, "seatThickness": SEAT_THICK, "seatDepth": SEAT_DEPTH, "backrestBottom": BACK_BOTTOM,
                    "backrestTop": BACK_TOP, "postTop": POST_TOP, "length": LENGTH},
}
json.dump(placement, open(os.path.join(OUT, "Placement.json"), "w"), indent=2)
shutil.copy(os.path.join(CAP, "Qori_Sit_Landmarks.json"), os.path.join(OUT, "Qori_Sit_Landmarks.json"))
shutil.copy(os.path.join(CAP, "Qori_Sit_320ppu.png"), os.path.join(OUT, "Reference_Qori_Rig_Sit_SideView.png"))
shutil.copy(os.path.join(os.path.dirname(os.path.abspath(__file__)), "check_batch25a7.py"), os.path.join(OUT, "check_batch25a7.py"))
print(json.dumps(placement, indent=1))

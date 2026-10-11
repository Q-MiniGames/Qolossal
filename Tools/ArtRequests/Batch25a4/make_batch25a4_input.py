"""Builds the Batch 25a-4 inputs (second version, 11 Oct: the user wants Qori and the bench facing the
field, seen three-quarter from behind). A small 3D model of the accepted bench and a seated Qori guide
are projected into the terrace view, so Codex paints over shapes whose perspective is already right.

  python make_batch25a4_input.py <sit capture folder> <Batch25a3 folder> <output folder>

Bench measurements: Town_Qvale_ListeningBench.png at 120 PPU from foot row 204 (seat top 0.75 u, seat
plank 0.12 u, backrest 0.95-1.40 u, posts to 1.47 u, length 3.19 u); the seat depth (0.5 u) is a choice.
Qori measurements: the rig's Sit pose (Qori_Sit_Landmarks.json, 320 PPU): head pivot 0.58 u above the
hip, head about 0.34 u wide and 0.31 u tall, ears 0.55 u, hip 0.07 u above the seat.
Camera: a pinhole 5.9 u behind Qori and 0.35 u to his right at eye height 1.38 u, looking down 7 degrees,
horizon at y 660 (the 25a-3 background's); the bench's front turned 45 degrees right of straight ahead;
365 px per unit at Qori.
"""
import json, math, os, shutil, sys
import numpy as np
from PIL import Image, ImageDraw

CAP, B25A3, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
W, H, S = 2560, 1440, 365.0
BETA, PITCH = math.radians(45), math.radians(7)
HORIZON_Y, EYE_HEIGHT, CAM_BACK, CAM_RIGHT = 660.0, 1.38, 5.9, 0.35

F = np.array([math.sin(BETA), 0, math.cos(BETA)])        # bench front: into the scene, to the right
L = np.array([-math.cos(BETA), 0, math.sin(BETA)])       # along the bench, near end (right) to far end (left)
UP = np.array([0, 1.0, 0])

SEAT_TOP, SEAT_THICK, SEAT_DEPTH = .75, .12, .50
BACK_BOTTOM, BACK_TOP, POST_TOP, LENGTH = .95, 1.40, 1.47, 3.19
QW = 0.95                                                 # Qori's place along the bench, from the near end
HIP3 = .26 * F + (SEAT_TOP + .07) * UP + QW * L
HIP_SCREEN_X = 1000.0

# A pinhole camera behind Qori and a little to his right, eye level just above his head, looking a
# little down, with the horizon where the 25a-3 background has it.
ZC = np.array([0, -math.sin(PITCH), math.cos(PITCH)]); YC = np.array([0, math.cos(PITCH), math.sin(PITCH)]); XC = np.array([1.0, 0, 0])
CAM = np.array([HIP3[0] + CAM_RIGHT, EYE_HEIGHT, HIP3[2] - CAM_BACK])
_zq = (HIP3 - CAM) @ ZC
FOCAL = S * _zq                                           # S px per unit at Qori's depth
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
FACE = math.radians(10)                                    # head turned a little more toward the river
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
for c, r, col in [(HIP3 + .2 * UP - .14 * F, (.2, .33, .09), GREEN),     # cloak falling behind him onto the seat
                  (HIP3 + .27 * UP, (.14, .26, .11), (225, 214, 190, 255)),   # leaf tunic
                  (HIP3 + .5 * UP, (.19, .1, .15), GREEN),               # mantle
                  (HIP3 + .15 * F + .02 * UP, (.09, .07, .2), BARK)]:   # thighs forward over the seat
    db.polygon(hull([proj(p) for p, _ in ellipsoid(c, *r)]), fill=col)
head = layer(); dh = ImageDraw.Draw(head)
HEAD_C = HIP3 + .75 * UP + .03 * HF
hp = ellipsoid(HEAD_C, .17, .155, .165, axes=(HL, UP, HF), n=48)
dh.polygon(hull([proj(p) for p, _ in hp]), fill=GREEN)
for p, nrm in hp:   # the face: forward hemisphere, where the camera can see it
    if nrm @ HF > .35 and nrm @ VIEW < -.05 and nrm @ UP < .75:
        x, y = proj(p); dh.ellipse((x - 3, y - 3, x + 3, y + 3), fill=CREAM)
ears = {}
for name, side in (("EarNear", -1), ("EarFar", 1)):
    base = HEAD_C + .1 * UP - .06 * HF + side * .07 * HL
    tip = base - .42 * HF + .3 * UP + side * .12 * HL
    mid = (base + tip) / 2; wdir = np.cross(tip - base, HF); wdir = wdir / np.linalg.norm(wdir) * .07
    im = layer(); ImageDraw.Draw(im).polygon([tuple(proj(base)), tuple(proj(mid + wdir)), tuple(proj(tip)), tuple(proj(mid - wdir))], fill=(110, 140, 72, 255))
    ears[name] = (im, proj(base), proj(tip))

for n, im in (("Bench_Blockout_Back", bench_back), ("Bench_Blockout_Front", bench_front), ("Qori_Guide_Body", body), ("Qori_Guide_Head", head),
              ("Qori_Guide_EarNear", ears["EarNear"][0]), ("Qori_Guide_EarFar", ears["EarFar"][0])):
    im.save(os.path.join(OUT, n + ".png"))
for old in ("Qori_Sit_Rig_Placed.png", "Qori_Sit_Rig_Blink_Placed.png"):
    if os.path.exists(os.path.join(OUT, old)): os.remove(os.path.join(OUT, old))

# Preview over the 25a-3 background (its bench and Qori left out).
reg = json.load(open(os.path.join(B25A3, "Retreat_OverlookTerrace_Registration.json")))
bg = Image.new("RGBA", (W, H), (0, 0, 0, 255))
for Lr in reg["layers"]:
    if Lr["name"] in ("Bench", "Weather") or Lr["name"].startswith("Qori_"): continue
    im = Image.open(os.path.join(B25A3, Lr["file"])).convert("RGBA")
    tmp = layer(); tmp.paste(im, tuple(Lr["offset"]), im); bg.alpha_composite(tmp)
bg.save(os.path.join(OUT, "Background_25a3_NoBench.png"))
pv = bg.copy()
for im in (bench_back, body, head, ears["EarFar"][0], ears["EarNear"][0], bench_front): pv.alpha_composite(im)
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
    "camera": {"benchFrontTurnDegrees": 45, "lookDownDegrees": 7, "eyeHeight_u": EYE_HEIGHT, "behindQori_u": CAM_BACK, "rightOfQori_u": CAM_RIGHT, "horizonY": HORIZON_Y, "pinhole": True},
    "qoriHip": P(HIP3), "qoriHeadCentre": P(HEAD_C), "qoriPlaceAlongBench_u": QW,
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
shutil.copy(os.path.join(os.path.dirname(os.path.abspath(__file__)), "check_batch25a4.py"), os.path.join(OUT, "check_batch25a4.py"))
print(json.dumps(placement, indent=1))

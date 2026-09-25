"""Cut Qori's existing painted assets into cutout-rig parts.

Coordinate space for everything ("rig px"): the Qori_IdleBody_v1 canvas
(1182x1330), y pointing DOWN. Unity conversion later: x_u=(x-591)/100,
y_u=(665-y)/100, which is exactly the space of the old QoriVisual child.
Each part: an RGBA image, its pivot (image px) and the rig-px joint where
that pivot sits in the rest layout (all rotations zero).
"""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import json, os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SRC = _os.path.join(_PROJECT,'Assets','Resources')
OUT = _os.path.join(_WORK,'parts')
os.makedirs(OUT, exist_ok=True)

def load(p): return Image.open(os.path.join(SRC, p)).convert('RGBA')

def trim(img, pivot, pad=4):
    """Crop to alpha bbox (+pad) and shift the pivot accordingly."""
    bb = img.getbbox()
    x0, y0 = max(bb[0]-pad, 0), max(bb[1]-pad, 0)
    x1, y1 = min(bb[2]+pad, img.width), min(bb[3]+pad, img.height)
    return img.crop((x0, y0, x1, y1)), (pivot[0]-x0, pivot[1]-y0)

def scaled(img, sx, sy):
    w, h = max(1, round(img.width*sx)), max(1, round(img.height*sy))
    return img.resize((w, h), Image.LANCZOS)

parts = {}
def add(name, img, pivot, joint):
    img, pivot = trim(img, pivot)
    parts[name] = dict(img=img, pivot=pivot, joint=joint)

# ---------------------------------------------------------------- head x4 + ears + blink (Codex v1 art)
# The heads are fitted into the old atlas cells (so the neck pivot, NECK joint and
# head size in the rig stay the same): one shared scale, face centre -> old face centre.
import newart
from newart import fit, fit_sr
cells = {  # old atlas cell boxes + neck pivot inside the cell (kept for the pivot frame)
    'Head_Neutral': ((0, 0, 640, 627), (480, 627-105)),
    'Head_Up':      ((640, 0, 1254, 627), (435, 627-115)),
    'Head_Down':    ((0, 627, 640, 1254), (475, 627-120)),
    'Head_Focus':   ((640, 627, 1254, 1254), (440, 627-135)),
}
OLD_FACE_C = {'Head_Neutral': (516, 406), 'Head_Up': (495, 376), 'Head_Down': (512, 383), 'Head_Focus': (496, 380)}
OLD_FACE_W = 242.0          # old neutral face width in atlas px
NECK = (681, 492)
_nb = newart.face_box(newart.load_new('Qori_Head_Neutral.png'))
HEAD_SCALE = OLD_FACE_W / (_nb[2]-_nb[0])
HEADS = {}
def fit_head(src, face_c, scale, cell_c, cell_piv):
    img, pimg, _ = fit_sr(src, face_c, scale, 0.0, cell_c)
    # pivot in the fitted image = where the cell's neck pivot lands
    piv = (pimg[0] + cell_piv[0]-cell_c[0], pimg[1] + cell_piv[1]-cell_c[1])
    return img, piv
for name, (box, piv) in cells.items():
    src = newart.load_new(f'Qori_{name}.png')
    fb = newart.face_box(src)
    img, p = fit_head(src, ((fb[0]+fb[2])/2, (fb[1]+fb[3])/2), HEAD_SCALE, OLD_FACE_C[name], piv)
    HEADS[name] = (img, p)
    add(name, img, p, NECK)

# Blink: Codex's blink painting drifts in outline/scale, so only its closed-eye lines are
# used. The neutral head's eyes are painted out (inpainted skin) and the lines drawn on
# top, giving a frame with exactly the neutral silhouette (no pop when blinking).
def make_blink():
    import cv2
    nimg, npiv = HEADS['Head_Neutral']
    bsrc = newart.load_new('Qori_Head_Blink.png'); bb = newart.face_box(bsrc)
    bscale = HEAD_SCALE * (_nb[2]-_nb[0]) / (bb[2]-bb[0])
    bimg, bpiv = fit_head(bsrc, ((bb[0]+bb[2])/2, (bb[1]+bb[3])/2), bscale, OLD_FACE_C['Head_Neutral'], cells['Head_Neutral'][1])
    canvas = Image.new('RGBA', nimg.size, (0, 0, 0, 0))
    canvas.alpha_composite(bimg, (round(npiv[0]-bpiv[0]), round(npiv[1]-bpiv[1]))) if (npiv[0]-bpiv[0] >= 0 and npiv[1]-bpiv[1] >= 0) else None
    if canvas.getbbox() is None:   # blink canvas larger than neutral: crop instead
        dx, dy = round(bpiv[0]-npiv[0]), round(bpiv[1]-npiv[1])
        canvas = bimg.crop((dx, dy, dx+nimg.width, dy+nimg.height))
    N = np.array(nimg); B = np.array(canvas)
    eyes, boxes = newart.eye_mask(nimg)
    k = max(5, int(nimg.width*.05)) | 1
    hole = cv2.dilate(eyes.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (k, k))).astype(np.float32)
    m = np.clip(cv2.GaussianBlur(hole, (0, 0), k*.12)*1.3, 0, 1) * (B[:, :, 3]/255.)
    # keep the neutral head's brows and silhouette outline untouched
    nl = N[:, :, :3].astype(float).mean(2)
    keep = ((nl < 110) & ~cv2.dilate(eyes.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)).astype(np.uint8)
    keep = cv2.GaussianBlur(cv2.dilate(keep, np.ones((3, 3), np.uint8)).astype(np.float32), (0, 0), 1.2)
    m = m * (1 - np.clip(keep*2, 0, 1)) * (N[:, :, 3]/255.)
    out = N.copy()
    out[:, :, :3] = (N[:, :, :3]*(1-m[..., None]) + B[:, :, :3]*m[..., None]).astype(np.uint8)
    return Image.fromarray(out), npiv
_bi, _bp = make_blink()
add('Head_Blink', _bi, _bp, NECK)

# Expressions (Codex v2): face-feature overlays painted on the neutral head's exact canvas,
# so they get the neutral head's transform and are composited onto it. The outline stays
# identical to Head_Neutral, so swapping never pops.
# Accepted, hash-pinned copies imported into the project (Tools/ArtImport).
FACES_DIR = _os.path.join(_PROJECT, 'Assets', 'Art', 'Codex', 'Player')
_neutral_src = newart.load_new('Qori_Head_Neutral.png')
_nfb = newart.face_box(_neutral_src)
def make_face(expr):
    overlay = Image.open(_os.path.join(FACES_DIR, f'Qori_Face_{expr}.png')).convert('RGBA')
    assert overlay.size == _neutral_src.size, f'Qori_Face_{expr} must share the neutral head canvas'
    fitted, piv = fit_head(overlay, ((_nfb[0]+_nfb[2])/2, (_nfb[1]+_nfb[3])/2), HEAD_SCALE,
                           OLD_FACE_C['Head_Neutral'], cells['Head_Neutral'][1])
    nimg, npiv = HEADS['Head_Neutral']
    assert fitted.size == nimg.size and max(abs(piv[0]-npiv[0]), abs(piv[1]-npiv[1])) < .01
    # keep the overlay inside the neutral silhouette
    a = np.array(fitted); a[:, :, 3] = (a[:, :, 3].astype(float) * np.array(nimg)[:, :, 3] / 255).astype(np.uint8)
    head = nimg.copy(); head.alpha_composite(Image.fromarray(a))
    return head, npiv
for _expr in ('Hurt', 'Effort'):
    if _os.path.exists(_os.path.join(FACES_DIR, f'Qori_Face_{_expr}.png')):
        _fi, _fp = make_face(_expr)
        add(f'Head_{_expr}', _fi, _fp, NECK)

# Ears: separate leaves on their own bones, hinged at the base (hidden under the leaf
# hair) and posed like the old atlas ears. Base = rounded nub (right), tip = left point.
_npiv = cells['Head_Neutral'][1]
def cell_to_rig(p): return (NECK[0] + p[0]-_npiv[0], NECK[1] + p[1]-_npiv[1])
EARS = {  # old atlas (neutral cell) base and tip of each ear
    'EarUpper': ((446, 292), (50, 132)),
    'EarLower': ((432, 336), (24, 302)),
}
# Per-ear correction (rotation deg, scale, dx, dy) found by matching the old atlas ear
# silhouettes: Codex's leaves bulge upward, so a plain base->tip fit made them read too high.
EAR_ADJUST = {'EarUpper': (0, 1.05, 15, 0), 'EarLower': (2, 1.05, 15, 35)}
for _e, (_rot, _sc, _dx, _dy) in EAR_ADJUST.items():
    (_b, _t) = EARS[_e]; _r = math.radians(_rot)
    _v = np.array(_t, float) - _b; _v = _sc*np.array([[math.cos(_r), -math.sin(_r)], [math.sin(_r), math.cos(_r)]]) @ _v
    _nb = (_b[0]+_dx, _b[1]+_dy); EARS[_e] = (_nb, (_nb[0]+_v[0], _nb[1]+_v[1]))
for part, (base, tip) in EARS.items():
    src = newart.load_new('Qori_Ear_Upper.png' if part == 'EarUpper' else 'Qori_Ear_Lower.png')
    sb = newart.end_point(src, 'right', .06); st_ = newart.end_point(src, 'left', .015)
    sb = (sb[0] - (sb[0]-st_[0])*.02, sb[1])          # a little inside the nub
    img, p, _ = fit(src, sb, st_, cell_to_rig(base), cell_to_rig(tip))
    add(part, img, p, cell_to_rig(base))

# ---------------------------------------------------------------- torso
torso = load('QoriRig/Qori_ThreeQuarter_Torso_v1.png')
TSX, TSY, SKY = 0.617, 0.561, 0.40
PELVIS = (668, 790)
# Upper torso (mantle, chest, belt). Pivot = belt centre (600,770).
up = torso.crop((0, 0, torso.width, 818))
# feather the bottom edge a touch so it never shows a hard line over the skirt
a = np.array(up); alpha = a[:, :, 3].astype(float)
for y in range(806, 818): alpha[y] *= (818-y)/12
a[:, :, 3] = alpha.astype(np.uint8); up = Image.fromarray(a)
up = scaled(up, TSX, TSY)
add('Torso', up, (600*TSX, 770*TSY), PELVIS)
# Skirt: starts under the belt, hangs from the belt centre.
SKIRT_TOP = 786
sk = torso.crop((0, SKIRT_TOP, torso.width, torso.height))
sk = scaled(sk, TSX, SKY)
SKIRT_JOINT = (PELVIS[0], PELVIS[1] + round((SKIRT_TOP-770)*TSY))
add('Skirt', sk, (600*TSX, 0), SKIRT_JOINT)

# ---------------------------------------------------------------- leg
leg = load('QoriRig/Qori_ThreeQuarter_Leg_v1.png')
LSX, LSY = 0.25, 0.30
L_HIP, L_KNEE, L_ANKLE = (492, 150), (598, 560), (548, 1168)
la = np.array(leg)
H, W = la.shape[:2]
yy, xx = np.mgrid[0:H, 0:W]
def seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx-ax, by-ay
    t = np.clip(((px-ax)*dx+(py-ay)*dy)/(dx*dx+dy*dy), 0, 1)
    return np.hypot(px-(ax+t*dx), py-(ay+t*dy))
d_th = seg_dist(xx, yy, L_HIP, L_KNEE)
d_sh = seg_dist(xx, yy, L_KNEE, L_ANKLE)
d_knee = np.hypot(xx-L_KNEE[0], yy-L_KNEE[1])
# thigh: closer to thigh segment, plus a knee cap disc (overlap hides seam)
thigh_m = ((d_th <= d_sh) | (d_knee < 70)) & (yy < 650)
shin_m = ((d_sh < d_th) | (d_knee < 78)) & (yy >= 470) & (yy < 1185)
foot_m = yy >= 1120
def masked(arr, m):
    out = arr.copy(); out[:, :, 3] = (out[:, :, 3] * m).astype(np.uint8)
    return Image.fromarray(out)
def S(p): return (p[0]*LSX, p[1]*LSY)
thigh = scaled(masked(la, thigh_m), LSX, LSY)
shin = scaled(masked(la, shin_m), LSX, LSY)
foot = scaled(masked(la, foot_m), LSX, LSY)
# Rest joints in rig px: two hips under the skirt, the leg hangs as painted.
def leg_joints(hip):
    hx, hy = hip
    k = (hx + (L_KNEE[0]-L_HIP[0])*LSX, hy + (L_KNEE[1]-L_HIP[1])*LSY)
    an = (hx + (L_ANKLE[0]-L_HIP[0])*LSX, hy + (L_ANKLE[1]-L_HIP[1])*LSY)
    return k, an
HIP_NEAR, HIP_FAR = (690, 888), (648, 884)
for side, hip in (('Near', HIP_NEAR), ('Far', HIP_FAR)):
    k, an = leg_joints(hip)
    add(f'Thigh{side}', thigh, S(L_HIP), hip)
    add(f'Shin{side}', shin, S(L_KNEE), k)
    add(f'Foot{side}', foot, S(L_ANKLE), an)
LEG_INFO = dict(thigh=math.dist(S(L_HIP), S(L_KNEE)), shin=math.dist(S(L_KNEE), S(L_ANKLE)))
# sole offset from ankle (lowest opaque foot pixel below ankle)
fa = np.array(foot)[:, :, 3] > 40
ys = np.nonzero(fa.any(axis=1))[0]
LEG_INFO['sole'] = ys.max() - S(L_ANKLE)[1] + 0  # before trim; foot img untrimmed here
LEG_INFO['toe'] = np.nonzero(fa.any(axis=0))[0].max() - S(L_ANKLE)[0]

# ---------------------------------------------------------------- arm (Codex v1 art)
# Upper arm / fist forearm / open-hand forearm painted separately. Landmarks are joint
# centres in the source files. Both segments share one scale, chosen so the total reach
# (shoulder->grip) equals the old arm, which keeps every IK hand target in the clips valid.
import newart
from newart import fit, fit_sr
body = load('QoriCloakPrototype/Qori_IdleBody_v1.png')
UA_SRC = newart.load_new('Qori_UpperArm.png')
FF_SRC = newart.load_new('Qori_Forearm_Fist.png')
FO_SRC = newart.load_new('Qori_Forearm_Open.png')
UA_SH, UA_EL = (518, 262), (497, 1215)
FF_EL, FF_WR, FF_GRIP = (462, 185), (596, 1040), (698, 1258)
FO_EL, FO_WR = (456, 150), (560, 1000)
# The painted arms are slimmer than the legs; widen them (across the bone) to match.
ARM_THICK = 1.25
def _widen(img, *pts):
    w = round(img.width*ARM_THICK); k = w/img.width
    return (img.resize((w, img.height), Image.LANCZOS),) + tuple((p[0]*k, p[1]) for p in pts)
UA_SRC, UA_SH, UA_EL = _widen(UA_SRC, UA_SH, UA_EL)
FF_SRC, FF_EL, FF_WR, FF_GRIP = _widen(FF_SRC, FF_EL, FF_WR, FF_GRIP)
FO_SRC, FO_EL, FO_WR = _widen(FO_SRC, FO_EL, FO_WR)
OLD_SH, OLD_EL, OLD_FIST = np.array((612, 622.)), np.array((584, 694.)), np.array((648, 828.))
REACH = np.linalg.norm(OLD_EL-OLD_SH) + np.linalg.norm(OLD_FIST-OLD_EL)
ARM_SCALE = REACH / (math.dist(UA_SH, UA_EL) + math.dist(FF_EL, FF_GRIP))
_u1 = (OLD_EL-OLD_SH)/np.linalg.norm(OLD_EL-OLD_SH); _u2 = (OLD_FIST-OLD_EL)/np.linalg.norm(OLD_FIST-OLD_EL)
SHOULDER = OLD_SH
ELBOW = SHOULDER + ARM_SCALE*math.dist(UA_SH, UA_EL)*_u1
FIST = ELBOW + ARM_SCALE*math.dist(FF_EL, FF_GRIP)*_u2
upper_img, up_piv, _ = fit(UA_SRC, UA_SH, UA_EL, SHOULDER, ELBOW)
fore_img, fo_piv, (WRIST,) = fit(FF_SRC, FF_EL, FF_GRIP, ELBOW, FIST, extra=[FF_WR])
open_img, op_piv, _ = fit(FO_SRC, FO_EL, FO_WR, ELBOW, WRIST)
# 'Near' = weapon arm on the chest side (drawn BEHIND the torso); 'Far' = free arm that
# comes out of the painted armhole on the camera side (drawn IN FRONT). Matches the 3/4 torso art.
for side, sh in (('Near', (712, 632)), ('Far', (590, 648))):
    off = (sh[0]-SHOULDER[0], sh[1]-SHOULDER[1])
    add(f'UpperArm{side}', upper_img, up_piv, sh)
    add(f'Forearm{side}', fore_img, fo_piv, (ELBOW[0]+off[0], ELBOW[1]+off[1]))
    if side == 'Far':   # open hand: swapped in at runtime for the free arm (same elbow pivot)
        add('ForearmOpen', open_img, op_piv, (ELBOW[0]+off[0], ELBOW[1]+off[1]))
ARM_INFO = dict(fist_from_elbow=(float(FIST[0]-ELBOW[0]), float(FIST[1]-ELBOW[1])),
                elbow_from_shoulder=(float(ELBOW[0]-SHOULDER[0]), float(ELBOW[1]-SHOULDER[1])))

# Reach arms for ledge hanging: the same v1 paintings with only the middle of the shaft
# lengthened, so thickness, joint caps and fist match the normal arms exactly. (Codex's
# v2 reach arms were painted far thinner and longer, so they aren't used.)
REACH_UPPER, REACH_FORE = 1.6, 1.8        # joint-to-joint length vs the normal arm
def stretch_shaft(img, y0, y1, points, factor):
    """Lengthen rows y0..y1 so the first->last landmark distance grows by `factor`."""
    (ax, ay), (bx, by) = points[0], points[-1]
    dy_new = math.sqrt((factor*math.dist(points[0], points[-1]))**2 - (bx-ax)**2)
    k = 1 + (dy_new - (by-ay)) / (y1-y0)
    mh = round((y1-y0)*k)
    mid = img.crop((0, y0, img.width, y1)).convert('RGBa').resize((img.width, mh), Image.LANCZOS).convert('RGBA')
    out = Image.new('RGBA', (img.width, img.height + mh - (y1-y0)), (0, 0, 0, 0))
    out.paste(img.crop((0, 0, img.width, y0)), (0, 0)); out.paste(mid, (0, y0))
    out.paste(img.crop((0, y1, img.width, img.height)), (0, y0+mh))
    def moved(p): return (p[0], p[1] if p[1] <= y0 else y0+(p[1]-y0)*mh/(y1-y0) if p[1] <= y1 else p[1]+mh-(y1-y0))
    return out, [moved(p) for p in points]
UAR_SRC, (UAR_SH, UAR_EL) = stretch_shaft(UA_SRC, 440, 1060, [UA_SH, UA_EL], REACH_UPPER)
FFR_SRC, (FFR_EL, FFR_WR, FFR_GRIP) = stretch_shaft(FF_SRC, 380, 930, [FF_EL, FF_WR, FF_GRIP], REACH_FORE)
ELBOW_R = SHOULDER + ARM_SCALE*math.dist(UAR_SH, UAR_EL)*_u1
FIST_R = ELBOW_R + ARM_SCALE*math.dist(FFR_EL, FFR_GRIP)*_u2
reach_upper_img, rup_piv, _ = fit(UAR_SRC, UAR_SH, UAR_EL, SHOULDER, ELBOW_R)
reach_fore_img, rfo_piv, _ = fit(FFR_SRC, FFR_EL, FFR_GRIP, ELBOW_R, FIST_R)
for side, sh in (('Near', (712, 632)), ('Far', (590, 648))):
    off = (sh[0]-SHOULDER[0], sh[1]-SHOULDER[1])
    add(f'UpperArmReach{side}', reach_upper_img, rup_piv, sh)
    add(f'ForearmReach{side}', reach_fore_img, rfo_piv, (ELBOW_R[0]+off[0], ELBOW_R[1]+off[1]))
ARM_INFO['reach_fist_from_elbow'] = (float(FIST_R[0]-ELBOW_R[0]), float(FIST_R[1]-ELBOW_R[1]))

# ---------------------------------------------------------------- cape (the three leaf cloak panels)
# Top-attachment points and scales measured from the old QoriLayeredCloak so the
# cape keeps its familiar size. Each panel is split into an upper and a lower
# half; the lower half's top edge is feathered so the hinge never shows a seam.
CAPE_ANCHORS = [(365, 40), (195, 42), (138, 40)]
CAPE_SCALES = [.69, .76, .87]
CAPE_JOINTS = [(664, 572), (651, 577), (638, 583)]   # upper back, hidden under the mantle
CAPE_INFO = []
NEW_CAPE = bool(int(os.environ.get('QORI_NEW_CAPE', '0')))
CAPE_WIDEN = 1.5
for i in range(3):
    im = load(f'QoriCloakPrototype/Qori_CloakPanel_{i+1}.png')
    sx, sy = CAPE_SCALES[i]*1.10, CAPE_SCALES[i]
    im = scaled(im, sx, sy)
    ax, ay = CAPE_ANCHORS[i][0]*sx, CAPE_ANCHORS[i][1]*sy
    if NEW_CAPE:   # Codex v1 leaf panel, fitted top-attachment -> old anchor, tip -> old tip
        _a = np.array(im)[:, :, 3] > 40; _ys, _xs = np.nonzero(_a); _j = np.argmax(_ys)
        old_tip = (float(_xs[_j]), float(_ys[_j]))
        src = newart.load_new(f'Qori_Cape_Panel_{i+1}.png')
        w0 = src.width; src = src.resize((round(w0*CAPE_WIDEN), src.height), Image.LANCZOS)
        top = newart.end_point(src, 'top', .02); tip = newart.end_point(src, 'bottom', .01)
        fimg, fp, _ = fit(src, top, tip, (ax, ay), old_tip)
        canvas = Image.new('RGBA', (im.width+400, im.height+200), (0, 0, 0, 0))
        canvas.alpha_composite(fimg, (max(0, round(ax+200-fp[0])), max(0, round(ay+100-fp[1]))))
        im = canvas; ax += 200; ay += 100
    ca = np.array(im)
    rows_alpha = np.nonzero((ca[:, :, 3] > 40).any(axis=1))[0]
    bottom = rows_alpha.max()
    split = int(ay + (bottom-ay)*0.46)
    xs = np.nonzero(ca[split, :, 3] > 40)[0]
    hx = float(xs.mean()) if len(xs) else ax
    F = 22
    up = ca.copy(); up[split+F:, :, 3] = 0
    lo = ca.copy(); lo[:split-F, :, 3] = 0
    ramp = np.clip((np.arange(split-F, split+F) - (split-F)) / (2*F), 0, 1)
    lo[split-F:split+F, :, 3] = (lo[split-F:split+F, :, 3] * ramp[:, None]).astype(np.uint8)
    jx, jy = CAPE_JOINTS[i]
    add(f'Cape{i+1}', Image.fromarray(up), (ax, ay), (jx, jy))
    add(f'Cape{i+1}Lower', Image.fromarray(lo), (hx, split), (jx + hx - ax, jy + split - ay))
    CAPE_INFO.append(dict(length=float(bottom-ay), hinge=float(split-ay)))

# ---------------------------------------------------------------- weapon preview (runtime uses WeaponDefinition art)
# Codex v1 single-weapon art, prepared by weapons_prep.py (the game loads the same files).
import weapons_prep
_W = weapons_prep.prep()
for part, name, length in (('Weapon', 'LeafSword', 7.4), ('Weapon_Mace', 'SeedpodMace', 6.5),
                           ('Weapon_Spear', 'ThornSpear', 7.5), ('Weapon_Whip', 'WhipHandle', 1.35)):
    im, (gx, gy) = _W[name]
    sc = length*100/(im.width-gx)
    parts[part] = dict(img=scaled(im, sc, sc), pivot=(gx*sc, gy*sc), joint=None)

meta = {}
for n, p in parts.items():
    p['img'].save(f'{OUT}/{n}.png')
    meta[n] = dict(pivot=[float(p['pivot'][0]), float(p['pivot'][1])], size=list(p['img'].size),
                   joint=None if p['joint'] is None else [float(p['joint'][0]), float(p['joint'][1])])
json.dump(dict(parts=meta, leg=LEG_INFO, arm=ARM_INFO, cape=CAPE_INFO), open(_os.path.join(_WORK,'parts.json'), 'w'), indent=1, default=float)
print({n: m_['size'] for n, m_ in meta.items()})
print(LEG_INFO, ARM_INFO)

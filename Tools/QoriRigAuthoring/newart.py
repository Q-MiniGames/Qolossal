"""Helpers for fitting Codex's high-res part paintings (IncomingArt/Codex_v1) onto the rig.

Every part is fitted by two landmarks: source-image points (px, y down) that must land
on two rig-px points. That gives one similarity transform (scale + rotation + move)
per part, so proportions never distort. Downscaling is done with LANCZOS first and on
premultiplied alpha so edges keep no dark fringe.
"""
import os as _os
_HERE=_os.path.dirname(_os.path.abspath(__file__))
_PROJECT=_os.path.abspath(_os.path.join(_HERE,'..','..'))
_WORK=_os.path.join(_HERE,'work')
_os.makedirs(_WORK,exist_ok=True)
import math, os
import numpy as np
from PIL import Image

NEW = os.environ.get('QORI_NEW_ART', _os.path.join(_HERE,'IncomingArt','Codex_v1'))

def load_new(name):
    return Image.open(os.path.join(NEW, name)).convert('RGBA')

def fit(img, src_a, src_b, dst_a, dst_b, extra=()):
    """Map img so src_a->dst_a and src_b->dst_b (rig px, y down).
    Returns (image, pivot_of_dst_a_in_image, [extra points in rig px])."""
    sa, sb, da, db = (np.array(p, float) for p in (src_a, src_b, dst_a, dst_b))
    s = np.linalg.norm(db - da) / np.linalg.norm(sb - sa)
    ang = math.atan2(*(db - da)[::-1]) - math.atan2(*(sb - sa)[::-1])
    return fit_sr(img, src_a, s, math.degrees(ang), dst_a, extra)

def fit_sr(img, src_a, s, rot_deg, dst_a, extra=()):
    """Scale by s, rotate by rot_deg (image coords, y down, +ve = clockwise on screen),
    and move src_a onto dst_a."""
    w, h = max(1, round(img.width*s)), max(1, round(img.height*s))
    sx, sy = w/img.width, h/img.height
    small = img.convert('RGBa').resize((w, h), Image.LANCZOS)
    c, sn = math.cos(math.radians(rot_deg)), math.sin(math.radians(rot_deg))
    R = np.array([[c, -sn], [sn, c]])
    corners = np.array([[0, 0], [w, 0], [0, h], [w, h]], float) @ R.T
    lo = np.floor(corners.min(0)) - 2; hi = np.ceil(corners.max(0)) + 2
    W, H = int(hi[0]-lo[0]), int(hi[1]-lo[1])
    Ri = R.T  # inverse rotation
    # output (X,Y) -> input: Ri @ ((X,Y) + lo)
    a, b = Ri[0]; d, e = Ri[1]
    cx = a*lo[0] + b*lo[1]; cy = d*lo[0] + e*lo[1]
    out = small.transform((W, H), Image.AFFINE, (a, b, cx, d, e, cy), resample=Image.BICUBIC).convert('RGBA')
    def img_pt(p):
        q = R @ np.array([p[0]*sx, p[1]*sy]) - lo
        return q
    piv = img_pt(src_a)
    off = np.array(dst_a, float) - piv
    return out, (float(piv[0]), float(piv[1])), [tuple(img_pt(p) + off) for p in extra]

def alpha_bbox(img, thr=40):
    a = np.array(img)[:, :, 3] > thr
    ys, xs = np.nonzero(a)
    return xs.min(), ys.min(), xs.max(), ys.max()

def end_point(img, side, frac=.04, thr=40):
    """Centroid of the opaque pixels in the outermost `frac` of the alpha bbox on one side."""
    a = np.array(img)[:, :, 3] > thr
    x0, y0, x1, y1 = alpha_bbox(img, thr)
    ys, xs = np.nonzero(a)
    if side == 'left':  m = xs <= x0 + (x1-x0)*frac
    if side == 'right': m = xs >= x1 - (x1-x0)*frac
    if side == 'top':   m = ys <= y0 + (y1-y0)*frac
    if side == 'bottom': m = ys >= y1 - (y1-y0)*frac
    return float(xs[m].mean()), float(ys[m].mean())

def _cream(a):
    rgb = a[:, :, :3].astype(float); lum = rgb.mean(2); sat = rgb.max(2) - rgb.min(2)
    return (lum > 175) & (sat < 75) & (a[:, :, 3] > 200), lum

def face_box(img):
    """Bounding box (x0,y0,x1,y1) of the cream face disc of an EARLESS head painting."""
    import cv2
    a = np.array(img.convert('RGBA'))
    cream, _ = _cream(a)
    k = max(3, int(min(img.size)*.01))
    m = cv2.morphologyEx(cream.astype(np.uint8), cv2.MORPH_OPEN, np.ones((k, k), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m)
    i = 1 + int(np.argmax(st[1:, 4]))
    x, y, w, h = st[i, :4]
    return float(x), float(y), float(x+w), float(y+h), (lab == i)

def eye_mask(img):
    """Mask of the two dark eye blobs inside the face."""
    import cv2
    a = np.array(img.convert('RGBA'))
    *_, face = face_box(img)
    cnts, _ = cv2.findContours(face.astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    hull = np.zeros(face.shape, np.uint8)
    cv2.drawContours(hull, [cv2.convexHull(max(cnts, key=cv2.contourArea))], -1, 1, -1)
    hull = cv2.erode(hull, np.ones((5, 5), np.uint8))
    lum = a[:, :, :3].astype(float).mean(2)
    dark = ((lum < 90) & (hull > 0)).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(dark)
    keep = sorted(range(1, n), key=lambda i: -st[i, 4])[:2]
    m = np.isin(lab, keep).astype(np.uint8)
    # fill the eyes' white highlights / inner holes
    cnts, _ = cv2.findContours(m, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    cv2.drawContours(m, cnts, -1, 1, -1)
    return m.astype(bool), [tuple(st[i, :4]) for i in keep]

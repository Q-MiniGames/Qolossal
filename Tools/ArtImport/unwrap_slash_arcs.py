"""Unroll Codex's painted slash arcs into straight strips for Qori's slash trail.

The trail (Assets/Scripts/QoriSlashTrail.cs) is a ribbon that follows the real blade path, so it
needs its texture as a strip: u runs along the swing (0 = the fading tail, 1 = the leading edge),
v runs across it (0 = the grip side, 1 = the blade tip). The painted arcs are crescents, so each
is unrolled around the circle fitted to its opaque pixels: angle becomes u, radius becomes v
(inner radius at v = 0). The leading end is the thicker end of the crescent. The thrust streak
is already straight and is only cropped.

Reads the accepted, imported art in Assets/Art/Codex/Weapons and writes
Assets/Resources/FX/Slash/SlashStrip_{Sword,Heavy,Thrust}.png. Deterministic; re-run it if
the source art changes.
Usage: python Tools/ArtImport/unwrap_slash_arcs.py [--preview out.png]
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
SOURCE = os.path.join(PROJECT, "Assets", "Art", "Codex", "Weapons")
OUT = os.path.join(PROJECT, "Assets", "Resources", "FX", "Slash")
STRIP = (512, 128)  # width along the swing, height across it


def load(name):
    return np.asarray(Image.open(os.path.join(SOURCE, name + ".png")).convert("RGBA")).astype(np.float64) / 255.0


def fit_circle(alpha):
    """Algebraic (Kasa) circle fit to the opaque pixels, weighted by alpha."""
    ys, xs = np.nonzero(alpha > 0.25)
    w = alpha[ys, xs]
    a = np.stack([xs, ys, np.ones_like(xs)], axis=1).astype(np.float64)
    b = (xs.astype(np.float64) ** 2 + ys.astype(np.float64) ** 2)
    sol, *_ = np.linalg.lstsq(a * w[:, None], b * w, rcond=None)
    cx, cy = sol[0] / 2, sol[1] / 2
    r = np.sqrt(sol[2] + cx * cx + cy * cy)
    return cx, cy, r


def bilinear(img, x, y):
    h, w = img.shape[:2]
    x = np.clip(x, 0, w - 1.001)
    y = np.clip(y, 0, h - 1.001)
    x0, y0 = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = (x - x0)[..., None], (y - y0)[..., None]
    p = img[y0, x0] * (1 - fx) * (1 - fy) + img[y0, x0 + 1] * fx * (1 - fy) \
        + img[y0 + 1, x0] * (1 - fx) * fy + img[y0 + 1, x0 + 1] * fx * fy
    return p


def straighten(strip, margin=0.06):
    """The crescents aren't true circles, so the unrolled band wanders up and down. Find the
    painted band in each column (alpha-weighted 5th to 95th percentile), smooth its centre along
    the strip, and shift each column so the band runs level through the middle. One scale for
    the whole strip keeps the painted taper (thin tail, thick leading edge)."""
    h, w = strip.shape[:2]
    alpha = strip[:, :, 3]
    rows = np.arange(h, dtype=np.float64)
    lo, hi = np.zeros(w), np.full(w, h - 1.0)
    for x in range(w):
        col = alpha[:, x]
        total = col.sum()
        if total < 1e-3:
            continue
        c = np.cumsum(col) / total
        lo[x], hi[x] = np.interp(0.05, c, rows), np.interp(0.95, c, rows)
    # Columns with little paint borrow the band from their neighbours.
    weight = alpha.sum(axis=0)
    kernel = np.hanning(61); kernel /= kernel.sum()
    def smooth(v):
        num = np.convolve(v * weight, kernel, mode="same")
        den = np.convolve(weight, kernel, mode="same") + 1e-6
        return num / den
    centre = smooth((lo + hi) / 2)
    thickest = np.percentile(smooth(hi - lo), 98)
    scale = thickest / (h * (1 - 2 * margin))   # source rows per output row
    out = np.zeros_like(strip)
    for x in range(w):
        src = centre[x] + (rows + 0.5 - h / 2) * scale
        for ch in range(4):
            out[:, x, ch] = np.interp(src, rows, strip[:, x, ch], left=0.0, right=0.0)
    return out


def unroll(name):
    img = load(name)
    alpha = img[:, :, 3]
    cx, cy, _ = fit_circle(alpha)
    ys, xs = np.nonzero(alpha > 0.1)
    ang = np.arctan2(ys - cy, xs - cx)
    rad = np.hypot(xs - cx, ys - cy)
    # The crescent's angular span: rotate so the largest empty gap in angle is at the seam.
    order = np.sort(ang)
    gaps = np.diff(np.concatenate([order, [order[0] + 2 * np.pi]]))
    k = int(np.argmax(gaps))
    start = order[(k + 1) % len(order)]
    span = (order[k] - start) % (2 * np.pi)
    r0, r1 = np.percentile(rad, 1), np.percentile(rad, 99)
    pad = (r1 - r0) * 0.04
    r0, r1 = r0 - pad, r1 + pad
    u = (np.arange(STRIP[0]) + 0.5) / STRIP[0]
    v = (np.arange(STRIP[1]) + 0.5) / STRIP[1]
    uu, vv = np.meshgrid(u, v)
    a = start + uu * span
    r = r0 + vv * (r1 - r0)
    strip = bilinear(img, cx + np.cos(a) * r, cy + np.sin(a) * r)
    strip = straighten(strip)
    # The leading end is the heavier end of the crescent: put it at u = 1.
    mass = strip[:, :, 3].sum(axis=0)
    half = STRIP[0] // 2
    if mass[:half].sum() > mass[half:].sum():
        strip = strip[:, ::-1]
    return strip  # row 0 = v 0.5/128 (inner, the grip side)


def crop_streak(name):
    img = load(name)
    ys, xs = np.nonzero(img[:, :, 3] > 0.03)
    crop = img[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    # The point is the leading end: put it on the right (u = 1).
    cols = crop[:, :, 3].sum(axis=0)
    n = len(cols)
    tip_right = cols[int(n * 0.9):].sum() < cols[:int(n * 0.1)].sum()
    if not tip_right:
        crop = crop[:, ::-1]
    return np.asarray(Image.fromarray((crop * 255).astype(np.uint8)).resize(STRIP, Image.LANCZOS)).astype(np.float64) / 255.0


def save(strip, name):
    os.makedirs(OUT, exist_ok=True)
    # Unity textures start at the bottom row: v = 0 (the grip side) must be the image's bottom.
    im = Image.fromarray((np.clip(strip, 0, 1) * 255).astype(np.uint8)[::-1], "RGBA")
    path = os.path.join(OUT, name + ".png")
    im.save(path)
    return im


def main(argv):
    strips = {
        "SlashStrip_Sword": unroll("FX_SlashArc_Sword"),
        "SlashStrip_Heavy": unroll("FX_SlashArc_Heavy"),
        "SlashStrip_Thrust": crop_streak("FX_Thrust_Streak"),
    }
    images = [save(s, n) for n, s in strips.items()]
    print("wrote", ", ".join(strips), "to", os.path.relpath(OUT, PROJECT))
    if "--preview" in argv:
        out = argv[argv.index("--preview") + 1]
        sheet = Image.new("RGBA", (STRIP[0], (STRIP[1] + 10) * len(images)), (40, 50, 50, 255))
        for i, im in enumerate(images):
            sheet.alpha_composite(im, (0, i * (STRIP[1] + 10)))
        sheet.save(out)


if __name__ == "__main__":
    main(sys.argv[1:])

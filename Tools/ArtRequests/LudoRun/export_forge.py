"""Cuts a Ludo clip of the forge hearth into a looping overlay for Town_Qvale_Smithy.png. Each frame is lined up on the
reference, scaled back to the art's own pixels, and kept only inside a feathered oval over the hearth mouth, so the
stone, hood and tools stay the approved painting. Ludo's clip starts and ends on the flameless painting, so only its
steadily burning middle (SECTION) is used, and the seam is closed by a short cross-fade. Played backward, the flames
visibly run downward: the user rejected that on 3 Nov.
Prints how far the clip moved outside the hearth, which is how much Ludo redrew what it should have left alone.
Frames come from Ludo's video: the crop sits in its canvas at CONTENT, because the reference had a transparent margin.
Usage: python export_forge.py <folder with frame_NNN.png> [out folder]
"""
import glob, os, sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Art', 'Codex', 'Town', 'Town_Qvale_Smithy.png')
BOX = (1306, 545, 1576, 815)                # the reference's crop of the art, 270 px square
HEARTH = (.5, .5, .37, .2)              # centre x, y and half width, height of the oval, as shares of the crop (y down)
FEATHER = 9                                 # px at the art's scale
CONTENT = (.05, .0875, .95, .9125)          # where the crop sits in Ludo's frame, as shares of its size
SECTION = (8, 30)                           # the source frames where the fire burns steadily
LOOP = 18                                   # frames kept (1.5 s at 12 fps)
FADE = 3                                    # frames of cross-fade at the seam
MAX_SHIFT = 12                              # px at the art's scale


def oval(size):
    s = size
    y, x = np.mgrid[0:s, 0:s]
    d = (((x + .5) / s - HEARTH[0]) / HEARTH[2]) ** 2 + (((y + .5) / s - HEARTH[1]) / HEARTH[3]) ** 2
    mask = Image.fromarray(np.uint8((d < 1) * 255)).filter(ImageFilter.GaussianBlur(FEATHER))
    return np.asarray(mask, dtype=np.float32) / 255


def lined_up(frame, art, outside):
    """The whole-pixel shift that best fits the painting outside the hearth."""
    best, shift = None, (0, 0)
    gray_a, gray_f = art.mean(axis=2), frame.mean(axis=2)
    for dy in range(-MAX_SHIFT, MAX_SHIFT + 1, 2):
        for dx in range(-MAX_SHIFT, MAX_SHIFT + 1, 2):
            moved = np.roll(np.roll(gray_f, dy, 0), dx, 1)
            error = (np.abs(moved - gray_a) * outside).sum()
            if best is None or error < best: best, shift = error, (dy, dx)
    return np.roll(np.roll(frame, shift[0], 0), shift[1], 1), shift


def main():
    source = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(ROOT, 'Assets', 'Art', 'Ludo', 'ForgeFire')
    paths = sorted(glob.glob(os.path.join(source, 'frame_*.png')))[SECTION[0]:SECTION[1]]
    if len(paths) < SECTION[1] - SECTION[0]: sys.exit('too few frames in ' + source)
    size = BOX[2] - BOX[0]
    art = np.asarray(Image.open(ART).convert('RGB').crop(BOX), dtype=np.float32)
    mask = oval(size)
    outside = (mask < .01).astype(np.float32)
    frames, shifts, drift = [], [], []
    for p in paths:
        f = Image.open(p).convert('RGB')
        box = (CONTENT[0] * f.width, CONTENT[1] * f.height, CONTENT[2] * f.width, CONTENT[3] * f.height)
        f = np.asarray(f.resize((size, size), Image.LANCZOS, box=box), dtype=np.float32)
        f, s = lined_up(f, art, outside)
        frames.append(f); shifts.append(s)
        drift.append(float((np.abs(f - art).mean(axis=2) * outside).sum() / outside.sum()))
    frames = np.stack(frames)
    # The most alike pair LOOP frames apart, judged inside the hearth, makes the cleanest loop.
    inside = mask[None, :, :, None]
    starts = range(0, len(frames) - LOOP - FADE + 1)
    closure = {i: float((np.abs(frames[i] - frames[i + LOOP]) * inside).sum() / inside.sum() / 3) for i in starts}
    start = min(closure, key=closure.get)
    loop = frames[start:start + LOOP].copy()
    for k in range(FADE):
        t = (k + 1) / (FADE + 1)
        loop[k] = frames[start + LOOP + k] * (1 - t) + frames[start + k] * t
    # Same overall light as the painting, so nothing pops when the overlay shows or hides.
    target = (art * mask[:, :, None]).sum(axis=(0, 1)) / mask.sum()
    mean = (loop * inside).sum(axis=(1, 2)).mean(axis=0) / mask.sum()
    loop = np.clip(loop * (target / mean) ** .5, 0, 255)
    os.makedirs(out, exist_ok=True)
    for i, f in enumerate(loop):
        rgba = np.dstack([f, mask * 255]).astype(np.uint8)
        Image.fromarray(rgba, 'RGBA').save(os.path.join(out, 'Forge_Fire_%02d.png' % i))
    print('%d source frames; loop %d-%d, seam difference %.1f of 255' % (len(frames), start, start + LOOP - 1, closure[start]))
    print('largest shift %s px; redrawn outside the hearth: mean %.1f, worst %.1f of 255' % (max(shifts, key=lambda s: abs(s[0]) + abs(s[1])), np.mean(drift), max(drift)))
    print('frames in', out)


if __name__ == '__main__':
    main()

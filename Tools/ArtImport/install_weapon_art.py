"""Install held-weapon art for Qori from a chosen Codex painting.

The game draws each weapon from Assets/Resources/Armory/Weapons/<Name>.png, horizontal with the
grip end on the left and the tip at the right edge, pivoted at the grip listed in Grips.txt
("Name,gripX,gripY": fractions of the canvas, y from the bottom). QoriArmoryFactory scales each
weapon so grip-to-tip is its world length.

This crops a source painting to its painted pixels, scales it to WIDTH px, finds the handle's
centre line at the grip, and writes the PNG and its Grips.txt line. The sources are the models
the user chose from Codex's renders (Tools/ArtImport/WeaponSources/*_source.png).
Usage: python Tools/ArtImport/install_weapon_art.py [--preview out.png]
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
SOURCES = os.path.join(HERE, "WeaponSources")
OUT = os.path.join(PROJECT, "Assets", "Resources", "Armory", "Weapons")
WIDTH = 1536

# name: grip x as a fraction of the cropped width (where Qori's leading hand holds it).
WEAPONS = {
    "SeedpodMace": 0.14,   # on the leaf-bound handle, past the end cap
    "ThornSpear": 0.13,    # on the cream wrap behind the bud pommel
    "LeafStaff": 0.16,     # a hand-width up from the butt end (Codex Review 20 art)
}


def install(name, grip_x):
    im = Image.open(os.path.join(SOURCES, name + "_source.png")).convert("RGBA")
    alpha = np.asarray(im)[:, :, 3]
    ys, xs = np.nonzero(alpha > 8)
    im = im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
    height = round(im.height * WIDTH / im.width)
    im = im.resize((WIDTH, height), Image.LANCZOS)
    a = np.asarray(im)[:, :, 3].astype(np.float64)
    # The handle's centre line at the grip: the alpha-weighted mean row over a few columns there.
    x0 = int(grip_x * WIDTH)
    band = a[:, max(0, x0 - 12):x0 + 12].sum(axis=1)
    centre_row = float((band * np.arange(height)).sum() / band.sum())
    grip_y = 1.0 - (centre_row + 0.5) / height
    os.makedirs(OUT, exist_ok=True)
    im.save(os.path.join(OUT, name + ".png"))
    return im, f"{name},{grip_x:.4f},{grip_y:.4f}"


def main(argv):
    grips_path = os.path.join(OUT, "Grips.txt")
    lines = [l for l in open(grips_path).read().splitlines() if l.strip()]
    images = []
    for name, grip_x in WEAPONS.items():
        im, line = install(name, grip_x)
        lines = [l for l in lines if not l.startswith(name + ",")] + [line]
        images.append((im, grip_x, float(line.split(",")[2])))
        print(line, im.size)
    with open(grips_path, "w", newline="\n") as f:
        f.write("\n".join(sorted(lines)) + "\n")
    if "--preview" in argv:
        out = argv[argv.index("--preview") + 1]
        h = sum(im.height for im, _, _ in images) + 20 * len(images)
        sheet = Image.new("RGBA", (WIDTH, h), (60, 70, 70, 255))
        y = 0
        for im, gx, gy in images:
            sheet.alpha_composite(im, (0, y))
            cx, cy = int(gx * WIDTH), y + int((1 - gy) * im.height)
            for d in range(-10, 11):   # mark the grip
                sheet.putpixel((cx + d, cy), (255, 0, 0, 255)); sheet.putpixel((cx, cy + d), (255, 0, 0, 255))
            y += im.height + 20
        sheet.save(out)


if __name__ == "__main__":
    main(sys.argv[1:])

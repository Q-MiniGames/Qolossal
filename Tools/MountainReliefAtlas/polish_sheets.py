"""Before/after sheets for the level polish, from MraPolishCapture's matched captures.

Each sheet pairs the same data-defined camera spot before (left) and after (right) the rebuild,
labelled, so terrain joins and composition can be judged side by side.

Usage: python polish_sheets.py <captureRoot> <outFolder>
  <captureRoot> holds polish_before/ and polish_after/ (MraPolishCapture -polishTag before|after).
"""
import os
import sys

from PIL import Image, ImageDraw


def sheet(root, out, region, names, title, width=1600):
    rows = []
    for name in names:
        a, b = os.path.join(root, "polish_before", name), os.path.join(root, "polish_after", name)
        if os.path.exists(a) and os.path.exists(b):
            rows.append((name, Image.open(a).convert("RGB"), Image.open(b).convert("RGB")))
    if not rows:
        return None
    cell_w = width // 2
    cell_h = int(cell_w * rows[0][1].height / rows[0][1].width)
    head = 34
    im = Image.new("RGB", (width, head + len(rows) * (cell_h + 22)), (24, 22, 20))
    d = ImageDraw.Draw(im)
    d.text((10, 10), f"{title}   (left: before, right: after; same camera spot)", fill=(240, 230, 210))
    y = head
    for name, a, b in rows:
        d.text((10, y + 4), name.replace("_1080.jpg", "").replace("_720.jpg", " (1280x720)"), fill=(220, 210, 190))
        im.paste(a.resize((cell_w - 4, cell_h)), (0, y + 20))
        im.paste(b.resize((cell_w - 4, cell_h)), (cell_w + 4, y + 20))
        y += cell_h + 22
    path = os.path.join(out, f"{region}_{title.split()[0].lower()}_before_after.jpg")
    im.save(path, quality=86)
    return path


def main(root, out):
    os.makedirs(out, exist_ok=True)
    after = sorted(os.listdir(os.path.join(root, "polish_after")))
    regions = sorted({n.split("_")[0] for n in after})
    made = []
    for r in regions:
        mine = [n for n in after if n.startswith(r + "_")]
        doors = [n for n in mine if "_door_" in n and n.endswith("_1080.jpg")]
        wide = [n for n in mine if "_wide_" in n]
        edges = [n for n in mine if "_edge_" in n]
        beats = [n for n in mine if "_beat_" in n and n.endswith("_1080.jpg")]
        for names, title in ((doors, "Caves"), (wide, "Composition (wide)"), (edges[::2][:8], "Climb samples"), (beats[::3], "Beats")):
            p = sheet(root, out, r, names, title)
            if p:
                made.append(p)
    print("\n".join(made))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])

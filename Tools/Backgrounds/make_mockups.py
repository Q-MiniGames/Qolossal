"""Proposal mockups: proposed background layers composited behind the real in-engine foreground
(terrain, props, Qori: a difference matte of renders over black and white), next to the current frame.
Every element is labelled: REUSED (an accepted asset, unchanged bytes, only placed/scaled/tinted here),
NEW (a proposed asset; shown by an illustrative placeholder), or CHANGE (placement/camera/code).
Mockups are illustrations, not renders: the final in-engine result must be re-captured.

  python Tools/Backgrounds/make_mockups.py [capture folder]
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CAP = sys.argv[1] if len(sys.argv) > 1 else r"C:\_temp\qp_cap\bg_audit"
OUT = os.path.join(ROOT, "Tools", "Backgrounds", "proposal")
BG = os.path.join(ROOT, "Assets", "Art", "Codex", "Backgrounds")
W, H = 1920, 1080
PX = 108.0   # screen pixels per world unit at orthographic size 5, 1080p
try:
    F = ImageFont.truetype("arial.ttf", 26); FS = ImageFont.truetype("arial.ttf", 20); FB = ImageFont.truetype("arialbd.ttf", 30)
except OSError:
    F = FS = FB = ImageFont.load_default()
COL = {"REUSED": (46, 140, 70), "NEW": (214, 110, 20), "CHANGE": (40, 100, 190)}


def load(path):
    return Image.open(path).convert("RGBA")


def fg(stem, lift_px=0):
    b = np.asarray(Image.open(os.path.join(CAP, stem + "_fg_black.png")).convert("RGB"), float)
    w = np.asarray(Image.open(os.path.join(CAP, stem + "_fg_white.png")).convert("RGB"), float)
    a = 1 - np.clip((w - b).mean(axis=2) / 255, 0, 1)
    col = np.where(a[..., None] > 1e-3, b / np.maximum(a[..., None], 1e-3), 0)
    im = Image.fromarray(np.dstack([np.clip(col, 0, 255), a * 255]).astype("uint8"), "RGBA")
    if lift_px:
        # Camera raised by lift_px: the scene moves down; the bottom rows (plain fill) leave the frame.
        out = Image.new("RGBA", (W, H), (0, 0, 0, 0)); out.alpha_composite(im.crop((0, 0, W, H - lift_px)), (0, lift_px)); im = out
    return im


def tint(im, rgb, amount):
    """Blend toward a haze colour (atmospheric perspective), keeping alpha."""
    if amount <= 0:
        return im
    over = Image.new("RGBA", im.size, rgb + (255,))
    blended = Image.blend(im, over, amount)
    blended.putalpha(im.getchannel("A"))
    return blended


def band(canvas, layer, height_px, skyline_y, skyline_row, x_offset=0, haze=None):
    """Scales a repeating layer to height_px and places it so its painted skyline row lands at screen
    y = skyline_y, tiling horizontally."""
    s = height_px / layer.height
    im = layer.resize((max(1, round(layer.width * s)), round(height_px)), Image.LANCZOS)
    if haze:
        im = tint(im, *haze)
    top = round(skyline_y - skyline_row * s)
    bottom = top + im.height
    if bottom < H:
        row = np.asarray(im)[-1]
        opaque = row[row[:, 3] > 127]
        if len(opaque):
            c = tuple(int(v) for v in opaque[:, :3].mean(axis=0))
            ImageDraw.Draw(canvas).rectangle([0, bottom - 2, W, H], fill=c + (255,))
    x = -((x_offset) % im.width)
    while x < W:
        canvas.alpha_composite(im, (int(x), 0) if top >= 0 else (int(x), 0), (0, max(0, -top))) if top < 0 else canvas.alpha_composite(im, (int(x), top))
        x += im.width
    return canvas


def sky(path, warm_from=None):
    s = load(path)
    # The live sky is 24 u tall around the camera: the frame shows its middle 10 u.
    h = s.height; crop = s.crop((0, int(h * 7 / 24), s.width, int(h * 17 / 24)))
    return crop.resize((W, H), Image.LANCZOS)


def tag(d, xy, kind, text, anchor_box=None):
    x, y = xy
    c = COL[kind]
    label = f"{kind}: {text}"
    tw = d.textlength(label, font=FS)
    d.rounded_rectangle([x, y, x + tw + 16, y + 30], 6, fill=(255, 255, 255, 230), outline=c, width=3)
    d.text((x + 8, y + 4), label, fill=c, font=FS)
    if anchor_box:
        d.rectangle(anchor_box, outline=c, width=4)


def compare(name, title, current_path, proposed, notes, kinds_used):
    cur = Image.open(current_path).convert("RGB").resize((W // 2 * 2, H))
    prop = proposed.convert("RGB")
    sheet = Image.new("RGB", (W * 2 + 24, H + 150 + 40 * len(notes)), (245, 242, 235))
    d = ImageDraw.Draw(sheet)
    d.text((16, 14), title, fill=(30, 26, 22), font=FB)
    d.text((16, 66), "CURRENT (in-engine capture, 1920x1080)", fill=(60, 55, 50), font=F)
    d.text((W + 40, 66), "PROPOSED (mockup: reused art placed by script; NEW items are placeholders)", fill=(60, 55, 50), font=F)
    sheet.paste(cur, (8, 104)); sheet.paste(prop, (W + 16, 104))
    y = H + 120
    for kind, text in notes:
        d.rounded_rectangle([16, y, 40, y + 24], 4, fill=COL[kind]); d.text((52, y), f"{kind}: {text}", fill=(30, 26, 22), font=FS); y += 40
    sheet.save(os.path.join(OUT, name + "_compare.jpg"), quality=88)
    prop.save(os.path.join(OUT, name + "_proposed_1080.jpg"), quality=90)
    prop.resize((1280, 720), Image.LANCZOS).save(os.path.join(OUT, name + "_proposed_720.jpg"), quality=90)
    print("wrote", name)


def mill():
    return load(os.path.join(BG, "BG_Terraces_Mill_Landmark.png"))


os.makedirs(OUT, exist_ok=True)
HAZE_DAWN = (247, 204, 168)

# ---------------------------------------------------------------- M1 Terraces, mid beat (Option A: accepted art only)
c = sky(os.path.join(BG, "BG_Sky_Terraces.png"))
far, mid, near = (load(os.path.join(BG, f"BG_Terraces_{n}.png")) for n in ("Far", "Mid", "Near"))
# One shared scale (10 u = 1080 px: 1:1 at 1080p) keeps the three canvases registered as painted.
# Far and Mid lifted 130 px so the valley floor shows above the walk line; the Near keeps the frame bottom.
for layer, lift in ((far, 130), (mid, 130), (near, 0)):
    band(c, layer, 1080, -lift, 0)
m = mill(); s = 300 / m.height; m = tint(m.resize((round(m.width * s), 300), Image.LANCZOS), HAZE_DAWN, .18)
c.alpha_composite(m, (1180, 300))
c.alpha_composite(fg("MR03_mid"))
d = ImageDraw.Draw(c, "RGBA")
tag(d, (40, 60), "REUSED", "BG_Sky_Terraces (unchanged)")
tag(d, (40, 330), "REUSED", "BG_Terraces_Far/Mid/Near: seamless Review 23 set, 1:1 at 1080p")
tag(d, (1120, 250), "CHANGE", "Mill landmark 5.5 u -> 2.8 u, seated on the far hill", (1180, 300, 1180 + m.width, 600))
compare("M1_MR03_Terraces_mid", "M1  MR03 The Terraces, mid beat  -  Option A (accepted art only)",
        os.path.join(CAP, "MR03_mid_1080.png"), c,
        [("REUSED", "BG_Sky_Terraces + BG_Terraces_Far/Mid/Near (accepted Review 23, wired at one shared scale; replaces the A5 strips and hazed far ridge)"),
         ("CHANGE", "Mill landmark smaller and set back on the far hill: it no longer stands on gameplay rock at gameplay scale"),
         ("CHANGE", "Layers lifted so the valley floor and river show above the walk line")], None)

# ---------------------------------------------------------------- M2 Qvale street (current level): Option B
c = sky(os.path.join(BG, "BG_Sky_Terraces.png"))
qfar, qmid, qnear = (load(os.path.join(BG, f"BG_Qvale_{n}.png")) for n in ("Far", "Mid", "Near"))
LIFT = 0   # the framing change is measured and described, not faked (a lifted capture would need a re-render)
band(c, qfar, 520, 300, 233, 0, (HAZE_DAWN, .22))
band(c, qmid, 760, 420, 262, 400, (HAZE_DAWN, .10))
# Placeholder for the NEW vault rim: accepted Qvale house exteriors, tiny and hazed, along the far slope.
houses = []
for n in ("Town_Qvale_HomeA_Closed", "Town_Qvale_HomeB_Closed", "Town_Qvale_HerbalistHome_Closed"):
    p = os.path.join(ROOT, "Assets", "Art", "Codex", "Town", n + ".png")
    if os.path.exists(p):
        houses.append(load(p))
x0 = 140
for i, hs in enumerate(houses * 2):
    s = 64 / hs.height
    im = tint(hs.resize((round(hs.width * s), 64), Image.LANCZOS), HAZE_DAWN, .5).filter(ImageFilter.GaussianBlur(.6))
    c.alpha_composite(im, (x0 + i * 300 + (i % 2) * 60, 418 + (i % 3) * 10))
band(c, qnear, 760, 560, 265, 900, (HAZE_DAWN, .04))
c.alpha_composite(fg("MR04_mid", LIFT))
d = ImageDraw.Draw(c, "RGBA")
tag(d, (40, 60), "NEW", "BG_Sky_Qvale_Dawn (placeholder: BG_Sky_Terraces)")
tag(d, (60, 345), "NEW", "BG_Qvale_VaultRim_Mid: far side of the basin, homes under overhangs (placeholder)", (110, 395, 1900, 500))
tag(d, (40, 560), "REUSED", "BG_Qvale_Far/Mid/Near, dawn-hazed")
tag(d, (1150, 980), "CHANGE", "proposed: camera +1 u on the street (not shown)")
compare("M2_MR04_Qvale_street", "M2  MR04 Qvale, street (current level)  -  Option B",
        os.path.join(CAP, "MR04_mid_1080.png"), c,
        [("NEW", "BG_Sky_Qvale_Dawn: the approved Quake_Qvale apricot dawn instead of the cool teal A5 sky (shown with BG_Sky_Terraces)"),
         ("NEW", "BG_Qvale_VaultRim_Mid: the lap basin's far rim with lantern homes under overhangs (shown with tiny hazed accepted house exteriors)"),
         ("REUSED", "BG_Qvale_Far/Mid/Near kept, tinted toward the dawn haze"),
         ("CHANGE", "Street framing: camera 1 u higher on flat street sections, walk line at ~30% from the bottom")], None)

# ---------------------------------------------------------------- M3 Qvale overlook, seated (scenic payoff)
over = os.path.join(CAP, "MR04_overlook_seated_1080.png")
c = sky(os.path.join(BG, "BG_Sky_Terraces.png"))
band(c, far, 1080, 470, 531, 300, (HAZE_DAWN, .08))
band(c, mid, 1080, 560, 520, 700, None)
c.alpha_composite(fg("MR04_overlook_seated"))
d = ImageDraw.Draw(c, "RGBA")
tag(d, (40, 60), "NEW", "BG_Qvale_Overlook_Vista: the valley below Qvale (placeholder: Terraces Far+Mid)", (0, 360, W, 820))
tag(d, (40, 860), "CHANGE", "non-repeating vista layer, only while seated or in its vista zone")
compare("M3_MR04_Qvale_overlook", "M3  MR04 Qvale, listening overlook (seated view)  -  Option B",
        over, c,
        [("NEW", "BG_Qvale_Overlook_Vista: one composed, non-repeating painting of the open valley (terraces, river, causeway below; no body)"),
         ("CHANGE", "Shown behind the overlook only (its own vista zone), cross-faded in as the seated camera eases out"),
         ("REUSED", "Tree, bench, terrain and Qori unchanged")], None)

# ---------------------------------------------------------------- M4 Cradle mid: Option A (coverage fix only)
c = sky(os.path.join(ROOT, "Assets", "Art", "Codex", "Backgrounds", "BG_Sky_A0.png"))
misty = load(os.path.join(ROOT, "Assets", "Art", "Backgrounds", "MistyValley_Background_v1.png"))
# The far painting at its current size (20 u), held across the whole region (no rectangle edge).
mh = round(20 * PX); ms = mh / misty.height
mi = misty.resize((round(misty.width * ms), mh), Image.LANCZOS)
top = round(H / 2 - (20 - 10.6) * PX)
c.alpha_composite(mi.crop((0, max(0, -top), mi.width, max(0, -top) + H)), ((W - mi.width) // 2, max(0, top)))
a0m, a0n = load(os.path.join(BG, "BG_A0_Mid.png")), load(os.path.join(BG, "BG_A0_Near.png"))
band(c, a0m, 7.2 * PX, 430, 267, 300)
band(c, a0n, 7.0 * PX, 640, 387, 800)
c.alpha_composite(fg("MR01_mid"))
d = ImageDraw.Draw(c, "RGBA")
tag(d, (40, 60), "CHANGE", "far painting follows at 0.985 (whole region covered): no hard rectangle edge")
tag(d, (40, 420), "REUSED", "MistyValley_Background_v1, BG_A0_Mid/Near (unchanged)")
compare("M4_MR01_Cradle_mid", "M4  MR01 The Cradle, mid beat  -  Option A (fix only)",
        os.path.join(CAP, "MR01_mid_1080.png"), c,
        [("CHANGE", "Far paintings (Cradle, Causeway, Ribwood, Heights) keep their own slow rate (0.985) so they cover the whole region: the visible rectangle edge goes"),
         ("REUSED", "All layers unchanged; Direction A palette for the Cradle needs Option B's new set (see M5)")], None)

# ---------------------------------------------------------------- M5 Summit mid: Option B/C (new set, placeholder)
qs = load(os.path.join(ROOT, "Assets", "Art", "Codex", "QuakeVistas", "Quake_Summit.png"))
c = qs.crop((0, 0, 1920, 1080)).filter(ImageFilter.GaussianBlur(1.2))
c = tint(c, HAZE_DAWN, .12)
c.alpha_composite(fg("MR07_mid"))
d = ImageDraw.Draw(c, "RGBA")
tag(d, (40, 60), "NEW", "BG_Summit_Far/Mid/Near: dawn high country, crags, falls (placeholder: Quake_Summit light only)", (0, 0, W, 560))
tag(d, (40, 620), "NEW", "terrain kit (out of BG scope): slate A6 rock clashes with the approved Summit", None)
compare("M5_MR07_Summit_mid", "M5  MR07 The Summit, mid beat  -  Option B (new layer set; placeholder art)",
        os.path.join(CAP, "MR07_mid_1080.png"), c,
        [("NEW", "Direction A Summit set: distant ranges over a cloud sea, limestone crags and falls, reed margins. The placeholder (the accepted Quake_Summit vista, blurred) shows the light and palette only: its lake belongs to the terrain, not to a background plane"),
         ("NEW", "Separately flagged: the slate A6 terrain kit also contradicts the approved Summit; that's a terrain request, not a background one")], None)

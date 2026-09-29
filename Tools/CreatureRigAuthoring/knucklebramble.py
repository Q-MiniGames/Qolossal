"""Turns Codex's Knucklebramble registration (Review 11) into the guardian's rig data.

Reads Tools/IncomingArt/Codex_v2/Review11/GUARDIAN_*.json (pixel space, y down, pre-normalisation
offsets scaled by GUARDIAN_SCALE's factor) and writes
Assets/Art/Characters/Knucklebramble/KnucklebrambleRig.json in Unity units (600 px/u, y up,
origin on the ground under the body's centre). Each pose gives every arm bone's world angle;
the builder turns them into local rotations. Also renders every pose (Codex's five plus the
three slams) to Tools/CreatureRigAuthoring/work/Knucklebramble_Poses.png, so a pose can be
checked by eye before it goes into the game.

  python knucklebramble.py
"""
import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
REVIEW11 = os.path.join(PROJECT, "Tools", "IncomingArt", "Codex_v2", "Review11")
PARTS = os.path.join(PROJECT, "Assets", "Art", "Codex", "Guardians")
OUT = os.path.join(PROJECT, "Assets", "Art", "Characters", "Knucklebramble", "KnucklebrambleRig.json")
PPU = 600.0

# Codex's poses (degrees, image space: y down) for each arm's upper and lower segment ...
CODEX_POSES = json.load(open(os.path.join(REVIEW11, "GUARDIAN_RIG.json")))["poses"]
# ... plus the slams, each arm striking its own side (facing right, Unity units from the origin):
# the back arm claws the floor behind, the front arm the floor in front, and the top arm swings
# over the head at jumping height in front. The elbow stays above the line to the target.
SLAM_TARGETS = {1: (-2.4, .15), 2: (2.2, .8), 3: (2.4, .15)}


def by_name(bones, name):
    return next(b for b in bones if b["name"] == name)


def ik(shoulder, l1, l2, target):
    """World angles (Unity degrees) of a two-bone arm reaching `target`, elbow on the upper side."""
    dx, dy = target[0] - shoulder[0], target[1] - shoulder[1]
    d = min(math.hypot(dx, dy), (l1 + l2) * .999)
    base = math.atan2(dy, dx)
    bend = math.acos((l1 * l1 + d * d - l2 * l2) / (2 * l1 * d))
    upper = base + bend if dx >= 0 else base - bend   # elbow above the shoulder-target line
    ex, ey = shoulder[0] + l1 * math.cos(upper), shoulder[1] + l1 * math.sin(upper)
    lower = math.atan2(target[1] - ey, target[0] - ex)
    return math.degrees(upper), math.degrees(lower)


def load(name):
    with open(os.path.join(REVIEW11, name)) as f:
        return json.load(f)


def main():
    reg = {r["name"].replace("Knucklebramble_", ""): r for r in load("GUARDIAN_REGISTRATION.json")}
    scale = load("GUARDIAN_SCALE.json")
    f = scale["normalization_factor"]
    rest = {r["name"]: r for r in load("GUARDIAN_POSE_TRANSFORMS.json")["Rest"]}

    def world_of(name, px):   # a saved sprite pixel -> final assembly pixels (y down)
        r = rest[name]
        rot = np.array(r["rotation_matrix"])
        return rot @ (np.array(px, float) - reg[name]["pad"]) + np.array(r["offset"]) * f

    body = reg["Body"]
    body_centre = world_of("Body", body["pivot_px_top_left"])
    origin = np.array([body_centre[0], scale["body_ground_y"]])

    def unity(p):   # final assembly pixels -> Unity units from the origin
        return [round((p[0] - origin[0]) / PPU, 5), round((origin[1] - p[1]) / PPU, 5)]

    def sprite_angle(name):   # the bone's direction inside the sprite, Unity degrees (y up)
        (x0, y0), (x1, y1) = reg[name]["bone_landmarks_px"]
        return -math.degrees(math.atan2(y1 - y0, x1 - x0))

    def length(name):
        (x0, y0), (x1, y1) = reg[name]["bone_landmarks_px"]
        return math.hypot(x1 - x0, y1 - y0) / PPU

    bones = [{"name": "Body", "parent": "", "sprite": "Knucklebramble_Body", "pos": unity(body_centre), "artRot": 0.0, "length": 0.0, "order": 10}]
    for i in (1, 2, 3):
        up, lo = f"Arm{i}_Upper", f"Arm{i}_Lower"
        shoulder = world_of(up, reg[up]["pivot_px_top_left"])
        bones.append({"name": up, "parent": "Body", "sprite": "Knucklebramble_" + up, "pos": unity(shoulder),
                      "artRot": -sprite_angle(up), "length": round(length(up), 5), "order": 2 * (3 - i)})
        bones.append({"name": lo, "parent": up, "sprite": "Knucklebramble_" + lo, "pos": None,
                      "artRot": -sprite_angle(lo), "length": round(length(lo), 5), "order": 2 * (3 - i) + 1})
    for name, parent, order in (("Head", "Body", 14), ("Jaw", "Head", 13), ("KnotGlow", "Body", 20)):
        bones.append({"name": name, "parent": parent, "sprite": "Knucklebramble_" + name,
                      "pos": unity(world_of(name, reg[name]["pivot_px_top_left"])), "artRot": 0.0, "length": 0.0, "order": order})

    poses = []
    for pose, arms in CODEX_POSES.items():
        angles = []
        for i in (1, 2, 3):
            angles += [-arms[str(i)][0], -arms[str(i)][1]]   # image degrees (y down) -> Unity (y up)
        poses.append({"name": pose, "armAngles": angles})
    rest_angles = poses[0]["armAngles"]
    for i, target in SLAM_TARGETS.items():
        up, lo = by_name(bones, f"Arm{i}_Upper"), by_name(bones, f"Arm{i}_Lower")
        angles = list(rest_angles)
        angles[2 * (i - 1):2 * i] = ik(up["pos"], up["length"], lo["length"], target)
        poses.append({"name": f"Arm{i}_Slam", "armAngles": [round(a, 3) for a in angles]})

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", newline="\n") as fh:
        json.dump({"ppu": PPU, "height": round(scale["rest_height_px"] / PPU, 4), "bones": bones, "poses": poses}, fh, indent=1)
        fh.write("\n")
    print("wrote", os.path.relpath(OUT, PROJECT))
    render(bones, poses)


def render(bones, poses):
    """Draws each pose from the saved parts, the same way the Unity rig will place them."""
    by = {b["name"]: b for b in bones}
    reg = {r["name"].replace("Knucklebramble_", ""): r for r in load("GUARDIAN_REGISTRATION.json")}
    size, cell = 1000, 4200   # a 7 x 7 u cell drawn at 600 px/u, then shrunk
    sheet = Image.new("RGB", (4 * 500, 2 * 520), "#485147")
    draw = ImageDraw.Draw(sheet)
    for n, pose in enumerate(poses):
        canvas = Image.new("RGBA", (cell, cell))
        ox, oy = cell / 2, cell * .85   # the origin (ground under the body) in the cell

        def place(name, pos_u, angle_deg, swap=None):
            im = Image.open(os.path.join(PARTS, f"Knucklebramble_{swap or name}.png")).convert("RGBA")
            px, py = reg.get(swap, reg[name])["pivot_px_top_left"]
            total = angle_deg + by[name]["artRot"]   # Unity degrees
            t = math.radians(-total)   # image rotation (y down)
            c, s = math.cos(t), math.sin(t)
            rot = np.array([[c, -s], [s, c]])
            target = np.array([ox + pos_u[0] * PPU, oy - pos_u[1] * PPU])
            off = target - rot @ np.array([px, py])
            inv = rot.T
            shift = -inv @ off
            canvas.alpha_composite(im.transform((cell, cell), Image.Transform.AFFINE, (*inv[0], shift[0], *inv[1], shift[1]), Image.Resampling.BILINEAR))

        items = []
        exposed = pose["name"] == "Knot_Exposed"
        for i in (1, 2, 3):
            up, lo = by[f"Arm{i}_Upper"], by[f"Arm{i}_Lower"]
            a_up, a_lo = pose["armAngles"][2 * (i - 1)], pose["armAngles"][2 * (i - 1) + 1]
            tip = [up["pos"][0] + up["length"] * math.cos(math.radians(a_up)), up["pos"][1] + up["length"] * math.sin(math.radians(a_up))]
            lift = 20 if pose["name"] == f"Arm{i}_Slam" and i == 2 else 0   # the top arm chops in front of the body
            items += [(up["order"] + lift, up["name"], up["pos"], a_up, None), (lo["order"] + lift, lo["name"], tip, a_lo, None)]
        for name in ("Body", "Head", "Jaw", "KnotGlow"):
            b = by[name]
            swap = "KnotExposed" if name == "Body" and exposed else "KnotGlow_Bare" if name == "KnotGlow" and exposed else None
            items.append((b["order"], name, b["pos"], 0.0, swap))
        for _, name, pos, ang, swap in sorted(items, key=lambda x: x[0]):
            place(name, pos, ang, swap)
        # Ground line and a 1 u tick either side of the origin.
        d = ImageDraw.Draw(canvas)
        d.line([(0, oy), (cell, oy)], fill=(255, 255, 255, 120), width=6)
        thumb = canvas.resize((500, 500), Image.Resampling.LANCZOS)
        x, y = (n % 4) * 500, (n // 4) * 520
        sheet.paste(thumb, (x, y + 20), thumb)
        draw.text((x + 10, y + 4), pose["name"], fill="white")
    os.makedirs(os.path.join(HERE, "work"), exist_ok=True)
    out = os.path.join(HERE, "work", "Knucklebramble_Poses.png")
    sheet.save(out)
    print("rendered", os.path.relpath(out, PROJECT))


if __name__ == "__main__":
    main()

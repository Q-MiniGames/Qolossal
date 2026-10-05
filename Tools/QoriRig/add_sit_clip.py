"""Add (or replace) Qori's "Sit" clip in Assets/Art/Characters/QoriRig/QoriRigData.json.

The seated pose for the listening bench: built from the Idle clip's first frame (arms, cape, ears,
weapon as at rest), with the hips settled, thighs forward along the seat, shins hanging over its
edge with a slow, gentle swing, the torso easing back a little and the head lifted toward the view.
A 3 s loop. The rig's parts, bones and every other clip are untouched. Rebuild the rig afterwards
(Qolossal > Qori Rig > Rebuild Rig Only) so the clip and its Animator state exist.

Usage: python add_sit_clip.py
"""
import json
import math
import os

HERE = os.path.dirname(os.path.abspath(__file__))
DATA = os.path.join(os.path.dirname(os.path.dirname(HERE)), "Assets", "Art", "Characters", "QoriRig", "QoriRigData.json")

LENGTH, KEYS = 3.0, 13
# Rotations (degrees, z) and other values that differ from Idle; a value or (base, swing) for the loop.
POSE = {
    ("Body", "localEulerAnglesRaw.z"): -2.0,
    ("Body", "m_LocalPosition.y"): -1.42,
    ("Body/Skirt", "localEulerAnglesRaw.z"): 10.0,
    ("Body/Skirt", "m_LocalScale.y"): .93,
    ("Body/Torso", "localEulerAnglesRaw.z"): 5.0,
    ("Body/Torso/Head", "localEulerAnglesRaw.z"): 6.0,
    ("Body/ThighNear", "localEulerAnglesRaw.z"): 88.0,
    ("Body/ThighNear/ShinNear", "localEulerAnglesRaw.z"): (-84.0, 7.0),
    ("Body/ThighNear/ShinNear/FootNear", "localEulerAnglesRaw.z"): 4.0,
    ("Body/ThighFar", "localEulerAnglesRaw.z"): 82.0,
    ("Body/ThighFar/ShinFar", "localEulerAnglesRaw.z"): (-76.0, -6.0),
    ("Body/ThighFar/ShinFar/FootFar", "localEulerAnglesRaw.z"): 12.0,
}


def main():
    data = json.load(open(DATA, encoding="utf-8"))
    idle = next(c for c in data["clips"] if c["name"] == "Idle")
    times = [round(LENGTH * i / (KEYS - 1), 5) for i in range(KEYS)]
    tracks, seen = [], set()
    for t in idle["tracks"]:
        key = (t["path"], t["attr"])
        seen.add(key)
        tracks.append({"path": t["path"], "attr": t["attr"], "v": [t["v"][0]] * KEYS, "t": [0.0] * KEYS})
    for key in POSE:
        if key not in seen:
            tracks.append({"path": key[0], "attr": key[1], "v": [0.0] * KEYS, "t": [0.0] * KEYS})
    for tr in tracks:
        p = POSE.get((tr["path"], tr["attr"]))
        if p is None:
            continue
        base, swing = (p, 0.0) if not isinstance(p, tuple) else p
        tr["v"] = [round(base + swing * math.sin(2 * math.pi * t / LENGTH), 5) for t in times]
        tr["t"] = [round(swing * 2 * math.pi / LENGTH * math.cos(2 * math.pi * t / LENGTH), 5) for t in times]
    clip = {"name": "Sit", "length": LENGTH, "loop": True, "times": times, "tracks": tracks}
    data["clips"] = [c for c in data["clips"] if c["name"] != "Sit"] + [clip]
    with open(DATA, "w", encoding="utf-8", newline="\n") as f:
        f.write(json.dumps(data, separators=(",", ":")))   # the file's own compact format
    print("Sit clip:", len(tracks), "tracks,", KEYS, "keys")


if __name__ == "__main__":
    main()

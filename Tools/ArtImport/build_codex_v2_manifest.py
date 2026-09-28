"""Build the import manifest for accepted Codex_v2 art.

The manifest (codex_v2_accepted.json) is the single source of truth that the
Unity importer (Assets/Editor/ArtImport/CodexArtImporter.cs) reads. It records,
per accepted file: the SHA-256 that was reviewed, where the file goes in
Assets/, and the sprite import settings (pixels per unit, pivot, wrap mode,
mesh type, 9-slice border, sub-sprite rects).

Pixels per unit are derived from the world size each asset should have in the
game (REQUEST_SNAPSHOT.md section 2, "Scale and resolution"), measured on the
opaque pixels so the transparent margin doesn't count. Every size that the
request doesn't state is marked "assumed" in the manifest's `basis` field.

Usage:
  python build_codex_v2_manifest.py accept NAME [NAME ...]   # add/refresh entries
  python build_codex_v2_manifest.py accept --all-present     # everything Codex lists as present
  python build_codex_v2_manifest.py rebuild                  # recompute settings, keep reviewed hashes
"""
import hashlib
import json
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
SOURCE = os.path.join(PROJECT, "Tools", "IncomingArt", "Codex_v2")
MANIFEST = os.path.join(HERE, "codex_v2_accepted.json")
DEST_ROOT = "Assets/Art/Codex"

QH = 1.65  # world units per Qori height (without ears)
TERRAIN_PPU = 120  # texel density of the terrain kit, backgrounds and decor
CHARACTER_PPU = 600  # nominal; rigs normalize final part scale from joints
PLAYER_PPU = 100  # the Qori rig's convention (QoriRigBuilder / Qori v1 parts)
UI_PPU = 100  # 1 sprite pixel = 1 canvas reference pixel

# Pairs whose files share one canvas and registration: the second uses the
# first's pixels per unit and pivot, so swapping states never shifts or scales.
SHARE_WITH = {
    "Barrier_Rubble_Pieces": "Barrier_Rubble_Intact",
    "Barrier_Thorns_Cut": "Barrier_Thorns_Intact",
    "Floor_Weak_Pieces": "Floor_Weak",
    "Switch_Seed_On": "Switch_Seed_Off",
    "Switch_Plate_Down": "Switch_Plate_Up",
    "Gate_Root_Closed_Bottom": "Gate_Root_Closed_Top",
    "Gate_Root_Open_Top": "Gate_Root_Closed_Top",
    "Gate_Root_Open_Bottom": "Gate_Root_Closed_Top",
    "Portal_Membrane_A0": "Portal_Gate_A0",
    "Portal_Gate_Hidden_Awake": "Portal_Gate_Hidden",
    "HUD_Resource_Bar_Fill": "HUD_Resource_Bar",
    "Platform_Crumble_Pieces": "Platform_Crumble",
    "Spitter_Head_Open": "Spitter_Head_Closed",
    "Shellback_Shell_Cracked": "Shellback_Shell_Intact",
    "GlowPod_Light_On": "GlowPod_Light_Off",
    "HUD_Health_Leaf_Empty": "HUD_Health_Leaf_Full",
    "HUD_Health_Leaf_Half": "HUD_Health_Leaf_Full",
    # Batch 5 world systems: each family shares one canvas (Phase 2 handoff).
    "Wakeknot_Dormant": "Wakeknot_Awake",
    "Wakeknot_Glow": "Wakeknot_Awake",
    "Wakeknot_RotSeal": "Wakeknot_Awake",
    "Waymark_Lit": "Waymark_Dormant",
    "Vein_Gate_Frame_Dormant": "Vein_Gate_Frame",
    "Vein_Membrane_Stable": "Vein_Gate_Frame",
    "Vein_Membrane_Wild": "Vein_Gate_Frame",
}

# World size of the opaque shape: (axis, world units, basis note).
WORLD_SIZE = {
    "Barrier_Rubble_Intact": ("h", 3.0, "request X-01: 1.5x3 u"),
    "Barrier_Thorns_Intact": ("h", 2.5, "assumed: passage-height curtain, 1.5 QH"),
    "Floor_Weak": ("w", 3.0, "request X-03: 3 u wide"),
    "Switch_Seed_Off": ("h", 1.0, "assumed: wall-mounted bud, 0.6 QH"),
    "Switch_Plate_Up": ("w", 1.5, "assumed: plate about one Qori stride"),
    "Gate_Root_Closed_Top": ("w", 1.0, "request X-06: 1x3 u (width)"),
    "Portal_Gate_A0": ("h", 3.5, "assumed: below the 4.5 u milestone arch (G-03)"),
    "Shrine_Ability": ("h", 1.6, "assumed: pedestal about Qori height"),
    "Shrine_Ability_RelicSlot": ("w", 1.4, "assumed: matches pedestal top"),
    "Relic_LivingThread": ("h", 0.8, "assumed: hovering relic, 0.5 QH"),
    "Relic_ClimbingMoss": ("h", 0.8, "assumed: hovering relic, 0.5 QH"),
    "Relic_Bloomfall": ("h", 0.8, "assumed: hovering relic, 0.5 QH"),
    "Platform_Crumble": ("w", 3.0, "assumed: same width as Floor_Weak"),
    "Platform_Moving_A0": ("w", 3.0, "assumed: 3 u moving platform"),
    "Platform_Moving_A1": ("w", 3.0, "assumed: 3 u including chains"),
    "Platform_Moving_A2": ("w", 3.0, "assumed: 3 u moving platform"),
    "Platform_Moving_A3": ("w", 3.0, "assumed: 3 u moving platform"),
    "Platform_Moving_A4": ("w", 3.0, "assumed: 3 u moving platform"),
    "GlowPod_Light_Off": ("h", 1.45, "Codex proof at 240 PPU: slightly shorter than Qori"),
}

# Opaque chain width in Platform_Moving_A1 (78 px) vs the chain tile (54 px).
CHAIN_SCALE = 54.0 / 78.0

TILE_H = {f"{a}_{k}" for a in ("A0", "A1", "A2", "A3", "A4") for k in ("Ground_Top", "Ceiling_Under", "Platform_OneWay_unused")} | { "Hazard_Thorns_Floor", "Hazard_Thorns_Ceiling",
          "Whip_Lash_Segment", "HUD_Vine_Segment", "Water_Surface", "Chart_Vein_Line"} | {f"Hazard_Thorns_Floor_A{i}" for i in range(1, 5)}
TILE_V = {f"{a}_Wall_Side" for a in ("A0", "A1", "A2", "A3", "A4")} | {"Waterfall_Column",  "Hazard_Thorns_Wall", "Platform_Moving_A1_Chain"}
TILE_FILL = {f"{a}_{k}" for a in ("A0", "A1", "A2", "A3", "A4") for k in ("Ground_Fill", "Wall_Climbable", "Wall_Slippery")} | {"Water_Body"}

BOTTOM_ANCHORED = {"Barrier_Rubble_Intact", "Barrier_Thorns_Intact", "Switch_Plate_Up", "Shrine_Ability",
                   "Portal_Gate_A0", "Spitter_Base", "GlowPod_Light_Off",
                   "Hazard_Thorns_Floor", "Waymark_Dormant", "Vein_Gate_Frame"}
TOP_ANCHORED = {"Hazard_Thorns_Ceiling"} | {f"{a}_Ceiling_Under" for a in ("A0", "A1", "A2", "A3", "A4")}

GROUND_TOP_WALK_LINE_PX = 96  # T-01: walk line measured from the top edge


def sha256(path):
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def source_path(category, name):
    return os.path.join(SOURCE, category, name + ".png")


def opaque_bbox(path):
    with Image.open(path) as im:
        return im.size, im.convert("RGBA").getchannel("A").getbbox()


def pivot_for(name, size, bbox):
    w, h = size
    x0, y0, x1, y1 = bbox
    cx = (x0 + x1) / 2 / w
    if name.endswith("_Ground_Top"):
        return [0.5, 1 - GROUND_TOP_WALK_LINE_PX / h]
    if name in BOTTOM_ANCHORED or name.startswith("Platform_") or name.startswith("Switch_") \
            or name.startswith("Floor_Weak") or "_Platform_" in name:
        return [round(cx, 5), round(1 - y1 / h, 5)]
    if name in TOP_ANCHORED:
        return [round(cx, 5), round(1 - y0 / h, 5)]
    return [0.5, 0.5]


QORI_PARTS = os.path.join(PROJECT, "Assets", "Art", "Characters", "QoriRig", "Parts")
# The Wilted's extra pieces share a Qori part's canvas and registration.
WILTED_SHARES = {"ThornArmor_Torso": "Torso", "Healed_Torso": "Torso", "ThornArmor_Head": "Head_Neutral"}
NINE_SLICE = {"UI_Dialogue_9Slice": (96, 96, 96, 96), "UI_NamePlate": (48, 24, 48, 24)}   # L, B, R, T


def qori_part_import(part):
    """Pivot and pixels per unit of a Qori rig part, from its .meta (the Wilted reuses the rig's bones)."""
    import re
    text = open(os.path.join(QORI_PARTS, f"Qori_{part}.png.meta"), encoding="utf-8").read()
    pivot = re.search(r"spritePivot: \{x: ([-\d.e]+), y: ([-\d.e]+)\}", text)
    ppu = re.search(r"spritePixelsToUnits: ([\d.]+)", text)
    return [float(pivot.group(1)), float(pivot.group(2))], float(ppu.group(1))


def settings_for(category, name):
    path = source_path(category, name)
    size, bbox = opaque_bbox(path)
    s = {"wrapU": "Clamp", "wrapV": "Clamp", "mesh": "Tight", "pivot": pivot_for(name, size, bbox), "border": None,
         "sprites": None, "mipmaps": False}

    # Repeat only along the tiling axis: a strip's transparent edge must not sample the
    # opaque pixels from its opposite edge, or a hairline appears along it.
    if name in TILE_H or name in TILE_FILL:
        s["wrapU"] = "Repeat"
    if name in TILE_V or name in TILE_FILL:
        s["wrapV"] = "Repeat"
    if name in TILE_H or name in TILE_V or name in TILE_FILL:
        s["mesh"] = "FullRect"  # required for SpriteRenderer tiled draw mode

    if name.startswith("Decor_Foreground_Frame"):
        s["ppu"], s["basis"] = TERRAIN_PPU, "screen-edge overlay at terrain density"
        s["pivot"] = [0.5, 0.5]
    elif category in ("Terrain", "Decor"):
        s["ppu"], s["basis"] = TERRAIN_PPU, "request: terrain density 120 px/u"
    elif category == "Backgrounds":
        s["ppu"], s["basis"] = TERRAIN_PPU, "request: terrain density 120 px/u"
        s["wrapU"], s["mesh"] = "Repeat", "FullRect"
        s["pivot"] = [0.5, 0.0]
    elif category == "Hazards" and name.startswith("Hazard_Thorns"):
        s["ppu"], s["basis"] = TERRAIN_PPU, "request: hazard strips at terrain density"
    elif category == "UI":
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"] = "FullRect"
    elif category == "Characters/Wilted":
        part = name[len("Wilted_"):]
        pivot, ppu = qori_part_import(WILTED_SHARES.get(part, part))
        s["ppu"], s["pivot"], s["basis"] = ppu, pivot, f"Qori rig registration (Qori_{WILTED_SHARES.get(part, part)})"
        s["mipmaps"] = True
    elif category in ("Characters/Loam", "Characters/Scribble", "Characters/Sproutling", "Characters/Echo"):
        s["ppu"], s["basis"] = CHARACTER_PPU, "request: character density 600 px/u (Characters/ASSEMBLY.json)"
        s["mipmaps"] = True
        if category == "Characters/Sproutling":
            s["pivot"] = [550 / 832, 1 - 748 / 832]   # the shared body anchor and ground line
    elif category in ("Characters/Portraits", "Characters/Dialogue"):
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"], s["pivot"] = "FullRect", [0.5, 0.5]
        if name in NINE_SLICE:
            s["border"] = list(NINE_SLICE[name])
    elif category in ("Chart", "StirVistas"):
        # The map screen and the stir's full-screen vistas are UI images; the map is zoomed, so it keeps mipmaps.
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"], s["pivot"], s["mipmaps"] = "FullRect", [0.5, 0.5], category == "Chart"
    elif category == "WorldSystems":
        s["ppu"], s["basis"] = TERRAIN_PPU, "Codex-normalized to terrain density 120 px/u (1 QH = 198 px)"
    elif category == "Player":
        s["ppu"], s["basis"] = PLAYER_PPU, "Qori rig convention; rig build sets final scale"
    elif name.startswith("Icon_"):
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"] = "FullRect"
    elif category in ("Enemies", "Effects", "Weapons"):
        s["ppu"], s["basis"] = CHARACTER_PPU, "request: character density 600 px/u (nominal)"
        # Rig parts are drawn at a fraction of their painted size: mipmaps keep them from aliasing.
        s["mipmaps"] = True
    elif name.startswith("Icon_"):
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"] = "FullRect"
    elif name in SHARE_WITH or name == "Platform_Moving_A1_Chain":
        s["ppu"], s["basis"] = None, "set from its primary in apply_shared"
    elif name in WORLD_SIZE:
        axis, units, basis = WORLD_SIZE[name]
        extent = (bbox[2] - bbox[0]) if axis == "w" else (bbox[3] - bbox[1])
        s["ppu"], s["basis"] = round(extent / units, 3), basis
    elif category in ("Props", "Hazards", "Backgrounds"):
        # From Batch 3 on, Codex normalizes props, hazards and paintings to terrain density.
        s["ppu"], s["basis"] = TERRAIN_PPU, "Codex-normalized to terrain density 120 px/u"
    else:
        raise SystemExit(f"No scale rule for {category}/{name}; add one to WORLD_SIZE.")
    return s


def load_status():
    with open(os.path.join(SOURCE, "DELIVERY_STATUS.json")) as f:
        return {x["name"]: x for x in json.load(f)}


def load_manifest():
    if not os.path.exists(MANIFEST):
        return {"source": "Tools/IncomingArt/Codex_v2", "destination": DEST_ROOT, "assets": []}
    with open(MANIFEST) as f:
        return json.load(f)


def decor_sprites(name):
    slices = os.path.join(SOURCE, "Decor", "QA", name.replace("_Sheet", "_Slices") + ".json")
    if not os.path.exists(slices):   # later batches name them A1_Slices.json
        slices = os.path.join(SOURCE, "Decor", "QA", name.replace("Decor_", "").replace("_Sheet", "_Slices") + ".json")
    if not os.path.exists(slices):
        return None
    with open(slices) as f:
        data = json.load(f)
    hanging = {"hanging_ivy", "hanging_root"}
    out = []
    for sp in data["sprites"]:
        x, y, w, h = sp["unity_rect_bottom_left_xywh"]
        out.append({"name": f"{name.replace('_Sheet', '')}_{sp['name']}", "rect": [x, y, w, h],
                    "pivot": sp["pivot"] if isinstance(sp.get("pivot"), list) else [0.5, 1.0] if sp["name"] in hanging else [0.5, 0.0]})
    return out


PIECE_SHEETS = {"Barrier_Rubble_Pieces", "Floor_Weak_Pieces", "Platform_Crumble_Pieces", "Shellback_Shell_Shards"} |     {f"Barrier_Rubble_Pieces_A{i}" for i in range(0, 5)} | {f"Shellback_Shell_Shards_A{i}" for i in range(1, 5)}


def rect_sprites(category, name, rects_file):
    """Sub-sprites from Codex's padded atlas rects (top-left origin), standing on their base."""
    with open(os.path.join(SOURCE, category, rects_file)) as f:
        rects = json.load(f)
    with Image.open(source_path(category, name)) as im:
        h = im.size[1]
    base = name.replace("_Sheet", "")
    return [{"name": f"{base}_{r['piece']:02d}", "rect": [r["rect"][0], h - r["rect"][1] - r["rect"][3], r["rect"][2], r["rect"][3]],
             "pivot": [0.5, 0.0]} for r in rects]


def piece_sprites(category, name):
    """One sub-sprite per separate chunk in a pieces sheet (connected opaque regions)."""
    import numpy as np
    from scipy import ndimage
    with Image.open(source_path(category, name)) as im:
        alpha = np.array(im.convert("RGBA"))[:, :, 3] > 30
    h = alpha.shape[0]
    labels, count = ndimage.label(ndimage.binary_dilation(alpha, iterations=3))
    boxes = ndimage.find_objects(labels)
    areas = ndimage.sum(alpha, labels, range(1, count + 1))
    keep = [i for i in range(count) if areas[i] >= .02 * max(areas)]
    out = []
    for n, i in enumerate(sorted(keep, key=lambda k: (boxes[k][1].start, boxes[k][0].start))):
        ys, xs = boxes[i]
        x0, x1 = max(0, xs.start - 2), min(alpha.shape[1], xs.stop + 2)
        y0, y1 = max(0, ys.start - 2), min(h, ys.stop + 2)
        out.append({"name": f"{name}_{n:02d}", "rect": [x0, h - y1, x1 - x0, y1 - y0], "pivot": [0.5, 0.5]})
    return out


def build_entry(category, name, reviewed_sha):
    entry = {"name": name, "category": category, "sha256": reviewed_sha,
             "dest": f"{DEST_ROOT}/{category}/{name}.png"}
    entry.update(settings_for(category, name))
    if category == "Decor" and name.endswith("_Sheet"):
        entry["sprites"] = decor_sprites(name)
    if name in PIECE_SHEETS:
        entry["sprites"] = piece_sprites(category, name)
    if name == "KnotChamber_Decor_Sheet":
        entry["sprites"] = rect_sprites(category, name, "KnotChamber_Decor_Rects.json")
    if name == "Chart_Frame_9Slice":
        with open(os.path.join(SOURCE, "Phase2", "QA", "NINE_SLICE.json")) as f:
            left, right, top, bottom = json.load(f)["border_left_right_top_bottom"]
        entry["border"] = [left, bottom, right, top]  # Unity order: x=L, y=B, z=R, w=T
    if category == "UI":
        notes_path = os.path.join(SOURCE, "UI", "QA", "NINE_SLICE_NOTES.json")
        with open(notes_path) as f:
            notes = json.load(f)
        if name in notes:
            left, top, right, bottom = notes[name]["border_left_top_right_bottom"]
            entry["border"] = [left, bottom, right, top]  # Unity order: x=L, y=B, z=R, w=T
    return entry


def apply_shared(entries):
    by_name = {e["name"]: e for e in entries}
    for name, primary in SHARE_WITH.items():
        if name in by_name and primary in by_name:
            by_name[name]["ppu"] = by_name[primary]["ppu"]
            by_name[name]["pivot"] = by_name[primary]["pivot"]
            by_name[name]["basis"] = f"shares scale and registration with {primary}"
    if "Platform_Moving_A1_Chain" in by_name and "Platform_Moving_A1" in by_name:
        chain = by_name["Platform_Moving_A1_Chain"]
        chain["ppu"] = round(by_name["Platform_Moving_A1"]["ppu"] * CHAIN_SCALE, 3)
        chain["basis"] = "matches Platform_Moving_A1 chain width (54 px tile core vs 78 px painted)"
        chain["pivot"] = [0.5, 0.5]


def write(manifest):
    manifest["assets"].sort(key=lambda e: (e["category"], e["name"]))
    apply_shared(manifest["assets"])
    with open(MANIFEST, "w", newline="\n") as f:
        json.dump(manifest, f, indent=1)
        f.write("\n")
    print(f"{len(manifest['assets'])} accepted assets -> {os.path.relpath(MANIFEST, PROJECT)}")


def main(argv):
    if not argv or argv[0] not in ("accept", "rebuild"):
        raise SystemExit(__doc__)
    status = load_status()
    manifest = load_manifest()
    existing = {e["name"]: e for e in manifest["assets"]}

    if argv[0] == "rebuild":
        manifest["assets"] = [build_entry(e["category"], e["name"], e["sha256"]) for e in existing.values()]
        return write(manifest)

    names = [n for n, x in status.items() if x["status"] == "present"] if argv[1:] == ["--all-present"] else argv[1:]
    for name in names:
        if name not in status:
            raise SystemExit(f"{name} is not in DELIVERY_STATUS.json")
        rel = status[name].get("path")
        category = os.path.dirname(rel).replace("\\", "/") if rel else status[name]["category"]
        path = source_path(category, name)
        if not os.path.exists(path):
            raise SystemExit(f"Missing file: {path}")
        existing[name] = build_entry(category, name, sha256(path))
    manifest["assets"] = list(existing.values())
    write(manifest)


if __name__ == "__main__":
    main(sys.argv[1:])

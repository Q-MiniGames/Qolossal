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
    "Platform_Crumble_Pieces": "Platform_Crumble",
    "Spitter_Head_Open": "Spitter_Head_Closed",
    "Shellback_Shell_Cracked": "Shellback_Shell_Intact",
    "HUD_Health_Leaf_Empty": "HUD_Health_Leaf_Full",
    "HUD_Health_Leaf_Half": "HUD_Health_Leaf_Full",
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
}

# Opaque chain width in Platform_Moving_A1 (78 px) vs the chain tile (54 px).
CHAIN_SCALE = 54.0 / 78.0

TILE_H = {"A0_Ground_Top", "A0_Ceiling_Under", "Hazard_Thorns_Floor", "Hazard_Thorns_Ceiling",
          "Whip_Lash_Segment"}
TILE_V = {"A0_Wall_Side", "Hazard_Thorns_Wall", "Platform_Moving_A1_Chain"}
TILE_FILL = {"A0_Ground_Fill", "A0_Wall_Climbable", "A0_Wall_Slippery"}

BOTTOM_ANCHORED = {"Barrier_Rubble_Intact", "Barrier_Thorns_Intact", "Switch_Plate_Up", "Shrine_Ability",
                   "Portal_Gate_A0", "Spitter_Base",
                   "Hazard_Thorns_Floor"}
TOP_ANCHORED = {"Hazard_Thorns_Ceiling", "A0_Ceiling_Under"}

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
    if name == "A0_Ground_Top":
        return [0.5, 1 - GROUND_TOP_WALK_LINE_PX / h]
    if name in BOTTOM_ANCHORED or name.startswith("Platform_") or name.startswith("Switch_") \
            or name.startswith("Floor_Weak") or name.startswith("A0_Platform"):
        return [round(cx, 5), round(1 - y1 / h, 5)]
    if name in TOP_ANCHORED:
        return [round(cx, 5), round(1 - y0 / h, 5)]
    return [0.5, 0.5]


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

    if category in ("Terrain", "Decor"):
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
    elif category == "Player":
        s["ppu"], s["basis"] = PLAYER_PPU, "Qori rig convention; rig build sets final scale"
    elif category in ("Enemies", "Effects", "Weapons"):
        s["ppu"], s["basis"] = CHARACTER_PPU, "request: character density 600 px/u (nominal)"
    elif name.startswith("Icon_"):
        s["ppu"], s["basis"] = UI_PPU, "UI canvas: 1 px = 1 reference px"
        s["mesh"] = "FullRect"
    elif name in SHARE_WITH or name == "Platform_Moving_A1_Chain":
        s["ppu"], s["basis"] = None, "set from its primary in apply_shared"
    elif name in WORLD_SIZE:
        axis, units, basis = WORLD_SIZE[name]
        extent = (bbox[2] - bbox[0]) if axis == "w" else (bbox[3] - bbox[1])
        s["ppu"], s["basis"] = round(extent / units, 3), basis
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
    if not os.path.exists(slices):
        return None
    with open(slices) as f:
        data = json.load(f)
    hanging = {"hanging_ivy", "hanging_root"}
    out = []
    for sp in data["sprites"]:
        x, y, w, h = sp["unity_rect_bottom_left_xywh"]
        out.append({"name": f"{name.replace('_Sheet', '')}_{sp['name']}", "rect": [x, y, w, h],
                    "pivot": [0.5, 1.0] if sp["name"] in hanging else [0.5, 0.0]})
    return out


def build_entry(category, name, reviewed_sha):
    entry = {"name": name, "category": category, "sha256": reviewed_sha,
             "dest": f"{DEST_ROOT}/{category}/{name}.png"}
    entry.update(settings_for(category, name))
    if category == "Decor":
        entry["sprites"] = decor_sprites(name)
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
        category = status[name]["category"]
        path = source_path(category, name)
        if not os.path.exists(path):
            raise SystemExit(f"Missing file: {path}")
        existing[name] = build_entry(category, name, sha256(path))
    manifest["assets"] = list(existing.values())
    write(manifest)


if __name__ == "__main__":
    main(sys.argv[1:])

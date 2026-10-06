"""Convert the Mountain Relief Atlas design package into the prototype's runtime data.

The package (Data/WORLD_BUILD_SPEC.json and friends) uses nested arrays that Unity's JsonUtility
can't read. This writes one JsonUtility-friendly file, Assets/MountainReliefAtlas/Resources/MRA/
mra_world.json, that both the editor builder and the runtime (Chart, exits, chambers) read. It also
copies the eight relief paintings into Assets/MountainReliefAtlas/Resources/MRA/Maps as review-only copies
(they are design-review sources, never accepted art) and records every source hash, so each
imported value can be traced back to the package.

Deterministic: the same package gives byte-identical output.

Usage: python convert_spec.py [package_folder]
"""
import hashlib
import json
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(os.path.dirname(HERE))
DEFAULT_PACKAGE = r"C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\DesignReview\MountainReliefAtlas_20261003"
OUT_DATA = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "mra_world.json")
OUT_MAPS = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Resources", "MRA", "Maps")
TRACE = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "SOURCE_TRACE.json")
OUT_REVIEW_ART = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "ReviewArt")
OUT_FOLK = os.path.join(PROJECT, "Assets", "MountainReliefAtlas", "Editor", "townsfolk_rigs.json")
FINAL_ASSEMBLY = "Batch11_A_Chart_Final_7Patches_DESIGNER_SPOILERS_CONCEPT_1920x1080.png"


def sha(path):
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


def v2(p):
    return {"x": float(p[0]), "y": float(p[1])}


def v4(r):
    return {"x": float(r[0]), "y": float(r[1]), "z": float(r[2]), "w": float(r[3])}


def rects(value):
    """A rect ([x, y, w, h]) or a list of them, as a list of Vector4."""
    if not value:
        return []
    return [v4(value)] if isinstance(value[0], (int, float)) else [v4(r) for r in value]


def points(value):
    """A point or a list of points, as a list of Vector2."""
    if not value:
        return []
    return [v2(value)] if isinstance(value[0], (int, float)) else [v2(p) for p in value]


def module(m):
    if not m:
        return {"template": ""}
    return {
        "template": m.get("template", ""),
        "position": v2(m["position"]),
        "gateSize": v2(m.get("gate_size", [0, 0])),
        "bridgeWidth": m.get("bridge_width", 0.0),
        "gapWidth": m.get("gap_width", 0.0),
        "anchorHeight": m.get("anchor_height", 0.0),
        "landingWidth": m.get("landing_width", 0.0),
        "catchDrop": m.get("catch_floor_drop", 0.0),
        "landingDrop": m.get("landing_drop", 0.0),
        "returnRise": m.get("return_steps_max_rise", 0.0),
        "saveFlag": m.get("save_flag", ""),
        "amplitude": m.get("visual_amplitude", 0.0),
        "period": m.get("period_seconds", 0.0),
    }


def main(package):
    spec_path = os.path.join(package, "Data", "WORLD_BUILD_SPEC.json")
    chart_path = os.path.join(package, "Data", "CHART_REGISTRATION.json")
    enc_path = os.path.join(package, "Data", "ENCOUNTER_SCHEDULE.json")
    spec = json.load(open(spec_path, encoding="utf-8"))
    chart = json.load(open(chart_path, encoding="utf-8"))
    enc = json.load(open(enc_path, encoding="utf-8"))
    chart_edges = chart.get("edge_chart_polylines", {})

    regions = []
    for r in spec["regions"]:
        xmin, ymin, xmax, ymax = r["camera_bounds"]
        nodes = [{
            "id": n["id"], "name": n["name"], "position": v2(n["position"]), "spawn": v2(n["spawn"]),
            "padWidth": n["safe_pad"]["width"], "padDepth": n["safe_pad"]["depth"],
            "checkpoint": n["checkpoint"], "waymark": n["waymark"], "checkpointId": n.get("checkpoint_id", ""),
            "ability": n.get("ability", ""), "knot": n.get("knot", ""), "showWhen": n.get("show_on_chart_when", ""),
            "chartUv": v2(n["chart_uv"]), "orthoSize": n["camera"]["ortho_size"],
        } for n in r["nodes"]]
        edges = [{
            "id": e["id"], "source": e["source"], "target": e["target"], "kind": e["kind"],
            "surfaceKind": e["surface"]["kind"], "points": [v2(p) for p in e["surface"]["points"]],
            "depth": e["surface"].get("solid_depth", 6), "requires": e["requires"],
            "module": module(e["module"]),
            # CHART_REGISTRATION is authoritative where it refines an edge (the Summit's dry shore).
            "chartLine": [v2(p) for p in chart_edges.get(e["id"], e["chart_polyline"])],
        } for e in r["edges"]]
        regions.append({
            "id": r["id"], "name": r["name"], "slug": r["slug"], "kit": r["kit"], "ordinal": r["ordinal"],
            "scene": r["proposedScene"], "map": os.path.splitext(r["file"])[0],
            "size": v2(r["size"]), "cameraMin": v2([xmin, ymin]), "cameraMax": v2([xmax, ymax]),
            "grant": r["grant"], "grantAt": r["grantAt"], "knotAt": r["knotAt"],
            "entry": r["entry"], "exit": r["exit"], "primaryWaymark": r["primary_waymark"],
            "visibility": r["source_visibility"], "landmarks": r["landmarks"], "chamberIds": r["chamber_ids"],
            "nodes": nodes, "edges": edges,
        })

    # The user's decision (4 Oct 2026): Qvale's homes and the musician's loft are enterable places,
    # each its own interior scene entered and left like a side chamber, instead of the package's
    # in-place cutaways. The smithy stays an open-fronted forge on the street.
    house_instances = {"MR04_C02", "MR04_C03", "MR04_C04", "MR04_C05", "MR04_C06"}

    chambers = []
    for c in spec["chambers"]:
        g = c["local_geometry"]
        house = c["id"] in house_instances
        chambers.append({
            "id": c["id"], "name": c["name"], "region": c["region_id"], "type": c["type"],
            "reward": c["reward"], "rewardId": c["reward_id"], "entranceNode": c["entrance_node"],
            "entrance": v2(c["entrance_position"]), "prerequisite": c["prerequisite"],
            "presentation": "house-instance" if house else c["presentation"],
            "scene": ("MRAtlas_" + c["id"]) if house else c["proposed_scene"],
            "playerSpawn": v2(c["player_spawn"]), "returnId": c["return_id"], "returnSpawn": v2(c["return_parent_spawn"]),
            "chartUv": v2(c["chart_entrance_uv"]),
            "size": v2(g["size"]), "objective": g.get("objective", ""), "solution": g.get("solution", ""),
            "platforms": rects(g.get("platforms")), "walls": rects(g.get("walls")), "gates": rects(g.get("gate")),
            "water": rects(g.get("water")), "hazards": rects(g.get("hazard")), "catchFloor": rects(g.get("catch_floor")),
            "switches": points(g.get("switches")) + points(g.get("switch")), "anchors": points(g.get("anchors")),
            "pods": points(g.get("pods")), "plate": points(g.get("plate")), "crate": points(g.get("crate")),
            "lever": points(g.get("lever")), "resident": points(g.get("resident")), "rewardAt": points(g.get("reward")),
        })

    links = [{
        "id": l["id"], "source": l["source"], "target": l["target"], "sourceScene": l["source_scene"],
        "targetScene": l["target_scene"], "arrival": v2(l["arrival_target"]), "reverseArrival": v2(l["reverse_arrival"]),
        "requires": l["requires"],
    } for l in spec["cross_region_links"]]

    encounters = [{
        "id": e["id"], "region": e["region"], "edge": e["edge"], "prefab": e["prefab"], "prefabSha": e["prefab_sha256"],
        "center": v2(e["ground_center"]), "patrol": v2(e["patrol_bounds_x"]), "count": e["count"], "shelfWidth": e["shelf_width"],
    } for e in enc["encounters"]]

    m = spec["conservative_metrics"]
    world = {
        "schema": 1,
        "source": "MountainReliefAtlas_20261003",
        "specSha256": sha(spec_path), "chartSha256": sha(chart_path), "encounterSha256": sha(enc_path),
        "revealFlag": spec["presentation"]["reveal_flag"],
        "metrics": {"jumpRise": m["jump_rise"], "jumpGap": m["jump_gap"], "stepRise": m["step_rise"],
                    "landingWidth": m["landing_width"], "anchorReach": m["anchor_reach"], "headroom": m["minimum_headroom"]},
        "regions": regions, "chambers": chambers, "links": links, "encounters": encounters,
    }
    os.makedirs(os.path.dirname(OUT_DATA), exist_ok=True)
    with open(OUT_DATA, "w", encoding="utf-8", newline="\n") as f:
        json.dump(world, f, indent=1, ensure_ascii=False)
        f.write("\n")

    # Review-only copies of the paintings, byte-identical to the package's archived sources.
    os.makedirs(OUT_MAPS, exist_ok=True)
    maps = {}
    for r in spec["regions"]:
        src = os.path.join(package, "Art", "Sources", r["file"])
        dst = os.path.join(OUT_MAPS, r["file"])
        if not os.path.exists(dst) or sha(dst) != sha(src):
            shutil.copyfile(src, dst)
        maps[r["file"]] = {"source": os.path.relpath(src, package).replace("\\", "/"), "sha256": sha(src),
                           "copy": os.path.relpath(dst, PROJECT).replace("\\", "/"), "status": "review-only design source, not accepted art"}
    # The post-reveal final assembly: the approved Batch 11 Chart concept (direction A), copied
    # unchanged as a review-only import for the prototype's reveal and post-reveal Chart.
    concept = os.path.join(PROJECT, "Tools", "IncomingArt", "Codex_v2", "Concepts", "Batch11", FINAL_ASSEMBLY)
    concept_out = os.path.join(OUT_REVIEW_ART, FINAL_ASSEMBLY)
    os.makedirs(OUT_REVIEW_ART, exist_ok=True)
    if not os.path.exists(concept_out) or sha(concept_out) != sha(concept):
        shutil.copyfile(concept, concept_out)
    maps[FINAL_ASSEMBLY] = {"source": os.path.relpath(concept, PROJECT).replace("\\", "/"), "sha256": sha(concept),
                            "copy": os.path.relpath(concept_out, PROJECT).replace("\\", "/"),
                            "status": "review-only concept (Review 22: Chart round 2, user pick pending production), shown only after the reveal"}
    # The accepted townspeople rigs' rest pose (Batch 8 W4 registrations), for static figures:
    # each part's sprite pivot goes to its placement on the shared assembly canvas (top-left px).
    folk = []
    reg_dir = os.path.join(PROJECT, "Tools", "IncomingArt", "Codex_v2", "Batch8", "W4")
    for who in ["Smith", "Farmer", "Keeper", "Herbalist", "Musician", "Elder", "Child"]:
        r = json.load(open(os.path.join(reg_dir, who + "_REGISTRATION.json"), encoding="utf-8"))
        # Body and Head placements are canvas top-left corners; arm shoulder/elbow points are the
        # parts' pivots (checked against Codex's Rest_AssembledRGBA proof). Everything is written as
        # the pivot's position. The feet: the body's lowest opaque row, under its pivot.
        body, head, pl = r["parts"]["Body"], r["parts"]["Head"], r["placements"]
        from PIL import Image
        body_png = os.path.join(PROJECT, "Tools", "IncomingArt", "Codex_v2", "Characters", "Town", body["file"])
        feet_row = Image.open(body_png).convert("RGBA").getchannel("A").getbbox()[3]
        def pivot_at(place, part):
            return [place[0] + part["pivot_px"][0], place[1] + part["pivot_px"][1]]
        ground = [pl["Body"][0] + body["pivot_px"][0], pl["Body"][1] + feet_row]
        parts = [{"part": "Body", "at": v2(pivot_at(pl["Body"], body)), "angle": 0.0},
                 {"part": "Head", "at": v2(pivot_at(pl["Head"], head)), "angle": 0.0}]
        for arm in pl["Rest"]:
            parts.append({"part": "Arm" + arm["side"] + "Upper", "at": v2(arm["shoulder"]), "angle": float(arm["upper_angle_deg"])})
            parts.append({"part": "Arm" + arm["side"] + "Lower", "at": v2(arm["elbow"]), "angle": float(arm["lower_angle_deg"])})
        order = r["draw_order"]
        for p in parts:
            p["order"] = order.index(p["part"])
        folk.append({"who": who, "ppu": r["ppu"], "ground": v2(ground), "parts": parts})
    os.makedirs(os.path.dirname(OUT_FOLK), exist_ok=True)
    with open(OUT_FOLK, "w", encoding="utf-8", newline="\n") as f:
        json.dump({"source": "Tools/IncomingArt/Codex_v2/Batch8/W4/*_REGISTRATION.json", "folk": folk}, f, indent=1)
        f.write("\n")

    trace = {"package": package, "data": {"WORLD_BUILD_SPEC.json": world["specSha256"], "CHART_REGISTRATION.json": world["chartSha256"],
                                          "ENCOUNTER_SCHEDULE.json": world["encounterSha256"]},
             "output": os.path.relpath(OUT_DATA, PROJECT).replace("\\", "/"), "maps": maps}
    with open(TRACE, "w", encoding="utf-8", newline="\n") as f:
        json.dump(trace, f, indent=1)
        f.write("\n")
    print(f"{len(regions)} regions, {sum(len(r['nodes']) for r in regions)} beats, {sum(len(r['edges']) for r in regions)} edges, "
          f"{len(chambers)} chambers, {len(links)} links, {len(encounters)} encounters -> {os.path.relpath(OUT_DATA, PROJECT)}")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else DEFAULT_PACKAGE)

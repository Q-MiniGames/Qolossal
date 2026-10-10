"""Builds Tools/Audio/SFX_SELECTOR.html: a local page listing every sound in SfxLibrary.asset with
each take, its audition state, whether the game plays it, and a Keep / Remove choice. Export saves
the choices as sfx_selection.json; nothing in the project changes until a session applies that file.

Run from the repo root:  python Tools/Audio/build_sfx_selector.py
"""
import glob, html, json, os, re, datetime

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LIB = os.path.join(ROOT, "Assets", "Audio", "Resources", "SfxLibrary.asset")
OUT = os.path.join(ROOT, "Tools", "Audio", "SFX_SELECTOR.html")
BUSES = ["Player", "Combat", "Enemy", "World", "Pickup", "Town", "UI", "Story", "Ambience"]
STATES = ["Unheard", "Candidate", "Favorite", "Rejected"]
# Sounds the library still lists as wired that the code no longer plays: QoriSounds.StepId is always
# Qori_Step_Water (8 Oct), so the per-surface steps are kept but unused.
NOT_PLAYED = {
    "Qori_Step_Moss": "unused: Qori_Step_Water Take 3 is the one footstep (QoriSounds.StepId)",
    "Qori_Step_Stone": "unused: Qori_Step_Water Take 3 is the one footstep (QoriSounds.StepId)",
    "Qori_Step_Wood": "unused: Qori_Step_Water Take 3 is the one footstep (QoriSounds.StepId)",
}


def scalar(v):
    v = v.strip()
    if len(v) >= 2 and v[0] == v[-1] == "'":
        v = v[1:-1].replace("''", "'")
    return v


def parse_library():
    """Line parser for the entries list (two-space YAML indents, as Unity writes it)."""
    lines = open(LIB, encoding="utf-8").read().splitlines()
    entries, e, take, section, key = [], None, None, None, None
    for line in lines:
        if line.startswith("  scenes:"):
            break
        m = re.match(r"^  - id: (.*)$", line)
        if m:
            e = {"id": scalar(m.group(1)), "takes": [], "clips": []}
            entries.append(e)
            section, take, key = None, None, None
            continue
        if e is None:
            continue
        m = re.match(r"^    (\w+):\s?(.*)$", line)
        if m:
            key, val = m.group(1), m.group(2)
            section = key if key in ("takes", "clips", "gains", "supersedes") and val == "" else None
            if section is None:
                e[key] = scalar(val)
            continue
        if section == "takes":
            m = re.match(r"^    - sourcePath: (.*)$", line)
            if m:
                take = {"sourcePath": scalar(m.group(1))}
                e["takes"].append(take)
                continue
            m = re.match(r"^      (\w+):\s?(.*)$", line)
            if m and take is not None:
                key = m.group(1)
                take[key] = scalar(m.group(2))
                continue
        if section == "clips":
            m = re.search(r"guid: ([0-9a-f]{32})", line)
            if m:
                e["clips"].append(m.group(1))
                continue
        # Continuation of a folded scalar (prompt, gameEvent, flags...).
        m = re.match(r"^\s{6,}(\S.*)$", line)
        if m and key:
            target = take if (section == "takes" and take is not None) else e
            if isinstance(target.get(key), str):
                target[key] = (target[key] + " " + scalar(m.group(1))).strip()
    return entries


def guid_map():
    out = {}
    for meta in glob.glob(os.path.join(ROOT, "Assets", "Audio", "SFX", "**", "*.wav.meta"), recursive=True):
        with open(meta, encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    out[line.split()[1]] = os.path.relpath(meta[:-5], ROOT).replace("\\", "/")
                    break
    return out


def build():
    guids = guid_map()
    entries = parse_library()
    referenced = set()
    sounds = []
    for e in entries:
        in_use = {guids.get(g, "?" + g) for g in e["clips"]}
        hooked = e.get("status", "") in ("wired", "wired-unplaced") and e["id"] not in NOT_PLAYED
        status = NOT_PLAYED.get(e["id"], e.get("status", ""))
        takes = []
        for i, t in enumerate(e["takes"]):
            src, loop = t["sourcePath"], t.get("loopPath", "")
            uses_loop = bool(loop) and t.get("useLoopDerivative", "1") == "1"
            referenced.add(src)
            if loop:
                referenced.add(loop)
            state = STATES[int(t.get("state", "0") or 0)]
            used = hooked and (src in in_use or (loop and loop in in_use))
            takes.append({
                "n": i + 1,
                "file": src,
                "loop": loop,
                "play": loop if uses_loop else src,
                "state": state,
                "heard": t.get("heard", "0") == "1",
                "inUse": bool(used),
                "flags": t.get("flags", ""),
                "note": t.get("note", ""),
                "choice": "remove" if state == "Rejected" else "keep",
            })
        sounds.append({
            "id": e["id"],
            "bus": BUSES[int(e.get("bus", "0") or 0)],
            "loop": e.get("loop", "0") == "1",
            "event": e.get("gameEvent", ""),
            "hook": e.get("hook", ""),
            "status": status,
            "hooked": hooked,
            "takes": takes,
        })
    on_disk = sorted(os.path.relpath(p, ROOT).replace("\\", "/")
                     for p in glob.glob(os.path.join(ROOT, "Assets", "Audio", "SFX", "**", "*.wav"), recursive=True))
    orphans = [p for p in on_disk if p not in referenced]
    return sounds, orphans, len(on_disk)


def main():
    sounds, orphans, files = build()
    data = {
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "library": "Assets/Audio/Resources/SfxLibrary.asset",
        "filesOnDisk": files,
        "sounds": sounds,
        "orphans": orphans,
    }
    template = open(os.path.join(os.path.dirname(__file__), "sfx_selector_template.html"), encoding="utf-8").read()
    page = template.replace("/*__DATA__*/null", json.dumps(data, ensure_ascii=False).replace("</", "<\\/"))
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write(page)
    takes = sum(len(s["takes"]) for s in sounds)
    print(f"{len(sounds)} sounds, {takes} takes, {len(orphans)} unlisted files -> {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    main()

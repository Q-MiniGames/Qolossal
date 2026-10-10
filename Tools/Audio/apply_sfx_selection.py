"""Applies an sfx_selection.json exported by SFX_SELECTOR.html to SfxLibrary.asset: every take marked
Remove becomes Rejected, every Rejected take marked Keep goes back to Unheard (never played, left out of builds; the WAV stays in the project), and each
sound's runtime clips and gains are rebuilt as SfxLibraryBuilder.RefreshClips does.

Run from the repo root:  python Tools/Audio/apply_sfx_selection.py [path/to/sfx_selection.json]
With --check it only rebuilds clips from the current states and reports any difference.
"""
import glob, json, math, os, re, struct, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
LIB = os.path.join(ROOT, "Assets", "Audio", "Resources", "SfxLibrary.asset")
REJECTED, FAVORITE = 3, 2


def f32(v):
    return struct.unpack("f", struct.pack("f", v))[0]


def unity_float(v):
    v = f32(v)
    if v == int(v):
        return str(int(v))
    for p in range(1, 10):
        s = f"{v:.{p}g}"
        if f32(float(s)) == v:
            return s
    return repr(v)


def guids():
    out = {}
    for meta in glob.glob(os.path.join(ROOT, "Assets", "Audio", "SFX", "**", "*.wav.meta"), recursive=True):
        with open(meta, encoding="utf-8") as f:
            for line in f:
                if line.startswith("guid:"):
                    out[os.path.relpath(meta[:-5], ROOT).replace("\\", "/")] = line.split()[1]
                    break
    return out


def main():
    check = "--check" in sys.argv
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    remove, keep = set(), set()
    if not check:
        path = args[0] if args else os.path.join(os.path.expanduser("~"), "Downloads", "sfx_selection.json")
        sel = json.load(open(path, encoding="utf-8"))
        assert sel.get("kind") == "qolossal-sfx-selection", "not an SFX selection export"
        remove = set(sel["remove"])
        keep = {t["file"] for snd in sel["sounds"] for t in snd["takes"] if t["choice"] == "keep"}

    text = open(LIB, encoding="utf-8").read()
    headroom = float(re.search(r"^  headroomDb: (.*)$", text, re.M).group(1))
    g = guids()
    lines = text.split("\n")
    out, i, changed, rejected_now, restored_now = [], 0, [], 0, 0

    # Walk entry by entry: copy lines, fix take states, then replace the clips/gains block.
    entry_id, loop, takes, take = None, False, [], None
    while i < len(lines):
        line = lines[i]
        m = re.match(r"^  - id: (.*)$", line)
        if m:
            entry_id, loop, takes, take = m.group(1).strip(), False, [], None
        elif entry_id and re.match(r"^    loop: ", line):
            loop = line.split(":", 1)[1].strip() == "1"
        elif entry_id and re.match(r"^    - sourcePath: ", line):
            take = {"src": line.split(":", 1)[1].strip()}
            takes.append(take)
        elif take is not None and re.match(r"^      (loopPath|gainDb|useLoopDerivative):", line):
            k, v = line.strip().split(":", 1)
            take[k] = v.strip()
        elif take is not None and re.match(r"^      state: ", line):
            take["state"] = int(line.split(":", 1)[1])
            if take["src"] in remove and take["state"] != REJECTED:
                take["state"] = REJECTED
                rejected_now += 1
                line = "      state: %d" % REJECTED
            elif take["src"] in keep and take["state"] == REJECTED:
                # Brought back by a later selection: in use again, no decision yet.
                take["state"] = 0
                restored_now += 1
                line = "      state: 0"
        elif entry_id and line == "    clips:" or (entry_id and line.startswith("    clips: [")):
            # Skip the old clips and gains blocks.
            j = i + 1
            while j < len(lines) and lines[j].startswith("    - {fileID"):
                j += 1
            assert lines[j].startswith("    gains:"), (entry_id, lines[j])
            j += 1
            while j < len(lines) and lines[j].startswith("    - ") and not lines[j].startswith("    - sourcePath"):
                j += 1
            old = "\n".join(lines[i:j])
            # Keep Unity's own gain for a clip that stays (ours can differ in the last digit).
            old_guids = re.findall(r"guid: ([0-9a-f]{32})", old)
            old_gains = [l[6:] for l in lines[i:j] if l.startswith("    - ") and "fileID" not in l]
            kept = dict(zip(old_guids, old_gains)) if len(old_guids) == len(old_gains) else {}
            anyfav = any(t["state"] == FAVORITE for t in takes)
            clips, gains = [], []
            for t in takes:
                if (t["state"] != FAVORITE) if anyfav else (t["state"] == REJECTED):
                    continue
                use_loop = loop and t.get("useLoopDerivative") == "1" and t.get("loopPath")
                p = t["loopPath"] if use_loop else t["src"]
                if p not in g:
                    print("missing clip:", p)
                    continue
                clips.append(g[p])
                gains.append(kept.get(g[p]) or min(1.0, f32(math.pow(10, f32(f32(f32(float(t["gainDb"])) - f32(headroom)) / 20)))))   # as Mathf.Pow
            block = (["    clips:"] + ["    - {fileID: 8300000, guid: %s, type: 3}" % c for c in clips] if clips else ["    clips: []"])
            block += (["    gains:"] + ["    - " + (v if isinstance(v, str) else unity_float(v)) for v in gains] if gains else ["    gains: []"])
            new = "\n".join(block)
            if new != old:
                changed.append(entry_id)
            out.extend(block)
            entry_id, take = None, None
            i = j
            continue
        elif line.startswith("  scenes:"):
            entry_id = None
        out.append(line)
        i += 1

    print(f"{rejected_now} takes newly Rejected, {restored_now} restored; clips rebuilt differently for {len(changed)} sounds")
    if check:
        print("differences:", changed)
        if os.environ.get("SFX_CHECK_OUT"):
            open(os.environ["SFX_CHECK_OUT"], "w", encoding="utf-8", newline="\n").write("\n".join(out))
        return
    with open(LIB, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(out))


if __name__ == "__main__":
    main()

"""Builds the Unity-side sound-effect library from the ElevenLabs delivery.

  python Tools/Audio/build_sfx_library.py [delivery_dir]

1. Audits the delivery (audit_sfx_library.py): every file hash must match Codex's receipts.
2. Copies each source WAV unchanged into Assets/Audio/SFX/<Bus>/ and checks its hash again there.
   Never overwrites a file whose hash differs (it stops instead).
3. Measures each take's active loudness and peak, for a gain that matches it to its bus target.
4. For every sound that must loop, checks each take's loop seam; a take whose seam would click,
   jump or dip gets a separate crossfaded derivative in Assets/Audio/SFX/Loops/ (the source is
   untouched). Each derivative's own seam is checked.
5. Writes Assets/Audio/SFX/sfx_manifest.json (read by Unity's library builder) and
   Tools/Audio/SFX_MAPPING.csv (the readable mapping).
.meta files are written with deterministic GUIDs (from the asset path) so the scratch project and
the open editor agree on every asset's identity.
"""
import csv, hashlib, json, os, shutil, subprocess, sys, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)
import audit_sfx_library as audit  # noqa: E402
import sfx_mapping as mapping      # noqa: E402

SFX = os.path.join(ROOT, "Assets", "Audio", "SFX")
LOOPS = os.path.join(SFX, "Loops")
MANIFEST = os.path.join(SFX, "sfx_manifest.json")
MAPPING_CSV = os.path.join(HERE, "SFX_MAPPING.csv")
LOOP_REPORT = os.path.join(HERE, "SFX_LOOP_REPORT.json")


def guid_for(asset_path):
    return hashlib.md5(("qolossal-sfx:" + asset_path).encode("utf-8")).hexdigest()


def write_meta(path, folder=False):
    meta = path + ".meta"
    if os.path.exists(meta):
        return
    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    body = f"fileFormatVersion: 2\nguid: {guid_for(rel)}\n"
    body += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n" if folder else ""
    with open(meta, "w", newline="\n", encoding="utf-8") as f:
        f.write(body)


def ensure_folder(path):
    parts = os.path.relpath(path, ROOT).split(os.sep)
    cur = ROOT
    for p in parts:
        cur = os.path.join(cur, p)
        if not os.path.isdir(cur):
            os.makedirs(cur)
        if p != "Assets":
            write_meta(cur, folder=True)


def bus_of(group):
    for prefix, bus in mapping.GROUP_BUS:
        if group.startswith(prefix):
            return bus
    raise ValueError("no bus for group " + group)


def active_level(samples, rate):
    """Loudness of the part that sounds: mean power of 20 ms windows within 20 dB of the loudest."""
    mono = samples.mean(axis=1)
    win = max(1, int(rate * .02))
    n = len(mono) // win
    if n == 0:
        return audit.db(float(np.sqrt(np.mean(mono ** 2))))
    p = (mono[: n * win].reshape(n, win) ** 2).mean(axis=1)
    loud = p[p >= p.max() * 10 ** (-20 / 10)]
    return audit.db(float(np.sqrt(loud.mean())))


def write_wav(path, samples, rate):
    """Writes the derivative only if its bytes changed (an unchanged file keeps its timestamp, so
    Unity doesn't reimport it)."""
    import io
    data = np.clip(np.round(samples * 32767.0), -32768, 32767).astype("<i2")
    buf = io.BytesIO()
    with wave.open(buf, "wb") as w:
        w.setnchannels(samples.shape[1]); w.setsampwidth(2); w.setframerate(rate)
        w.writeframes(data.tobytes())
    new = buf.getvalue()
    if os.path.exists(path) and open(path, "rb").read() == new:
        return
    with open(path, "wb") as f:
        f.write(new)


def make_loop(samples, rate, ambience):
    """Trims faded edges, then folds the tail over the head with an equal-power crossfade, so the
    last sample runs straight into the first."""
    mono = samples.mean(axis=1)
    win = int(rate * .05)
    n = len(mono) // win
    env = np.sqrt((mono[: n * win].reshape(n, win) ** 2).mean(axis=1) + 1e-12)
    body = np.percentile(env, 60)
    keep = np.nonzero(env >= body * 10 ** (-10 / 20))[0]
    start, end = (keep[0] * win, (keep[-1] + 1) * win) if len(keep) else (0, len(mono))
    y = samples[start:end]
    length = len(y)
    fade = int(min(2.0 if ambience else max(.25, .15 * length / rate), .3 * length / rate) * rate)
    t = np.linspace(0, 1, fade, endpoint=False)[:, None]
    fo, fi = np.cos(t * np.pi / 2), np.sin(t * np.pi / 2)
    out = np.concatenate([y[fade: length - fade], y[length - fade:] * fo + y[:fade] * fi])
    peak = np.abs(out).max()
    if peak > .999:
        out = out * (.999 / peak)
    return out, start / rate, (len(samples) - end) / rate, fade / rate, seam_check(out, y, fade)


def seam_check(out, y, fade):
    """The derivative's wrap joins y[fade-1] to y[fade], neighbours in the source, so the wrap's step
    should be no bigger than the source's own step there, and the crossfade should neither dip nor
    swell against the audio either side of it."""
    def rms(a):
        return audit.db(float(np.sqrt(np.mean(a ** 2)) + 1e-9))
    wrap = float(np.abs(out[-1] - out[0]).max())
    own = float(np.abs(y[fade] - y[fade - 1]).max())
    xf, after = rms(out[-fade:]), rms(out[:fade])
    before = rms(out[-2 * fade:-fade]) if len(out) >= 2 * fade else after
    bump = xf - (before + after) / 2
    reasons = []
    if wrap > own + .01:
        reasons.append(f"wrap step {wrap:.3f} vs source step {own:.3f}")
    if abs(bump) > 4.0:
        reasons.append(f"crossfade level {bump:+.1f} dB against its neighbours")
    return {"wrap_step": round(wrap, 4), "source_step": round(own, 4), "crossfade_bump_db": round(bump, 2),
            "verdict": "seamless" if not reasons else "check", "reasons": reasons}


def main():
    src_dir = sys.argv[1] if len(sys.argv) > 1 else audit.DEFAULT_DIR
    print("auditing", src_dir)
    sys.argv = [sys.argv[0], src_dir]
    audit.main()
    report = json.load(open(audit.OUT, encoding="utf-8"))
    integrity = [p for p in report["problems"] if not p.startswith("clipping")]
    if integrity:
        sys.exit("audit failed: " + "; ".join(integrity[:10]))

    rows = {r["Save as"][4:-7]: r for r in csv.DictReader(open(audit.CSV_PATH, encoding="utf-8-sig"))}
    ensure_folder(SFX); ensure_folder(LOOPS)
    entries, loop_report, copied = [], [], 0
    for s in report["entries"]:
        sid = s["id"]
        if sid not in mapping.MAP:
            sys.exit("no mapping for " + sid)
        event, hook, status, over = mapping.MAP[sid]
        bus = bus_of(s["group"])
        tune = dict(mapping.BUS_DEFAULTS[bus]); tune.update(over)
        loop = s["loop"] or sid in mapping.EXTRA_LOOPS
        folder = os.path.join(SFX, bus); ensure_folder(folder)
        takes = []
        for t in s["takes"]:
            src = os.path.join(src_dir, t["file"])
            dst = os.path.join(folder, t["file"])
            if os.path.exists(dst):
                if audit.sha256(dst) != t["sha256"]:
                    sys.exit(f"{dst} exists with different content; not overwriting")
            else:
                shutil.copy2(src, dst); copied += 1
                if audit.sha256(dst) != t["sha256"]:
                    sys.exit("copy hash mismatch " + dst)
            write_meta(dst)
            samples, rate, *_ = audit.read_wav(dst)
            level = active_level(samples, rate)
            peak = t["peak_dbfs"]
            # Up to the target, but never so far that the peak passes -1 dBFS; down freely.
            gain = round(min(tune["target"] - level, -1.0 - peak), 2)
            flags = []
            if t["clipped_samples"]:
                flags.append(f"clipped samples in source: {t['clipped_samples']}")
            if tune["target"] - level > -1.0 - peak + .5:
                flags.append(f"quiet: {tune['target'] - level - (-1.0 - peak):.1f} dB short of target at peak limit")
            take = {"source": os.path.relpath(dst, ROOT).replace("\\", "/"), "sha256": t["sha256"],
                    "duration_s": t["duration_s"], "active_dbfs": round(level, 2), "peak_dbfs": peak,
                    "gain_db": gain, "loop": "", "flags": flags}
            if loop:
                seam = t.get("loop_seam") or audit.loop_seam(samples, rate)
                entry = {"file": t["file"], "source_seam": seam}
                if seam["verdict"] != "clean":
                    out, cut_in, cut_out, fade, after = make_loop(samples, rate, bus == "Ambience")
                    name = t["file"][:-4] + "_loop.wav"
                    dpath = os.path.join(LOOPS, name)
                    write_wav(dpath, out, rate); write_meta(dpath)
                    entry.update({"derivative": os.path.relpath(dpath, ROOT).replace("\\", "/"), "trimmed_start_s": round(cut_in, 3),
                                  "trimmed_end_s": round(cut_out, 3), "crossfade_s": round(fade, 3),
                                  "derivative_duration_s": round(len(out) / rate, 3), "derivative_seam": after,
                                  "derivative_sha256": audit.sha256(dpath)})
                    take["loop"] = entry["derivative"]
                    if after["verdict"] != "seamless":
                        take["flags"].append("loop derivative seam still flagged: " + "; ".join(after["reasons"]))
                loop_report.append(entry)
            takes.append(take)
        entries.append({"number": s["number"], "id": sid, "group": s["group"], "bus": bus, "loop": loop,
                        "csv_loop": s["loop"], "requested_duration_s": s["requested_duration_s"],
                        "event": event, "hook": hook, "status": status,
                        "volume": tune["volume"], "pitch_jitter": tune["pitch"], "cooldown": tune["cooldown"],
                        "max_voices": tune["voices"], "positional": tune["positional"], "target_dbfs": tune["target"],
                        "supersedes": tune.get("supersedes", []), "prompt": rows[sid]["Prompt (paste into ElevenLabs)"],
                        "takes": takes})

    manifest = {"source_dir": src_dir, "csv_sha256": report["csv_sha256"], "sounds": len(entries),
                "files": sum(len(e["takes"]) for e in entries),
                "headroom_db": 12.0,
                "scenes": [{"prefix": p, "ambience": a, "surface": s} for p, a, s in mapping.SCENES],
                "low_voice_speakers": ["Grandfather Tallow"], "entries": entries}
    with open(MANIFEST, "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, indent=1)
    write_meta(MANIFEST)
    with open(LOOP_REPORT, "w", encoding="utf-8") as f:
        json.dump(loop_report, f, indent=1)

    with open(MAPPING_CSV, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["#", "Sound ID", "Bus (mixer group)", "Event", "Hook", "Status", "Loop", "Requested s",
                    "Take 01", "Take 02", "Take 03", "Take 04", "Loop derivatives", "Volume", "Cooldown s", "Max voices", "Positional", "Flags"])
        for e in entries:
            w.writerow([e["number"], e["id"], e["bus"], e["event"], e["hook"], e["status"], "yes" if e["loop"] else "",
                        e["requested_duration_s"]] + [os.path.basename(t["source"]) for t in e["takes"]] +
                       [" ".join(os.path.basename(t["loop"]) for t in e["takes"] if t["loop"]), e["volume"], e["cooldown"],
                        e["max_voices"], "yes" if e["positional"] else "", " | ".join(f"{i+1:02d}: {', '.join(t['flags'])}" for i, t in enumerate(e["takes"]) if t["flags"])])

    statuses = {}
    for e in entries:
        statuses[e["status"]] = statuses.get(e["status"], 0) + 1
    derived = sum(1 for l in loop_report if "derivative" in l)
    still = sum(1 for l in loop_report if "derivative" in l and l["derivative_seam"]["verdict"] != "seamless")
    print(f"copied {copied} new source files; {len(entries)} sounds; statuses {statuses}")
    print(f"loop takes {len(loop_report)}: {derived} derivatives made, {still} still flagged after crossfade")


if __name__ == "__main__":
    main()

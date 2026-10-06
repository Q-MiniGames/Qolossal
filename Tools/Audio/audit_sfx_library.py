"""Audit the ElevenLabs sound-effect library against the prompt CSV and Codex's download receipts.

Read-only: it never writes to the delivery folder. Writes Tools/Audio/SFX_AUDIT.json.

  python Tools/Audio/audit_sfx_library.py [delivery_dir]
"""
import csv, hashlib, json, os, re, sys, wave
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CSV_PATH = os.path.join(ROOT, "Tools", "Audio", "ELEVENLABS_SFX_PROMPTS.csv")
OUT = os.path.join(ROOT, "Tools", "Audio", "SFX_AUDIT.json")
DEFAULT_DIR = os.path.expanduser(r"~\OneDrive\Documents\ChatGPT\Qolossal\Audio\ElevenLabs_SFX")
TAKES = 4


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def read_wav(path):
    with wave.open(path, "rb") as w:
        ch, width, rate, frames = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(frames)
    if width != 2:
        raise ValueError(f"unexpected sample width {width}")
    data = np.frombuffer(raw, dtype="<i2").astype(np.float32) / 32768.0
    if len(data) != frames * ch:
        raise ValueError("truncated sample data")
    return data.reshape(-1, ch), rate, ch, width, frames


def db(x):
    return float(20 * np.log10(max(x, 1e-9)))


def analyse(samples, rate):
    mono = samples.mean(axis=1)
    peak = float(np.abs(samples).max())
    rms = float(np.sqrt(np.mean(mono ** 2)))
    gate = 10 ** (-50 / 20)
    loud = np.nonzero(np.abs(mono) > gate)[0]
    lead = (loud[0] / rate) if len(loud) else len(mono) / rate
    trail = ((len(mono) - 1 - loud[-1]) / rate) if len(loud) else len(mono) / rate
    clipped = int(np.count_nonzero(np.abs(samples) >= 0.999))
    return {"peak_dbfs": round(db(peak), 2), "rms_dbfs": round(db(rms), 2),
            "lead_silence_s": round(float(lead), 3), "tail_silence_s": round(float(trail), 3),
            "clipped_samples": clipped}


def loop_seam(samples, rate):
    """How the end joins the start when played as a loop."""
    win = int(rate * 0.05)
    head, tail = samples[:win], samples[-win:]
    rms_h = float(np.sqrt(np.mean(head ** 2)))
    rms_t = float(np.sqrt(np.mean(tail ** 2)))
    body = float(np.sqrt(np.mean(samples ** 2)))
    # The wrap's step against the signal's typical sample-to-sample step.
    step = float(np.abs(samples[0] - samples[-1]).max())
    typical = float(np.median(np.abs(np.diff(samples[: rate], axis=0)).max(axis=1))) + 1e-6
    level_jump = abs(db(rms_h) - db(rms_t))
    edge_vs_body = min(db(rms_h), db(rms_t)) - db(body)
    verdict = "clean"
    reasons = []
    if level_jump > 3.0:
        verdict = "needs-crossfade"; reasons.append(f"level jump {level_jump:.1f} dB")
    if step > max(0.02, typical * 8):
        verdict = "needs-crossfade"; reasons.append(f"wrap click {step:.3f} (typical {typical:.4f})")
    if edge_vs_body < -12:
        verdict = "needs-crossfade"; reasons.append(f"fade at an edge ({edge_vs_body:.1f} dB vs body)")
    return {"level_jump_db": round(level_jump, 2), "wrap_step": round(step, 4),
            "typical_step": round(typical, 4), "edge_vs_body_db": round(edge_vs_body, 2),
            "verdict": verdict, "reasons": reasons}


def main():
    src = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_DIR
    rows = list(csv.DictReader(open(CSV_PATH, encoding="utf-8-sig")))
    receipts = {}
    rpath = os.path.join(src, "DOWNLOAD_RECEIPTS.jsonl")
    for line in open(rpath, encoding="utf-8"):
        if line.strip():
            r = json.loads(line); receipts[r["file"]] = r
    on_disk = {f for f in os.listdir(src) if f.lower().endswith(".wav")}
    problems, sounds, expected = [], [], set()
    for row in rows:
        stem = row["Save as"][:-len("_01.wav")]
        sid = stem[len("SFX_"):]
        want = float(re.sub(r"[^0-9.]", "", row["Duration"]) or 0)
        loop = row["Loop"].strip().lower() == "yes"
        takes = []
        for t in range(1, TAKES + 1):
            name = f"{stem}_{t:02d}.wav"; expected.add(name)
            path = os.path.join(src, name)
            if name not in on_disk:
                problems.append(f"missing {name}"); continue
            digest = sha256(path)
            rec = receipts.get(name)
            if rec is None:
                problems.append(f"no receipt for {name}")
            elif rec["sha256"] != digest:
                problems.append(f"hash mismatch {name}")
            samples, rate, ch, width, frames = read_wav(path)
            info = {"file": name, "sha256": digest, "bytes": os.path.getsize(path), "channels": ch,
                    "rate": rate, "bits": width * 8, "frames": frames,
                    "duration_s": round(frames / rate, 3)}
            info.update(analyse(samples, rate))
            if loop:
                info["loop_seam"] = loop_seam(samples, rate)
            if info["clipped_samples"] > 0:
                problems.append(f"clipping in {name}: {info['clipped_samples']} samples")
            takes.append(info)
        sounds.append({"number": int(row["#"]), "id": sid, "group": row["Group"], "requested_duration_s": want,
                       "loop": loop, "takes": takes})
    extra = sorted(on_disk - expected)
    for e in extra:
        problems.append(f"unexpected file {e}")
    lower = [n.lower() for n in on_disk]
    if len(set(lower)) != len(lower):
        problems.append("case-insensitive filename collision")
    csv_hash = sha256(CSV_PATH)
    out = {"source_dir": src, "csv": os.path.relpath(CSV_PATH, ROOT).replace("\\", "/"), "csv_sha256": csv_hash,
           "sounds": len(sounds), "files": sum(len(s["takes"]) for s in sounds),
           "total_bytes": sum(t["bytes"] for s in sounds for t in s["takes"]),
           "problems": problems, "entries": sounds}
    json.dump(out, open(OUT, "w", encoding="utf-8"), indent=1)
    loops = [(s["id"], t["file"][-6:-4], t["loop_seam"]["verdict"]) for s in sounds if s["loop"] for t in s["takes"]]
    print(f"{out['sounds']} sounds, {out['files']} files, {out['total_bytes']/1e6:.1f} MB, problems: {len(problems)}")
    for p in problems[:40]:
        print("  ", p)
    print("loop takes:", len(loops), "clean:", sum(1 for l in loops if l[2] == "clean"))


if __name__ == "__main__":
    main()

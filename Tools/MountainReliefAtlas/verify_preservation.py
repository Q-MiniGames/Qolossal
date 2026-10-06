"""Accepted-art preservation receipt and global name check for the Mountain Relief Atlas prototype.

Hashes every accepted file (Tools/ArtImport/codex_v2_accepted.json) in staging and in Assets, and
checks every new name under Assets/MountainReliefAtlas (scenes, scripts, data, review images)
case-insensitively against accepted names, accepted sub-sprite names and every other Assets stem.
Usage: python verify_preservation.py <receipt.json>
"""
import collections, hashlib, json, os, sys

P = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(P, "Tools", "IncomingArt", "Codex_v2")
NEW = os.path.join(P, "Assets", "MountainReliefAtlas")


def sha(path):
    with open(path, "rb") as f:
        return hashlib.sha256(f.read()).hexdigest()


acc = json.load(open(os.path.join(P, "Tools", "ArtImport", "codex_v2_accepted.json")))["assets"]
manifest_sha = sha(os.path.join(P, "Tools", "ArtImport", "codex_v2_accepted.json"))
bad = []
for e in acc:
    s = os.path.join(SRC, e["category"], e["name"] + ".png")
    d = os.path.join(P, e["dest"])
    if not os.path.exists(s) or sha(s) != e["sha256"]: bad.append(("staging", e["name"]))
    if not os.path.exists(d) or sha(d) != e["sha256"]: bad.append(("Assets", e["name"]))

taken = collections.defaultdict(list)
for e in acc:
    taken[e["name"].lower()].append("accepted")
    for sp in e.get("sprites") or []: taken[sp["name"].lower()].append("accepted sub-sprite")
for root, _, files in os.walk(os.path.join(P, "Assets")):
    if os.path.commonpath([root, NEW]) == NEW: continue
    for f in files:
        if not f.endswith(".meta"): taken[os.path.splitext(f)[0].lower()].append(os.path.relpath(os.path.join(root, f), P))
new_names = collections.Counter()
for root, _, files in os.walk(NEW):
    for f in files:
        if not f.endswith(".meta"): new_names[os.path.splitext(f)[0].lower()] += 1
collisions = {n: taken[n][:3] for n in new_names if n in taken}
dups = [n for n, c in new_names.items() if c > 1]
receipt = {"accepted_files": len(acc), "manifest_sha256": manifest_sha, "mismatches": bad,
           "new_names_checked": len(new_names), "collisions": collisions, "duplicate_new_names": dups}
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(NEW, "PRESERVATION_RECEIPT.json")
json.dump(receipt, open(out, "w"), indent=1)
print(f"{len(acc)} accepted, {len(bad)} mismatches, {len(new_names)} new names, {len(collisions)} collisions, {len(dups)} duplicates")

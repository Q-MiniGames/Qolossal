# Codex prompt: Batch 27b, resume and finish

Written 12 Oct 2026 after Claude looked at the unfinished `Codex_v2_full\Batch27b\` (its `WORK_IN_PROGRESS.md` says playback review and packaging stopped when Computer Use was stopped). Not sent: the user sends it. Review notes and images: `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_74_Seams.jpg`, also copied into `Batch27b_Input`.

---

Qolossal, Batch 27b, resume: finish the walk-fix you started in `Codex_v2_full\Batch27b\`. Keep everything from `CODEX_PROMPT_BATCH27B_QORI_WALK_CLOAK.md` and work on the current frames in that folder.

**Where it stands (checked by Claude):** the current 16 frames pass the 434-test checker, and the cloak now hangs from under the mantle in every frame. Well done. The mantle steps are 0.77–1.5 (limit 1.5; 14→15 sits exactly on it). Four visible patch faults remain, shown in `CLAUDE_REVIEW_74_Seams.jpg`:

1. **Frame 14:** a detached, semi-transparent smear of leaf fragments floats off the top-left edge of the cloak (516 px not connected to the figure).
2. **Frame 6:** the same kind of smeared leaf fragments and a blurred band along the cloak's top-left edge, still touching the figure.
3. **Frame 3:** the repair patch shows: a straight vertical seam and a dark horizontal stroke across the cloak top, and the lower mantle leaves are see-through over the arm.
4. **Frame 7:** the cloak's outer edge is doubled: two parallel outlines a few pixels apart down the back.

Fix those four by repainting the cloak top as one continuous surface in each frame, with the same leaf, veins and edge line as its neighbours, opaque mantle leaves and one clean outline. Don't blend patches, and leave no stray pixels. If you touch frames 14 and 15 again, bring their mantle closer to their neighbours so 14→15 drops below 1.4.

**Checker update:** `Batch27b_Input\check_walk_27.py` and `batch27_targets.json` now add one test per frame: no detached specks or smears off the figure (0 px allowed). That makes 450 tests. The current frames fail only frame 14 (516 px). The rest of the input package is unchanged. Re-run:

```
python "<Codex_v2_full>\Batch27b_Input\check_walk_27.py" "<Codex_v2_full>\Batch27b" "<Codex_v2_full>\Batch27b_Input"
```

Do not deliver until it says `ALL PASS` (450 tests). Then do the playback review you had started: watch `Qori_Walk_Proof_Loop.gif` and your 45 ms preview, and check that the cloak top stays attached and clean through the whole loop and no frame flashes a different mantle or cape.

**Then finish the package** as the 27b prompt asks: `MANIFEST.json`, `RECEIPT.json`, `PRESERVATION_CHECK.json` (re-read the canonical manifest), `NAME_COLLISION_CHECK.json`, `sources\`, and `REVIEW_HANDOFF.md`. The handoff must include the before/after table of the two Batch 27b checks for every frame, which frames changed, and the final checker output in full. Delete `WORK_IN_PROGRESS.md` when it's done. Platform readiness: "not tested". Original Switch excluded. Stop for Claude's review; nothing is imported.

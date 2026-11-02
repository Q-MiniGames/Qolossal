# Codex prompt: Batch 27b, Young Adventurer walk: attach the cloak, match the repaired frames

Written 12 Oct 2026 after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_73.md` (Batch 27 is on model with good feet and poses, but the cloak is cut off under the mantle in 12 frames, and the repaired frames 6, 14 and 15 pop). Not sent: the user sends it. Same canvas, block-out, model and timing as `CODEX_PROMPT_BATCH27_QORI_WALK.md`. `Batch27b_Input` holds that same block-out and model with the updated checker (434 tests; the Batch 27 frames fail 17 of them).

---

Qolossal, Batch 27b: a correction pass on your Batch 27 Young Adventurer walk. Start from your delivered `Codex_v2_full\Batch27\Qori_Walk_01.png` … `_16.png` and deliver to `Codex_v2_full\Batch27b\`. Inputs: `Codex_v2_full\Batch27b_Input\` (the Batch 27 package with an updated `check_walk_27.py` and `batch27_targets.json`; read only). Do not write under the live project, and do not touch the music.

**Keep exactly as delivered:** the poses, foot positions, ground line, head and face, ears, arms, legs, timing and registration. Everything in Batch 27 that passed still has to pass.

## Fix 1: the cloak hangs from under the mantle (frames 1–4, 7–13, 16)

In these frames the cape's top ends in a straight horizontal cut, with background showing between it and the mantle (see `CLAUDE_REVIEW_73_CapeTop.jpg`, beside the review). Repaint the cape's upper part so it rises up behind the shoulders and disappears under the mantle's lower leaves, continuous with the rest of the cape: the same olive leaf, midrib and veins, no gap, no straight edge. Frames 5, 6, 14 and 15 show it attached; match how they do it. The mantle's lower leaves sit on top of the cape and are fully opaque (no see-through leaves on frames 3, 9 and 11).

Check: the longest straight top edge in the cloak area between the neck and the hips must be **15 px or less** (Batch 27 measured 29–74 px on these frames).

## Fix 2: frames 6, 14 and 15 match their neighbours

These were repainted separately, so their mantle, tunic, skirt and cape differ from the frames around them about twice as much as any other pair. Bring their mantle, clasp, tunic wrap and skirt leaves back to the frame-5 painting (moved and tilted to the pose, not redrawn). Make their cape's veins and hem lobes continue from frames 5 and 7 (for 6) and 13 and 16 (for 14 and 15). Keep their correct cloak attachment and their leg repairs.

Check: lined up by the neck, the mantle's difference between neighbouring frames must be within **1.5 times the median pair**. Batch 27 measured 2.1, 1.7, 2.1, 2.0 and 2.0 for 5→6, 6→7, 13→14, 14→15 and 15→16.

## While you are there (not checked separately)

- Carry the cape's hem lobes and leaf edges over from the previous frame instead of redrawing them, so the outline stops boiling. Batch 27's neighbouring frames changed shape 1.3–2× more than the planned motion; aim for under 1.5×.
- Frames 6–8: let the far leg reach as far forward as the block-out has it (the blue areas in your `Qori_Walk_Proof_BlockoutOverlay.png`).

## Acceptance

```
python "<Codex_v2_full>\Batch27b_Input\check_walk_27.py" "<Codex_v2_full>\Batch27b" "<Codex_v2_full>\Batch27b_Input"
```

Do not deliver until the last line says `ALL PASS` (434 tests). Change the painting, never the checker, block-out or targets. Then watch `Qori_Walk_Proof_Loop.gif` yourself: the cloak top must stay attached through the whole loop, and no frame should flash a different mantle or cape.

## Delivery

`Codex_v2_full\Batch27b\`: the 16 frames (all 16, even the unchanged ones), `Qori_Walk_Registration.json` (unchanged), the checker's `Qori_Walk_CHECK.json` and its six proofs, `MANIFEST.json`, `RECEIPT.json`, `PRESERVATION_CHECK.json` against `C:\UnityProjects\Qolossal\Tools\ArtImport\codex_v2_accepted.json` (re-read it), `NAME_COLLISION_CHECK.json`, `sources\`, and `REVIEW_HANDOFF.md`. In the handoff, list which frames changed and the before and after values of the two new checks for every frame. Platform readiness: "not tested". Original Switch excluded. Stop for Claude's review; nothing is imported.

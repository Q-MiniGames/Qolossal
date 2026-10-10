# Codex prompt: Batch 22f, Qori v3 wrap-edge repaint

Written 8 Oct 2026 after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_38.md` (22e: the wrist passes; the bark at the sides of the leaf wrap is a dark, hard-edged band that reads as brown flaps in the rig). Not sent: the user sends it. Every number below was measured on the Batch 22e files by `Tools/ArtRequests/Batch22f/make_batch22f_input.py`, which also wrote the masks, targets and the acceptance checker into `Codex_v2_full\Batch22f_Input`. The checker fails the 22e arm on 5 of 85 tests (exactly the edge faults) and passes a feasibility mock (`Tools/ArtRequests/Batch22f/mock/make_mock22f.py`; not art), which also showed no flaps in the Unity test rig.

---

Qolossal, Batch 22f: repaint **only the outer edge of the leaf wrap on `Qori_v3_Arm.png`**, starting from the Batch 22e arm. Only the pixels in `Qori_v3_Arm_Mask_EdgeZone.png` may change; every other pixel, and every other file, must come back identical to 22e. Deliver to `Codex_v2_full\Batch22f`. Do not write under the live project, and do not touch the music.

## 0. Inputs (read first; do not edit them)

`Codex_v2_full\Batch22f_Input\`:

| File | What it is |
|---|---|
| `reference_22e\Qori_v3_Arm.png` | The 22e arm: your starting file. |
| `reference_22e\CLAUDE_REVIEW_38_Wrists.jpg` | The Unity rig at 2x; the bottom crop shows the flaps. |
| `Qori_v3_Arm_Mask_EdgeZone.png` | White = the only pixels that may differ from 22e: in rows 180–242, the 22 pixels in from each side of the 22e silhouette and the transparent pixels outside it, never a leaf or its painted outline. |
| `Qori_v3_Arm_EdgeZone_4x.png` | 4x view of rows 170–250: the 22e arm with the edge zone in blue. |
| `Qori_v3_Arm_Mask_Outer.png` | White = the only pixels where alpha may be above 0 (unchanged from 22e). |
| `Qori_v3_Arm_Mask_Inner.png` | White = pixels that must have alpha ≥ 250. Beside the wrap (rows 180–235) only the arm's tube core, x 56–97, is required now, so the sides may tuck in. |
| `batch22f_targets.json` | Every target and threshold, with the measured 22e values. |
| `check_arm_22f.py` | The acceptance checker (Python 3, numpy and Pillow only). |
| `reference_22d\` | Read-only, used by the inherited tests. |

Pixel coordinates are top-left PNG pixels of the arm canvas: x to the right, y (row) down.

## 1. What is wrong with the 22e wrap edge (measured)

Everything else in 22e passed review: the wrist runs into every hand, and the leaves, the arm above and the hand cover are right. At the sides of the wrap, rows 180–242:

- **Dark band.** On the arm above, the dark shading at each edge (luminance below 115) is 4–5 px on average, 8 px at most. Beside the wrap on the right it is 8.5 px on average and up to 20 px, so a dark brown band sticks out beside the leaves. 336 bark pixels lie outside the arm tube there.
- **Hard edge.** On the arm above, every row's outermost pixel is partly transparent (an antialiased edge). Beside the wrap, only 6% of rows have one, so the edge is stair-stepped.
- **Notch.** On the left, the silhouette jumps 3 px between neighbouring rows.

## 2. What to paint

Canvas, pivot, PPU and straight RGBA as before (RGB = 0 wherever alpha = 0). Inside the edge zone only:

- **Silhouette:** a smooth outline from the arm (x 52–99 at row 179) widening into the flare (as in 22e at rows 240–242), with no flaps, notches or steps: the edge moves at most 2 px from one row to the next. Keep every leaf as painted; where a leaf reaches the edge, the outline goes just outside it. Rows 240–242 meet the locked hand cover below, so do not narrow them.
- **Edge:** antialiased like the arm above: the outermost pixel of each row partly transparent (alpha 1–223) on both sides.
- **Bark:** the arm's own bark, with the same edge shading as above the wrap: a dark rim of about 4–7 px, then the normal bark tone, with lengthwise grain. A little darker in the leaves' shadow is fine; no wide dark band and no new outline.
- **Inner edge of the zone:** blend into the untouched 22e bark so the zone boundary can't be seen.

## 3. Registration and other files

`Qori_v3_Registration.json` identical to 22e (only `status`, `supersedes` ("22e wrap edge") and the Arm notes may change; Arm canvas and pivot exact). All 19 other PNGs byte-identical (same bytes as 22c, 22d and 22e).

## 4. Acceptance: run the checker, fix, repeat

```
python "<Codex_v2_full>\Batch22f_Input\check_arm_22f.py" "<Codex_v2_full>\Batch22f" "<Codex_v2_full>\Batch22f_Input"
```

It prints PASS or FAIL for 85 tests and writes `Qori_v3_ARM_CHECK.json` and three proofs into the delivery. **Do not deliver until the last line says `ALL PASS`.** If a test fails, change the painting (never the checker, the masks or the targets) and run it again. Sections 1–9 are the Batch 22d and 22e tests and must keep passing. Section 10 is new:

1. The file differs from 22e, and every pixel outside the edge zone is identical to 22e.
2. No dark band, per side, rows 180–242: dark-rim width at most **7 px on average** and **9 px in any row** (arm above 4.1 / 8 left, 4.9 / 7 right; 22e right 8.5 / 20).
3. Soft edge, per side: at least **80%** of rows 180–242 have a partly transparent outermost pixel (arm above 100%; 22e 6%).
4. Smooth silhouette, per side: the edge moves at most **2 px** between neighbouring rows (22e left 3 px).
5. The repaint blends into the untouched bark at the zone's inner edge: across that line the mean luminance step stays within **6** of 22e's, and the mean absolute step at most **6** above 22e's.

Then look at the proofs (`Qori_v3_Proof_ArmCheck_HandJoin_2x.png`, `_Elbow_2x`, `_Template`) and compare the wrap's sides with `reference_22e\CLAUDE_REVIEW_38_Wrists.jpg`: no brown flaps, no stair steps.

## 5. Delivery (same files and checks as 22e)

`Codex_v2_full\Batch22f\`:

- `Qori_v3_Arm.png` (new), the 19 unchanged PNGs and `Qori_v3_Registration.json`.
- The checker's `Qori_v3_ARM_CHECK.json` and its three proof images, from the final run.
- The proof set, re-rendered with the new arm: `Qori_v3_Proof_Rest`, `_Overlay` (with the landmark table), `_Stride`, `_Bend`, `_Bend_Detail_2x`, `_Staff`, `_ScaleCheck_1080`; plus `Qori_v3_LANDMARK_CHECK.json`, `Qori_v3_QA_CHECK.json`, `Qori_v3_SKINNING_CHECK.json`.
- `MANIFEST.json` (every file but itself, with path, SHA256, byte size, pixel size), `PRESERVATION_CHECK.json` against the canonical accepted manifest (currently 1158 entries, SHA256 `a60f9c91bbc0e4c1816633274468a85f24a4edc7322b130bbd4e53f943cf0c26`; re-read it, do not assume), `NAME_COLLISION_CHECK.json`.
- `sources\`: prompts, native outputs, selected and rejected attempts, build and proof scripts.
- `REVIEW_HANDOFF.md`: the checker's final output pasted in full, the section 10 values, and anything still off-target. Platform readiness: "not tested". Original Switch excluded.

Stop after delivery for Claude's review. Nothing is imported until the user approves it.

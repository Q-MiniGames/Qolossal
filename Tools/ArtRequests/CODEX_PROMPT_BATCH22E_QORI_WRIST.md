# Codex prompt: Batch 22e, Qori v3 wrist repaint

Written 8 Oct 2026 after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_37.md` (22d: the arm passes down to the wrap; the bark under the wrap and the hand cover are smeared). Not sent: the user sends it. Every number below was measured on the Batch 22d files by `Tools/ArtRequests/Batch22e/make_batch22e_input.py`, which also wrote the template, masks, targets and the acceptance checker into `Codex_v2_full\Batch22e_Input`. The checker fails the 22d arm on 12 of 75 tests, each one a fault from the review, and passes a feasibility mock (`Tools/ArtRequests/Batch22e/mock/make_mock22e.py`: real arm bark mirrored down from row 178; not art), which also showed no line, smear or ledge at any wrist in the Unity test rig.

---

Qolossal, Batch 22e: repaint **only the lower end of `Qori_v3_Arm.png`, rows 180 to 256** (the leaf wrap, the bark under it and the part that runs over the hand). Rows 0 to 179 must stay pixel-identical to Batch 22d, and every other file must come back byte-identical. Deliver to `Codex_v2_full\Batch22e`. Do not write under the live project, and do not touch the music.

## 0. Inputs (read first; do not edit them)

`Codex_v2_full\Batch22e_Input\`:

| File | What it is |
|---|---|
| `Qori_v3_Arm_Template.png` | 154 × 259, the final canvas. Rows 0–179 show the 22d arm ghosted (locked). Below: grey = must be opaque, yellow = may be filled, blue = fade zone over the hands, red column = bone line x 76, cyan row = row 180, the first row you paint. |
| `Qori_v3_Arm_Mask_Outer.png` | White = the only pixels where alpha may be above 0. |
| `Qori_v3_Arm_Mask_Inner.png` | White = pixels that must have alpha ≥ 250. |
| `Qori_v3_Arm_Mask_HandCover.png` | White = rows 243–256 inside all three hands. Below row 242 the arm may exist only here. |
| `Qori_v3_Arm_Template_4x_Annotated.png` | 4x view of rows 160–259 with row labels, the 22d leaves ghosted, the three hand outlines (Relaxed green, Grip magenta, Spread orange) and the stray pixel boxed in red. |
| `batch22e_targets.json` | Every target and threshold below, the measured 22d values, and the SHA256 of the 19 files that must not change. |
| `check_arm_22e.py` | The acceptance checker (Python 3, numpy and Pillow only). |
| `reference_22d\` | Read-only copies of the 22d Arm, the three hands, the registration, and the 22d hand-join proof showing the fault. |

Pixel coordinates are top-left PNG pixels of the arm canvas: x to the right, y (row) down.

## 1. What is wrong with the 22d arm (measured)

Everything from the rounded top to row 179 passed review and the Unity rig: straight, no shoulder knob, no seam, bark matched to the leg, clean elbow. The faults are all below:

- **Smeared, not painted.** Detail (mean luminance step between horizontal neighbours) is **12.4** on the arm bark above, 9.6–11.3 on the three hands' top rows, but **4.8** in the flare under the wrap (rows 236–242), **2.6** in the opaque hand cover (243–245) and **2.7** in the fade (246–252). Rows 243–245 are near-copies of row 242, and the fade is vertical streaks. In the rig this is a pale, blurry band across the back of every hand.
- **Cross streaks beside the wrap.** The bark between and below the lower leaves (rows ~216–235) carries pale horizontal smudges. Grain-direction ratio (detail across the arm ÷ detail along it): arm bark **3.17**, the 22c leg beside its own wrap **1.71**, 22d beside the wrap **1.47**, 22d flare **0.94** (the grain there runs the wrong way or not at all).
- **Ledge over the hands.** The opaque rows 243–245 run to x 118, past the Grip hand by 14 pixels and the Spread hand by 24. Over the Leaf Staff it shows as a straight-bottomed ledge.
- **Fade.** The mean alpha drops 35 between rows 251 and 252.
- **Stray pixel.** One bright-green pixel at x 58, row 231, at the lower left tip of the wrap.

## 2. What to paint

Canvas **154 × 259**, pivot **(76.428, 41.239)**, PPU 100, all unchanged. Straight RGBA: RGB = 0 wherever alpha = 0. No glow, no cast shadow, no outline across the limb.

**Start from the 22d arm file and keep rows 0–179 exactly as they are.** Paint rows 180–256 as one continuous piece of the same bark, so that row 180 continues row 179 with no visible line.

| Part | Rows | What to paint |
|---|---|---|
| Leaf wrap | 180 → 235 | The same wrap: keep its silhouette (inside the masks, which follow the 22c wrap within 3 px), the olive median about (139, 122, 60), and the wrap centre at row 207.5 ± 4. Repaint the leaves and the bark showing between and beside them together. The bark there is the arm's bark with its lengthwise grain, a little darker in the leaves' shadow, never paler, and with no horizontal smudges. No bright-green pixel anywhere. |
| Flare (back of the wrist) | 236 → 242 | The arm's bark widening into the wrist, with the same fine lengthwise grain as above the wrap and the same light from the left. Keep the 22d width at row 236 to 238. In rows 239–242 you may round off the bottom corners inward to the hands' top width (x 43–109 at row 243). |
| Hand cover, opaque | 243 → 245 | Fully opaque across x 45–107 (row 243), 44–108 (244), 44–109 (245), and nothing outside the white of `Mask_HandCover`. This hides every hand's straight top edge. Same painted grain, now turning into the hands' own bark: their grain runs down the back of the hand toward the fingers. |
| Hand cover, fade | 246 → 256 | Alpha falls smoothly to 0 by row 256 (no drop above 30 between neighbouring rows, mean alpha of row 256 at most 10), only inside `Mask_HandCover`. Keep painting grain in the fading rows; do not stretch or repeat rows. Tone between the arm bark and the hands (median luminance 136–166 over rows 236–245). |

The 22d colour targets still apply to the arm above the wrap and are unchanged there; the hands are unchanged and still draw under the arm (NearHand 8.9, FarHand 1.9).

## 3. Registration

`Qori_v3_Registration.json` identical to 22d. Only `status`, `supersedes` (say "22d wrist") and the `Arm` entries of `sourceExtraction` and `barkColourNormalization` may change, and the Arm canvas [154, 259] and pivot stay exactly as they are.

## 4. Keep byte-identical (re-deliver)

All 19 other PNGs: Head, the nine Face overlays, Ear, Torso, Cape, Mantle, Leg, Foot and the three Hands (the same bytes as 22c and 22d). The checker compares their SHA256.

## 5. Acceptance: run the checker, fix, repeat

```
python "<Codex_v2_full>\Batch22e_Input\check_arm_22e.py" "<Codex_v2_full>\Batch22e" "<Codex_v2_full>\Batch22e_Input"
```

It prints PASS or FAIL for 75 tests with the measured value and the limit, writes `Qori_v3_ARM_CHECK.json`, and writes three proofs into the delivery. **Do not deliver until the last line says `ALL PASS`.** If a test fails, change the painting (never the checker, the masks or the targets) and run it again. Sections 1–8 are the Batch 22d tests (format, masks, straightness, wrap, seams, colour, unchanged files, registration, skinned elbow, hand joins). Section 9 is new:

1. Rows 0–179 pixel-identical to the 22d arm, and the file differs from 22d.
2. No bright-green pixel (g > r + 15 and g > b + 40).
3. Painted grain, not smeared: detail ≥ **7.4** in the flare (rows 236–242) and separately in the opaque cover (243–245), ≥ **6.2** in the fade (246–252, alpha ≥ 128).
4. Grain runs along the arm: grain-direction ratio ≥ **1.6** for the bark beside the wrap and ≥ **1.8** in the flare.
5. No seam below the wrap: the Batch 22d seam limits applied from the row under the wrap to row 245.
6. Wrist tone: median luminance of rows 236–245 between 136 and 166.
7. For each hand (Relaxed, Grip, Spread): 0 pixels with alpha > 32 below row 242 outside that hand.
8. Fade: no mean-alpha drop above 30 between neighbouring rows 245–256, and row 256 mean alpha ≤ 10.

Then look at the proofs yourself: `Qori_v3_Proof_ArmCheck_HandJoin_2x.png` (the three hands; the bark grain should run from the wrap down into each hand with no pale band, no straight edge and nothing hanging past the hand), `Qori_v3_Proof_ArmCheck_Elbow_2x.png` (unchanged elbow) and `Qori_v3_Proof_ArmCheck_Template.png` (any pixel outside the template or a hole inside it is red). Compare against `reference_22d\Qori_v3_Proof_ArmCheck_HandJoin_2x.png`, which shows the fault.

## 6. Delivery (same files and checks as 22d)

`Codex_v2_full\Batch22e\`:

- `Qori_v3_Arm.png` (new) and the 19 unchanged PNGs, `Qori_v3_Registration.json`.
- The checker's `Qori_v3_ARM_CHECK.json` and its three proof images, from the final run.
- The proof set, re-rendered with the new arm: `Qori_v3_Proof_Rest`, `_Overlay` (with the landmark table), `_Stride`, `_Bend`, `_Bend_Detail_2x`, `_Staff`, `_ScaleCheck_1080`; plus `Qori_v3_LANDMARK_CHECK.json`, `Qori_v3_QA_CHECK.json`, `Qori_v3_SKINNING_CHECK.json`. The hanging wrist wrap landmark should stay where it is (22d measured 0.561H).
- `MANIFEST.json` (every file but itself, with path, SHA256, byte size, pixel size), `PRESERVATION_CHECK.json` against the canonical accepted manifest (currently 1158 entries, SHA256 `a60f9c91bbc0e4c1816633274468a85f24a4edc7322b130bbd4e53f943cf0c26`; re-read it, do not assume), `NAME_COLLISION_CHECK.json`.
- `sources\`: prompts, native outputs, selected and rejected attempts, build and proof scripts.
- `REVIEW_HANDOFF.md`: the checker's final output pasted in full, the section 9 values, and anything still off-target. Platform readiness: "not tested". Original Switch excluded.

Stop after delivery for Claude's review. Nothing is imported until the user approves it.

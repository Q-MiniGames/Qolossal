# Codex prompt: Batch 22d, Qori v3 arm repaint

Written 8 Oct 2026 after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_35.md` (22c: legs pass, arm fails) and the Unity test rig (`Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_36.md`). Not sent: the user sends it. Every number below was measured on the Batch 22c files by `Tools/ArtRequests/Batch22d/make_batch22d_input.py`, which also wrote the template, masks, targets and the acceptance checker into `Codex_v2_full\Batch22d_Input`. The checker passes on a feasibility mock and fails the 22c arm on exactly the review's faults (19 of 59 tests).

---

Qolossal, Batch 22d: repaint **only `Qori_v3_Arm.png`** so it has no seam and no shoulder knob, and change two draw-order numbers in the registration. Everything else in Batch 22c passed review and must come back byte-identical. Deliver to `Codex_v2_full\Batch22d`. Do not write under the live project, and do not touch the music.

## 0. Inputs (read first; do not edit them)

`Codex_v2_full\Batch22d_Input\`:

| File | What it is |
|---|---|
| `Qori_v3_Arm_Template.png` | 154 × 259, the final canvas. Grey = must be opaque. Yellow = 3 px edge band you may fill or leave. Blue = hand cover (section 2.4). Red column = bone line x 76. Magenta crosses = shoulder, elbow, wrist. Cyan rows = wrap top and bottom. |
| `Qori_v3_Arm_Mask_Outer.png` | White = the only pixels where alpha may be above 0. |
| `Qori_v3_Arm_Mask_Inner.png` | White = pixels that must have alpha ≥ 250. |
| `Qori_v3_Arm_Mask_HandCover.png` | White = the hand-cover zone, rows 243–256. |
| `Qori_v3_Arm_Template_4x_Annotated.png` | 4x view with row labels, the 22c arm ghosted, and the mantle over the near arm at rest (green hatch). |
| `batch22d_targets.json` | Every target and threshold below, plus the SHA256 of the 19 files that must not change. |
| `check_arm_22d.py` | The acceptance checker (Python 3, numpy and Pillow only). |
| `reference_22c\` | Read-only copies of the 22c Arm, Leg, three hands, Mantle and registration. |

Pixel coordinates are top-left PNG pixels of the arm canvas: x to the right, y (row) down.

## 1. What is wrong with the 22c arm (measured)

- **Seam.** A straight horizontal edge at **row 172** (the step from row 171 to 172): mean brightness jumps +11.8, mean absolute step 25.9, and 58% of the row's pixels step more than 10 the same way. The bark from row 172 down, beside the wrap leaves and below them to row 242, is a pasted 22b block: its median is **+10.8 brighter** than the 30 bark rows above the wrap. (The 22c leg's worst row: +2.1 / 5.8 / 15%; its bark beside its wrap is 12.3 *darker*, a natural wrap shadow.)
- **Knob.** The painted arm leans off its bone line. Its centre is x 62.0 at the shoulder row (41) against the bone line x 76.43, 14.4 px to the left. The top is a ball 61 px wide (x 32–92) on a 47 px arm. 755 pixels lie outside the new template, in the box x 32–60, rows 16–71. At rest the mantle (drawn above the near arm) covers only x ≥ 65 to 79 in rows 24–60, so all 755 show beside the mantle.
- **Hand join (found in the Unity rig).** The arm stops at row 242 and every hand starts at arm row 243.15 (hand top row 64, hand pivot y 30, wrist y 209.15). The two soft edges meet, and the background shows through as a light 1 px line across the back of the hand. With the darker Grip hand, the hand's straight top edge also shows as a line.
- **Colour.** The 22c arm bark has more contrast than the leg: shadow p5 (133, 93, 65) against the leg's (155, 104, 73), highlight p95 (236, 192, 152) against (224, 181, 148), and 20.4% dark grain against the leg's 11.4%.

## 2. The new arm

One painting, straight, hanging down, from the rounded top to the end of the hand cover. Canvas **154 × 259**, pivot **(76.428, 41.239)** (the shoulder), as in 22c. PPU 100. Straight RGBA: RGB = 0 wherever alpha = 0. No glow, no cast shadow, no outline across the limb.

### 2.1 Shape (paint inside `Qori_v3_Arm_Template.png`)

| Part | Rows | Shape |
|---|---|---|
| Rounded top | 16 → 41.24 | Half-ellipse, half-width 24 px, centred on x 76.43, top at row 16. No bulge, no ball, no knob. |
| Upper arm, elbow, forearm | 42 → 179 | **Straight tube 48 px wide, x 52.43 to 100.43**, centred on the bone line. Same width all the way down; do not swell at the elbow and do not taper before the wrap. (48 = the 22c arm's median width, 47.0, rounded to keep the bone line centred.) |
| Leaf wrap | 180 → 235 | Keep the 22c wrap silhouette (outer edges within 3 px of the 22c ones), olive median about (139, 122, 60). Repaint it as part of this painting, not pasted. |
| Bark flare under the wrap | 236 → 242 | Keep the 22c silhouette (x 43–117 at row 240). This is the back of the wrist. |
| Hand cover | 243 → 256 | See 2.4. |

Every row's centre (rows 30–179) within 3 px of x 76.43. Every edge within 3 px of the template edge.

### 2.2 The top end and the mantle

At rest the mantle draws over the near arm. In arm pixels it covers x ≥ 65–68 in rows 24–40 and x ≥ 77–79 in rows 44–60; below row 60 only a corner at x ≥ 120, outside the arm; it never covers x < 65 below row 20 (see the green hatch in the 4x view). So the left part of the top, x 52–64 from about row 18 down, is visible at rest. Paint it as the plain rounded end of the same bark: same tone and grain as the arm below, no highlight blob, no rim, nothing that reads as a joint or bone end. The top still has to exist above the pivot (rows 16–41) so the shoulder stays covered when the arm swings.

### 2.3 Bark, grain and colour (match the 22c leg)

Sampled on the interior bark of `reference_22c\Qori_v3_Leg.png` (rows 40–300, 3 px in from the edge, wrap excluded):

| Measure | Leg value | Allowed for the arm |
|---|---|---|
| Shadow, p5 | (155, 104, 73) | ± 12 per channel |
| Mid-tone, p50 | (199, 150, 112) | ± 8 |
| Highlight, p95 | (224, 181, 148) | ± 10 |
| Outline (edge 2 px) | (129, 84, 59) | ± 12 |
| Dark grain (share of bark more than 35 below the median) | 0.114 | 0.07–0.16 |
| Light across the limb, 10 bins left to right (luminance) | 115, 143, 165, 174, 172, 170, 155, 136, 115, 87 | each ± 15 |

So: light from the left, the brightest band a little left of centre, the right edge darkest; fine grain running lengthwise, as on the leg. Also: the bark beside and below the wrap leaves must be the same bark as above it, never lighter (a little darker for the wrap's shadow is right), and the 8 bark rows just above the wrap within 8 luminance of the 24 rows above them.

### 2.4 Hand cover (rows 243–256)

The hands are unchanged; only their draw order changes (section 3), so they now draw **under** the arm. Carry the row-242 flare straight down for 3 more rows, **rows 243–245 fully opaque across x 42–118**, so every hand's straight top edge is under solid arm. Then fade it out over **rows 246–256** (alpha falling to 0 by row 256), only inside the white of `Qori_v3_Arm_Mask_HandCover.png`. Paint the fade as the back of the wrist running into the hand, in the hands' own bark; no line, no rim, nothing below row 256.

### 2.5 Bending

Unity bends this one painting with two bones: Upper (shoulder) and Lower (elbow at row 143.15), blended linearly over 90 px, rows 98–188. Keep that zone plain bark at an even width: no crease, no ring, no change of shading, grain running straight through.

## 3. Registration

`Qori_v3_Registration.json`: same version 2 structure, part names and order as 22c. Change exactly two values:

- `NearHand.drawOrder`: 10 → **8.9** (still above Torso 8 and LeafStaff 8.5, now below NearArm 9)
- `FarHand.drawOrder`: 3 → **1.9** (still above Cape 1, now below FarArm 2)

Allowed besides those: `status`, `supersedes` (say "22c arm"), and the `Arm` entries of `sourceExtraction` and `barkColourNormalization` (keep the Arm canvas [154, 259] and pivot exactly). Every other value, bone chain, weight and guide stays identical.

## 4. Keep byte-identical (re-deliver)

All 19 other 22c PNGs: Head, the nine Face overlays, Ear, Torso, Cape, Mantle, Leg, Foot and the three Hands. The checker compares their SHA256 with the 22c files.

## 5. Acceptance: run the checker, fix, repeat

From any folder:

```
python "<Codex_v2_full>\Batch22d_Input\check_arm_22d.py" "<Codex_v2_full>\Batch22d" "<Codex_v2_full>\Batch22d_Input"
```

It prints PASS or FAIL for 59 tests with the measured value and the limit, writes `Qori_v3_ARM_CHECK.json`, and writes three proofs into the delivery. **Do not deliver until the last line says `ALL PASS`.** If a test fails, change the painting (never the checker, the masks or the targets) and run it again. The tests:

1. Canvas 154 × 259, RGBA, RGB = 0 where alpha = 0, and not the 22c bytes.
2. No pixel with alpha > 0 outside `Mask_Outer`; every `Mask_Inner` pixel alpha ≥ 250; top row 16 ± 3; every row's centre (rows 30–179) within 3 px of x 76.43; 0 pixels outside the tube near the shoulder (the knob).
3. Wrap: olive rows inside 176–239, centre row 207.5 ± 4, olive median within 12 of (139, 122, 60).
4. Seam, rows 30–179 (middle 70% of each row): largest mean row-to-row step ≤ 6.0, largest mean absolute step ≤ 10.0, and no row where more than 25% of pixels step by more than 10 the same way. Bark beside/below the wrap not lighter than the bark above (≤ +4). The 8 rows above the wrap within 8 of the 24 above them.
5. Colour against the leg, as in 2.3.
6. The 19 files byte-identical; registration as in section 3.
7. The arm alone skinned at elbow +90°, +45° and −20° (two-bone linear blend, the registration's own weights): 0 see-through holes inside the limb, and 0 enclosed see-through pixels in the straight arm.
8. Hand joins: arm and each hand (Relaxed, Grip, Spread) at the registered positions and draw order, upsampled 2x with bilinear filtering like the GPU: coverage ≥ 0.95 across the join line, no row where more than 25% of pixels step by more than 10 the same way, no mean brightness step above 6.

Then look at the proofs yourself: `Qori_v3_Proof_ArmCheck_Elbow_2x.png` (the elbow at +90, +45 and −20 at 2x; no cap, ring, block or step), `Qori_v3_Proof_ArmCheck_HandJoin_2x.png` (the three hands; no line), `Qori_v3_Proof_ArmCheck_Template.png` (any pixel outside the template or a hole inside it is red).

## 6. Delivery (same files and checks as 22c)

`Codex_v2_full\Batch22d\`:

- `Qori_v3_Arm.png` (new) and the 19 unchanged PNGs, `Qori_v3_Registration.json`.
- The checker's `Qori_v3_ARM_CHECK.json` and its three proof images, from the final run.
- The 22c proof set, re-rendered with the new arm and draw order: `Qori_v3_Proof_Rest`, `_Overlay` (with the landmark table), `_Stride`, `_Bend`, `_Bend_Detail_2x`, `_Staff`, `_ScaleCheck_1080`; plus `Qori_v3_LANDMARK_CHECK.json`, `Qori_v3_QA_CHECK.json`, `Qori_v3_SKINNING_CHECK.json` as in 22c. The hanging wrist wrap landmark should stay where it is (22c measured 0.561H).
- `MANIFEST.json` (every file but itself, with path, SHA256, byte size, pixel size), `PRESERVATION_CHECK.json` against the canonical accepted manifest (currently 1158 entries, SHA256 `a60f9c91bbc0e4c1816633274468a85f24a4edc7322b130bbd4e53f943cf0c26`; re-read it, do not assume), `NAME_COLLISION_CHECK.json`.
- `sources\`: prompts, native outputs, selected and rejected attempts, build and proof scripts.
- `REVIEW_HANDOFF.md`: the checker's final output pasted in full, the new arm's sampled colours, the two draw-order changes, anything still off-target. Platform readiness: "not tested". Original Switch excluded.

Stop after delivery for Claude's review. Nothing is imported until the user approves it.

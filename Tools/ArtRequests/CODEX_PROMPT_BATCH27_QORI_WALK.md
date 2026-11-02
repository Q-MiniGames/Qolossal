# Codex prompt: Batch 27, Young Adventurer Qori walk cycle (16 new frames)

Written 12 Oct 2026. The user's ask: "give me a prompt to give to Codex to draw 16 frame by frame walk for Qori 'the young adventurer version' with all the walking animation included like the cloak movement and hand movement and body poses and ears movement", then "the whole idea is to revive the 'Young Adventure Model' the one we ignored because the rig method cannot do it. so i need totally new 16 frames based on that model."

So this batch is **frame-by-frame painting of the Young Adventurer model**, not the in-game rig and not the parked Batch 22 rig parts. The model is the look the user approved in Batch 19b (it grew out of the user's own Young Adventurer image in Batch 19): the large three-quarter figure of `Codex_v2_full\Batch19b\Concept_Qori_v3b_Turnaround.png` is the model authority (its handoff says so), with the staff idle, cape study, ears and motion sheets beside it. Not sent: the user sends it. Batch 27 and the `Qori_Walk_` names were checked free on 12 Oct 2026.

There is no rig to render for this model, so `Tools/ArtRequests/Batch27/walk_blockout.py` builds a **walk block-out**: a flat-shape construction figure with the model's proportions, measured on the 19b figure and scaled ×1.13, posed through the walk below by a small 2D skeleton. The legs are solved by IK so planted feet stay put, and the cloak is a damped chain walked to a steady loop. `make_batch27_input.py` wrote the block-out, masks, the model placed on the frame canvas, targets measured on the model, a registration template and the checker into `Codex_v2_full\Batch27_Input`. The checker (`check_walk_27.py`, 402 tests) passes a mock built from the block-out and a plausible repaint of it (3 px off, softened, warmer), and fails seven broken mocks: smeared, 12% too big, feet skating 14 px, red flicker on odd frames, a held frame, wrong palette, and a frozen cloak and ears. The earlier rig-guide version of this batch is superseded; its scripts are in `Tools/ArtRequests/Batch27_RigGuides_Superseded/`.

**Why 16 frames.** 8 per step: contact, down, passing and up on the odd frames, in-betweens on the even ones. At the drawn pace (0.72 s a cycle, 45 ms a frame, 22 drawings a second) that is smooth for a hand-painted walk. The cloak, ears, skirt and pouch all overlap within those 8 frames per step, so more frames would not show more motion. More frames would add painting, and with a generated painter, more chances for the model to drift between frames. If the game later plays this walk much slower than drawn, add in-betweens then; the block-out script makes 24 or 32 as easily.

**Weapon (default; change it if you want the staff in hand).** The walk is drawn empty-handed, so both arms swing freely: the user asked for the hand movement. The Leaf Staff and other weapons stay out of these frames. Carrying the staff while walking would need either a second set of frames or a staff layer the game attaches per frame.

---

Qolossal, Batch 27: paint a 16-frame walk cycle of **Qori, the Young Adventurer**, as 16 new full-figure paintings, three-quarter view facing right, on one fixed canvas. Every frame is the model from `Model_A_Turnaround_19b.png` (large left figure): same face, ears, crest, mantle, clasp, tunic, belt, pouch, skirt, cape, limbs, wraps, root feet and palette, at the same size. Only the pose changes, and it follows the 16 block-out frames in `Batch27_Input`. Deliver to `C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch27\`. Do not write under the live project, and do not touch the music.

## 0. Inputs (read first; do not edit them)

`Codex_v2_full\Batch27_Input\`:

| File | What it is |
|---|---|
| `reference\Model_A_Turnaround_19b.png` | **The model.** Paint the large left figure. The two small views on the right are front and back, at a smaller presentation scale. |
| `reference\Model_B_StaffIdle_19b.png`, `Model_C_CapeStudy_19b.png`, `Model_D_Ears_19b.png`, `Model_E_Motion_19b.png` | The same character holding the staff, the flat cape and how it bends, the ears, and moving poses. Use them for how the cape and ears bend; when they disagree with Model A, Model A wins. |
| `reference\Model_F_YoungAdventurer_original.png` | The user's original Young Adventurer image the model grew from. For spirit only: the cape here is the 19b leaf cape, not the original cloth cape, and there are no berries on the pouch. |
| `reference\Model_OnCanvas.png` | Model A's figure cut out and placed on the 1400 × 1400 frame canvas at the exact size to paint (×1.13), soles on the ground line. This is the size of every frame. |
| `Batch27_Model_vs_Blockout.png` | That model beside block-out frame 5, to see how the block-out maps onto him. |
| `blockout\Qori_Walk_NN_Blockout.png` (16) | **The pose to paint, frame NN.** Flat colours per part on the final canvas (legend below). |
| `blockout\Qori_Walk_NN_Annotated.png` (16) | The same on grey with the ground line (red, row 1340), the pivot column (cyan, x 760), the top of the head (green), the skeleton (hips, knees, ankles, shoulders, elbows, wrists, spine to head) and the cloak's centre line. |
| `blockout\Qori_Walk_NN_Blockout_Toned.png` (16) | The block-out in the model's flat colours. |
| `Batch27_Blockout_Sheet.png`, `Batch27_Annotated_Sheet.png`, `Batch27_Blockout_Toned_Sheet.png` | All 16 on one sheet each, with the key pose names. |
| `masks\Qori_Walk_NN_Mask_<part>.png` | Alpha of each block-out part: `head`, `ear_far`, `ear_near`, `mantle`, `torso`, `skirt`, `pouch`, `arm_near`, `arm_far`, `leg_near`, `leg_far`, `cape`, plus `Core` (everything but the cloak and ears) and `All`. |
| `batch27_blockout.json` | Every joint and angle per frame (pelvis, neck, head, hips, knees, ankles, heels, toes, foot angles, shoulders, elbows, wrists, arm swing, ear angles, cloak chain, pouch angle, skirt lift). |
| `batch27_targets.json` | Every limit the checker uses, with the model's measured face size and colours. |
| `Qori_Walk_Registration_TEMPLATE.json` | Canvas, ground line, pivot, timing and ground speed. Return it unchanged. |
| `check_walk_27.py` | The acceptance checker (Python 3, numpy and Pillow only). |

Block-out colours: head cream, ears light green, mantle dark green, tunic sand, skirt pale sand, pouch brown, **near arm red**, **far arm purple**, **near leg orange**, **far leg blue**, cloak green.

"Near" is the side toward the camera. In this three-quarter view the near arm and leg are on the screen-left of the body, as in Model A (the near hand is the big one in front of the cape, and the pouch hangs on the near hip). Near limbs are drawn in front, far limbs behind.

**Attach to the image model for every frame:** `Model_A_Turnaround_19b.png`, `Model_OnCanvas.png`, that frame's `_Blockout.png` and `_Annotated.png`, and your painted previous frame. Add `Model_C_CapeStudy_19b.png` for cloak frames and `Model_D_Ears_19b.png` for the ears. Never attach the in-game Qori (`Qori_Idle_v1.png`, the `QoriRig` parts) or any Batch 22 rig part: this is a different look.

## 1. The model (lock all of it)

Qori, Young Adventurer: a large cream face with big dark eyes, small brows and a tiny nose, and no mouth. Two long olive leaf ears with pale pink undersides sweep back from a crest of leaves. A shingled green leaf mantle with a leaf-gem clasp, over a cream wrapped-leaf tunic with a woven grass belt and one or two small leaves tucked in. A small brown pouch hangs on the near hip. A cream leaf skirt has olive leaf tips under its hem. Bark-brown limbs have grass wraps at the wrists and ankles, the hands have four digits, and the feet are big three-toed root feet. Behind is the one-piece leaf cape: one olive leaf with a central midrib, branching veins and a shallowly lobed hem. Morning Herbarium palette, soft light from the upper left, a thin warm-dark edge line, painted texture like Model A.

Lock these in every frame: **the head is the same size and shape** (the checker measures the cream face against the model's, within 7%, and between neighbouring frames within 3%); both ears are the same leaf, the same length; the pouch stays on the near hip in front of the cape; the cape stays behind the body. Nothing new: no staff, props, belt charms, berries or text.

## 2. Canvas

16 transparent PNGs, `Qori_Walk_01.png` … `Qori_Walk_16.png`, **1400 × 1400**, straight RGBA (RGB = 0 where alpha = 0). No background, no ground shadow, no glow, no border or labels; nothing within 8 px of the canvas edge.

- **Ground line row 1340.** In every frame the lowest planted sole touches row 1340 (the checker allows 1336–1341).
- **Pivot column x 760:** the pelvis sits on it in every frame. The game moves Qori; the frames walk on the spot.
- **Size:** Model A ×1.13, as `Model_OnCanvas.png` shows (top of the head 1000 px above the soles standing; about 950 while walking, because the knees stay soft).
- Facing right; the game mirrors for left.

## 3. The walk (direction)

A light, curious young walk: springy but not bouncy, the head steady, the cloak and ears doing the talking. 0.72 s a cycle, 16 frames of 45 ms. Stride 492 px a cycle: **a planted foot moves back 30.7 px a frame** and must do so exactly, or the feet slide in game. Each foot is down for 60% of the cycle.

- **Bob:** the pelvis is lowest on the down (frames 3, 11) and highest on the up (7, 15), 32 px apart.
- **Lean:** the torso leans 5° forward, 6.5° on the down.
- **Arms** swing opposite the legs, about 22° each way, one frame behind the legs (drag). The elbows bend more on the forward swing (14° back, up to 36° forward). The hands are relaxed and loosely curled, and the wrists trail the forearms by a frame.
- **Head:** rides the body with a small nod a frame late, chin down about 2° after each landing and up 2° after each push-off. It never turns or changes shape.
- **Ears** trail the head: as the body drops the tips lift, and as it rises they droop, by about 10°. The far ear is 2 frames behind the bob and the near ear 3 frames behind. Each tip bends a frame behind its own base.
- **Cloak:** it trails behind at walking speed. The hem swings back most at frames 3 and 11, forward most at 7 and 15, lifts highest at 5 and 13 and hangs lowest at 2 and 10. A ripple runs from the shoulders down to the hem over half a step (4 frames). The midrib bends with the ripple; the leaf shape, veins and the count of hem lobes never change.
- **Skirt** answers the bob a frame late. The hem leaves lift on frames 1–3 (most on 2) and hang low on 5–7 (most on 6). The front edge is pushed forward by the swinging thigh at passing.
- **Pouch** swings on the belt a couple of frames behind the hips: furthest forward at frames 4 and 10 (+7°), furthest back at 15 (−14°).
- **Mantle:** the leaves stay put on the shoulders. Only their tips flick a frame after each down.
- **Feet:** the heel strikes with the toes up, the foot rolls flat, then rolls over the ball with the heel up and the toes flat on the ground. In swing it lifts toes-down, then reaches heel-first. A planted foot never changes shape. The root toes spread under weight.

### Frame table

Bob is the pelvis against its cycle average (+ up). Near arm swing: − back, + forward; the far arm is the opposite. Soles are the block-out's lowest row of each foot (1340 = on the ground).

| # | Key | Near leg (orange) | Far leg (blue) | Bob | Lean | Near arm | Head | Ears (far / near) | Cloak | Skirt, pouch |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | **Contact A** | Forward, heel strikes, toes up, knee nearly straight; sole 1337 | Back, on the ball, heel up; 1340 | 0 | −5.0° | −20° back | Chin starting down | Neutral / still down | Hem mid, swinging back | Hem lifting; pouch just behind centre, swinging forward |
| 2 | | Foot rolls flat, knee takes the weight | Toes pushing off; 1340 | −11 | −6.0° | **−22°** (furthest back) | Chin down | Lifting / lifting | **Hem lowest** | **Hem lifted most** |
| 3 | **Down A** | Knee bent most, foot flat; 1340 | Just off the ground, foot trailing; 1334 | **−16** | −6.5° | −20° | Chin down | **Far tip highest** / lifting | **Hem furthest back** | Hem still lifted; pouch forward |
| 4 | | Straightening under the hip | Swinging through, knee bending; 1333 | −11 | −6.0° | −16° | Level | Falling / **near tip highest** | Ripple halfway down | Hem settling; **pouch furthest forward** |
| 5 | **Passing A** | Straight, under the hip | Passing the near leg, 20 px clear; 1320 | 0 | −5.0° | −8° | Chin up | Falling / falling | **Hem highest** | Hem low; front pushed by the far knee |
| 6 | | Heel starting to lift | Reaching forward, knee leading; 1313 | +11 | −5.0° | 0° | Chin up | Low / falling | Hem swinging forward | **Hem lowest** |
| 7 | **Up A** | On the ball, heel up, pushing off | Shin swinging forward, foot highest, 32 px clear; 1308 | **+16** | −5.0° | +8° | Chin up | **Far tip lowest** / low | **Hem furthest forward** | Hem low |
| 8 | | Toes only | Leg extending, heel leading, toes lifting; 1309 | +11 | −5.0° | +16° | Level | Rising / **near tip lowest** | Hem turning back | Hem settling |
| 9 | **Contact B** | Back, on the ball, heel up; 1340 | Forward, heel strikes, toes up; 1337 | 0 | −5.0° | +20° forward | As 1 | As 1 | As 1 | As 1 |
| 10 | | Toes pushing off | Foot rolls flat | −11 | −6.0° | **+22°** (furthest forward) | As 2 | As 2 | As 2 | As 2 |
| 11 | **Down B** | Just off the ground; 1334 | Knee bent most | **−16** | −6.5° | +20° | As 3 | As 3 | As 3 | As 3 |
| 12 | | Swinging through; 1333 | Straightening | −11 | −6.0° | +16° | As 4 | As 4 | As 4 | As 4 |
| 13 | **Passing B** | Passing, 20 px clear; 1320 | Straight, under the hip | 0 | −5.0° | +8° | As 5 | As 5 | As 5 | As 5 |
| 14 | | Reaching forward; 1313 | Heel starting to lift | +11 | −5.0° | 0° | As 6 | As 6 | As 6 | As 6; pouch swinging back |
| 15 | **Up B** | Foot highest, 32 px clear; 1308 | On the ball, pushing off | **+16** | −5.0° | −8° | As 7 | As 7 | As 7 | As 7; **pouch furthest back** |
| 16 | | Extending, heel leading; 1309 | Toes only | +11 | −5.0° | −16° | As 8 | As 8 | As 8 | As 8 |

Frame 16 must lead into frame 1 as smoothly as any other pair; the checker tests 16 → 1 like the rest.

## 4. How to work (this keeps Qori on model and stops flicker)

1. **Paint frame 5 (passing) first**, the pose closest to Model A, over its block-out at `Model_OnCanvas` size. Check it against the model side by side (`Qori_Walk_Proof_ModelCompare.png` from the checker) before going on.
2. Then the other key poses: 1, 3, 7, then 9, 11, 13, 15, each from frame 5's painting re-posed over its block-out. Do not repaint the head from scratch: reuse the frame-5 head, face, crest and mantle and only move and tilt them as the table says.
3. Then the in-betweens 2, 4, 6, 8, 10, 12, 14, 16, each from its painted neighbours.
4. Keep one brush, edge line, texture scale and light. Compare neighbours side by side, and play `Qori_Walk_Proof_Loop.gif` before delivering.

The block-out is a construction drawing, not the silhouette to trace: paint the model's real shapes (the rounded head, the leaf mantle, the leaf cape) over it. Keep the block-out's pose, joint positions, foot placement, head position and cloak position. The checker allows the paint to differ from the block-out by 28 px at the edges, and the cloak and ears up to 55 and 40 px.

## 5. Registration

Return `Qori_Walk_Registration.json`, a copy of the template (canvas 1400 × 1400, footLineRow 1340, pivotColumn 760, frameCount 16, cycleSeconds 0.72, groundPxPerFrame 30.74, and per frame its file, key pose and 45 ms duration). You may change only `status` and add `notes`.

## 6. Acceptance: run the checker, fix, repeat

```
python "<Codex_v2_full>\Batch27_Input\check_walk_27.py" "<Codex_v2_full>\Batch27" "<Codex_v2_full>\Batch27_Input"
```

It prints PASS or FAIL for each of its 402 tests with the measured value and the limit, writes `Qori_Walk_CHECK.json` and six proofs. **Do not deliver until the last line says `ALL PASS`.** If a test fails, change the painting (never the checker, the block-out or the targets) and run it again. The sections:

1. **Format:** 16 files, 1400 × 1400 RGBA, straight alpha, no glow, nothing touching the edge, all different.
2. **Block-out:** at least 90% of the block-out body (shrunk 10 px) painted; no more than 3% of the paint beyond the block-out + 28 px (cloak and ears free); figure area 0.8–1.3 times the block-out's.
3. **Ground:** lowest sole on row 1336–1341; the bottom 60 rows overlap the block-out's feet (IoU ≥ 0.45); every planted foot within 12 px of the block-out's; and planted feet keep the 30.7 px-a-frame ground speed (no skating, worst error 7 px).
4. **Model:** the cream face found where the block-out head is, its width and height within 7% of the model's, its centre within 16 px of where the block-out puts it (so it bobs right); at least 90% of the colours within 42 of the model's palette; no neon; the bark, cloak and face medians within 24 of the model's; painted detail 0.55–1.6 times the model's (painted, not smeared or noisy).
5. **Loop**, for all 16 pairs including 16 → 1: silhouette change 0.45–2 times the block-out's, no pair more than 2.3 times the median change (no pop or held frame), face size steady (within 3%), detail steady (within 18%), overall colour steady (mean within 6); and the cloak and both ears really move between frames 4 and 8 (at least 35% of the block-out's motion there).
6. **Registration** unchanged.

Then look at the proofs yourself:

- `Qori_Walk_Proof_Loop.gif` (drawn speed) and `_Loop_Slow.gif` (0.6x): the feet stick to the ground and roll, the body dips after each contact and rises through each push-off, the arms swing against the legs, the ears and cloak trail and settle, and nothing on the face, mantle or tunic crawls or flickers.
- `Qori_Walk_Proof_ModelCompare.png`: the model beside your frame 5. The same character at the same size.
- `Qori_Walk_Proof_Sheet.png`, `Qori_Walk_Proof_BlockoutOverlay.png` (grey = on the block-out, red = paint outside it, blue = block-out left empty), `Qori_Walk_Proof_FeetOnion.png` (all 16 feet on the ground line, stepping back evenly).

## 7. Delivery

`Codex_v2_full\Batch27\`:

- `Qori_Walk_01.png` … `Qori_Walk_16.png`, `Qori_Walk_Registration.json`.
- The checker's `Qori_Walk_CHECK.json` and its six proofs from the final run.
- `MANIFEST.json` (every file but itself: path, SHA256, byte size, pixel size), `RECEIPT.json` (batch, date, file count, the checker's final line, the manifest's SHA256), `PRESERVATION_CHECK.json` against the canonical accepted manifest `C:\UnityProjects\Qolossal\Tools\ArtImport\codex_v2_accepted.json` (read only; 1226 entries on 12 Oct 2026, but re-read it and report its count and SHA256), and `NAME_COLLISION_CHECK.json` for the `Qori_Walk_NN` names (the old `Assets/Art/Characters/Qori/Walk/Qori_Walk_0N.png` six-frame files are a different, older walk; report them and don't touch them).
- `sources\`: prompts, native outputs, selected and rejected attempts, any scripts.
- `REVIEW_HANDOFF.md`: the checker's final output pasted in full, which frames took the most attempts and why, and anything still off-model or off the block-out. Platform readiness: "not tested". Original Switch excluded.

Stop after delivery for Claude's review. Nothing is imported until the user approves it.

---

## Notes for Claude (not for Codex)

- Reviving this model frame by frame means Qori's other states (idle, run, jump, fall, glide, land, attacks, ledge, wall) would need frames too: the current cut-out rig and its clips cannot wear this look (that is why it was parked). The walk is the pilot. Before ordering more, review it in game with the user at real size (about 180 px tall at 1080p) on all four platforms' target resolutions.
- Rebuild the inputs: `python Tools/ArtRequests/Batch27/make_batch27_input.py "<Codex_v2_full>\Batch27_Input"`. The direction constants are at the top of `walk_blockout.py`. Frame count, cycle time, stride, bob, lean, arm swing, ear and cloak lag are each one number there.

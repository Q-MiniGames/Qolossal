# Codex prompt: Batch 22, Qori v3 rig parts

Written 8 Oct 2026, after the user approved the Batch 19b look ("Let's proceed with the rig parts request"). Carries the fixes from `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_31.md`. The user didn't answer the limb-width question, so this uses the default from Review 31: about 15% thinner than 19b, with slightly darker bark. Not sent: the user sends it. The `Qori_v3_` names were checked free in `Tools/ArtImport/codex_v2_accepted.json` on 8 Oct 2026. The part-registration format follows `Batch10/W1/Sources/Reference_QORI_CLOAK_REGISTRATION.json` (version 2). This batch delivers parts only: no import and no game changes.

---

Qolossal, Batch 22: production rig parts for Qori's approved v3 look (Batch 19b). Qori's rig will be rebuilt from separate skinned parts, so every part must be painted once, cleanly, at one shared scale, ready to bend. Deliver to `Codex_v2_full\Batch22` with `REVIEW_HANDOFF.md`, `MANIFEST.json` (path, sha256, byte size and pixel size per file), `Qori_v3_Registration.json` (below), a preservation check of the canonical accepted manifest, a name-collision check, and a source archive with prompts, as in earlier batches. Do not write under the live project.

**The approved look:** `Codex_v2_full\Batch19b\`. Use the large 3/4 figure in `Concept_Qori_v3b_Turnaround.png` and `Concept_Qori_v3b_StaffIdle.png` as the model, and `Concept_Qori_v3b_CapeStudy.png` for the cape. Keep everything there: the face, ears, crest, feathered mantle and clasp, the cream leaf tunic with belt leaves and skirt-hem leaves, the pouch on the near hip, the wrist and ankle wraps, the root feet, the one-leaf cape and the Morning Herbarium palette.

## Fixes to apply while painting the parts

1. **Angle.** Turn the body and head about 10–15° back toward the side, to match the current in-game Qori, `C:\UnityProjects\Qolossal\Assets\Art\Characters\Qori\Qori_Idle_v1.png` (read only). The far eye shows only partly past the face, and the chest is a three-quarter angle, not near-frontal. Facing right; the game mirrors for left.
2. **Limbs.** Upper arms, forearms, thighs and shins about 85% of the 19b width (about 15% thinner). Make the bark a shade darker and warmer, closer to the current Qori's brown, with less muscle bulge. Hands stay the 19b size, with four digits. The root feet stay the 19b size.
3. **Ears.** One ear painting used for both ears. Do not paint a second ear.
4. **Cape.** Paint the cape flat, from the cape study's leaf shape, at about **1.2x the 19b rest width**. The rig gathers it in about 15% at rest and opens it about 20% for the glide, so it never stretches more than that.
5. **Leaf Staff.** Do not paint a staff. The rig uses the existing accepted Leaf Staff art. Hands that hold it are drawn as a grip (part 7b) with a gap sized for the current shaft.

## Common rules for every part

- **Scale:** all parts at one shared scale. Assembled at rest, Qori measures **1200 px from the top of the head (not ear tips) to the sole**. No part is resampled to a different scale. Record `pixelsPerUnit: 100` in the registration.
- **Canvas:** transparent PNG, straight (non-premultiplied) alpha, with 16 px of empty padding on every side. No halo, glow or soft aura at the edges (Batch 19's scale study had one). No baked cast shadow and no background.
- **Light:** soft, from the upper left, matching the 19b sheets. No rim light that would look wrong when a part rotates.
- **Rest pose:** each limb part hangs straight down (0°). The head is level. The ear is at neutral (Batch 19b `Ears` top left). The cape is flat and straight, as in the cape study.
- **Joint overlap:** every limb part continues past its joint with a rounded end about half the limb's width. Bending the joint 90° then never shows a gap or a hard edge. Paint those ends fully; they are hidden at rest, not cut off.
  - The shoulder end of the upper arm goes under the mantle.
  - The hip ends of the thighs go under the skirt.
  - The wrist end of the forearm sits under the wrist wrap, and the ankle under the ankle wrap.
  - The elbow and knee each get a round cap on both pieces.
- **Hidden areas are painted.** Wherever another part covers this one at rest (under the mantle, under the skirt, behind the head), keep painting the surface normally, so moving parts never reveal a blank or cut edge.
- **No baked detail that crosses a joint.** No wrap, leaf or shadow is painted across two parts.

## Parts (14 pieces from 10 paintings, plus swaps)

Far limbs reuse the near-limb paintings (the rig darkens them). Names start `Qori_v3_`.

| # | File | Contents | Pivot |
|---|---|---|---|
| 1 | `Qori_v3_Head` | Head and cream face with **no eyes, brows or nose**, crest and cheek foliage. The crest's lower leaves extend down over the ear bases. | Neck point under the chin |
| 2 | `Qori_v3_Ear` | One ear at neutral, green top with pale underside, as one leaf. The base is a rounded stub that tucks under the crest. Even shading, so a bend along its length looks natural. | Centre of the base |
| 3 | `Qori_v3_Torso` | Leaf-wrap tunic, woven belt with its tucked leaves, skirt with the olive hem-leaf tips, and the pouch on the near hip, all painted as one piece. The skirt reaches down far enough to cover both thigh tops at rest. A short neck stub goes up under the head. | Pelvis centre |
| 4 | `Qori_v3_Mantle` | Feathered shoulder mantle with the leaf-gem clasp, as in 19b. It covers the cape top and both shoulder joints. | Neck base |
| 5 | `Qori_v3_Cape` | The one-leaf cape, flat, 1.2x the rest width, with the straight midrib, even veins, shallow even hem lobes and outer face only. The top 15% is the hidden attachment zone, painted plainly. | Top centre of the midrib |
| 6 | `Qori_v3_UpperArm` | Upper arm, rounded at both ends. | Shoulder centre |
| 7a | `Qori_v3_Forearm_Relaxed` | Forearm, wrist wrap and a relaxed open hand. | Elbow centre |
| 7b | `Qori_v3_Forearm_Grip` | The same forearm with the hand closed round a gap sized for the current Leaf Staff shaft. | Elbow centre |
| 7c | `Qori_v3_Forearm_Spread` | The same forearm with the fingers spread for the glide and falling. | Elbow centre |
| 8 | `Qori_v3_Thigh` | Thigh, rounded at both ends. | Hip centre |
| 9 | `Qori_v3_ShinFoot` | Shin, ankle wrap and root foot in one piece, the foot flat on the ground at rest. The foot will bend at the ankle by mesh skinning, so the bark grain flows smoothly through the ankle. | Knee centre |

Paired pieces: the upper arm, forearm, thigh and shin/foot are used twice, near and far. The ear is used twice. That gives 14 rig pieces, with 7a/7b/7c as swaps for the same piece.

**Face overlays** (`Qori_v3_Face_*`), painted to sit exactly on the blank head, on the head's canvas size so they register with no offset: eyes, brows and the small nose. Make these: `Neutral`, `Blink` (eyes closed), `Curious`, `Determined`, `Sad`, `Effort`, `Hurt`, `Surprise` and `Sleep`. They match the 19b ear sheet's expressions and the moods of the current `Assets/Art/Codex/Player/Qori_Face_*` (read only). No mouth in any of them.

## Draw order (back to front, facing right)

Far ear, cape, far upper arm, far forearm, far thigh, far shin/foot, near thigh, near shin/foot, torso, near upper arm, near forearm, mantle, head, near ear, face overlay. The Leaf Staff sits between the torso and the near upper arm.

Both ears sit behind the crest at their base, so the near ear's base is covered by the head's crest leaves even though the ear is drawn in front.

## `Qori_v3_Registration.json`

Use the version 2 format of `Reference_QORI_CLOAK_REGISTRATION.json`. For each part, give:
- `name`, `path` and `canvas`
- `pivotTopLeftPx` and `pivotUnity`
- `parentBone`, `restPosition`, `restRotationDegrees`, `drawOrder` and `pixelsPerUnit: 100`
- where it attaches on its parent, and where its child joint sits (`boneEndpointLocal`)

Add near and far entries for the paired parts, with the far ones offset as in the 19b pose.

For the cape, also give a **bone guide**: five points down the midrib from the pivot to the hem, plus three points down each side edge. Do the same for the ear: three points from the base to the tip.

## Proofs (concept images only, not parts)

1. `Qori_v3_Proof_Rest`: all parts composited from the registration JSON at rest, on neutral grey. It should look like the 19b staff idle at the corrected angle, without the staff, hands relaxed.
2. `Qori_v3_Proof_Bend`: the same parts composited by script, not repainted, in a test pose: both elbows and knees bent 90°, the head tilted 10°, the ears in the Determined angle and the cape rotated 40° back. It proves there are no gaps or hard edges at the joints.
3. `Qori_v3_Proof_Staff`: the rest pose with the Grip forearms and the existing Leaf Staff art from the live project (read only), composited by script.
4. `Qori_v3_Proof_ScaleCheck_1080`: the rest proof in the same archived Qvale capture as Batches 19 and 19b, scaled to about 178 px, next to the current Qori.

## REVIEW_HANDOFF.md

List each part with its size and pivot. Say what the proofs show, and anything that still drifts from the 19b look. Platform readiness is "not tested". Stop after delivery for Claude's review; nothing is imported until the user approves it.

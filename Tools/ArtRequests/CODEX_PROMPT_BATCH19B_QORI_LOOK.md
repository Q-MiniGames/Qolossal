# Codex prompt: Batch 19b, Qori v3 look revision (concept only)

Written 8 Oct 2026 from the user's answers to `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_30.md`. Not sent: the user sends it. Assumes Qori's rig will be rebuilt from separate skinned parts. This batch is still concept only: no rig parts, no slicing, no import.

---

Qolossal, Batch 19b: revise the Qori v3 look from Batch 19. **Concept only.** Do not slice rig parts, do not write under the live project, do not touch music. Deliver to `Codex_v2_full\Batch19b` with `REVIEW_HANDOFF.md`, a `MANIFEST.json` (path, sha256 and pixel size per file), a preservation check of the canonical accepted manifest, and the source archive with prompts, exactly as in Batch 19. All image stems start `Concept_Qori_v3b_`.

**Keep from Batch 19:** the cream face with no mouth, the small nose, the big dark eyes and brows, the olive leaf ears with pale undersides, the leaf-gem clasp, the cream leaf-wrap tunic and woven belt, the wrist and ankle wraps, the root feet, the Morning Herbarium palette and the one-piece leaf cape. The shoulder mantle stays as it is in Batch 19; its leaves may be slightly larger, but keep the same look. The crest stays as is.

**References:**
- Batch 19 delivery: `Codex_v2_full\Batch19\` (turnaround, motion, ears, shape map).
- The user's Young Adventurer image: `Codex_v2_full\Batch19\sources\Concept_Qori_v3_YoungAdventurer_reference.png` (belt and skirt leaves, see change 4).
- **The view to match:** current in-game Qori, `C:\UnityProjects\Qolossal\Assets\Art\Characters\Qori\Qori_Idle_v1.png` (3/4 view facing right, chest turned slightly toward the camera, the near shoulder and both legs visible, holding the Leaf Staff). Read only.

## Changes

1. **The 3/4 view, not the strict side view.** The main gameplay view is the 3/4 angle of `Qori_Idle_v1.png`, facing right. Draw the model sheet's main view and every motion pose at this angle. Batch 19 used a strict profile for the side view and motion; replace it.
2. **Limbs halfway between old and new.** Make the upper arms, forearms, thighs and shins about 75% of the Batch 19 width. That is thinner than Batch 19 and thicker than the twig limbs in `Qori_Idle_v1.png`. Keep the bark grain, but with less muscle bulge. Make the hands slightly smaller, keeping four digits. The root feet stay the Batch 19 size.
3. **Both ears identical.** The two ears must be the same leaf shape and the same length. Show depth only by placement and a slightly darker far ear, never by drawing one ear shorter. One ear painting will be reused for both.
4. **Small leaves on the belt and skirt.** As in the Young Adventurer reference, tuck one or two small olive leaves into the woven belt. Add a few short olive leaf tips peeking out under the cream skirt hem. Keep them small and attached, painted as part of the torso, not loose hanging pieces.
5. **Pouch fixed on the near hip.** The pouch hangs from the belt on the near hip, in front of the cape, in every view and pose. It may carry one small leaf, as in the reference, but no seed charms.
6. **One locked model.** The head-to-body ratio, ear length, limb length, cape length and pouch size must match the 3/4 model view in every drawing. Batch 19's scale study and landing pose drifted slimmer and smaller-headed.
7. **The Leaf Staff.** Add one idle pose holding the existing Leaf Staff exactly as it is in `Qori_Idle_v1.png`: same shaft, same leaf blade, held across the body in both hands. Do not redesign it. The hands must visibly grip it, and the cape must stay behind it.

## The one-piece cape (must animate as smoothly as possible)

The cape will be a single skinned mesh bent by a chain of bones. Design it for that:

- **Construction.** One leaf-shaped membrane with a single straight midrib running from the neck to the hem, and side veins branching evenly off it. The midrib is where the main bone chain will run. No painted folds, rolls, curls or creases in the rest shape, because painted folds fight the bending and look wrong once the cape moves. Show the shape with the veins and soft shading only.
- **Top attachment hidden under the mantle.** The top of the cape (about 15% of its length) continues up under the shoulder mantle, so the mantle covers the joint. The cape can then swing without a visible seam or gap at the shoulders.
- **Behind the body.** In the 3/4 view the cape sits entirely behind the torso, arms and legs, never wrapping in front of them. Its near edge can show beside the body, but no part of the cape crosses in front of a limb or the staff.
- **Hem.** Shallow, even lobes along the hem (Batch 19's lobes are about right), with no deep notches, holes, thin points or separate leaves. Thin bits tear visually when a mesh stretches.
- **Outside face only.** The cape shows its olive outer face in every pose. Do not show the pale underside curling over, as in Batch 19's glide and landing drawings. A second surface would need a second painted layer.
- **Rest width allows the glide.** The rest cape should hang at a medium width, so the glide's open shape needs no more than about 25% widening. Show the glide outline over the rest outline once, so the stretch can be checked.
- **Clean outline.** The cape's edge and midrib are drawn the same in every pose, only bent. Do not repaint new detail per pose.

Deliver **`Concept_Qori_v3b_CapeStudy`**, a separate sheet:
1. The rest cape alone, flat, from the 3/4 view, with the midrib, the covered attachment area (lightly marked) and the hem.
2. Cape key shapes on a faint body silhouette: idle (with a slight sway either way), run trail, jump rise, fall, glide open (with the rest outline overlaid) and landing settle. They show how far the cape bends and lags, not tested animation.

## Ear expressions

Neutral, Curious and Determined, drawn in the 3/4 view at the locked ear size:
- **Curious:** one ear perks up and tilts forward, the other relaxed; the brows raised.
- **Determined:** both ears swept back flat along the head line; the brows lowered.
- **Sad:** both ears lowered back and down behind the head, about 60° down from neutral. They do **not** hang down beside the face, which would flip the ear and need new art. The brows are tilted up in the middle.

Each expression must be reachable from the neutral ear by rotating it at the base and bending it along its length, with no new ear shape. The ears keep their pale underside facing the same way as in the neutral pose.

## Sheets

1. `Concept_Qori_v3b_Turnaround`: the 3/4 main view (large, matching `Qori_Idle_v1.png`'s angle), plus front and back. Leave plenty of space between the views.
2. `Concept_Qori_v3b_StaffIdle`: the 3/4 idle holding the Leaf Staff.
3. `Concept_Qori_v3b_Motion`: idle, run, jump rise, fall, glide and land, all at the 3/4 angle, on the locked model.
4. `Concept_Qori_v3b_CapeStudy`, as above.
5. `Concept_Qori_v3b_Ears`: Neutral, Curious, Determined and Sad.
6. `Concept_Qori_v3b_ScaleCheck_1080` and `_720`: the new look next to the accepted Qori in the same archived Qvale capture as Batch 19.

## Notes for REVIEW_HANDOFF.md

List any place where the drawing still drifts from the locked model. Platform readiness stays "not tested", as in Batch 19. Stop after delivery for Claude's review. No rig parts until that review and the user approve the look.

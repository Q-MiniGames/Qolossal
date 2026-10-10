# Codex prompt: Batch 22b, Qori v3 rig parts correction

Written 8 Oct 2026 at the user's request, after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_32.md`. Not sent: the user sends it. A correction pass on Batch 22, not a new look. No import and no game changes. The target fractions below were measured by Claude on `Batch19b/Concept_Qori_v3b_StaffIdle.png` (crest top y≈148, sole y≈1160) and on `Batch22/Qori_v3_Proof_Rest.png`.

---

Qolossal, Batch 22b: correct the Batch 22 Qori rig parts so that, assembled, they match the approved Batch 19b look. The parts are well painted; the problem is how they fit together. Deliver to `Codex_v2_full\Batch22b` with `REVIEW_HANDOFF.md`, `MANIFEST.json`, an updated `Qori_v3_Registration.json` (same version 2 structure as Batch 22), the preservation and name-collision checks, and a source archive with prompts, as before. Keep the `Qori_v3_` names; this delivery supersedes Batch 22. Do not write under the live project.

**Template:** `Codex_v2_full\Batch19b\Concept_Qori_v3b_StaffIdle.png` (and the large figure in `Concept_Qori_v3b_Turnaround.png`). The assembled rest pose must line up with it, apart from the arm pose and the small angle turn asked for in Batch 22.

## Keep unchanged

- `Qori_v3_Torso`'s design (tunic, belt leaves, skirt-hem leaves, pouch and colours). Rescale it, and repaint it only as far as the angle turn below needs.
- `Qori_v3_Ear` painting, and its use for both ears.
- All nine `Qori_v3_Face_*` overlays, still registered on the head canvas. They scale with the head.
- The cape's leaf drawing (midrib, veins, hem lobes, outer face only).
- The registration JSON structure, the bone guides, PPU 100, 16 px padding, straight alpha and no halo.
- The existing Leaf Staff, read from the live project.

## Target proportions

H is the assembled rest height from the crest top (not ear tips) to the sole, **1200 px**. Every value is a fraction of H measured down from the crest top. Match each one within ±0.02H.

| Landmark | 19b target | Batch 22 now |
|---|---|---|
| Top of the cream face | 0.11 | about 0.06 |
| Chin (bottom of the cream face) | 0.30 | about 0.21 |
| Cream face height | 0.19 (about 225 px) | about 0.145 |
| Visible neck | **none**: the chin meets the mantle | about 0.05 of bare twig neck |
| Mantle top / lowest leaf tips | 0.30 / 0.45 | 0.24 / 0.40 |
| Shoulder joints (hidden under the mantle) | 0.41 | about 0.46, below the mantle |
| Belt centre | 0.50 | about 0.40 |
| Skirt hem tips | 0.69 | about 0.58 |
| Knee joint | 0.76 | about 0.74 |
| Ankle wrap centre | 0.88 | about 0.89 |
| Hanging arm: wrist wrap / fingertips | 0.58 / 0.64 (fingertips just above the skirt hem) | 0.61 / 0.74 (at the knee) |
| Skirt width at its widest | 0.24 | about 0.21 |
| Cape lowest hem lobe | 0.85 (mid-shin, above the ankle wraps) | about 1.0 (past the feet) |
| Cape width as worn at rest | about 0.50 | about 0.53 |

In plain terms:
- **Head:** scale the head piece, crest and face overlays up about 1.3x, so the cream face is about 225 px tall. The head is shown at about 180 px in game, so the upscale costs no visible detail. Repaint it at the new size if that is cleaner.
- **Neck:** close it. The chin sits down onto the mantle's top edge, and the neck stub stays hidden under the head and mantle.
- **Torso:** scale it up about 1.2x and register it lower, so the belt is at 0.50 and the skirt hem at 0.69. The thighs then show only from 0.69 to the knee, as in 19b.
- **Mantle:** it sits on the shoulders, from the chin (0.30) to its leaf tips (0.45). It must cover both shoulder joints (0.41), so the arms come out from under its edge, not from mid-torso.
- **Arms:** shorten the upper arm and forearm so a hanging hand's fingertips stop at 0.64. Repaint them shorter, at the same Batch 22 width and bark; do not squash them.
- **Cape:** keep the master width (about 720 px painted, gathered to about 0.50H at rest). Redraw the leaf shorter, so its lowest hem lobe ends at 0.85, with the side lobes about 0.80. That is about 660 px from the pivot to the lowest lobe, plus the hidden attachment zone. Redraw it rather than squashing it, so the veins keep their spacing. Its top attaches under the mantle at about 0.31–0.33 and must never show above the shoulders or behind the neck.

## Angle

Turn the torso and mantle to the same 3/4 angle as the head (Batch 22's head angle is good). The chest shows three-quarters, with the clasp off-centre toward the facing direction, as in `Qori_Idle_v1.png`. Repaint the mantle for this angle if needed; keep its leaves and clasp. The torso painting may be redone only for this angle, keeping its leaves, pouch and colours exactly.

## Joints

- Repaint the elbow and knee round caps with **no outline or contour line**, in the limb's own bark tone and grain, so an overlapping cap blends into the part above it. No visible ball joints, at rest or bent.
- Knees bend backward (the shin swings behind the thigh), never sideways.

## Colour

Match the crest, the mantle and the ear's green to the 19b olive. Batch 22's crest is a little yellower than the other greens.

## Proofs (composited by script from the parts and JSON, never repainted)

1. `Qori_v3_Proof_Rest`: the rest pose, relaxed hands, Neutral face.
2. `Qori_v3_Proof_Overlay`: the rest proof at 50% opacity over the 19b staff idle, scaled to the same crest-top-to-sole height. Add a table in REVIEW_HANDOFF.md of each landmark's measured fraction against the target above.
3. `Qori_v3_Proof_Stride`: a walking stride. Near thigh +25° and knee −30°; far thigh −20° and knee −10°; near elbow +45° and far elbow −20°; cape rotated 15° back; ears 10° back.
4. `Qori_v3_Proof_Bend`: both elbows at 90°, the head tilted 10°, Determined face and ears.
5. `Qori_v3_Proof_Staff`: the Grip forearms with the existing Leaf Staff.
6. `Qori_v3_Proof_ScaleCheck_1080`: the rest proof in the same archived Qvale capture as before, next to the current Qori.

## REVIEW_HANDOFF.md

List each part's new size and pivot, the landmark table and anything still off-target. Platform readiness is "not tested". Stop after delivery for Claude's review; nothing is imported until the user approves it.

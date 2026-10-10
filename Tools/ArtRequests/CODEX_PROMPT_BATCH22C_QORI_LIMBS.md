# Codex prompt: Batch 22c, Qori v3 continuous limbs

Written 8 Oct 2026 at the user's request ("write the 22c prompt and set up the test rig"), after `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_33.md` and `CLAUDE_REVIEW_34.md`. Not sent: the user sends it. A limbs-only correction pass on Batch 22b, not a new look. No import and no game changes. Fractions are of H (crest top to sole, 1200 px) and were read from the Batch 22b registration and landmark check; the bark colour was sampled on `Batch19b/Concept_Qori_v3b_StaffIdle.png`.

---

Qolossal, Batch 22c: replace Qori's separate upper-arm/forearm and thigh/shin pieces with one continuous painting per arm and per leg, built for mesh skinning in Unity. The rest of Batch 22b passed review and stays as it is. Deliver to `Codex_v2_full\Batch22c` with `REVIEW_HANDOFF.md`, `MANIFEST.json`, an updated `Qori_v3_Registration.json` (same version 2 structure as 22b, with the new limb entries and their bone guides), the preservation and name-collision checks, and a source archive with prompts, as before. Do not write under the live project.

**Why:** in 22b the elbow and knee overlap caps still show as separate outlined pieces (wooden-doll joints), after two rounds of repainting them. Rigid overlapping parts always leave a seam. A single painting per limb, bent by two or three bones inside one texture, has no caps and no seam.

## Keep unchanged (byte-identical to Batch 22b, re-delivered for completeness)

- `Qori_v3_Head`, all nine `Qori_v3_Face_*`, `Qori_v3_Ear`, `Qori_v3_Torso`, `Qori_v3_Cape`.
- Their registration entries: pivots, parents, positions, draw order and the cape and ear bone guides.
- Every landmark in the 22b landmark table. The new limbs must reproduce 22b's joint positions exactly (below).
- PPU 100, 16 px padding, straight alpha, no halo, no baked shadow.

## New paintings

1. **`Qori_v3_Arm`**: one continuous arm from the shoulder to just past the wrist wrap, hanging straight down, at the 22b arm width. The leaf wrap at the wrist is part of this painting. No hand.
   - Shoulder joint at 0.421H, elbow at about 0.506H, wrist-wrap centre at 0.561H (the 22b values).
   - The top end is rounded and plain, since the mantle hides it.
2. **`Qori_v3_Hand_Relaxed`**, **`Qori_v3_Hand_Grip`**, **`Qori_v3_Hand_Spread`**: the three 22b hands, cut just below the wrist wrap and slid up under it, so the wrap hides the join. Same pixels as the 22b forearm swaps below the wrap; the Grip hand must still close round the Leaf Staff shaft. Relaxed fingertips stay at 0.646H.
3. **`Qori_v3_Leg`**: one continuous leg from the hip to the ankle wrap, standing straight, with the ankle leaf wrap included.
   - Hip at about 0.663H (hidden under the skirt), knee at 0.761H, ankle-wrap centre at 0.900H.
   - Taper it like 19b: fuller at the thigh, slimmer toward the ankle. No even-width tube.
4. **`Qori_v3_Foot`**: the 22b root foot, cut above the sole and tucked up under the ankle wrap, so the wrap hides the join. Sole at 1.000H.

One painting each, used for the near and far side, as in 22b (the far side gets the registered darker tint).

## How to paint them for skinning

- **No joint shapes at all.** No ball, cap, ring, crease line, outline or change of shading at the elbow or knee. The bark grain runs straight through the bend zone, as on a young branch.
- Paint the outline only along the outer silhouette; nothing crosses the limb.
- Keep the width even across each bend zone (about one limb-width either side of the joint), so the inside of a bend compresses cleanly instead of pinching.
- Straight pose only: arm hanging, leg standing. Unity bends them.
- Add a little extra transparent margin (at least 32 px) along the long sides, so a bent mesh never clips the alpha.

## Bark colour

Match the 19b tan-brown, not the darker orange-red of 22b. Sampled mid-tone on the 19b staff idle legs: about RGB (186, 136, 99) / #BA8863, with darker grain. The 22b limbs averaged about (128, 76, 48), which reads too orange and too dark at game size. Hands and feet use the same bark.

## Other fixes from Reviews 33 and 34

- **Mantle:** remove the few clipped leaf fragments above the clasp at the top right of `Qori_v3_Mantle.png`. Keep everything else in the mantle, its size and its pivot.
- **Near ear:** no repaint. In Unity both ears now draw behind the head, so the crest covers the ear bases. Your proofs should use that draw order (`NearEar` and `FarEar` both below `Head`) and drop the repeated-crest-pixel patch.

## Registration JSON

- Replace the `NearUpperArm`/`NearForearm`/`FarUpperArm`/`FarForearm` and `NearThigh`/`NearShinFoot`/`FarThigh`/`FarShinFoot` entries with `NearArm`, `FarArm`, `NearHand`, `FarHand`, `NearLeg`, `FarLeg`, `NearFoot`, `FarFoot`.
- Arms and legs parent to `Torso` at the 22b shoulder and hip points. Hands parent to the arm's wrist bone; feet parent to the leg's ankle bone.
- For each arm and leg, give a **bone chain** in source pixels and local Unity units: shoulder, elbow, wrist (arm) and hip, knee, ankle (leg). Add an optional mid-thigh and mid-upper-arm point if a third bone helps.
- Add suggested **bone weights**: a short list of sample points along the limb's centre line with their weights per bone, so the bend zone is a smooth blend over about one limb-width.
- Keep the 22b draw order; arms and legs take the slots their two old pieces used, and hands and feet sit just above their limb.

## Proofs (composited by script from the parts and JSON, never repainted)

Bend the limbs in the proof script with simple two-bone linear-blend skinning on a mesh grid, using your own bone chain and weights, so the proofs show what Unity will show.

1. `Qori_v3_Proof_Rest`: rest pose, relaxed hands, Neutral face. Must match 22b's rest proof everywhere except the limbs' colour and taper.
2. `Qori_v3_Proof_Overlay`: the rest proof at 50% over the 19b staff idle, scaled to the same height, plus the landmark table in REVIEW_HANDOFF.md.
3. `Qori_v3_Proof_Stride`: a clear walking step. The character faces right. The near leg swings forward (its foot ahead of the body, to the right) and the far leg back, with the **feet clearly apart**, about 0.15H between the heels. Knees bend backward only (the shin swings behind the thigh). Near thigh forward 25° and knee 30°; far thigh back 20° and knee 10°; near elbow 45° and far elbow −20°; cape 15° back; ears 10° back. State the sign convention you used.
4. `Qori_v3_Proof_Bend`: both elbows at 90°, head tilted 10°, Determined face and ears. Plus one crop at 2x of an elbow and a knee at 90°, to show there is no cap or seam.
5. `Qori_v3_Proof_Staff`: the Grip hands with the existing Leaf Staff.
6. `Qori_v3_Proof_ScaleCheck_1080`: the rest proof in the same archived Qvale capture as before, next to the current Qori.

## REVIEW_HANDOFF.md

List each new part's size, pivot and bone chain, the landmark table, the sampled bark colour of the new limbs, and anything still off-target. Platform readiness is "not tested". Stop after delivery for Claude's review; nothing is imported until the user approves it.

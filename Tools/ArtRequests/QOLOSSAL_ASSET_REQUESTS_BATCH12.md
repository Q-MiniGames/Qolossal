# Qolossal: asset request for Codex, Batch 12 (DRAFT: not sent)

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

Work through the workstreams below in parallel (one agent per workstream plus the integrator), and stop only once, at the end, for **Review 23**.

## Notes
- **Folder name:** use `Batch12/` for this run's QA, handoffs and manifests.
- **Read first:**
  - `CLAUDE_REVIEW_22.md` (the Batch 11 verdict and the redo list);
  - `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2, sections 10–11);
  - `Concepts/Titan/Batch8_ChalkOrchard_Model_Reference.png` (the canonical seated pose and head);
  - the accepted `Cinematics/Spoilers/Reveal_Face_01_Brow` (the continuity source for W2).
- **Direction A, Morning Herbarium, for everything** (warm apricot dawn, olive survey ink).
- **The user's decision:** the towns in the distant valley stay in the quake vistas and the title art. Don't remove them.
- The art manifest holds **1090** accepted files. Re-read `Tools/ArtImport/codex_v2_accepted.json` rather than trusting this count.

## The rules (unchanged)
- Keep all accepted files byte-identical, and never write under `Assets/`.
- Use deterministic post-processing, and archive native sources and exact prompts.
- Fill in the Batch 6 self-review checklist per workstream.
- Run the **global name check** (case-insensitive, including sub-sprite names) before delivering.
- **The mystery rule:** nothing before the final level shows or names the titan's body. Spoilers are labelled in file names or QA sheets, never in production pixels.
- **No text baked into production images.**
- **The Leaf Staff** matches the accepted `Weapons/Qori_Weapon_LeafStaff`: a dark grainy shaft and one broad curved leaf blade, drawn at the sword's length relative to Qori (Review 19).
- **Qori** matches the accepted rig, including the v2 mantle and cloak.
- **The platform section in each handoff:** keep source checks, imported-resource notes and device tests (marked **not tested**) separate. Add 1280×720 readability proofs.

These are redos of unaccepted Batch 11 files, so reusing their names isn't a collision.

---

## W1: Terraces Far, Mid and Near, seamless wrap (production, P1)
Review 22 accepted `BG_Sky_Terraces` and found a repeat seam in the other three. `ParallaxLayer` tiles each layer endlessly, so its left and right edges must match.

| File | Measured left/right mean edge difference (Batch 11) | Target |
|---|---|---|
| `BG_Terraces_Far` | 15.2 | **0** |
| `BG_Terraces_Mid` | 54.2 (plus a 55–58-row skyline mismatch) | **0**, skyline height equal at x=0 and x=1919 |
| `BG_Terraces_Near` | 40.9 (plus a 55–58-row skyline mismatch) | **0**, skyline height equal at x=0 and x=1919 |

- **Make each layer wrap like the accepted `BG_A*` / `BG_Qvale_*` layers.** Every one of those measures exactly 0.
  - You may repaint a blended band near both edges.
  - Keep everything else in the Batch 11 paintings.
- **Far layer:** warm its ridges slightly toward the apricot sky. Its cool midday blue reads as pasted in front of a dawn sky.
- **Unchanged from Batch 11:** 1920×1080, 120 PPU, bottom-center pivot, transparent above each layer's own skyline, and clean alpha with no halo.
- `BG_Sky_Terraces` and `BG_Terraces_Mill_Landmark` are accepted; don't touch them.
- **Proofs:**
  - a 3× horizontal repeat of each layer, with the seam columns marked and the edge-difference numbers printed in the QA sheet;
  - the full four-layer stack at three parallax offsets, at 1080 and 720, with Qori at 1.4 u.

## W2: The reveal, 02 and 03 (production, P1, spoiler)
`Reveal_Face_01_Brow` is accepted. 02 and 03 lost the reveal's payoff.

- **Continuity is the point:** what Qori just walked past *is* the face.
  - The reed bed from 01 is the lashes.
  - The lake from 01 lies on the closed lid.
  - The waterfall shelves from 01 are on the brow.
  - Repaint 02 and 03 so the same lake, reeds and shelves sit on the face. No clean stone blocks, and no literal eyelashes.
- **`Reveal_Face_02_Pullout`:** a mid pull-out where the shelves and lake *start* to read as a brow and a closed eye.
- **`Reveal_Face_03_Whole`:** a clearly wider shot than 02.
  - Show the whole bowed head with clear sky around the two-pillar crown (the crown must not touch the top edge), plus the shoulder line.
  - Qori stays on the brow ledge from 01, not on a crown pillar. Qori may be tiny; give a small rim light or contrast so the eye can find Qori.
- The head matches the Chalk Orchard model: the two-pillar crown, the closed eye shutter, root cords.
- **Format:** 1920×1080, opaque, 100 PPU. Native full-frame paintings, not crops of 01.
- **Proofs:** 01 → 02 → 03 side by side, with the lake, reeds and shelves circled in each frame (QA sheet only).

## W3: Ending 03 Shoulder (production, P1, spoiler)
`Ending_Careful_01_HeadLift` and `_02_Lap` are accepted. Everything in 03 is right except the staff.

- **The staff is a pasted sticker:** it has a hard black outline and is about 1.6× Qori's height.
- **The fix:**
  - Scale it to the live rig's staff-to-body ratio (the sword's length).
  - Match the painting's edge softness and light: no hard outline, and warm dawn rim light on the shaft.
  - Keep the accepted blade shape and grain.
- Keep the all-over healed Wilted, the composition and the palette.
- **Format:** 1920×1080, opaque, 100 PPU.
- **Proofs:** a crop of Qori with the staff next to the live rig at the same scale.

---

## W4: The Chart, production [HOLD: waiting for the user's approval of round 2]
> **Integrator: skip this workstream** unless this banner has been removed. Claude will finalize it after the user decides.

Draft scope, if approved:
- Produce the seven region patches from round 2's production breakdown (`Batch11/W2`), as transparent UI sprites at 100 PPU, plus final-assembly registration.
- **Draw the play-time patches nearer their final scale.** Round 2 drew them at about half size, leaving most of the parchment empty.
- **During play, patch outlines must not hint at the final silhouette.** Label final-assembly frames DESIGNER SPOILERS (QA only).
- Keep A's paper, olive ink and the accepted `Chart_Frame_9Slice`.

---

## Delivery
- **The integrator:**
  - merges the manifests;
  - runs the preservation check against the live manifest and the global name check;
  - writes `REVIEW23_HANDOFF.md`, with one section per workstream, each with its filled checklist and platform section;
  - builds clean, labelled summary sheets.
- **One stop, at the end:** Review 23.

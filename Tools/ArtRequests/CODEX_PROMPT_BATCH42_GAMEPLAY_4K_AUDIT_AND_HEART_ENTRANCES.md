Qolossal: Codex Batch 42 onward. Bring every gameplay-plane asset up to the 4K density of 216 px per world unit, region by region, and design the five special heart-approach cave entrances. **This first run does three things in order: the audit and the entrance block-outs (Batch 42), then the first region's new paintings (MR01 Cradle, Batch 43). Then stop.** After that, stop for Claude's review after every region batch.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch42\ (then Batch43\, Batch44\, ... one per region, see Part 3)
Don't run Unity, don't open or save scenes, and don't write anywhere under the project. Other sessions share this checkout.
Preserve every earlier batch folder byte-identical; read them only.

---

## 0. Read first

1. `CLAUDE.md`, `Tools\Design\QOLOSSAL_PLATFORM_DIRECTION.md`.
2. `Tools\Design\QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` sections 2, 10 and 11: the mystery rules. Nothing may read as a body before the final reveal: no faces, ribs, knuckles, joints, veins, skin, organs or heart shapes, no symmetric left/right pairs.
3. `Tools\ArtRequests\CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md` sections 2 to 7: the 4K frame is 3840x2160 = 17.8 x 10 world units, so **216 px per world unit**.
4. `Tools\ArtImport\codex_v2_accepted.json`: the canonical accepted manifest (name, sha256, dest, ppu, pivot, wrap, border, sprites). Use the current file, not an old count.
5. `Assets\MountainReliefAtlas\Editor\MraBuilderArt.cs` (`FamilyOf`: which art each region uses), `MraWorldBuilder.cs`, `MraTownBuilder.cs`, `MraChamberBuilder.cs` and the terrain builder they call: how each sprite is placed and scaled.
6. The density and finish reference at 216 px/u: `Assets\Art\Codex\Underground\Cistern\`, `Hollow\` and `Shared\` (Batch 29). This is what "enough detail" means.
7. The style reference per region: its accepted 4K backgrounds (table in Part 3).
8. The underground design package, the source of truth for the entrances: `C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\DesignPackages\Underground_Master_v1_2026-10-17\` (`REVIEW_HANDOFF.md`, `data\ENTRANCES.json`, `ROOMS.json`, `GRAPH.json`, `STATE_CONTRACT.json`, `maps\MR03..MR07_REGION.png`).

## Hard rules (every batch)

- **New paintings only.** Every replacement is painted fresh at native 216 px/u. Never upscale an old asset, never paint over an upscaled one, never generate small and enlarge. The old asset may be used only as a layout guide on a separate guide layer that never reaches the output pixels.
- **Codex's output limit is about 1672x941.** Anything larger is painted as native 1:1 sections with at least 25% overlap and stitched, with seams blended in paint, not by resampling.
- **Everything stands on something. Nothing floats.** Every prop, house, shrine, step, ledge and entrance has its base on the ground line: opaque contact across its footprint, no transparent gap under it, overhangs hang from a mass that reaches the ground.
- **Drop-in geometry.** A replacement covers the same world-unit size, pivot, walk line, collider contact edge, slice borders, sprite-sheet rects and strip joint columns as the piece it replaces, all multiplied by 216 / old effective density. Repeat-U textures wrap with zero seam at every row.
- **Same design.** Keep the accepted piece's silhouette, composition and colour identity, and match the finish of that region's accepted 4K backgrounds and the Batch 29 underground art. It should look like the same object, seen sharper.
- **Mystery rules** (section 0.2) on every piece. Existing names such as `Pillar_Knuckle_Joint` stay as names only; paint rock.
- Names: `<OriginalName>_4K.png`. Never reuse or overwrite an accepted name.
- One drawn full-resolution alpha mask per piece, never generator alpha.

---

## Part 1 (Batch 42): the density audit

### Scope
- **Tier A, gameplay plane (target 216):** everything on or at the walk plane that Qori stands on, touches, enters or passes in front of: `Terrain\` (all kits, including `Terrain\Body\*`), `Town\` (homes, cutaways, front walls, interiors, town set pieces), `Props\` (including all `Prop_*Chamber_Entrance*`, shrines, rings), `Mechanics\`, `Hazards\`, `Decor\` sheets drawn on the gameplay plane, and `Underground\` (expected to pass; verify).
- **Tier B, report only (target 144):** decor drawn on parallax planes behind gameplay, and pieces never larger than 1.0 x 1.0 u on screen.
- **Out of scope:** characters, enemies, guardians, player, weapons, effects, UI, chart, cinematics, vistas and backgrounds (backgrounds have their own 4K batches).

### Measure effective density, not the import PPU
`effective px/u = PPU / s`, where `s` is the largest absolute world scale the sprite is ever drawn at (product of all parent scales; e.g. chamber entrances are placed at `EntranceScale = 0.74`, so a 120 PPU entrance is 162 px/u).
- Tiled sprites: density is PPU / s. Sliced sprites: also measure the stretched centre (`centre source px / centre world size`) and take the worst.
- Use the saved scenes in `Assets\MountainReliefAtlas\Scenes\` (read the YAML: `m_LocalScale` chains, `m_DrawMode`, `m_Size`) as truth, and the builder code to explain them. Take the worst case over all placements.
- If a value can't be resolved from files, mark it `UNRESOLVED` with the reason. Never guess.
- Verify every asset's sha256 against the manifest; report any mismatch and don't use that file.

### Verdicts
- Tier A: **PASS** at 210 px/u or more (216 within rounding), otherwise **SHORT**.
- Tier B: **PASS** at 144 or more, otherwise **SHORT-B** (reported, not scheduled).

### Region attribution
Use `MraBuilderArt.FamilyOf`: MR01 Cradle = `Terrain\Body\Body` kit + A0; MR02 Causeway = `Terrain\Body\Causeway` + A1; MR03 Terraces = `Terrain\Body\Terraces` + A5; MR04 Qvale = `Terrain\Body\Qvale` + Causeway caves + Town; MR05 Ribwood = A2; MR06 Heights = A3; MR07 Summit = A6; MR08 Descent = A4 (default). Confirm each against the scenes. A piece used in two or more regions is listed as **Shared** with every region that uses it, and the report proposes either one neutral replacement or one per region, for Claude to decide.

### Report (all in Batch42\AUDIT\)
- `DENSITY_AUDIT.csv`, one row per asset: name, dest path, sha256 ok, tier, regions, px size, PPU, placements, scale min/max, effective px/u (worst), on-screen size in u, verdict, target px size at 216, sections needed at 1672x941, notes.
- `DENSITY_AUDIT.md`: totals per region and tier; the SHORT list per region in painting priority order (walk surfaces, steps, corners, faces and fills first; then walls and caves; then houses and entrances; then props, mechanics, hazards; then decor); the Shared list with your proposal; every UNRESOLVED item.
- `DENSITY_AUDIT.html`: per region, each SHORT piece shown at in-game size inside a 3840x2160 frame next to an underground 216 px/u piece at the same scale, so the gap is visible.
- A proposed batch plan: one batch per region; if a region has more than 30 SHORT pieces, split it into part 1 (walk surfaces through houses and entrances) and part 2 (the rest).

---

## Part 2 (Batch 42): the five heart-approach entrances, block-outs only

These are the five surface entrances of the underground's heart approaches, from `ENTRANCES.json` (`ENT_A` to `ENT_E`). Each leads into its own three-room route (A1-A3 ... E1-E3) and then to one port of the shared Great Hollow. They must look special, unlike the 38 ordinary cave mouths, but **must not reveal what they lead to**.

| Entrance | Region, scene | Node, door feet (scene units) | What it is (design note) | Requires |
|---|---|---|---|---|
| ENT_A | MR04 Qvale, `MRAtlas_MR04_Qvale` | MR04_N06, (145, 8) | Qvale's public drain. A separate mouth from the atlas-stall doorway; keep the stall and keep 6 u clear of it. | none |
| ENT_B | MR03 Terraces, `MRAtlas_MR03_Terraces` | MR03_N09, (297, 140) | The east mill's service arch, after the Bloomfall cave. | living-thread |
| ENT_C | MR05 Ribwood, `MRAtlas_MR05_Ribwood` | MR05_N07, (253, 140) | A root cleft on the main descent from Seed Choir. Bark, not flesh; no parallel rib-like roots. | living-thread, bloomfall |
| ENT_D | MR06 Heights, `MRAtlas_MR06_Heights` | MR06_N09, (347, 232) | A sheltered ledge recess beyond the Glidecap lesson. Paint rock and wind; nothing that reads as a neck. | climbing-moss, wind-leaf, glidecap |
| ENT_E | MR07 Summit, `MRAtlas_MR07_Summit` | MR07_N11, (355, 148) | The **existing** MR08 Descent entrance, dressed. Not a second door and not a second finale. Sealed until the reveal. | mra:reveal-complete |

Geometry (all five):
- A back-plane mouth: the player walks in front of it on the walk line and presses to enter. Bottom-centre pivot at the door feet.
- Art 7.0 u wide x 6.0 u tall at 216 px/u = **1512 x 1296 px** (4 stitched sections), drawn at scale 1.
- The threshold sill is flush with the walk line at the feet; opaque ground contact across the full 6 u entrance pad `(feet.x - 3 to feet.x + 3)`.
- The dark opening is at least 2.0 u wide and tall enough for 1.8 x Qori's standing height (measure Qori from the accepted `Player\` sprites at their PPU; state the number). The door trigger is 1.2 x 1.6 u centred at feet + (0, 0.8) and must sit wholly inside the opening.
- Each mouth is built from its own region's material and shape language (the region's 4K backgrounds and terrain kit), so it belongs there.
- **Family resemblance that only makes sense in hindsight:** all five share the same weathered carved keystone mark (a simple ring with one notch; not a heart, eye or face) and the same faint warm-amber light breathing from deep inside. Nothing else repeats between them.
- Deliverable pieces for later: `Entrance_Heart<X>_Open`, a separate additive `Entrance_Heart<X>_Glow` (so the game can pulse it), and for E also `Entrance_HeartE_Sealed`.

Batch 42 delivers for each entrance, in `Batch42\ENTRANCES\`:
- A greyscale value block-out at 25% (378 x 324) with overlays: pivot, walk line, 6 u pad, door trigger, opening size, Qori silhouette to scale, the 1672x941 section grid.
- The block-out placed in a 3840x2160 frame of its region at the feet position, over that region's accepted 4K background stack where one exists (MR03 Terraces4K, MR04 Qvale4K) and over a plain region-palette backdrop otherwise.
- A short note per entrance: material, how it differs from the ordinary cave mouth in that region, and the mystery check.
- For E: the sealed and open block-outs side by side, with the existing MR08 entrance's position and size from the scene shown for comparison.

Write `Batch42\REVIEW_HANDOFF.md` (audit totals, Shared decisions needed, entrance sheets, everything "not tested in game"). **Then continue straight into Part 3 for MR01 Cradle only (Batch 43).** For Shared pieces in that batch, paint only those used solely in MR01 or whose audit proposal is "one neutral replacement"; leave the rest for Claude's decision. Stop after Batch 43.

---

## Part 3 (Batch 43 onward): one region per batch, with a stop after each

Order, and each region's style reference:

| Order | Region | 4K style reference | Entrance painted in this batch |
|---|---|---|---|
| 1 | MR01 Cradle | `Assets\Art\Codex\Backgrounds\Cradle4K\` | none |
| 2 | MR02 Causeway | `Backgrounds\Causeway4K\` | none |
| 3 | MR03 Terraces | `Backgrounds\Terraces4K\` | ENT_B |
| 4 | MR04 Qvale | `Backgrounds\Qvale4K\` | ENT_A |
| 5 | MR05 Ribwood | Batch 37c, once accepted | ENT_C |
| 6 | MR06 Heights | Batch 38, once accepted | ENT_D |
| 7 | MR07 Summit | Batch 39, once accepted | ENT_E |
| 8 | MR08 Descent | Batch 40, once accepted | none |

- Each region batch takes the next free batch number at the time it starts. Check `Codex_v2_full\` and `Tools\ArtRequests\` on disk first. A split region uses `b` for its second part.
- **Don't start a region whose 4K backgrounds aren't accepted yet.** Stop and say so.
- Paint only that region's SHORT list (plus any Shared pieces Claude assigned to it at review), in the audit's priority order, and that region's entrance from its block-out (after Claude has reviewed the Batch 42 block-outs).
- MR01 Cradle runs in the first run as Batch 43, straight after the audit. If MR01 has more than 30 SHORT pieces, deliver part 1 as Batch 43 and stop.

### Automatic self-checks (each piece must pass before delivery; record the numbers)
1. Size: exactly the target px size; PPU 216; pivot, borders, slices and joint columns scaled from the original.
2. Geometry: the alpha mask matches the original's mask scaled to the new size (nearest) at IoU of 0.95 or more for terrain and walk pieces, 0.85 or more for props, town and entrances. The walk line and collider top edge are within 2 px.
3. Grounding: opaque contact along the bottom footprint matches the original's contact profile; no transparent row between the object and its base; nothing floating, in a flood-fill of every opaque island (each island touches the base or a mass that does).
4. Not upscaled: downsample by 120/216 and back up; if the result is nearly identical to the piece (PSNR above 38 dB), it has no real 216 detail. Fail.
5. Detail: median Laplacian variance per 216x216 tile is at least 70% of the Batch 29 underground reference's.
6. Wrap: for Repeat-U pieces, seam difference 0 at every row; for a set (steps, strips, corners), every join between pieces that meet in game is checked.
7. Mystery: viewed at 100%, 10% and blurred, no face, limb, joint, rib, vein, skin or heart read.
8. Hashes: every accepted source and earlier batch unchanged (preservation receipt).

If a piece fails after a fair second attempt, leave it out, list it with its numbers, and carry on with the rest.

### Proofs per region batch
- Old vs new for every piece at in-game size inside a 3840x2160 frame, and a 100% crop of each.
- A full mock frame per scene area: the new terrain set assembled as the builder assembles it (steps, strips, corners and fill joined) over the region's 4K background stack, with Qori to scale.
- 3x repeats with seam numbers; alpha over black, white and magenta; grounding overlays.
- The entrance at its feet position in the region frame, with the trigger and pad overlay, and its Glow over the Open piece.

### Deliverables per region batch
The new PNGs, `MANIFEST.json` (name, sha256, px, PPU 216, pivot, wrap, border, slices, world size, and the accepted name and sha256 it replaces), `QA_INDEX.html`, `QA_SHEET.md`, `sources\`, the preservation receipt, the name-check log and `REVIEW_HANDOFF.md` (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review.

**STOP after each region batch** for Claude's review. Don't start the next region until told.

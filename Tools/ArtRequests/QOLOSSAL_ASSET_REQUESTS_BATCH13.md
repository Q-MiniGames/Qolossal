# Qolossal: asset request for Codex, Batch 13

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

**Why this batch:** the Mountain Relief Atlas level polish (`Assets/MountainReliefAtlas/LEVEL_POLISH_HANDOFF.md`, round 2) now draws every wall procedurally. Each wall gets a band of the kit's `Cliff_Face` with an irregular contour and a dark rim, a moss lip over the top, and hanging plants from the decor sheets. That's better than the straight cut, but the limestone kits (Body, Causeway, Terraces) have no painted cliff side, corner or foot pieces, so the silhouettes are still plainer than painted rock. The legacy kits have `Wall_Side` and corner art; the Body-surface kits don't. Two more gaps came from the round: the musician's loft has no exterior or interior art, and only one cave-mouth style exists.

Work through the workstreams in parallel (one agent per workstream plus the integrator), and stop once, at the end, for **Review 24**.

## Notes
- **Folder name:** use `Batch13/` for QA, handoffs and manifests.
- **Read first:**
  - `CLAUDE_REVIEW_23.md`;
  - `Assets/MountainReliefAtlas/LEVEL_POLISH_HANDOFF.md`;
  - the redesign proposal v2, sections 10–11;
  - the accepted kit files named under each workstream.
- **Direction A, Morning Herbarium.** Re-read `Tools/ArtImport/codex_v2_accepted.json` for the live accepted count.

**Screenshots (in-engine, current state):**
- `Tools/MountainReliefAtlas/QA/LevelPolish/round2/` holds the cliffs before and after, the overlook, and the house fronts.
- The cliff close-ups to beat are `MR02_edge_MR02_E06_1` and `MR03_edge_MR03_E05_1`.

## The rules (unchanged)
- Keep all accepted files byte-identical, and never write under `Assets/`.
- Use deterministic post-processing, and archive native sources and exact prompts.
- Fill in the Batch 6 checklist per workstream, and run the global name check, including sub-sprite names.
- No baked text. The mystery rule applies: nothing reads as a body before the final level.
- The platform section in each handoff keeps source checks, imported-resource notes and device tests (**not tested**) separate.

---

## W1: limestone cliff sides, corners and feet (production, P1)
For each of the three kits, `<Kit>` = `Body`, `Causeway`, `Terraces`, matching that kit's accepted `_Fill`, `_Top_Strip` and `_Cliff_Face` in material, scale (120 px/u), stone size and light (top-left).

| File | Canvas | Content and registration |
|---|---|---|
| `<Kit>_Cliff_Side_L` | 512 × 1024 RGBA | The open face of a wall whose rock lies to the **right**. Irregular limestone silhouette on the left (cracks, small ledges, weathered corners); opaque to the right edge. **Tiles vertically** (rows 0 and 1023 identical; Repeat V). The collision line is at **x = 64 px**: the contour may reach 0–54 px left of it (0–0.45 u) and recede up to 24 px right of it, averaging about 20 px outside. From x = 448 px rightwards the paint blends seamlessly into `<Kit>_Fill` at world scale. Pivot (64/512, 0.5). |
| `<Kit>_Cliff_Side_R` | 512 × 1024 | The mirror use: rock to the **left**, collision line at x = 448 px. Paint it fresh, not mirrored, so the light stays top-left. |
| `<Kit>_Corner_Top_L` / `_R` | 512 × 512 | The outer top corner where `<Kit>_Top_Strip` turns down into the cliff side: the moss or grass lip curling over the edge, roots and trailing plants, a rounded broken stone shoulder. The walk line is at **96 px from the top** (as in `_Top_Strip`). The collision corner is at (64, 96) for L and (448, 96) for R. It must join `_Top_Strip` on its rock side and `_Cliff_Side` below with no seam. |
| `<Kit>_Corner_Foot_L` / `_R` | 512 × 384 | Where a wall meets the lower tread: a talus of fallen stone and grass tucking into the lower `_Top_Strip`. Its top joins `_Cliff_Side`; its bottom row sits **0.35 u below** the lower walk line. Collision line at x = 64 / 448. |
| `<Kit>_Cliff_Ledge_A` / `_B` | 384 × 192 | Two decorative (non-walkable) rock outcrops that sit on a cliff side, protruding 0.3–0.6 u. Pivot at the wall line, middle. |

- **Assembly proof per kit:** walls of 1.2 u (a step), 2.7 u (a ledge) and 5.6 u (a climb), each in L and R, assembled from Top_Strip, Corner_Top, Cliff_Side (tiled), Corner_Foot and Fill. Show them with Qori standing at the foot and on top, at 1920×1080 and 1280×720 (camera 10 u tall, Qori 1.4 u).
- **Seams:** state the measured vertical seam of each `_Cliff_Side` (top vs bottom rows, mean 0), and the join error of the corner pieces against `_Top_Strip`.
- **Scope:** no change to any accepted file.

## W2: the musician's loft, exterior and interior (production, P1)
Qvale's homes are now enterable (one interior scene each). The loft is the only one without its own paintings: it currently uses a stand-in cave mouth outside and plain cave rock inside.

| File | Canvas | Content |
|---|---|---|
| `Town_Qvale_Loft_Closed` | 2136 × 1202 RGBA | A small two-storey rock-and-timber loft beside the listening tree: a door at ground level, a lantern, instruments in the window. Same family as `Town_Qvale_HomeA_Closed`. **Pivot at the door threshold, (0.07865, 0.0316)**, as the other homes; door centre about 2.0 u right of the pivot. |
| `Town_Qvale_Loft_Cutaway` | 2136 × 1202 RGBA | The interior on the same registration: the ground floor with the musician's corner, and **a built-in wooden stair** rising in three 1.2 u steps to a loft floor 3.6 u up, where a song shell rests. The step heights must match the in-game stair (the risers stand about 5.4, 10.4 and 14.4 u right of the door, each 1.2 u high, and the top floor is 3 u deep): mark the walk lines in the registration. |

- **Proof:** the exterior on the street beside the accepted listening tree and bench, and the interior with Qori at the door, on the stair and on the loft floor.

## W3: cave mouths per legacy kit (production, P2)
`Prop_Chamber_Entrance_WallCave` (pale limestone) is now tinted in the bark, sandstone, slate and deep-green regions; a mouth per kit would integrate properly.

- **Files:**
  - `Prop_Chamber_Entrance_A2` (bark and roots);
  - `Prop_Chamber_Entrance_A3` (sandstone);
  - `Prop_Chamber_Entrance_A4` (deep green, roots);
  - `Prop_Chamber_Entrance_A6` (slate).
- **Registration:** each at **720 × 600, 120 PPU, pivot bottom-centre (0.5, 0)**, matching the WallCave's registration.
- **The opening:** at least **1.6 u tall and 1.2 u wide at the in-game scale of 0.74**, with its base flat on the bottom row (no transparent padding under the base: the builder grounds by the lowest painted row).
- **Fit:** the right third should read as rock running into a wall, because most mouths are set against a rising cliff.

## W4: input glyphs (production, P2)
The prompts now draw text key caps ("W", "D-pad Up").

- **Files:**
  - `UI_Glyph_Key_W`, `UI_Glyph_Key_E`, `UI_Glyph_Key_Esc`;
  - `UI_Glyph_Pad_DpadUp`, `UI_Glyph_Pad_South`, `UI_Glyph_Pad_East`, in a neutral style that suits every pad;
  - per-platform variants only where a platform holder's guidelines require them; note which, and keep the trademark rules in the handoff.
- **Format:** 96 × 96 RGBA, UI 100 PPU, centre pivot.
- **Readability:** they must read on the dark prompt plate at 1280×720.

---

## Delivery
- **The integrator:**
  - merges the manifests;
  - runs the preservation and global name checks;
  - writes `REVIEW24_HANDOFF.md`, with each workstream's checklist and platform section;
  - builds labelled summary sheets.
- **One stop, at the end:** Review 24.

# Codex prompt: Batch 20, drift stones and the reclining route's new places

Written 8 Oct 2026 for the reclining titan route. Not sent: the user sends it. Names checked free in the accepted manifest (`Tools/ArtImport/codex_v2_accepted.json`) on 8 Oct 2026. Until this lands, the four floating-platform levels use each region kit's accepted floating and moving platforms, the accepted crumbling slab, and the accepted `Mechanics/Breath_Vent_Exhale` and `FX_Breath_Current` as the launch point (see `Tools/MountainReliefAtlas/QA/Reclining/drift_sheet.jpg`).

---

Qolossal, Batch 20: game art for the floating-platform levels and two new cave mouths. Deliver to `Codex_v2_full\Batch20` with a `MANIFEST.json` (path and sha256 per file, plus pixels-per-unit and pivot per file), as in earlier batches. Transparent PNGs, 120 px per unit, pivot at bottom centre unless noted. Approved Chalk Orchard look: limestone caps, root-tendon seams, olive meadow moss, warm dawn light from the upper left (match `BG_Sky_Terraces`). The world must read only as strange landscape: no faces, no anatomy, no text.

1. **Drift stones** (slabs of rock floating on warm rising air), a kit that tiles to any width:
   - `Drift_Stone_S` (2.5 u wide), `Drift_Stone_M` (4 u), `Drift_Stone_L` (7 u). A flat walkable top (mossy limestone cap) on the top 0.2 u, a tapered rocky underside with a few hanging root tendrils, about 1.6 u deep in all. Pivot: the centre of the walkable top edge.
   - `Drift_Stone_Moving` (3 u wide): the same stone, slightly paler underside with a faint warm haze beneath it, so a player reads it as the one that moves.
   - `Drift_Stone_Crumble` (3 u wide) and `Drift_Stone_Crumble_Pieces` (a sheet of 6 broken fragments of it, on a 6 x 1 grid): cracked cap, loose pebbles on top.
2. **The launch point** `Drift_Launch_Vent` (about 2.5 u wide, 2 u tall): a low ring of limestone and roots on the ground with a shimmering column of warm air rising from it (the column as a separate `Drift_Launch_Air`, 2 u wide, 8 u tall, soft and semi-transparent, pivot bottom centre). It must look like a place to step into, not a cave.
3. **Two cave mouths**, in the style of `Prop_Chamber_Entrance_WallCave`:
   - `Entrance_UnderArch`: a very large, low, shadowed arch under a rounded rock mass, 9 u wide, 5 u tall, roots hanging from its lip.
   - `Entrance_NavelWell`: a round, deep, mossy shaft opening set into gently sloping ground, 3 u wide, with a stone rim.
4. **Route bar and Chart icon** `Map_Icon_Drift` (64 x 64, matching the existing `Map_Icon_*` set): a small floating slab with three rising air lines.

Proofs: each file on a neutral grey and on a dawn sky, plus one composite showing three drift stones, one moving and one crumbling, at gameplay scale next to a 1.2 u tall character silhouette.

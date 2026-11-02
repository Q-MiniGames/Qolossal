Qolossal: Codex Batch 43b. Finish the audit's terrain rows, paint the Cradle (MR01) terrain kit at 4K density, and make colour concepts for the five heart entrances. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch43b\
Preserve Batch42 and Batch43 byte-identical; read them only. Don't run Unity, open or save scenes, or write under the project.

READ FIRST
1. `Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_95.md`: the verdicts and decisions this batch acts on. It overrides Batch 42/43 where they differ.
2. `Tools\ArtRequests\CODEX_PROMPT_BATCH42_GAMEPLAY_4K_AUDIT_AND_HEART_ENTRANCES.md`: the hard rules, self-checks and deliverables still apply.
3. `Assets\MountainReliefAtlas\Scripts\MraTerrain.cs`: lines 124-134 (fill), 283-356 (top strip), 397-830 (steps), 894-1007 (cliff pieces, under).

## 1. Audit addendum (Batch43b\AUDIT_ADDENDUM\)
- Reclassify every MraTerrain-referenced piece using Review 95: confirm unit transform scale, then SHORT at 120 px/u (note any strip shear above 3%).
- Reclassify pieces with no reference in any scene, prefab, asset or builder code path as **UNUSED** (list the search done for each).
- Re-issue the per-region totals and SHORT lists in painting priority order. Whatever is still UNRESOLVED gets a one-line reason.

## 2. Cradle terrain kit (MR01)
**MraTerrain pieces: exactly 2.000x the original pixel size, PPU 240.** Every landmark lands at exactly 2x: walk line, kerb, moss, riser column (256 to 512), upper px (160 to 320), step column (32 to 64), strip joint columns, course rows, riser tables. Record each landmark old/new in the manifest. Other pieces: ceil to 216 as before.

Paint, in this order:
1. **Body_Fill_4K (2048x2048), a new painting.** Natural weathered pale limestone like the accepted Body_Fill: irregular rounded slabs, fine cracks, moss and lichen flecks, the same cream value and hue. No horizontal courses, no masonry or mortar, no visible section boundaries. Seamless in U and V.
2. Body_Top_Strip, Body_Fill_Shadow, Body_Cliff_Face, Body_Cliff_Side_L/R, Body_Cliff_Ledge_A/B, Body_Corner_Foot_L/R, Body_Corner_Top_L/R, Body_Pale_Cliff, and the `Body_Step_*` set at their accepted versions (`_v2`/`_v3`, as in the manifest).
3. The Cradle versions of Cave_Fill, Cave_Edge_Strip, Prop_Chamber_Entrance_WallCave and the six Body creases (shared pieces; Review 95 decided one per region).
4. Retry Pillar_2_Middle and Decor_A0_Sheet.

If more than 30 pieces remain after the reclassification, deliver items 1 and 2 only and list the rest.

Extra gates for this batch:
- **Assembly proof:** rebuild the MraTerrain meshes offline from the saved scene data for at least three Cradle stretches (a flat run, a step flight with Outer/Inner/Wall pieces, and a cliff with corners and ledges), old set vs new set, side by side in 3840x2160 frames over the Cradle4K stack with Qori to scale. Joins must match and the walk line must be identical.
- Old vs new crops at 100% for every piece: same design, sharper. A piece that changes material or composition fails (as Body_Fill did).
- Steps: overlay the old landmarks at 2x on the new piece; every one within 1 px.

## 3. Heart entrance colour concepts (Batch43b\ENTRANCES\)
Keep the Batch 42 geometry (7x6 u, 6 u pad, trigger inside an opening at least 2.39 u wide and 3.2 u tall). Repaint each concept at 25% (378x324), in colour and in its region's palette, with **a distinct silhouette for each**:
- **ENT_A Qvale:** a low, wide civic culvert mouth under a warm rock eave, with an inset sluice channel and worn steps going down. **Re-anchor it:** survey the Qvale scene and pick the nearest spot on the same walk level where the whole 6 u pad is clear of house art, lamps and triggers, and at least 6 u from the atlas-stall door. Show the old and new anchors on the frame.
- **ENT_B Terraces:** a masonry service arch beside the east mill, with timber framing and the mill race running in.
- **ENT_C Ribwood:** a cleft between two or three massive furrowed roots, of different girths and angles (never parallel; bark, not flesh).
- **ENT_D Heights:** a recess under a wind-sheltered rock overhang, a cairn, and one wind-bent pine.
- **ENT_E Summit:** dressing around the **existing** Descent arch, in place at (362.4, 149.437), 3.27x3.9 u. Sealed and Open states.
- All five: the opening is arched or irregular, never a rectangle; the one notched-ring keystone mark; the faint warm-amber light from deep inside. Mystery check at full, 10% and blurred.
- Place each concept in its 4K region frame, as in Batch 42. These are concepts only; the final paintings come in each region's batch after review.

DELIVERABLES
New PNGs, MANIFEST.json (with the 2x landmark table), QA_INDEX.html, QA_SHEET.md, sources\, preservation receipt (Batch42 and Batch43 unchanged), name-check log, REVIEW_HANDOFF.md (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review. **Stop after Batch 43b.**

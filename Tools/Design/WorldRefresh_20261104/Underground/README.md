# Underground world refresh â€” 4 November 2026

The 89-room network keeps its approved physical adjacency, room bounds, 63 passages, all portal/save/checkpoint identities, challenge routes and mechanics. The relocated MR06_C01/PC11/PC12 remain at their actual scene positions. The five Qvale houses are excluded.

## Twelve interior themes

| Set | Theme | Titan / terrain relationship |
|---|---|---|
| A | Sluice Cisterns | Old channels through deep stone folds; teal pools, spillways and small arch bridges |
| C | Root Gardens | Woven roots in sheltered internal vaults, fern gardens, seed pods and narrow streams |
| D | Wind Hollows | Blue-grey vaulted clefts, hanging nests and suspended fog |
| E | Memory Sanctuary | Lilac recesses, worn shrine details and quiet reflected light |
| H | Great Heart Hall | Amber and garnet fossil vaults framing the existing four-sector Great Heart |
| MR01 | Moss Hollows | Sheltered foot/ankle recesses, fern pockets and small watercourses |
| MR02 | Resonant Underarches | Shin stone seams, grey/brass masonry and resonant arches |
| MR03 | Sluice Undercrofts | Knee/thigh hollows with ochre channels, old mill details and warm pools |
| MR05 | Hidden Rib Gardens | Belly/rib folds containing deep olive root gardens |
| MR06 | Sheltered Wind Clefts | Collarbone/shoulder fissures, pale violet windows, nests and distant falls |
| MR07 | Pale Echo Chambers | Face stone recesses, pearl limestone, cairns and silver pools |
| P | Secret Niches | Small moss-lined echo alcoves that remain visually neutral across connected regions |

## Resolution and rendering

All twelve new vista paintings are native 1774 Ã— 887. None is enlarged or resampled. At the closest underground orthographic size 5, 2160 vertical output pixels cover 10 world units, requiring 216 source pixels per unit. Each scenic painting repeats horizontally at **8.212963 Ã— 4.106481 units**, in a camera-height band that fades into native tiled rock above/below. Horizontal mirroring softens seams; the scenery never repeats vertically as stacked rivers or buildings. Existing rock backdrops also sample at 216 PPU. The near layer's alpha is 0.22 to retain distant scene readability.

The new Heart detail texture is native 1254 Ã— 1254 and tiles at **5.805556 Ã— 5.805556 units**. The four original quarter renderers, position/size, sorting, flip orientation and awakening references remain. A screen-antialiased analytical quarter ellipse replaces the original bitmap edge; fresh detail fills it at 216 PPU. Two texture samples, one rotated orthonormally by 37 degrees, soften recognizable mirror ornaments without enlarging either sample. This is a deliberate minor silhouette simplification to an exact ellipse. Original Quarterlight PNG bytes are retained. SpriteRenderer color and alpha still control sleeping, waking and pulse states.

`native_assets.json` records source files, exact dimensions, SHA256, world footprint and complete generation prompts. These are native density calculations, not a claim that all platforms have passed hardware testing. Smaller camera sizes would require a revised density budget.

## Terrain and clearance scope

Only ordinary static `UGP solid` and `UGP rock` boxes at least 1 Ã— 0.6 units receive a 0.06-unit corner radius. Their box size decreases by twice the radius, preserving the original outer envelope and walk height. HEART, triggers, one-way platforms, movers, attached rigidbodies, gates and mechanic colliders remain unchanged. No jump span or room slope is flattened. `room_manifest.json` preserves the puzzle intent for each room and lists eligible original shapes.

The applicator checks actual collision after applying: a 0.70 Ã— 1.10 body overlap, three downward ground-support rays and trigger-hazard overlap at each active arrival, checkpoint and registered CaveConnectionPoint arrival. This is an entrance screening test, not a full movement playthrough. Failures need inspection against actual player/body conventions before moving a registered entrance. Surface entrances remain registered and coordinated with the surface terrain pass.

## Application and verification

Root owns serialized Unity operations. Run `WorldRefreshUnderground.Apply(true)` in edit mode with saved scenes. It saves a before-copy, imports native artwork, applies all room art and ordinary corners, saves the underground scene, and writes `application.json` plus `audit.json`. `WorldRefreshUnderground.Audit()` reruns collision screening while the underground scene is active. The builder has hooks to preserve these treatments on future rebuilds, but this refresh does not run a full puzzle rebuild.

Runtime components: `WorldRefreshCaveVista`, `WorldRefreshHeartSurface` and `WorldRefreshCaveRockField`, with shaders under `Assets/Resources/FX`. All 253 far/near/passage backdrops now use one Simple quad each, preserving the exact outside envelope. World-space shader sampling supplies 216 PPU independently of quad scale. The prior gallery-wide P backdrop required 155,472 tiled vertices at 2238 × 773 units, causing three runtime mesh errors; `UpgradeBackdropMeshes(true)` removes that failure at its source, and builder hooks preserve the fix. `CaveDepth` skips the upgraded scenic material and supplies camera position plus `_CaveHalfHeight`; its original near/passage behavior remains. Density auditing must measure these shader world tiles, not the renderer's full-room bounding box. Map extraction should include the collider edge radius when deriving its outside envelope.

Baseline screening found eleven faults across eight rooms: D04 edge support, E05 obsolete low checkpoint, HEART generic spawn in the unreachable central bay, two lower-shaft checkpoints and three C02 arrivals clipping the first block. `RepairLandings(true)` validates explicit safe anchors against current collision before moving only these arrival/checkpoint transforms, preserving IDs and portal positions. It writes `landing_repairs.json`. The updated audit counts current embedded CaveConnectionPoint links, not retired MraChamberReturn components.

## Executed verification

Root applied and saved the refresh: **89 rooms**, **93 ordinary corners** rounded, **629 terrain pieces** redressed. The corrected actual-collider audit reports **89 rooms, 37 distinct cave pairs, zero landing failures and zero greybox warnings**. Eleven initial marker faults across eight rooms were repaired. A subsequent runtime pass exposed gust-trigger respawn loops and nearby direct enemy pushes: seven additional checkpoints and two generic arrivals were moved to existing safe floor/shelter anchors through `RepairLiveLandings(true)`, with IDs preserved. The updated static audit includes MraUgGust triggers. Evidence is kept separately in `live_landing_repairs.json`, `Before/live_landing_repairs_v1.json`, `Before/settle_initial.json` and `Before/settle_second_run.json`. The final generic Heart spawn/checkpoint sits at `(1774, 182.66)`, clear of the awakening root.

All **15 final 3840 × 2160 captures** were visually inspected: one play-scale representative for every theme, plus Heart asleep/awake overviews and native detail. The initial bright band and incorrectly mirrored Heart quarters were caught and corrected; final views have upright scenery, one joined Heart silhouette, quiet overview framing and sharp surface detail. `visual_qa.json` records final capture hashes and observations. Root reported the shader-aware cross-project density audit passing with zero native/import failures; see root's canonical density report for its broader scope.

Artistic limits remain visible: the scenery is a modular system of repeated cavern openings, most noticeable in E05 and MR06, and MR02_C06 retains a comparatively plain stucco-like lower wall treatment. These should not be described as 89 unique panoramic paintings. The edited play-scale frames preserve readable terrain, thorns, wind and shrine silhouettes.

The isolated runtime test `../underground_settle_test.cs` checks all 89 checkpoints and 37 cave arrivals after 0.8 simulation seconds each, with 4.4 seconds for each of the seven newly repaired checkpoints to cover a complete gust cycle. It preserves normal saves. The first full run found six unsafe checkpoints and the oversized backdrop error; gust-aware review found one more unsafe checkpoint that had been tested during its calm phase. The second full run passed 125 of 126 placements with no runtime errors. A04 alone still respawned because its actual PumpBloaterEnemy jet pushed the player toward water; the reused GustMoth blast sound name does not identify the actor. Its checkpoint and generic arrival are now at (1564, 84.66), ten units left of the enemy home and beyond its eight-unit detection and nine-unit jet ranges. IDs and the enemy puzzle are preserved. The final full rerun passed all 126 placements with zero errors, preserving normal saves and Editor settings. The seven repaired checkpoints each remained under observation for 4.4 simulation seconds. See `settle_result.json` for the final receipt. This test does not claim full cave transitions, puzzle traversal, console hardware performance or console readiness.

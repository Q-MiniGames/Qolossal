# Surface cave restoration and terrain joins

The previous refresh failed visual review. Its silhouette conversion preserved the outline of approved cave mouths but replaced their painted ceiling undersides and inward-facing facets with repeating terrain Fill. That removed the depth described in the October28 review and reproduced an already-rejected flat-cutout appearance. Later clipping of copied roof rectangles addressed symptoms without restoring those paintings.

`WorldRevisionSurfaceCaves.RestoreCurrent()` restores the currently active fitted cave assemblies from their retained `CaveRollout_20261027/Upper_<ID>` mesh and material assets. It does not re-enable abandoned entrance prototype groups. The exact original rim image, alpha, crop/mirror registration, inward tint, regional inner-wall painting and passage shadow return. The separate accepted Cradle assembly is left intact where no fitted replacement exists. Portals, arrivals, terrain arrays and collider serialization remain unchanged.

The new attachment shader preserves the original approved painted-rim RGB calculation. Only the outer attachment samples the new native terrain Fill through world-position coordinates. Its final0.45units blend into the surrounding terrain beyond the carved aperture. Detached copied roof decorations are disabled; the live terrain owns its continuous walking cap. Native walking-strip mapping remains.

Original assets are immutable. Every original mesh, material and sampled image is hashed before use and rechecked before saving. Restored meshes/materials are separate siblings. Per-region receipts include previous references, restored references, actual source dimensions and per-triangle density audits for both approved painted RGB layers. A source-density exception remains an explicit exception; the helper never enlarges a bitmap or replaces an approved painting to manufacture a passing number.

`RoundUpperCornersCurrent(.18f)` is a separate small terrain change for root's visual review. It replaces exposed convex right-angle upper turns with six-sample quadratic arcs of radius0.18world units. It retains macro endpoints, concave floor joins and carved aperture polygons. Near a cave, the radius is allowed only if at least0.08units of solid roof remain above the aperture after the maximum cut. Existing accepted Cradle kits are not reshaped. Unlike the art restoration, this helper rebuilds the actual collision profile and requires scene-map regeneration plus traversal checks.

Required review: compare approved and restored cave frames, confirm readable dark recesses, inspect ground/wall/roof continuity at actual camera scale, resolve any native-source density exceptions honestly, and verify every affected entrance after parent-owned relocations. Prior refresh QA is not acceptance of this revision.

The separate `WorldRevisionSurfaceJoins` pass addresses remaining native-strip joins. `RoundSlopedCornersCurrent(.18f)` extends the bounded convex bevel to vertical-to-slope and slope-to-vertical turns (adjacent slope magnitude at most 1), and writes a separate receipt rather than overwriting the first corner pass. `BlendWalkingEndsCurrent()` retains the cave-floor mesh, native UV and vertex colors, while a dedicated material fades the last 0.3 units of each overlaid strip into the terrain/painted wall behind it. Run this after entrance relocation because its material bounds are world coordinates.

`EnableNativeTopJoinsCurrent()` sets the default-off `MraTerrain.nativeStripEndBlend` only on new-art cave hosts and terrain named in reviewed corner receipts. The upper painting now fades over 0.25 units at the exposed ends of continuous shallow top runs. It recognizes steep bevels as cliff transitions, keeps concave floor-to-wall joins intact, and leaves accepted Cradle kits unchanged. Four additional sample columns smooth the optical blend. Surface arrays and collider serialization must remain identical; source bitmap bytes and UV scale are preserved.

The two authorized mound entrances have a separate helper, `WorldRevisionEntranceMoves`. PC04 mirrors its whole approved passage into the actual Causeway cliff at mouth (88.5,30), inward right. A01 translates into Qvale's western mountainside at mouth (21.178,20.625), inward left; its exterior painting follows the existing descending approach. Both disable their old artificial mound, carve the original doorway polygon into the actual route, retain persistent identities and destination predicates, and assert arrival support/doorway clearance. A01's paired surface/underground area metadata becomes MR04_N00; the underground assembly does not move. Parent owns execution, map regeneration, traversal tests and final frame acceptance.

Restored-painting density receipts currently show all 66 audited layers in MR02–MR07 above 216 native pixels per world unit. Five of eight audited Cradle painted layers remain below that threshold (minimum about 117.77 PPU). Those are explicit retained approved-source exceptions, not a license to replace the approved painting with flat Fill or report universal native-4K coverage.

The November4 follow-up explicitly retains these five approved Cradle exceptions; no further repaint is authorized without asking the user. The read-only `audit_visible_cradle.py` checks 28 samples per triangle, shader clipping, source alpha, actual painted-rim RGB contribution, and foreground-rim occlusion of the interior. All five deficits remain at a painted contribution threshold of 50%; they are not invisible-triangle false alarms. Existing geometry, composition and source bytes remain unchanged.

| Instance | Source | Native minimum PPU | Required whole-source dimensions at 216 PPU |
| --- | --- | ---: | ---: |
| PC01 interior | `Assets/Art/Prototypes/CaveRollout_20261027/Causeway_Masonry.png`, 2172×724 | 155.276 | 3022×1008 |
| PC01 rim | `Assets/Art/Prototypes/CradleRockwork_20261027/MR01_C01_Rockwork.png`, 1605×980 | 117.766 | 2944×1798 |
| PC02 rim | Same approved Rockwork source | 184.459 | 1880×1148 |
| PC03 rim | Same approved Rockwork source | 195.715 | 1772×1082 |
| MR01_C06 rim | Same approved Rockwork source | 165.427 | 2096×1280 |

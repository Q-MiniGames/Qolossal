# Revision map and traversal pipeline

Prepared for the revised surface corner profiles and two relocated mouths. No map refresh or Unity test is implied by the existence of these scripts. Execute only after all scene mutations are saved.

## Immutable baseline

`Before/Map/manifest.json` hashes seven copied inputs: canonical layout, runtime atlas, topology source, surface/underground geometry, scene inventory and passage geometry. The copied atlas/topology hashes match the revision's pre-edit scene/map backup manifest. Do not replace these copies with revised data. They preserve the approved rigid per-component cartography.

The intended topology change is `CAVE_A01: MR04_N06 → MR04_N00`, still paired to underground A01. PC04 keeps MR02_N02. PC04 mouth anchor moves from (81,30) to (88.5,30), facing inward +1; Qvale A01 mouth moves from (142.6,8) to (21.178,20.625). Mouth anchors are not entrance trigger coordinates. The fresh Unity extraction records mouth, facing, threshold and arrival separately.

All 89 underground room bounds, static Ground collision paths, 63 passage centerlines, lower entrances and lower arrivals must remain unchanged. The preflight rejects changes outside the two authorized upper pairs and A01's shared area metadata. It also checks 84 surface zone IDs/bounds, 13 current scene hashes, and agreement between independent geometry/inventory extracts.

## Coordinated execution order

1. Save every changed scene. In the existing Unity session run `Tools/Design/ActualSlopeComparison_20261030/extract.cs` to refresh seven surfaces, then `Tools/Design/ActualUndergroundComparison_20261030/extract.cs` to refresh the gallery. Both use read-only preview scenes.
2. Run this revision's `extract_inventory.cs` through the same Unity session. It writes `Map/inventory_fresh.json` and `Map/presentations_fresh.json` using real TransformPoint coordinates, including mirrored entrances. It does not save scenes.
3. Run `python Tools/Design/WorldRevision_20261104/refresh_map_sources.py` for preflight only. Investigate any rejection. Once it passes, run the same command with `--apply` to write canonical inventory, topology and the placement lock.
4. Run `python Tools/Design/WorldRevision_20261104/rebuild_map.py --publish`. It rebuilds the canonical layout/viewer, verifies every underground node position/outline/transform and passage against the immutable baseline, then exports runtime data and runs both revision and canonical static verification. Publication occurs only after the pre-export checks pass. Omit `--publish` for review-only generation.
5. Run `node Tools/Design/MainWorldLayout/render.cjs` to refresh canonical review renders, then inspect the moved mouth links and revised terrain. This is map rendering, not gameplay QA.
6. Reimport the updated atlas through the coordinating Unity session and run this revision's `map_playtest.cs`. It retains the existing discovered/undiscovered, 1080p/4K, seven-region, controller and 267 marker-position checks. It additionally asserts A01 originates at MR04_N00 and both changed pairs match canonical mouth markers. Current contour counts are read from the new canonical export.

`MainWorldLayout/forward_placement.py` now honors `component-placement-lock.json` when present. The source refresh creates that lock from the immutable baseline. It requires unchanged room IDs/bounds and equal XY scale, retains every per-node mappingId and map position, and checks that each physical passage remains inside one component. This prevents the ordinary packing search from rearranging PC04 and later caves merely because a surface entrance moved. Future physical underground geometry changes must explicitly reconcile the lock; deleting it to bypass an assertion is not a refresh procedure.

Static evidence is written to `Map/source_refresh.json`, `canonical_map_validation.json` and `runtime_map_validation.json`. The live fixture writes `Map/ui_tests.json`. Static receipts never claim a live UI pass.

## Focused traversal fixtures

`portal_playtest.cs` exercises PC04 and A01 with real virtual-gamepad walking: upper approach, transition, lower arrival, lower approach, return, upper arrival, control restoration and stable checkpoint ID reload. It checks four departures/emergences across the two pairs and zero arrival fallbacks, captures native 4K/1080p views, isolates saves and restores Editor configuration. It is bounded to 360 wall seconds. Output: `Portals/final_regression.json` and progress/captures. This is focused relocation coverage; it does not retest every unchanged cave pair.

`corner_playtest.cs` derives cases from vertices actually replaced in the applied `Surface/MRxx_rounded_corners.json` receipts, rather than treating planned corners as applied. It consumes both original and sloped-corner receipts and supports upper turns adjoining slopes up to grade 1. The original receipts plus recovered Causeway evidence contain 80 corners across eight terrain objects; the optional `MRxx_rounded_sloped_corners.json` receipts add the later convex vertical-to-slope refinements. For each lip it places the real player on the high ledge, allows settling, and walks outward past the rounded edge through normal input/physics. Short 1.05-unit treads use a shorter safe starting inset. It requires horizontal clearance and no respawn; dropping after clearing an edge is expected. This catches protruding/snaggable lips without asserting that steep walls should be walkable. It does not establish upward ledge climbing or full platforming traversal. Output: `Corners/corner_playtest_result.json`, durable cases and progress. Recheck the dynamic case count if receipts change.

The separate `camera_playtest.cs` measures low-foot composition across all underground checkpoints and surface jumps. Run fixtures one at a time; each expects clean saved scenes and owns Play mode temporarily. Inspect failures and rendered ceiling risks rather than treating script completion as approval.

Targets remain Windows PC, PS5, Xbox Series X/S and Nintendo Switch 2 handheld/docked. These Editor checks do not certify platform builds, physical controllers, 60 FPS, memory use or console readiness.

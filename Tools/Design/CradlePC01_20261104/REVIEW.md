# Cradle PC01 cave replacement — 4 November 2026

The user's phone screenshot identified the oversized diagonal cave in the Cradle's First Gorge. This local rebuild supersedes PC01's appearance and measurements in the earlier terrain-revision gallery.

The 7.8-unit descending cave is replaced by a 3.9-unit level recess using the existing painted limestone rim, a dark interior and native Cradle walking surface. Three overlapping legacy terrain assemblies are inactive. The actual route now owns one continuous approach, a rounded roof shoulder and an eased onward slope. The old medium-step decoration at x59.9 is omitted because the fitted cave rim owns that lip; it otherwise left a detached sliver. Runtime terrain code is unchanged by this repair.

The mouth is (60,24.7), inward right; trigger (62.9,24.7); return arrival (58.4,25.36). The trigger fits inside the passage and does not reach the playable roof. Connection CAVE_PC01, room PC01 / Firefly Nook, surface area MR01_N02, arrival ID and requirements remain unchanged. Underground scene geometry, other cave entrances and all other scene hashes remain unchanged.

## Validation

`QA.json` aggregates the final results. `Portals/final_regression.json` passes 19 steps using virtual gamepad input: approach, entry, underground return, normal jump onto the roof, onward hill traversal and checkpoint reload. Both transitions use their actual passages with zero fallback arrivals. Enemy knockback is suppressed only in the fixture. Normal saves and Editor settings are restored.

Fresh surface and inventory extraction, canonical layout generation, runtime atlas export and source verification are recorded in `Map/source_refresh.json`. The atlas now contains 646 actual contours. All 89 underground rooms, connected components and cartographic transforms retain their positions. Only PC01's illustrated tunnel endpoint registration changes; connectivity and IDs stay fixed. The live atlas fixture passes 37 steps at 1080p and 4K, including PC01 mouth registration and all underground marker samples.

## 4K source requirements

The closest camera half-height remains 5: 2160 / 10 = 216 screen pixels per world unit. `density_audit.cs` measures every nondegenerate triangle and the largest affine texel footprint, including the rounded rim's deformation. All four rebuilt PC01 colour placements pass source and imported resolution. Exact values are in `resolution_4K.csv`; no image was enlarged or repainted.

| Layer | Existing source | Required source at final scale |
|---|---:|---:|
| Interior | 2172 × 724 | 1588 × 530 |
| Painted limestone rim | 1605 × 980 | 1387 × 847 |
| Native fill | 2048 × 2048 | 1844 × 1844 |
| Native walking strip | 3072 × 1536 | 1844 × 922 |

All 1,351 canonical accepted source hashes pass unchanged. This local assembly's pass does not remove resolution exceptions elsewhere in the world. Windows Editor checks do not qualify console builds, memory or 60 FPS.

## Evidence and recovery

Phone-friendly actual game capture: `output/images/Cradle_Cave_Rebuilt.png`. Native 3840 × 2160 capture: `output/images/Cradle_Cave_Rebuilt_4K.png`. The old mobile PDF remains historical and is superseded for this entrance.

`Before/MRAtlas_MR01_Cradle.unity.backup` and `Before/Map` preserve the prior scene and map inputs. `rebuild.cs` is the localized Editor authoring script; it preserves non-PC01 openings and terrain outside x55..81. Do not rerun the older region-wide cave builder to restore the rejected assembly. Future terrain changes must repeat extraction and map publication with a new immutable baseline.

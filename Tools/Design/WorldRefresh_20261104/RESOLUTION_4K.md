# 4K environment resolution audit — 4 November 2026

Target display: **3840 × 2160**. Closest authored orthographic half-height: **5 world units** across the shipping cameras and vista zones. Therefore **2160 / (2 × 5) = 216 screen pixels per world unit**. Horizontal view is 17.777778 world units at 16:9.

**PASS: 1,526 active placements, 224 distinct raster assets, zero native or imported resolution failures, zero skipped meshes.** No bitmap was enlarged to claim native detail.

For each mesh triangle, the audit computes the largest singular value of the affine world-distance-per-source-texel transform; its reciprocal is the worst directional texel density. The required aspect-preserving source canvas is `ceil(source dimension × 216 / worst native density)`. This catches sheared sloping strips as well as ordinary scaling. Parallax zoom is evaluated analytically at the closest camera; tiled sprites use native tile scale. The cave, Heart and fitted-stone shaders use explicitly measured world-space sampling.

This covers the refreshed environment. It is not a certification of characters, UI, cinematics, render performance or every asset in the project. Continuous sky gradients and geometric doorway silhouettes rasterize at output resolution and do not need a bitmap canvas.

The complete per-asset calculation is in [CSV](asset_resolution_requirements.csv) and [JSON](asset_resolution_requirements.json), including source/import sizes, exact required canvas, usage count and source hash. Individual scene placements are in [density_instances.json](density_instances.json).

## Newly painted assets

| Asset | Native pixels | Required pixels at closest camera | Lowest native pixels/world unit |
|---|---:|---:|---:|
| MR01/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR01/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR02/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR02/Fill.png | 1254 × 1254 | 754 × 754 | 359.656 |
| MR02/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR02/Top.png | 2172 × 724 | 2077 × 693 | 225.961 |
| MR03/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR03/Fill.png | 1254 × 1254 | 753 × 753 | 359.972 |
| MR03/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR03/Top.png | 2172 × 724 | 2095 × 699 | 223.967 |
| MR04/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR04/Fill.png | 1254 × 1254 | 754 × 754 | 359.348 |
| MR04/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR04/Top.png | 2172 × 724 | 2070 × 690 | 226.694 |
| MR05/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR05/Fill.png | 1254 × 1254 | 753 × 753 | 359.884 |
| MR05/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR05/Top.png | 2172 × 724 | 1919 × 640 | 244.576 |
| MR06/Fill.png | 1254 × 1254 | 753 × 753 | 359.959 |
| MR06/Top.png | 2172 × 724 | 1969 × 657 | 238.345 |
| MR07/Far.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR07/Fill.png | 1254 × 1254 | 753 × 753 | 359.965 |
| MR07/Mid.png | 2172 × 724 | 2160 × 720 | 217.2 |
| MR07/Top.png | 2172 × 724 | 2050 × 684 | 228.965 |
| Sky/SunsetCirrus.png | 2172 × 724 | 2160 × 720 | 217.2 |
| Underground/A_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/C_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/D_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/E_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/H_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/Heart_Detail.png | 1254 × 1254 | 1254 × 1254 | 216 |
| Underground/MR01_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/MR02_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/MR03_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/MR05_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/MR06_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/MR07_Vista.png | 1774 × 887 | 1774 × 887 | 216 |
| Underground/P_Vista.png | 1774 × 887 | 1774 × 887 | 216 |

The existing approved Heights river, mid/near scenery and neutral cloud sources are included in the full table. A 10 × 3⅓ world-unit scenic panel requires 2160 × 720 pixels; the 2172 × 724 native paintings meet it. The 1254-square terrain tiles repeat at 360 PPU, so one 3.483333-unit tile requires 753 × 753 pixels. The Heart and underground paintings sample at 216 PPU with no enlargement.

## Silhouettes and resource limits

Old enlarged cave alpha masks are reconstructed as geometric outlines; the original source PNGs remain unchanged. Most boundary triangles have at most 4 screen-pixel edges, with a few at 4.5–6 and four complex roof trims at 12. These are approximation limits for contour geometry, not enlarged color texels. Each doorway is bounded to 40,000 visible triangles. Total new mesh vertex/index estimate across all seven separately loaded regions is 20.28 MB; hardware measurements are still required.

| Target | Validation in this change |
|---|---|
| Windows PC | Unity Editor native 4K captures, density/import audit, live player and map tests; see final QA |
| PS5 | Not built or hardware tested |
| Xbox Series X | Not built or hardware tested |
| Xbox Series S | Not built or hardware tested; memory/frame-time validation remains required |
| Nintendo Switch 2 handheld | Not built or hardware tested; output-specific resource profile remains required |
| Nintendo Switch 2 docked | Not built or hardware tested; output-specific resource profile remains required |

60 FPS is the project target, not a verified result here. Source masters remain intact; platform runtime variants must come from measured import/build profiles. Original Switch is excluded.

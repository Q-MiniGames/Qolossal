# Revised environment resolution audit

Target: 3840 × 2160. Closest camera half-height: 5 world units. The display requires 216 pixels/world unit and spans 17.777778 × 10 world units.

Placements: 2264. Distinct raster sources: 297. Resolution exceptions: 3. Skipped mappings: 0.

Required dimensions are calculated from the largest affine world-distance-per-source-texel singular value of every nondegenerate mesh triangle. This includes the approved rim painting’s registered UV1 channel and the live relocated/bent entrance meshes. Native world-sampling shaders are audited from their explicit pixels-per-unit mapping. Parallax is evaluated at the closest authored zoom. Requirements are rounded upward per source canvas, preserving aspect ratio.

The CSV and JSON contain every source/import size, exact required canvas, usage and hash. A painting does not need to be a full 4K image if it occupies only part of the screen. No bitmap enlargement counts as new native detail.

| Newly painted source | Native pixels | Required pixels | Native/import pass |
|---|---:|---:|---|
| CaveVista/A_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/A_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/A_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/A_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/C_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/C_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/C_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/C_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/D_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/D_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/D_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/D_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/E_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/E_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/E_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/E_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/H_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/H_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/H_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/H_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR01_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR01_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR01_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR01_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR02_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR02_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR02_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR02_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR03_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR03_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR03_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR03_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR05_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR05_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR05_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR05_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR06_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR06_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR06_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR06_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/MR07_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/MR07_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR07_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/MR07_Vault.png | 1774 × 887 | 1774 × 887 | True/True |
| CaveVista/P_Extension.png | 1254 × 1254 | 1254 × 1254 | True/True |
| CaveVista/P_Left.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/P_Right.png | 1024 × 1536 | 1024 × 1536 | True/True |
| CaveVista/P_Vault.png | 1774 × 887 | 1774 × 887 | True/True |

## Exceptions

- Assets/Art/Prototypes/CaveRollout_20261027/Causeway_Masonry.png: source 2172 × 724; required 3022 × 1008.
- Assets/Art/Prototypes/CradleRockwork_20261027/MR01_C01_Rockwork.png: source 1605 × 980; required 2944 × 1798.
- Assets/Art/Prototypes/CradleUndergroundExit_20261027/MR01_C01_ReturnRim.png: source 1448 × 1086; required 1660 × 1245.

## Derived coverage masks

These masks close geometry above retained approved paint. They add no painted RGB detail. Their inherited source-boundary sampling is measured separately and is not counted as new native artwork.
- Assets/Art/Prototypes/WorldRevision_20261104/RoofClosure/AbovePaint_3b737daf139eff044b10b4ea1cf997a1.png: 1448 × 1086; measured requirement 1145 × 859; native/import pass True/True. Derived from Assets/Art/Prototypes/CradleUndergroundExit_20261027/MR01_C01_ReturnRim.png.

Windows Editor evidence is separate from platform certification. PS5, Xbox Series X, Xbox Series S, Nintendo Switch 2 handheld and docked builds/hardware were not tested. 60 FPS remains a target. Preserve native masters and derive runtime platform profiles from measured builds. Original Switch is excluded.

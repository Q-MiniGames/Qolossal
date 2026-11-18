# Native cap-join follow-up

The final 4K cave frames exposed narrow rectangular sky-fading strips beneath several cliff tips. `MraTerrain.TopMesh` extends a top strip by `lipOverhang` at a cliff or open end, including its entire fading soil tail. That geometry previously sat in front of the painted cliff kit. Retiring the old low-density cliff art exposed the strip outside its supporting Fill. The repeated defect at both ordinary risers and cave roofs distinguishes it from a single doorway collision gap.

The outside-Assets evaluation script `fix_native_cap_joins.cs` handles the current MR02–MR07 scene:

- Native top strips terminate at the supporting cliff (`lipOverhang = 0`); surface points and collisions remain unchanged.
- The old static roof-top duplicate is disabled only when a current terrain Top renderer already overlaps it. Static native stone contour pieces remain.
- Static walking strips fade before their new UV crosses the source bottom. This removes stretched clamp-edge tails inherited from the taller old texture footprint.
- Rectangular passage shadows taper over their own column ceiling, preserving the sloping lower entrance and interior depth.

The script clones changed static meshes and reports before/after vertex and triangle equality, checks all serialized Collider2D components unchanged, and writes a per-region receipt. Root must run and inspect MR03/MR04 before the remaining scenes. No editor operation was performed by the surface agent during the active playtest.

## Final attachment cleanup

The first pass removed the rectangular sky slots in the MR03/MR04 checks. Remaining copied stone contours protruded outside the actual cliff edge, and Qvale exposed the fitted outer-rock grid's abrupt back edge: screenshot x1520 maps to world x137.40, exactly `mouth142.6 - (depth4.6 +0.6)`.

`fix_native_cap_joins_v2.cs` has an independent completion marker. It clips those copied roof contours against the nearest supporting vertical terrain edge, using the original cave placement and actual current terrain. It feathers the outer-rock grid over its last0.45units; the grid extends0.6units beyond the aperture, so the feather remains outside the walking opening. Summit's artificial `.74` cap-line arch repair is included only if the earlier pass has not already done it. Materials retain native world-space mapping. All collision is checked unchanged.

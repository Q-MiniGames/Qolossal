# Surface capture review

**Latest inspection: 4 November, final V2 cave-cap and Summit arch repairs.** The final scoped recheck below resolves the previously reported cap-end fade strips and clipped Summit cave shadow in the affected samples. Earlier observations remain as historical evidence. This is visual QA, separate from runtime and hardware validation.

## First integration pass (historical)

Reviewed 21 native 3840 × 2160 captures: each region's entry, middle and cave view. This is a static visual inspection, before the pending sky/cloud and fitted cave-trim repairs. It is not a gameplay, collision, frame-time or hardware pass. Capture paths may be replaced by subsequent verification renders; these observations describe the first integration set.

The new landscape paintings provide the requested rivers, settlements, orchards, roots and alpine lakes. Their regional themes remain distinct, and the newly painted terrain top and fill combinations read clearly. Ordinary slope transitions in the sampled entry/middle views are continuous. The remaining major visual problem is the junction between existing cave-mouth artwork and the new terrain.

## Required correction and recapture

| Finding | Evidence | Recommended action |
|---|---|---|
| Existing cave trims form rectangular overlays, stretched edges and mismatched material scale/color against the new terrain. Some caps protrude above the hill contour. | All seven `Captures/MRxx_cave_4K.png`; especially MR02–MR07. Also MR02 middle, MR04 middle left edge and MR07 middle right edge. | Complete the fitted-trim repair and recapture every cave view. Keep the opening/landing silhouette registered to collision and remove the large obsolete top/bottom overlays. |
| Hanging decorative plants are visibly unattached. | MR06 entry at approximately (2030,1010) and (3200,300); MR07 entry (1990,650); MR05 middle (750,1640); MR07 middle (730,1690). Coordinates are pixels from image top-left. | Attach their upper end to a visible rock/canopy surface, or disable the decorative renderer. Preserve any gameplay component. MR06 middle ivy is correctly attached and should remain. |
| Large nearly blank sky field; clouds occupy inconsistent vertical positions between camera placements. | MR01–MR05 entry/middle, MR07 middle. | Root already repairing gradient anchoring and cloud vertical follow. Recapture after the fix; review the distance impression and sky coverage during vertical camera motion. |

## Composition and source limitations

Mirrored 10-unit scenic repeats visibly duplicate landmarks within one frame. The clearest cases are the two large Qvale trees in MR04 entry and paired cistern hills in MR02 entry/middle. Repetition also appears in arches and streams elsewhere. A longer cycle with additional independently painted native panels would reduce this; simply enlarging the current texture would violate the established density budget. This is composition polish, distinct from a broken seam.

Existing Qvale service-house art and some existing decorative props look softer at this display scale than the new native terrain/background sources. Source resolution for this refresh must not be described as a full audit or repaint of every legacy prop. The separate density audit determines where those old assets fall below the target.

Several retained cliff endpoints have perfectly vertical sides (MR04 entry around x=2870; MR03 middle around x=780; MR06 middle around x=775 and x=3020). Confirm these are deliberate platform edges/gaps. If so, use edge decoration that fits the silhouette rather than changing their geometry merely to hide the join.

## Sample coverage

| Region | Entry | Middle | Cave |
|---|---|---|---|
| MR01 Cradle | Spring arches and foreground rock readable; mirrored scenery visible. | Shrine/basin framed clearly; terrain continuous. | Dark sloping recess readable; exposed rectangular top endpoints and old trim require repair. |
| MR02 Causeway | Aqueduct theme clear; repeated cistern landmarks obvious. | Shrine readable; oversized old cave overlay intrudes below landing. | Large pale rectangular old mouth and top cap mismatch new stone. |
| MR03 Terraces | Orchard and mill theme clear; ascending lip continuous. | Shrine and ascent clear; hard left cliff endpoint. | Old pale mouth overlay and oversized flower/grass trim mismatch honey stone. |
| MR04 Qvale | Village and flowering landscape clear; duplicated tree landmark; hard right endpoint. | Service house and walking edge readable; old cave sliver at left. | Old brown cave overlay/cap sits conspicuously on peach stone. |
| MR05 Ribwood | Rib arches and rooted stone form a coherent theme; ascent continuous. | Shrine and rolling slope clear; floating hanging root. | Old bark-like mouth trim has rectangular cutoff against new rock. |
| MR06 Heights | Approved river vista retained; clean pale slope; two floating ivy props. | Shrine readable; cliff ivy attached; vertical terrain endpoints. | Old engraved cap and mouth trim mismatch new ivory fill. |
| MR07 Summit | Cool lakes/stone palette distinct; slope clear; floating dark root. | Lake panorama clear; mirrored arches; floating root and old trim at far right. | Dark mouth opening readable, but large rectangular dark cap/trim is not integrated with pale rock. |

Characters and relics are posed for diagnostic capture. Apparent suspension of the entry character is not used as evidence of a physics defect. No source pixels were modified during inspection.

## Follow-up evidence

After repairs, replace or version the 21 captures and add a dated follow-up below recording what was actually re-inspected. A screenshot's existence or its loading in the HTML gallery is not a visual pass. Validate motion and traversal separately.

## Follow-up — 4 November, repaired trim/decor capture set

Re-inspected all seven native cave frames and the entry/middle frames for MR05, MR06 and MR07 (13 frames). The surface cave artwork now matches the corresponding terrain color and texture much better. The obsolete large painted overlay mismatch is resolved in these samples. Mouth edges and landings generally meet cleanly. The lighter caves read as shallow rock recesses; Ribwood has stronger depth contrast. This is a stylistic distinction rather than a missing texture.

The previously floating MR05 middle roots, MR06 entry ivy, and MR07 entry/middle roots now attach to visible terrain. No unattached plant was found in those six entry/middle frames. Ordinary slope lips remain visually continuous. Detailed rivers, roots, waterfalls and alpine lakes remain visible beyond them. This does not establish a collision or traversal pass.

| Remaining finding | Native capture evidence (approximate pixel coordinates) | Action |
|---|---|---|
| Straight vertical strips fade to the sky at cliff/cap endpoints. | MR03 cave x1160–1200, y820–990; MR04 cave x2640–2680, y820–990; MR05 cave x1150–1200, y560–820 and x2740–2790, y310–500; MR06 cave x1135–1200, y515–990. Similar smaller endpoint treatment in MR01/MR02/MR07. | Fit or cover the cap ends so the exposed boundary is intentional rock, without a narrow rectangular transparent slot. Preserve collision. |
| Summit cave shadow is cut off along a perfectly horizontal top edge. | MR07 cave x1195–1810 at y950; also visible at the far right of MR07 middle. | Extend the shadow mask beyond its clipping edge or fit its contour to the rock roof, then recapture both views. |
| A straight texture join remains on the Qvale cave face. | MR04 cave around x1520; top-right cap also projects beyond the normal grass trim. MR02 has a similar raised cap crest. | Blend/fit the decorative face and cap silhouette if the endpoint repair does not remove these joins. |

These residual cave joins were reported to the integrating agent for correction. They prevent an unconditional final visual pass on this capture set. Background landmark repetition and legacy prop softness remain the composition/source limitations described above. No screenshots or source pixels were edited during QA. Play-mode results, map validation, performance and target-hardware validation are separate evidence.

### MR03/MR04 endpoint repair recheck

Re-inspected the next MR03 and MR04 cave captures after the endpoint repair. The narrow rectangular sky slots at y820–990 are resolved in both. The correction is suitable for the other affected regions. A smaller decorative cap overhang still fades into the sky (MR03 x1105–1195, y650–820; MR04 x2640–2730, y735–820), and the Qvale face texture join at x1520 remains. Clipping the decorative overhang to the cliff boundary would give a cleaner silhouette. This recheck covers these two cave frames only.

## Final V2 repair recheck — 4 November

Re-inspected the final native MR03 and MR04 cave frames after the second cap repair. The overhanging faded caps are removed and no sky slot remains. Qvale's left face texture join is softened. The cliff ends remain deliberately straight where the terrain footprint ends; no detached or fading appendage remains. These two frames pass the specific seam/overhang recheck.

After the repair was applied across the remaining affected regions, re-inspected final `MR05_cave_4K.png`, `MR06_cave_4K.png`, `MR07_cave_4K.png` and `MR07_mid_4K.png` at native 3840 × 2160. Ribwood and Heights no longer show the narrow transparent cap slots or faded cliff appendages. Their mouths and landings meet the surrounding terrain. Summit's former horizontal shadow cutoff is replaced by a continuous curved arch in the cave view; the corresponding middle-view edge no longer shows the former horizontal clipping. No new blocking visual defect was found in these four frames.

This final recheck covers the six named cave/middle frames, not a new inspection of every room or every frame. The previous vegetation recheck remains the evidence for the repaired MR05–MR07 entry/middle props. Background landmark repetition remains optional composition polish. The final environment density audit is authoritative for measured resolution; visual softness alone is not a failed density measurement. No capture pixels or source art were modified during QA. Actual movement, underground settling, map checks and platform performance remain separately recorded validation.

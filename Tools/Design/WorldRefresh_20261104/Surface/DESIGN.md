# Surface redesign: living over a reclining Titan

The actual seven saved surface profiles already express the approved anatomy. This pass retains the connected macro route and its landmarks, while easing ordinary repeated steps into flowing walkable connectors. Do not warp the map to the old Titan illustration. Regenerate the map from the resulting scene geometry.

| Region | Titan structure | Terrain and distant-scene theme | Preserved challenge |
| --- | --- | --- | --- |
| Cradle | Toes, foot hollows, ankle | Soft mossy limestone folds; sheltered springs and pale bluffs | Grip shrine, quake crossing, catching floor |
| Long Causeway | Gradually ascending shin | Eroded gray road-stone; aqueduct gorge and cisterns | Living Thread lift wall and approach |
| Terraces | Kneecap crest, descending thigh | Honey limestone, orchard banks, irrigation waterfalls | Sluice gate/lintel, Bloomfall progression |
| Qvale | Sheltered lap basin | Cream worn paving and warm-spring town | Town buildings, entrances, shops and listening tree |
| Ribwood | Belly rise and rib ridges | Root-bound umber stone, fern moss, ancient canopy | Wind Leaf shrine and deliberate climbing walls |
| Windward Heights | Shoulder crest, collarbone and neck dip | Ivory limestone, sparse wind grass, detailed river valley | Glidecap shrine, 18-unit glide gorge and catch floor |
| Summit | Chin, face, brow and crown | Cool silver limestone, pale grass/reeds, high lakes and waterfalls | Reveal ledge, lake shore and ending approach |

`WorldRefreshSurface.AnalyzeCurrent()` writes a proposed edit report without changing the scene. `ApplyCurrent(true)` updates the current consolidated surface scene and writes a manifest covering all twelve zones. It never invokes the retired scene generators. `ValidateCurrent()` checks protected geometry and actual collision under each edited connector.

Ordinary risers up to 5.4 units can become eased connectors when there is adequate space. Repeated 4.2-unit climb walls and taller jumps are retained. New ramp grade is at most 1.0; each end eases into the adjoining tread. Changes farther than 3.2 units from the original local walkline are rejected. Beat pads, cave mouths, arrivals, carved apertures, props and encounter footprints remain protected. All cave IDs and transforms remain registered. Candidate changes that cannot fit around these constraints are explicitly retained, not silently moved.

Six new native top strips replace low-resolution MR02–MR07 top/step/face overlays; root owns matching fill textures and repairs to separate fitted cave meshes. Cradle's accepted 360-PPU top and existing reviewed art remain intact. Source bytes are preserved. Art source dimensions are measured after generation; images are never enlarged to manufacture detail.

The new top strips use 360 pixels/unit and world-space horizontal repeat. Walklines are registered at the measured near-opaque ground rows: MR02/MR03 row309, MR04 row333, MR05 row327, MR06 row324 and MR07 row321 (reference walkLinePx equals source row/3 at 120 PPU). Maximum painted slope is 1.0; a sheared top UV at that slope retains 360*(sqrt(5)-1)/2 = 222.49 native pixels/unit in its weakest direction, above the 216 display pixels/unit required at 3840×2160 and camera size 5. Fill tiles stay at 360 pixels/unit. Old low-resolution riser/step overlays are disabled where the new material is applied; wall collision and climb permissions remain.

Platform targets: Windows PC, PS5, Xbox Series X/S, Nintendo Switch 2 handheld/docked. Editor assertions and screenshots are not hardware/60-FPS evidence. Test protected skill gates, full traversal, physical gamepad, memory, CPU/GPU cost and native platform builds separately. All84-zone manifests describe what changed and what stayed protected.

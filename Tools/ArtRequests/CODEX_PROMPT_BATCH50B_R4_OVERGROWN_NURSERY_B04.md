# Codex brief: Batch 50b-r4, Overgrown Nursery B04, side-elevation repaint

10 Nov 2026, Claude. Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_112.md` first. The r3 method and most of its art are accepted. This round fixes one cause (perspective) and two related issues. Route names A-D; B04 = internal C04.

Everything in the r3 brief and its predecessors still applies. Deliver into `Codex_v2_full\Batch50\50b-r4\`. Never overwrite earlier files. No scene edits, prototype folder only, BC7, mipmaps off. Unity: if another session is using it, stop and ask the user. Painting may use image generation; every check is computed from real captures and delivered PNGs.

## 1. Repaint as side elevation

Repaint `B04_ROOM_LAYOUT` and then the eight `SET_B04_Rock_01`-`08` vignettes (same names and sizes; keep placements unless a change is needed, and say why) **as a flat side elevation**:
- The rear wall faces the camera. **No visible floor, ground plane, paving, stair treads, or tops of walls and rocks seen from above.** No receding perspective.
- Depth comes from overlap, value and recesses: arches, niches and cave mouths falling into darkness.
- Masonry and rocks show their faces. Their tops are broken, rounded off, or covered in growth and roots.
- Keep r3's composition per camera: the arch with the lantern and trellis ring (upper right), the arched jar niches (lower left), the seed archive under roots (upper left), and the broken masonry beside the central drop. Redraw each in elevation.

Prompt tip: describe the wall as "orthographic side view of a cave wall, flat elevation, no floor visible, as in a 2D side-scrolling platformer background layer".

## 2. Cooler grade

Moss-black and slate stone (Route B palette), amber only within lantern pools, mint at root tips and lilac in blooms. The wall must read as a cave, not warm brown or sepia. Paint it at its intended value and hue; don't recolour afterwards.

## 3. No giant trunk

Replace the large curved bark trunk (upper left) with several knotted, branching roots of varied thickness, none over about 1.2 u thick, diverging, never parallel.

## 4. QA

All r3 QA again, plus:
- **An all-layer ledge sweep** (rear wall, SET, MID, FAR, every kit placement): no lit or pale horizontal top longer than 1.5 u with a slope of 30° or less, within 4.5 u above any deck, across the full camera clamp at each layer's motion. Report the worst cases.
- A **perspective check**: for each vignette, state that it contains no floor or tread surface. Then show each one beside its r3 version.
- The four layout-vs-capture comparisons, the r2a concept comparisons for centre and upper left, and the route memory total (at most 260 MiB).

## Deliver and stop

`MANIFEST_50b-r4.json`, `QA_INDEX_50b-r4.html`, `REVIEW_HANDOFF_50b-r4.md` (verdict per item; re-check the next free CLAUDE_REVIEW number; don't write a review). Leave the preview ready to play.

**Stop.** Claude reviews and the user plays before 50c.

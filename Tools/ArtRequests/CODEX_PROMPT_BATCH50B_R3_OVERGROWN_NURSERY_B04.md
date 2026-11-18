# Codex brief: Batch 50b-r3, Overgrown Nursery B04, composed from a room layout

10 Nov 2026, Claude. Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_111.md` first: r2b is rejected because the room was assembled from tiles and overlays instead of composed. Route names A-D; B04 = internal C04.

**Everything in the 50b-r2 brief and its "r2a review result" conditions still applies**: the natural direction, the 1 : 2 built-to-natural ratio, no outdoor read, no scale-like rock, mint only at root tips, no parallel roots, flowers at 0.2-0.4 u, and the technical rules (216 px/u, no upscaling, multiples of 4 px, max 4096, BC7, mipmaps off, prototype folder only, no scene edits). **Target look: the two approved r2a concepts**, `50b-r2/B04_center.png` and `B04_upper_left.png`.

Deliver into `Codex_v2_full\Batch50\50b-r3\`. Never overwrite earlier files. Unity: if another session is using it, stop and ask the user.

Painting may use image generation. **Every check must be computed from actual Unity captures and the delivered PNGs.**

---

## 1. Room layout first

Paint all of B04 (64 × 36 u) as one composition at concept resolution (at least 20 px/u), over the real terrain:
- It matches the approved concepts at (1596, 278) and (1574, 296), and continues their look across the rest of the room. Upper right and lower left get the same richness: overhangs, layered rock, roots, remnants and light.
- Every layer is in place: rear wall, MID SeedFall, FAR (shown at its centre-camera offset) and FG. Lights are painted in.
- Built remnants make up about a third of the visible environment: the seed archive, jar niches, broken terrace fragments, a bent trellis and lantern niches cut into rock, grouped into 2-4 places, not scattered evenly.

The layout is the reference for everything below. Archive it; none of its pixels go into production files.

## 2. Production, cut from the layout

| File | Size | Notes |
|---|---|---|
| SET_B04_Rock_01 … _08 (new, up to 8) | each at most 2592 × 2592 (12 × 12 u), any multiple-of-4 size | **Unique rear-wall vignettes** painted natively from the layout's regions, overlapping each other by at least 1 u, with your own hand-drawn masks. No straight edge anywhere a viewer can see. They cover everything the four cameras show that isn't very dark |
| NK_Wall_Rock_01/02 | unchanged | Only where the wall is very dark: local mean at most 10% of the walk rim |
| FAR_B04_Terraces | same name and six tiles | **Repaint**, dark and deep: median at most 6% of the walk rim in every capture, P99.9 15-25%. Layered cave depth, detail only in light pockets (far lanterns, root tips, a few blooms). Objects at about half rear-wall scale |
| SET_B04_SeedArchive | same name and size | Local retouch only: separate the paired roots so they diverge; repaint its flowers at 0.2-0.4 u |
| LM_B04, LM_B04_Lilac, passage maps | same sizes | Pools only, each at most 4 u in radius around a visible source. The rest stays neutral and unlit. No large coloured washes |
| Other r2b pieces (SeedFall, overgrowth, niches, buttresses, ribs, arches, FG) | unchanged | Reuse where the layout calls for them. Never the same piece twice in one frame unless flipped, cropped or partly hidden |

**Budget:** the Route B planning ceiling is now **260 MiB** (Review 111). Report B04's new total and the route total.

## 3. QA (all measured)

Everything from r2b, plus:
- **Layout vs capture:** the four B04 cameras, each shown as the layout crop beside the actual Unity capture. The layout replaces "nearest concept" for upper right and lower left.
- **FAR median** at most 6% of the walk rim, and P99.9 15-25%, in every capture.
- **Seams:** no straight luminance discontinuity longer than 2 u where two placements meet, in every capture.
- **Light-map pools:** component radius at most 4 u, each centred on a visible source.
- **Built share:** about 33% (25-40% is acceptable), per camera.
- Archive root divergence and flower sizes, re-measured.

## 4. Deliver and stop

`MANIFEST_50b-r3.json`, `QA_INDEX_50b-r3.html`, the layout, the comparisons, and `REVIEW_HANDOFF_50b-r3.md` with a verdict per item. Re-check the next free CLAUDE_REVIEW number; don't write a review. Leave the preview ready to play.

**Stop.** Claude reviews and the user plays before 50c.

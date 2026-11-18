# Codex brief: Batch 49b-r2, the depth pass (Keepers' Cistern A04 slice)

8 Nov 2026, Claude, after the user's play test of 49b-r1. The user's verdict, with two marked screenshots: "there is nothing in the art making it feel like different layers; it looks like drawings next to each other."

The user is right. Every piece in the slice is painted flat, lit evenly from the front, and placed edge to edge with a straight cut. Where two pieces meet, nothing tells the eye which one is in front: no shadow, no thickness, no step in value, no contact with the water. Parallax only helps while the camera moves. A still frame has to read in depth too.

This brief replaces "the four fixes at the start of 49c" from Review 100: they are now part of r2 (section 3). The main brief (`CODEX_PROMPT_BATCH49_KEEPERS_CISTERN_ROUTE_A.md`) still applies. Sizes, layer motions and the value gate don't change. Stop after r2 for Claude's review and the user's play test. 49c still waits.

## 1. The joins the user marked

| Where | What's wrong now | Required |
|---|---|---|
| Terrain wall next to the rear wall (A05, the left edge of screenshot 1) | The terrain's straight vertical edge sits on the teal masonry with no shadow. Both read as one flat plane | **Contact shadow from terrain:** the rear wall darkens toward every terrain edge. About 0.6 u wide falling to nothing, and about 45% darker at the contact |
| Pier in front of the wall (screenshot 1, middle) | The grey pier has the same value as the wall and no side face. It looks like a grey stripe painted on the wall | **Projection:** each pier has a visible return face, 0.15-0.25 u wide and clearly darker than its front, on the side away from the nearest lantern. It casts a soft shadow on the wall on that side, about 0.5 u wide and 35-40% darker. Add a darker joint line where the pier meets the floor or water |
| Water against the wall base (screenshot 2, bottom) | A straight horizontal cut where the far water painting meets the 100% masonry band. The wall seems to stand in front of a picture of water | **A reservoir edge:** a coping course (a stone lip with a lit top face, 0.25-0.35 u) runs along the wall at the water line. Under it, the courses continue into the water, darker and slightly broken by ripples. A thin wet-dark band sits just above the waterline. The far water stays far; the basin edge is near |
| Piers standing in water (screenshot 2, left and right) | They end at the water's edge with no reflection and no waterline | **Contact with the water:** a darker wet band at the waterline, a broken reflection of each pier in the water below it (fading and rippled, at most 40% opacity, never mirrored detail), and a thin meniscus highlight where stone meets water |

## 2. Rules for every join in the route (and for 49c)

1. **Overlap is the main depth cue.** Wherever a nearer piece covers a farther one, at least one of these must show at the edge: a cast or contact shadow, a step in value of at least 15% between the two sides, or a visible side face. A straight edge with matching values on both sides fails.
2. **One light logic.** Shadows fall away from the nearest light source (lanterns, mint roots). In the dark between lights, use soft ambient occlusion with no direction. No piece is lit evenly from the front.
3. **Depth sets value and edges.** Near (100%) pieces have the crispest edges and the most local contrast. MID pieces sit a step softer. The FAR is softest and lowest in contrast, and gets a faint cool mist between it and the near layer, with a value difference of at least 8% wherever they meet on screen. That mist is painted or a soft gradient sprite, never a tint over the whole frame.
4. **Nothing ends on a straight cut** unless it is a built edge that has thickness (a coping stone, a pier side).
5. **The water belongs to the room.** A water surface always meets something with a waterline, wet band and reflection: a basin edge, a pier or a wall.

How to do it: paint the return faces, coping and submerged courses (repaint or new pieces, at 216 px/u, native). Contact shadows, AO and reflections may be soft-alpha sprites, or done in the prototype shader **if** it uses each piece's own silhouette and the terrain's real edges, not rectangles. Report which you used for each.

New pieces you may add (same naming and density rules, all multiples of 108 px):

| File | u | px | Notes |
|---|---|---|---|
| CK_Basin_Coping | 10 × 0.5 | 2160 × 108 | Tiles horizontally; a lit top face |
| CK_Basin_Submerged | 10 × 2 | 2160 × 432 | Tiles horizontally; soft alpha; courses fading into the water |
| CK_Shadow_Soft | 2 × 2 | 432 × 432 | Soft alpha; a 9-sliced contact-shadow sprite |

## 3. Carried over from Review 100

1. The piers are one block section stacked in a grid, and grey where the walls are teal. Paint `CK_Column` as native tall shafts with irregular coursing in the teal-slate palette, with the return face from section 1. Clean up the slivers and offsets near the pier tops at A04 approach.
2. `CK_Roots_Crawl` stays diagonal or draping, at least 2 u from any walk line, and never parallel to a platform within reach (A05 and A03 currently read as ledges).
3. A03's left mouth arch: a plain square base, or tuck it into the terrain.
4. Gauge tower in the glide view: don't line its cap up under the moving platform.

## 4. Deliver (same folder conventions, suffix `49b-r2`)

- The same cameras and play captures as r1, near on and off.
- **Before and after crops of every join in section 1**, at 100%, at the user's two screenshot positions (A05 lower; A04 bottom, by the checkpoint).
- A greyscale version of each camera, and the value-gate table (unchanged rule).
- Same preservation, manifest, platform and "not tested" sections as before.

Stop for review.

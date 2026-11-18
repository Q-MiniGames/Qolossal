# Codex brief: Batch 50b-r5, Overgrown Nursery B04, readability pass

11 Nov 2026, Claude. The user played r4 and said "not that good". Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_114.md` first: in play, the background competes with the terrain. The paintings stay; this round fixes value, placement and QA coverage. Route names A-D; B04 = internal C04.

All earlier 50b rules still apply. Deliver into `Codex_v2_full\Batch50\50b-r5\`. Never overwrite earlier files. No scene edits, prototype folder only. Unity: if another session is using it, stop and ask the user.

**The user's two screenshots are the test cases.** Find their camera positions in the preview (screenshot 1: Qori on a cobbled floor beside a save shrine, a black gap between two walls; screenshot 2: Qori on a deck whose height matches the top edge of a long fern-covered masonry wall), record them, and include them in every check.

## Do

1. **Grade the background back.** Rear wall and SET layers at about 45-55% of their current brightness, contrast compressed by about a third, plus a slight cool haze toward the cave's shadow colour. Lantern, mint and lilac pools stay bright within their radii. Implement it as per-layer grade parameters in the preview shader, record them in `grade.json` for integration, and don't recolour PNGs. MID and FAR keep their relative order: FAR darkest.
2. **Band-contrast gate (new):** in a band from 1 u below to 3 u above every walk line, the background median luminance is at most 35% of the terrain median, on real captures.
3. **No deck-aligned edges:** no background boundary (painting top or bottom, wall top, masonry course, mask edge) within ±1 u of a deck's height for more than 3 u along that deck. Fix by moving placements, extending masonry upward, or breaking edges with growth and roots. Report every case found and how it was fixed.
4. **One continuous place:** blend the upper rock and FAR region into the lower walls (screenshot 2): continuous rock, roots crossing the boundary, the same grade.
5. **Close the black void strip** (screenshot 1) with an overlap or a buttress.
6. **Whole-room QA:** a camera grid every 4 u across the full camera clamp of B04, the B03-B04 and B04-B05 passages and B05's lower section, plus both screenshot positions. Every check runs on every grid camera: band contrast, deck-aligned edges, seams and voids, the value gate, the all-layer ledge sweep and the FAR median. Contact sheets at 1920 × 1080, grouped by area, with failures highlighted.

If 1 and 2 can't be met without the walls turning to mud, stop and say which vignettes need a lower-contrast repaint, with before and after examples.

## Deliver and stop

`grade.json`, `MANIFEST_50b-r5.json`, `QA_INDEX_50b-r5.html`, the contact sheets, both screenshot positions before and after, and `REVIEW_HANDOFF_50b-r5.md` (verdict per item; re-check the next free CLAUDE_REVIEW number; don't write a review). Leave the preview ready to play.

**Stop.** Claude reviews and the user plays.

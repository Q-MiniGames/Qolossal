Qolossal: Codex Batch 37c, the Ribwood (MR05) native 4K backgrounds, a fresh start on the Near. Stop once, at the end, for Claude's review.

Project (read only, never write under it): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch37c\
**Preserve Batch37 and Batch37b byte-identical.** Don't write into either; read them only.

READ FIRST
1. Tools\ArtRequests\CODEX_PROMPT_4K_BACKGROUNDS_ALL_REGIONS.md: sections 2 and 4-7, and MR05's row. That is the base brief; everything below overrides or adds to it.
2. Your Batch37 REVIEW_HANDOFF.md: why the Near failed (long narrow trunks; a wider bend that swelled into a joint-like knot).
3. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_90.md to 93: the passed Causeway and Qvale, nothing floating, no columns.
4. The in-game reference: Assets\Art\Codex\Backgrounds\Cradle4K\ and Causeway4K\ (accepted quality and placement).

KEEP
- **BG_Ribwood_Far_4K: copy the Batch37 Far byte-identical** (sha256 b1a17de96e772a4e4670e5aab65eb169a18f7d097471b5e835c72eb2326dd037). Re-verify its hash and wrap; don't repaint it.

MAKE (216 PPU, RGBA8 sRGB, bottom-centre pivot, Repeat U / Clamp V)
1. **BG_Ribwood_Near_4K, 3840x8192, depth 5, Near rate 0.963, its bottom edge at the frame's bottom at the start.** This is the plane that failed, so its rules are specific:
   - **Huge old forest trees seen from the side.** Clusters of two or three trunks of different girth, standing on a root-and-boulder floor that rises and falls along the bottom of the canvas. Above it, more trees stand on rock outcrops and broken stone at several heights. Every outcrop is grounded on a slope or root mass that reaches the canvas bottom.
   - **Trunks are thick and tapered**: 350-700 px at the base, narrowing steadily toward the crown, never parallel-sided over any 2160 px window. A tree is the one natural exception to the no-columns rule, but only when its trunk tapers.
   - **Bark, not flesh.** Bends are gentle and gradual, with no swelling, bulge or rounded knot at a bend. Branches leave in forks (Y shapes), never elbow or knee shapes. No smooth pale skin-like bark: furrowed bark, moss, lichen, ivy.
   - Broad, irregular canopies with gaps of sky between them. Branches may cross overhead between neighbouring trees.
   - At least 50% fully transparent in every 2160 px window, and no window empty. Never evenly spaced, never parallel, no repeated tree.
   - **The Ribwood's stone walls stay out of the Near.** No parallel walls or ribs in any plane.
   - Warm sunlit greens and bark browns, the nearest and warmest plane, with contrast at about 70% of the gameplay terrain's.
2. **BG_Ribwood_Mid_4K, 3840x2700, depth 10, its median skyline at rows 300-400.**
   - The forest beyond, side-on: a canopy line over a valley wall of mossy rock.
   - A few great stone walls are allowed here, but at different angles, heights and spacing, broken and overgrown, never two alike, never parallel.
   - Opaque below the skyline, softer than the Near.
3. **BG_Ribwood_High_4K, 3840x2160, depth 25:** mist over the forest canopy from above, with far peak tips. Alpha 0 over its lowest 30%; its mist colour matches the kept Far's lower mist (median RGB within 6).
4. **BG_Ribwood_Sky_4K, 3840x3240:** a soft gradient, warm low and paler high, as in the passed regions.

METHOD
- Layout first: a small wrap-ready full-width guide per plane. **For the Near, show the layout to yourself at 10% and blurred before painting; reject any trunk that reads as a limb or joint at that size.**
- Native 1:1 sections only (at least 25% overlap), never upscaled. One drawn full-resolution mask per plane, never generator alpha. Cyclic wrap, 0 at every row. No guide pixels in a master.
- Gates in order: Near (size, transparency per window, grounding marked, taper measured per trunk, mystery at full, 10% and blurred), then Mid, High, Sky, then the stack.
- If the Near fails after a fair second attempt, stop and deliver what you have, with the numbers.

PROOFS
- The climb strip at 0, 15, 30, 50, 75 and 100% of the 180 u climb, with and without MR05_mid_foreground_rgba.png (and MR05_low).
- Near window sheets with grounding and trunk-taper marks.
- 3x repeats with seam numbers; alpha over black, white and magenta; every join; the High/Far mist match.

DELIVERABLES
The four new PNGs plus the carried Far, MANIFEST.json, QA_INDEX.html and QA_SHEET.md, sources\, preservation receipt (including Batch37 and Batch37b unchanged), name-check log, and REVIEW_HANDOFF.md (every device "not tested"). Re-check the next free CLAUDE_REVIEW number; don't write a review.

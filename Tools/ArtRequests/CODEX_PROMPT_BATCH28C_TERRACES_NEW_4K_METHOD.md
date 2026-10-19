# Codex prompt: Batch 28c, new Terraces backgrounds at native 4K, with a fixed method (pilot)

Written 18 Oct 2026. The user picked option 1 of Review 78 ("lets go with 1"): retry with the method fixes. Not sent: the user sends it. Claude reviews the delivery as the next free CLAUDE_REVIEW number (79 on 18 Oct; re-check just before writing).

---

Qolossal: Codex Batch 28c (new Terraces backgrounds at native 4K, with a fixed painting method). Same two stages as Batch 28b: make BG_Terraces_Far_4K first and stop if it fails; otherwise continue to all five planes and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch28c\

READ FIRST
1. Tools\ArtRequests\CODEX_PROMPT_BATCH28B_TERRACES_NEW_4K_BACKGROUNDS.md. It is the request: the five planes, sizes, depths, travel, rules, proofs and deliverables all still apply, except where this prompt changes them.
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_78.md: why 28b's Far failed.
3. Your own Batch28b package: reuse its scripts and keep what worked (1:1 native sections, generous overlaps, no upscaling, the stop gate).

WHAT 28b GOT WRONG, AND THE REQUIRED METHOD NOW

1. **Layout first.** Before painting any section, make one small full-width layout of each plane: the whole 3840-wide composition at low resolution, for example 960 px wide.
   - It fixes where the skyline, peaks, slopes, valley and haze go.
   - Every section is painted from its window of that layout, so all sections agree on distance, scale and light.
   - The layout is a guide only. None of its pixels may reach the master, because that would be upscaling. Archive it.
   - Design the layout to wrap: its left and right edges continue into each other.

2. **Alpha from one drawn skyline, never from the generator.**
   - Paint every section fully opaque, with the sky area filled with flat sky colour.
   - Separately, draw one skyline mask for the whole width at full resolution: a single natural silhouette curve, derived from the layout and refined against the painted edge, equal at both ends.
   - Alpha = 255 under the curve and 0 above it, with at most a 2 px anti-aliased edge.
   - Below the skyline, nothing is transparent. Above it, nothing is opaque.
   - Remove fringes by decontaminating the edge colour against the sky fill.

3. **Cyclic wrap.** Paint the sections so that the last column continues from the first column's left edge (outpaint across the wrap), then blend that overlap like any other. Measure the wrap on the final master at the top, middle and bottom rows and at every row: the mean difference must be 0 after the final seam blend, and the skyline equal at both ends (±2 px).

4. **Depth sets the look. Far (depth 25) is soft:**
   - below the skyline, contrast and saturation fall to at most about half of the near plane's;
   - blue-grey haze increases toward the bottom, where the distance is greatest;
   - no individual bushes readable at 1:1 below the top third: fields, tree masses and rock faces as soft shapes;
   - detail density must not rise toward the bottom: report the luminance-gradient means per 540 px band, which should stay flat or fall.
   - Mid (depth 10) sits between: readable groves and walls, softer than Near. Near (depth 5) carries the sharpest detail and the warmest colour. High and Sky are soft by nature.

5. **Fewer, wider sections** where the tool allows, overlaps at least 25%, and every join shown in the QA sheet. Scale and distance must not drift between sections: compare the median tree and wall size per section and report it.

6. **No repeated motifs.** No parallel diagonal terrace bands marching across the canvas. Vary the slopes' direction and the spacing of walls and cypresses.

STAGE 1 PASS GATE (Far), all measured on the master:
- 3840x2160, RGBA8 sRGB.
- Alpha: the top row is all 0. Partial-alpha pixels only within 2 px of the skyline. Clean over black, white and magenta.
- Wrap: mean edge difference 0, derivative matched, skyline ends ±2 px.
- Haze and contrast as in rule 4, with the band numbers.
- No black, empty or wrong-sky patches anywhere.
- Mystery check passed (full, 10% and blurred).

If Far passes, continue with Mid, Near, High and Sky by the same method and deliver everything listed in the 28b prompt, including the climb strip with the gameplay matte. If Far fails, stop and deliver it with the numbers and what limits you.

# Codex brief: Batch 50b-r5b, three grade presets for the user to pick in play

12 Nov 2026, Claude. Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_115.md`. r5 fixed readability but went too dark (walk-band median 1-4% of the terrain). That was partly Claude's spec. This is a small preview change. Route names A-D; B04 = internal C04.

Everything from r5 stays: placements, buttresses, boundary blending, grade structure, no PNG changes. Deliver into `Codex_v2_full\Batch50\50b-r5b\`. Never overwrite earlier files. No scene edits. Unity: if another session is using it, stop and ask the user.

## Do

1. **Three presets in the preview, switched with F9**, with an on-screen label "Grade: Light / Medium / Dark" (preview only):
   - **Light:** walk-band background median about 28% of the terrain median.
   - **Medium:** about 20%.
   - **Dark:** about 12%.
   Tune each preset's rear/SET, MID and FAR brightness, contrast and haze to hit its target on the r5 350-camera grid. Keep the order FAR darkest, then MID, then rear/SET.
2. **Light must read as light in every preset:** each lantern, mint and lilac pool peaks at least 3× the surrounding unlit wall.
3. **Start in Medium.**
4. Record all three in `grade.json` (exact parameters and formula).

## QA, per preset

- Band contrast on all 350 grid cameras: median and worst case against the preset's target. The 35% ceiling still applies.
- Deck-aligned edges, the value gate, FAR median and the all-layer ledge sweep: re-run.
- Pool-to-wall ratio for every light pool.
- Contact sheets: the user's two cases and four representative grid cameras, with **Light, Medium and Dark side by side** at 1920 × 1080.

## Deliver and stop

`grade.json`, `QA_INDEX_50b-r5b.html`, the sheets, and `REVIEW_HANDOFF_50b-r5b.md`. Re-check the next free CLAUDE_REVIEW number; don't write a review. Leave the preview ready to play in Medium.

**Stop.** The user plays and picks a preset.

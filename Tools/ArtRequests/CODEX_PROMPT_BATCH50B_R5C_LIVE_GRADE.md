# Codex brief: Batch 50b-r5c, live brightness control and two fixes

12 Nov 2026, Claude. Read `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_116.md`. The presets didn't work, for two reasons Codex already found or showed. This replaces them with a live control the user tunes in play. Route names A-D; B04 = internal C04.

Everything else from r5/r5b stays: no PNG changes, the same placements, no scene edits. Deliver into `Codex_v2_full\Batch50\50b-r5c\`. Never overwrite earlier files. Unity: if another session is using it, stop and ask the user.

## Do

1. **Fix the legacy FAR passage backdrop** that breached the 35% ceiling in r5b: inside the slice, darken it to the Batch 50 FAR grade, or hide it where the new FAR covers it. Name the object and its source in the handoff.
2. **Replace the F9 presets with a live control:**
   - **F9 darker, F10 brighter**, in 5% steps of the rear/SET brightness. MID and FAR follow in proportion, and FAR stays darkest.
   - An on-screen label (preview only): "Brightness: N%   Walk-band median: M%", with M measured live on the current frame using the r5 method.
   - Start at the value where the 350-camera grid median is about 20%.
   - **F11** writes the current value and all derived parameters to `grade.json`, with a timestamp. That becomes the integration grade.
3. **Pools look like light on stone:** brighten and warm the underlying texture multiplicatively, with a soft falloff. No additive disc, and no visible disc edge. Peak at least 3× the surrounding unlit wall. Replace r5b's `poolGain` 8 approach.

## QA

- The r5 grid checks at the start value (band contrast, deck-aligned edges, value gate, FAR median, ledges).
- The full-grid median and worst case at three other values (start −20%, +20% and +40%), so the user knows the safe range. Any value where a band exceeds 35% is marked.
- Close-ups of all 10 light pools at the start value, beside their r4 versions.
- The user's two cases at the start value.

## Deliver and stop

`grade.json`, `QA_INDEX_50b-r5c.html`, and `REVIEW_HANDOFF_50b-r5c.md`. Re-check the next free CLAUDE_REVIEW number; don't write a review. Leave the preview at the start value.

**Stop.** The user plays, sets the brightness with F9 and F10, and presses F11.

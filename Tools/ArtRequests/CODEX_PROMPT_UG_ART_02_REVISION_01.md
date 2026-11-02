# Codex brief: underground art 02, revision 01

Date: 2 November 2026. I checked your Routes C, D and E kits. The technical work is good: colliders, import formats, scope audit and Route A all pass, and your report was honest about the weak spots. Keep every deck, crumble, mover, gate and seal exactly as they are.

Repaint 7 PNGs: `Solid_01.png` in C, D and E, `Thorns_01.png` in C, D and E, and `Rock_01.png` in D. Don't touch anything else. Everything in UG_ART_02's "Before you start" and "What you may and may not change" still applies, except that the scene backup goes in `Tools/IncomingArt/Codex_v2/UG_Art_02/revision_01/before/`.

## One code change, made by me

The small repeat on the solids was caused by my brief, not your painting. A canvas had to fit the shortest piece, which left only a 0.72 u repeat. `UndergroundArtKit.Dress` now treats only `deck`, `crumble` and `mover` as thin, so `solid`, `rock` and `thorns` fill their rect whatever the canvas size. This hasn't been built yet. Your first build tests it. If a short piece draws wrongly, report it and stop on that piece. Don't patch the code.

## Solid (C, D, E)

**The problem:** at room scale they read as woven fabric, wicker or carpet, with an obvious small pattern and vertical bands in E. They also don't belong to their rooms: Route C's is more orange than its terrain, and Route D's is sandy against lilac-grey terrain.

- Paint them as the same rock as the room's terrain, a little darker, so they read as heavy blocks standing in that room. Reference: `UG_Art_02/handheld/C_thorns.png`, `D_rock.png`, `E_thorns.png` for each route's terrain, and Route A's accepted `Solid_01.png` for how a block should sit.
- **Canvas 1024 x 1024,** with large, irregular rock forms: cracks and masses at least 1 to 3 u across, no small repeating motif. The repeat is now about 4.4 u.
- Keep `border` [24, 24, 24, 60] and overhang 0.125. Keep the clear pale top edge, which works well. Its visible top must sit within 6 px of pixel 27. E's is currently about 16 px too high.
- Route D keeps visible moss grip, because Qori can cling to it. C and E keep some moss or root grip.
- Check 3 x 1.2, 8 x 14 and 30 x 14 u. Neither the fine pattern nor vertical bands may show at 30 x 14.

## Rock (D)

**The problem:** it reads as polished marble and is the brightest thing in D06, so it looks unfinished and pulls the eye from the decks. Its top sits about 21 px above the walk line.

- Weathered bare limestone from the same family as Route D's terrain, in a mid value. Smooth and wind-worn, with no moss, so it stays clearly different from Route D's `solid`.
- Keep the 1296 x 240 canvas, `border` and overhang. Register the visible top at pixel 27, within 6 px. Check it at 7 x 1 and 10 x 1.5 u.

## Thorns (C, D, E)

**The problem:** at game scale they read as a mat of straw or twigs on the floor, and they're the same colour as the wood around them. This is the only piece here that can hurt the player, and it doesn't look like it.

- **Fewer, bigger spikes:** clear individual thorns with air between their tips, readable at 72 pixels per unit. Dark stems, pale sharp tips, and a strong contrast against both black and the route's floor. Keep the route's material, but keep the same silhouette language in all three.
- **Canvas 416 x 160, overhang 0.2407.** That's 108 px for the hazard's 0.5 u plus 52 px of tips above it. Keep side borders under 108 px in total.
- Route C's version must still tile as a 0.5 x 12 u column, with spikes visible on both sides.
- Check 1.5 x 0.5, 5 x 0.5, 34 x 0.5 and 0.5 x 12 u, plus 1280x720 crops beside Qori, in colour and greyscale. Thorns must be told apart from decks in greyscale at a glance.

## Deliver in `Tools/IncomingArt/Codex_v2/UG_Art_02/revision_01/`

- `CODEX_REVISION_01.md`: what changed, sizes, formats and memory per route, anything you couldn't check.
- Before and after images for each repainted kind, in the real rooms: C04, D05, D06, D08, E03, E05.
- The seam checks and crops above.
- A scene audit: changes only in Art transforms and SpriteRenderers on `solid`, `rock` and `thorns` in Routes C, D and E. The 17 other files in these kits and all of Route A must be byte-identical.
- **Platform QA:** as before. Don't claim any console, frame-rate or hardware result you didn't measure.

Then stop and wait for the user's verdict.

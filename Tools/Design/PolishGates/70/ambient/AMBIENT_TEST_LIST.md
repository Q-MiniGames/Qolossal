# Ambient life: what to try (WS5, first pass)

Nothing here has been played by a person. The check proves it runs and leaves gameplay alone, not that it feels right. It starts by itself in all 8 game scenes, with no scene edited, so just press Play.

## Try these

1. **Causeway or Terraces:** stand by the grass and flowers, then walk through them. They should sway in slow swells and part around Qori. Butterflies hover over the flowers. Birds cross the sky about once a minute, behind the far peaks.
2. **Ribwood:** falling leaves that fade where they land, spores, hanging roots moving, light shafts between the trees. Are the shafts strong enough? They barely show against the pale sky in my captures.
3. **Heights and Summit:** plants should lean harder than anywhere else, and dust should carry on the wind. These scenes aren't edited, so Codex's new backgrounds won't conflict.
4. **Qvale:** after Wick's rescue the lamps flicker with a warm glow. After Brannick's rescue the forge throws embers, and it should throw none while the forge is cold. Fireflies drift in the street. I haven't seen the lit lamps or the forge in a capture, because a fresh save has neither.
5. **Any rest shrine, surface or cave:** the flame glows and flickers, and small embers rise.
6. **Thorns, for example D03:** they creep slowly, shiver as Qori comes within about 3.5 u, and spark when he touches them. Do they still read as danger, and do they still look as far-reaching as they hurt?
7. **Water, for example A08 or E05:** the surface moves, glints come and go, soft light plays on the wall above, and there's a splash when Qori falls in. Falls and spills run downward.
8. **Gust lanes, for example A08, E05, PC12:** the streaks are faint in the calm, build in the warning and rush in the pulse. They blow toward the shelter the gust sends Qori back to. Can you read the warning better now?
9. **Caves in general:** dust in every cave, drips with a ripple on the floor, mist behind the terrain. Each route has its own mood.

## What to tell me

- **Stronger or weaker, per thing.** Each effect's strength is a single number, so this is quick to change. Surface dust is very faint on pale stone and may need to go up.
- **Birds, butterflies, splashes (Ludo):** sparrows sit by the plants and fly off as you come near. Do they match the painted world? Butterflies and far birds use the same art. Splashes run at the foot of falls and spills.
- **Performance:** does anything hitch? The layer's own script costs under 0.05 ms a frame in the editor, but draw cost and a real build are not measured.

## Not built yet

- Footprints and surface-specific dust.
- Sparks when the staff hits thorns. Thorns aren't attack targets, and making them targets would change combat.
- Cloud drift on the sky paintings themselves. Soft wisps drift over the sky instead.
- "The breath", the optional signature idea. It waits for your decision, because it must not give the titan away.
- Sound. It's deferred to the later sound set.
- Console tuning. Tiers 1 and 2 start and spend less, but no console has been measured, so nothing here says Series S or Switch 2 are ready.

## Rerun the check

`-executeMethod AmbientLifeCheck.Run -saveFile ambient_check.json -captureDir Tools/Design/PolishGates/70/ambient/film [-mraOnly Underground]` (no `-quit`, exits 1 on failure).

If you change the art list: `AmbientKitBuilder.Build`.

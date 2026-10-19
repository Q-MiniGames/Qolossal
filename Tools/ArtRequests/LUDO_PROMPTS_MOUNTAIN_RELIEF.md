# Ludo requests: Mountain Relief Atlas (17 Oct 2026)

Source: every art asset the eight region scenes reference (287 files, `Assets/MountainReliefAtlas/Scenes/MRAtlas_MR0*.unity`), checked against the code that draws them.

## Already moving, nothing to request
- **Qori:** the Ludo clips already under way (`LUDO_PROMPTS_QORI_MOVEMENT.md`).
- **The seven regular enemies** (Thornwing, Pod Spitter, Shellback, Ripple Newt, Grub, Gust Moth, Sentinel): their Ludo frames already exist. They are still test-only (`EnemyFramesOnCreature`); the levels draw the cutout rigs, so this is an import job, not a Ludo job.
- **Thornheart:** prompts are ready (`LUDO_PROMPTS_THORNHEART.md`).
- **Code handles these:** Mill_Wheel (spins), Wakeknot_Glow and the street lamp glow (pulse), Sluice_Gate and Switch_Plate (open/closed swap), pickups and relics (bob).

## Request from Ludo, most visible first

| # | Asset (file) | Where | Now | Prompt |
|---|---|---|---|---|
| L1 | Deep water (`Water_Body`) | Side chambers | A still picture; it only fades when drained | `Calm dark mountain water surface, gentle looping ripples and small light glints, flat side view, seamless loop` |
| L2 | Warm-air vent (`Breath_Vent_Exhale`, `FX_Breath_Current`) | The Drift | A still placeholder | `A stone vent in the ground puffs warm air upward in soft rhythmic gusts with drifting leaves, side view, seamless loop` |
| L3 | Townsfolk: Smith, Farmer, Herbalist, Keeper, Musician, Elder, Child (`Town_*_Body/Head/Arm*`) | Qvale | Cutout rigs | One idle per person, from their full-body picture: smith hammering at the anvil, musician playing, farmer leaning on a hoe, herbalist sorting leaves, keeper tending a lantern, elder resting on a staff, child bouncing on their heels. `<role> idle, small breathing motion, side view, seamless loop` |
| L4 | Scribble the mapmaker (`Scribble_*`, `Scribble_MapRoll`) | Qvale | Cutout rig | `Small mapmaker idles, then unrolls a paper map and looks at it, side view` (cut into idle + map) |
| L5 | Knucklebramble guardian | Grip knot fight | Cutout rig | Same shape as the Thornheart set: idle/roar, arm slam, hurt/death |
| L6 | Root gate (`Gate_Root_Closed_*` → `Gate_Root_Open_*`) | All regions | Closed/open swap | `A gate of thick tree roots slowly untangles and pulls apart to open a passage, side view` |
| L7 | Wakeknot waking (`Wakeknot_Dormant` → `Wakeknot_Awake`) | Each region's knot | Swap plus a glow pulse | `A dormant knot of roots slowly wakes, glowing mint green as it loosens, side view` |
| L8 | Checkpoint shrine lighting (`Checkpoint_Shrine_A1`-`A6`, `Waymark_Lit`) | All regions | Swap | `A small stone shrine lights up, a warm flame flickers to life and keeps flickering, side view, loop the flame` |
| L9 | Seed pod (`Carrier_SeedPod_A2`) | Chambers | Still picture | `A seed pod sways and pulses gently, then bursts open, side view` |

Notes
- Ludo is at its best on figures. L1 and L2 are textures and puffs; if Ludo's result doesn't loop cleanly, a scrolling ripple in code costs nothing (the main game's `ScrollingStrip` already does this for its water).
- L3 to L5 replace rigs, so each one needs an in-game test like the Qori clips did.
- Grass, banners and hanging ivy on the decor sheets can sway with a shader; no Ludo needed.

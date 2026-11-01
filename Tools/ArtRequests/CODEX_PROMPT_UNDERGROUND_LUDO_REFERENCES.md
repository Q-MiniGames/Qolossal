# Codex brief: Ludo reference images for the underground creatures

Date: 31 October 2026. The user approved the 18 underground creature designs (concept sheets in
`Tools/IncomingArt/Ludo/EnemyConcepts/Underground_20261031/`) and tested them as stand-ins in game. Their
animation will be generated with Ludo.ai, which animates from **one** image. This pass makes that image for each creature.

## Read first

- The concept sheets and `designs.json` in the folder above (identity, scale, poses, weak points).
- `Tools/ArtRequests/LUDO_PROMPTS_THORNHEART.md`, "Switched to the wide model": what went wrong with Ludo before.
  Ludo mixes views if the image has more than one, and it reinvents limbs it can't see clearly.

## What every reference image must be

- **One creature, one view: a clean side view facing right.** No turnarounds, insets, labels, arrows, scale bars, Qori, text or sound words.
- **Neutral, ready pose**: the creature's idle stance, weight settled, able to move into its attack. Not mid-attack.
- **Every limb, wing, claw, root and tail clearly separated** from the body and from each other, with no overlaps that hide a joint. Ludo can only animate what it can see.
- **Weak points visible but closed** (the glowing heart, valve, bloom, eye or belly), so Ludo can open them.
- **Background:** a transparent PNG; if transparency fails, use flat mid-grey (#808080) with no gradient, floor, shadow or texture.
- **Framing:** the whole body inside the image with about 10% margin on every side; nothing cropped (wings and tails included). Feet or base on a common baseline for walkers; flyers centred.
- **Size:** 1024 x 1024 px, the creature filling about 75% of the longer side. Same lighting from the upper left on every creature, soft and even, no strong cast shadows.
- **Style:** match the approved concept sheet exactly: same shapes, proportions, materials and the existing painted style. This is a clean-up and re-pose, not a redesign.

## Two fixes from the concept review (the user can strike these before you start)

1. **Route palettes**, keeping the forms:
   - Cistern, A (E-10 to E-12, G-03): wet stone and teal moss.
   - Root Gardens, C (E-13 to E-15, G-04): green with bloom pink.
   - Wind, D (E-16 to E-18, G-05): pale, cold silver-green.
   - Descent, E (E-19, E-20): dry, wilted gold.
   - Caves (E-21, M-01 to M-03): keep their own region's colours.
2. **Look-alike pairs:** make the Seed Swarm (E-14) and the Gale Wisp (E-17) clearly different in outline. Do the same for the Sluice Crab (E-11) and the Bramble Creeper (E-15).

## The images (18)

Save as `Tools/IncomingArt/Ludo/Underground/<ID>_<Name>_ForLudo.png`, plus `_Contact.jpg` and a `MANIFEST.json` with each file's size and hash.

| ID | Creature | Pose to draw | Must be clearly visible |
|---|---|---|---|
| E-10 | Silt Leech | Hanging straight down from a short ceiling stub (stub drawn as a plain dark bar at the top edge, nothing else) | The mouth disc and the body segments |
| E-11 | Sluice Crab | Standing low, claws open and lowered | Both claws, every leg, the soft membrane under the shell |
| E-12 | Pump Bloater | Floating, nozzle pointing right, half full | The water-filled body, the nozzle, the hanging roots |
| E-13 | Root Snapper | Rooted, jaws closed, stem upright | The jaw seam, the stem, the root base |
| E-14 | Seed Swarm | The glowing core with 12 to 20 seeds spaced loosely around it | Each seed separate; the core |
| E-15 | Bramble Creeper | Walking, uncurled | The legs, the head, the shell bands (they will curl) |
| E-16 | Kite Ray | Gliding, wings spread level | Both wingtips, the tail |
| E-17 | Gale Wisp | Hovering, leaves loosely open | The eye and the spiral of leaves |
| E-18 | Hail Sentry | Perched on a low plain stone block, beak closed | The beak, the wings folded, the crest |
| E-19 | Hollow Shade | Not needed: made in game from Qori's own frames, tinted and faded | (skip) |
| E-20 | Wilted Husk | Dormant, drooping, head down | The head, the leaf arms, the root feet |
| E-21 | Lantern Moth | Resting, wings half open | Both wings, the glowing abdomen |
| G-03 | Sluice Warden | Standing, both claws lowered, shell closed | Both claws, the legs, the valve heart on the back (closed) |
| G-04 | Root Matron | Upright, root arms down and apart | Each root arm, the closed bloom |
| G-05 | Tempest Owl | Standing on the ground, wings half spread | Both wings and the talons; the chest eye (closed) |
| M-01 | Moss Matriarch | Standing on all fours | The arms, the claws, the moss mantle, the belly seed |
| M-02 | Echo Warden | Standing, hammer arm lowered | The bell head and the clapper, both arms |
| M-03 | Grain Golem | Standing, fists down | The fists, the millstone core (closed) |

## Then stop

Report which images departed from the concept sheet and why. Stop for the user's review: no Ludo runs, sprites, rigs or Unity changes in this pass.

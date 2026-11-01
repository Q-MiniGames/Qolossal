# Codex brief: underground enemies and bosses (design pass 1)

Date: 31 October 2026. Requested by the user: more enemies and bosses for the underground, designed to each route's theme.
This pass is **concept design only**: a concept sheet per creature for the user's review. No animation, rigs or Unity changes until the user approves a design.

## Read first

- `CLAUDE.md`, `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`, `QOLOSSAL_SESSION_HANDOFF.md`.
- The greybox underground: `Tools/Design/UndergroundPlatforming_20261030/` (captures in `captures/`, room notes in `layouts.py`). Every room's size, hazards and skills are there; design creatures that fit those rooms.
- The existing enemy concepts, for style and scale: `Tools/IncomingArt/Ludo/EnemyConcepts/` (18 images + `_Contact.jpg`).

## Fixed rules (gameplay)

- Scale: 216 px per unit. Qori's body is 0.8 x 1.2 u. Jump rise 2.39 u, double jump 4.39 u; running jump clears 6.1 u (10.1 with the double jump).
- Rooms are 12 to 72 u wide and 8 to 66 u tall; ceilings in corridors can be as low as 8 u. Regular enemies: 0.6 to 2.5 u tall. Mini-bosses: up to 4 u. Guardians: 4 to 7 u, fitting their arena with at least 2.4 u of headroom over Qori everywhere he must stand.
- Every attack is telegraphed (a pose plus a glint or sound) at least 0.5 s before it can hurt (0.35 s in a boss's last phase). Contact costs one heart; nothing kills outright.
- Each new enemy has a **counter** that uses a skill the player already has on that route (below), plus an ordinary staff hit.
- No enemy blocks a required jump: they are placed on landings, corridors and arenas, never mid-gap.
- Bosses: 3 phases with a readable change between them, a weak point that opens after an attack (the house pattern from the Knucklebramble and the Thornheart), 9 to 12 hits total.

Skills by route: A (Cistern) run, jump, double jump, staff, Leaf Bolt. C (Root Gardens) + Climbing Moss, Living Thread, Bloomfall. D (Wind) + Wind Leaf dash, Glidecap. E (Descent) everything.

## Existing creatures (keep; don't redesign)

Grub (E-06), Ripple Newt (E-07), Shellback (E-05), Pod Spitter (E-04), Bark Sentinel (E-08), Thornwing (E-03), Gust Moth (E-09), Crawler; bosses Knucklebramble guardian and Thornheart.

## New regular enemies (E-10 to E-21)

| ID | Name (working) | Route / theme | Behaviour | Counter |
|---|---|---|---|---|
| E-10 | Silt Leech | A, water works | Hangs from ceilings over water, drops when Qori passes under, then crawls back up the wall. | Bait the drop, hit while it climbs; Leaf Bolt from range. |
| E-11 | Sluice Crab | A | Scuttles along decks sideways; closes its claws as a shield when faced. | Double-jump over and strike from behind. |
| E-12 | Pump Bloater | A, pistons | Floats in shafts, puffs a water jet that pushes (no damage) then needs to refill. | Strike during the refill. |
| E-13 | Root Snapper | C, gardens | A flytrap rooted beside thread anchors; snaps at anything swinging past its mouth. | Time the swing; Bloomfall pogo on its closed head. |
| E-14 | Seed Swarm | C | A slow cloud of drifting seeds that follows Qori and stings. | Bloomfall burst or a charged sweep scatters it for a while. |
| E-15 | Bramble Creeper | C, moss walls | Crawls along walls and ceilings on moss, curls into a thorn ball and rolls down. | Hit while it uncurls; the moss wall jump avoids the roll. |
| E-16 | Kite Ray | D, wind | Rides gust lanes in loops; contact hurts. | Dash through its gap; strike when it turns. |
| E-17 | Gale Wisp | D | Inhales, pulling Qori toward it (no damage), then exhales a short push. | Wind Leaf dash breaks the pull; hit it when it's exhaling. |
| E-18 | Hail Sentry | D, cold heights | A perched sentry that fires ice shards along the wind. | Glide under the arc; Leaf Bolt. |
| E-19 | Hollow Shade | E, memory | A faint copy of Qori that repeats his last 2 s of moves; touching it hurts. | Change rhythm; it fades when struck. |
| E-20 | Wilted Husk | E | Dormant flower husks that wake one by one as Qori passes, then lunge once. | Keep moving; one hit each. |
| E-21 | Lantern Moth | caves (MR01-MR07), all | Harmless glow moths that light dark caves and scatter; a living landmark, not a threat. | None needed. |

## New bosses

| ID | Name (working) | Where | Size | Idea |
|---|---|---|---|---|
| G-03 | Sluice Warden | A08 Sluice Guardian Arena (52 x 28 u, flat floor, two 6 u ledges at 4 u) | ~6 u | A great stone crustacean wedged in the sluice. Phase 1 claw sweeps; phase 2 floods the arena floor in pulses (Qori takes the ledges); phase 3 both. Weak point: the valve heart on its back, open after a sweep. |
| G-04 | Root Matron | C08 Root Guardian Arena (40 x 28 u) | ~6.5 u | A rooted mother-plant. Thread anchors in the arena: she lashes roots along the floor, and Qori swings over them; phase 3 she seeds Snappers on the anchors. Weak point: the bloom that opens after a lash, best hit with Bloomfall. |
| G-05 | Tempest Owl | D08 Wind Guardian Arena (40 x 28 u) | ~5 u (wingspan larger) | A storm owl that toggles the arena's gust lanes. Glide and dash play: phase 1 wing gusts; phase 2 dive bombs; phase 3 the lanes flip direction. Weak point: the eye of the storm on its chest, open after a dive. |
| M-01 | Moss Matriarch | Cradle cave network (MR01) | ~3.5 u | A moss-covered sloth on the walls; climbs and drops. Mini-boss in the Moss Pocket (MR01_C02, 40 x 24 u). |
| M-02 | Echo Warden | Causeway cave network (MR02) | ~3.5 u | A bell-headed guard whose ring shows where its next strike will land. Mini-boss in the Echo Vault (MR02_C03, 36 x 22 u). |
| M-03 | Grain Golem | Terraces cave network (MR03) | ~4 u | Mill-stone body, sheds crumbling grain platforms as it moves. Mini-boss in the Mill Undercroft (MR03_C04, 48 x 26 u). |

The Knucklebramble and the Thornheart stay available; the user may still place them instead of G-03 to G-05.

## Deliverables (this pass)

For each creature, one concept sheet: full body at game scale against Qori, the idle, the telegraph pose, the attack pose, the vulnerable pose (bosses: one per phase), and a one-paragraph note. Keep the existing enemy concepts' painted style. Put them in `Tools/IncomingArt/Ludo/EnemyConcepts/Underground_20261031/` with a contact sheet, and report which designs depart from this brief and why. Then stop for the user's review.

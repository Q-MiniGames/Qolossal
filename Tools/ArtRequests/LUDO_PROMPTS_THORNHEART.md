# Ludo prompts: the Thornheart (final boss)

Reference image: `Tools/IncomingArt/Ludo/Thornheart/Thornheart_Side_ForLudo.png` (the side view cropped from the three-view banner, 14 Oct). Upload this crop, not the banner, or Ludo mixes the three views. It faces right; the game flips it to face Qori.

4 clips = 36 credits. Each chains two moves, and I cut them apart.

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| T1 | Idle and roar | `Thornheart breathes slowly with its green heart glowing, then rears up and roars, side view facing right` | Idle loop, roar (phase change, brood summon) |
| T2 | Tail slam | `Thornheart lifts its thorn tail high over its back, then slams it down in front of its head, side view facing right` | Tail wind-up, slam, recovery |
| T3 | Charge | `Thornheart lowers its head and charges forward, then skids to a stop, side view facing right` | Charge loop, skid stop |
| T4 | Hurt and death | `Thornheart flinches back as its green heart flashes, then slowly collapses to the ground, side view facing right` | Hurt flinch, death |

## Checked (14 Oct)

- **T1 kept.** Idle frames 0-11 (heart lights up), roar 12-29, settle 30-35. The tail tip opens as a jaw and a clawed arm reaches out in the roar, which reads well.
- **T2 redo.** Tail lift (0-20) is fine, but at frame 22 the body shrinks and the tail coils back over itself; it never strikes in front. Redo prompt: `Thornheart swings its thorn tail forward over its head and smashes the tip into the ground in front of it, side view facing right`
- **T2 redo kept (`Thornheart-tail slam (redo)`).** Lift 6-14, strike 15-16, impact with dust 16-26, return 27-35. The tail is too short to reach past the head, so it hits the ground at the front feet. In game the impact sends a thorn shockwave rolling forward along the ground for reach. The pale blue dust in 28-33 gets erased in cleanup.

## Switched to the wide model (v4, 15 Oct)

Reference: `Tools/IncomingArt/Ludo/Thornheart/Thornheart_v4_Wide_ForLudo.png`. Longer reach, heart exposed in the middle. The old-model clips are kept as a fallback only.

- **v4-1 (claw smash test) kept in part.** Frames 0-11: idle, heart lights up, limbs stay clean. 12-25: the right claw rises and opens like a jaw, which doubles as the wind-up and the roar. Frame 26 pops: the claw vanishes, the body drops into a crouch and an extra clawed limb appears across the front; 26-35 are not used.
- **The slam is made in engine:** hold the raised claw (25), a fast swing smear down to the ground in front of the head, the impact dust and shockwave, then frames 25 back to 12 played in reverse to lift it. This avoids another redo, since Ludo keeps popping on "smash".
- **v4-3 (charge) redo.** At frame 7 it turns into a different, sleeker creature: the overhead tail and the back claw are gone. The run loop (7-23) and skid (24-35) are smooth, but the shape doesn't match the idle. Redo prompt, one move only (the skid stop is done in engine): `Thornheart scuttles forward fast on its claws, tail curled high over its back, side view facing right`
- **v4-3 (redo) not used.** The tail stays up, but from frame 0 it is a different body (scorpion tail at the back, extra legs, no back claw), and an extra arm reaches out in 14-20. Ludo reinvents the body for any big locomotion, so the charge is built in engine instead: the v4 idle pose leans forward and slides fast with dust and a body bob, after the claw wind-up as the warning. No more credits on the charge.
- **v4-4 (hurt and death) kept.** Frames 0-11 are a strong hurt flash (the heart bursts with light). From 12 the overhead tail is lost as it slumps; the collapse (12-35) is used for death only, and the change is covered by a crossfade and the dying flash.
If a clip turns the body toward the camera, redo just that clip with "stays in side view" added.

## How the fight uses them

The design (`Tools/Design/QOLOSSAL_WORLD_DESIGN.md`) has three phases on pulse bridges and ends with Qori cutting out the heart. The heart is the weak point:

- **Tail slam:** the long wind-up is the warning. The slam hits the ground at its front feet and sends a thorn shockwave rolling forward (jump it), then the tail stays down for a moment, and Qori can climb or hit it.
- **Charge:** it crosses the arena. Qori jumps or dashes over it, and the skid leaves its side open.
- **Heart window:** after a slam or skid the heart glows brighter, and only hits there do real damage. The hurt flinch plays on each heart hit.
- **Phases:** the roar marks each phase change and calls the brood (Bramble Crawlers, Thornwings). Phase two chains slam into charge; phase three is faster with shorter windows.
- **End:** the death clip, then the existing "cut out the Thornheart" ending.

# Ludo prompts: underground creatures (31 Oct 2026)

Reference images: upload `Tools/IncomingArt/Ludo/Underground/ForLudo_Padded/<ID>_<Name>_ForLudo.png`. These are Codex's
images (`CODEX_PROMPT_UNDERGROUND_LUDO_REFERENCES.md`, delivered 31 Oct) on a larger transparent canvas, with the creature at about 70%
so wings and arms have room to move; the art itself is unchanged (originals one folder up).
Face changes from the user (31 Oct): Kite Ray, pointed leaf snout and narrow teal eye; Gale Wisp, eye recessed within leaf
plates; Lantern Moth, compound eye and coiled proboscis. Upload the creature's own image for each of its clips. Each faces
right; the game flips it.

**How these work** (from the Qori, enemy and Thornheart runs):
- A clip is 3 s = 36 frames = 9 credits.
- Most clips chain two moves; I cut them apart afterwards.
- Ludo redraws the body during big moves (runs, charges, dives, rolls), so those are made in engine from a pose instead. They're marked *engine* below.
- Every prompt ends with `, side view facing right`. If a clip turns toward the camera, redo just that clip with `, stays in side view` added.
- Save the frames as transparent PNGs in `Tools/IncomingArt/Ludo/Underground/<ID> - <clip name>/`.

## Order (most needed first)

1. The three guardians (G-03 to G-05): they gate each route.
2. Route A, C and D regulars.
3. The mini-bosses (M-01 to M-03).
4. E-20, E-21.

Total: 58 clips = 522 credits. Regulars come to 31 clips (279 credits), bosses and mini-bosses to 27 clips (243 credits). Start with the guardians and check each clip before spending more.

## Guardians (5 clips each, 135 credits)

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| G3-1 | Idle and roar | `Sluice Warden breathes slowly with its stone shell rising and falling, then rears up on its back legs and roars with both claws raised, side view facing right` | Idle loop; phase-change roar |
| G3-2 | Claw sweep | `Sluice Warden raises its right claw high, then sweeps it low and fast along the ground in front of it, then pulls it back, side view facing right` | Warning, sweep, recovery |
| G3-3 | Flood slam | `Sluice Warden lifts both claws over its head and slams them down onto the ground, then holds still, side view facing right` | Flood warning and slam (the water is made in engine) |
| G3-4 | Valve open | `Sluice Warden slumps forward tired as the glowing valve on its back opens and pulses, then closes it and stands up, side view facing right` | Weak point open, close |
| G3-5 | Hurt and death | `Sluice Warden flinches back as its valve flashes bright, then slowly collapses onto its side and goes still, side view facing right` | Hurt; death |
| G4-1 | Idle and roar | `Root Matron sways gently with her root arms swaying, then throws her arms wide and lets out a cry as her bloom shivers, side view facing right` | Idle; roar |
| G4-2 | Root lash | `Root Matron raises one root arm high, then whips it down and along the ground in front of her, then draws it back, side view facing right` | Warning, lash, recovery |
| G4-3 | Seed fling | `Root Matron shakes her whole body and flings a spray of seeds up into the air, then settles, side view facing right` | Seed cast (the Snapper seeds are made in engine) |
| G4-4 | Bloom open | `Root Matron bows forward as the bloom on her chest opens wide and glows, then closes it and straightens, side view facing right` | Weak point open, close |
| G4-5 | Hurt and death | `Root Matron recoils as her bloom flashes bright, then wilts and sinks to the ground, petals falling, side view facing right` | Hurt; death |
| G5-1 | Idle and screech | `Tempest Owl stands with wings half spread, ruffling its feathers, then spreads its wings wide and screeches, side view facing right` | Idle; phase screech |
| G5-2 | Wing gust | `Tempest Owl draws both wings back, then beats them forward hard in one great gust, then folds them halfway, side view facing right` | Warning, gust, recovery |
| G5-3 | Dive wind-up | `Tempest Owl crouches low with wings raised high above its back, then leans forward ready to launch, side view facing right` | Dive warning (the dive itself is *engine*: this pose, swept) |
| G5-4 | Eye open | `Tempest Owl lands heavily and staggers as the glowing eye on its chest opens and pulses, then closes it and stands tall, side view facing right` | Weak point open, close |
| G5-5 | Hurt and death | `Tempest Owl jolts back as its chest eye flashes, then falls onto its side with wings spread limp, side view facing right` | Hurt; death |

## Regular enemies (3 clips each unless noted)

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| E10-1 | Hang and drop | `Silt Leech hangs from the ceiling swaying slowly, then contracts and lets go, falling straight down, side view facing right` | Idle hang, warning, drop |
| E10-2 | Crawl | `Silt Leech crawls forward along the ground by bunching and stretching its body, seamless loop, side view facing right` | Crawl (wall climbs rotate it in engine) |
| E10-3 | Hurt and death | `Silt Leech flinches and curls, then goes limp and flat on the ground, side view facing right` | Hurt; death |
| E11-1 | Scuttle and guard | `Sluice Crab scuttles forward on its legs, then stops and raises both claws together in front of its face like a shield, side view facing right` | Walk; guard |
| E11-2 | Claw snap | `Sluice Crab lowers its claws, snaps them forward in a quick low pinch, then sags back panting with its soft underside showing, side view facing right` | Warning, attack, vulnerable |
| E11-3 | Hurt and death | `Sluice Crab jolts back, then flips over onto its shell with legs still, side view facing right` | Hurt; death |
| E12-1 | Float and swell | `Pump Bloater floats bobbing gently, then swells up round and full with its nozzle trembling, side view facing right` | Idle; warning |
| E12-2 | Jet and refill | `Pump Bloater blasts a strong water jet from its nozzle, shrinks limp and wrinkled, then slowly fills back up, side view facing right` | Jet; refill (vulnerable) |
| E12-3 | Hurt and death | `Pump Bloater squashes as it is hit, then deflates and sinks to the ground, side view facing right` | Hurt; death |
| E13-1 | Sway and gape | `Root Snapper sways on its stem, then opens its jaws wide, glowing inside, side view facing right` | Idle; warning |
| E13-2 | Snap | `Root Snapper lunges forward and snaps its jaws shut, then droops down with jaws closed, side view facing right` | Bite; vulnerable droop |
| E13-3 | Hurt and death | `Root Snapper recoils, then wilts and collapses onto its roots, side view facing right` | Hurt; death |
| E14-1 | Drift and sting | `Seed Swarm drifts and swirls slowly around its glowing core, then pulls in tight and bursts outward in a pulse, side view facing right` | Idle; warning and sting |
| E14-2 | Scatter | `Seed Swarm is struck and its seeds scatter far apart, then slowly drift back together, side view facing right` | Scatter (vulnerable); death plays the scatter without the return. 2 clips only |
| E15-1 | Crawl | `Bramble Creeper crawls forward slowly on its short legs, seamless loop, side view facing right` | Walk |
| E15-2 | Curl and uncurl | `Bramble Creeper tucks its head and curls into a tight thorny ball, then uncurls and lies belly up, side view facing right` | Warning (curl); vulnerable (the roll is *engine*: the ball spun) |
| E15-3 | Hurt and death | `Bramble Creeper flinches, then rolls onto its back and goes still, side view facing right` | Hurt; death |
| E16-1 | Glide | `Kite Ray glides forward with slow, wide wing beats, seamless loop, side view facing right` | Glide loop |
| E16-2 | Bank and swoop | `Kite Ray tilts its wings and swoops down, then flattens out level again, side view facing right` | Swoop (the lane path is *engine*) |
| E16-3 | Hurt and death | `Kite Ray jolts in the air, then folds its wings and drops, side view facing right` | Hurt; death |
| E17-1 | Spin and inhale | `Gale Wisp spins slowly in place, then swells as its leaves draw inward in a strong inhale, side view facing right` | Idle; pull |
| E17-2 | Exhale | `Gale Wisp blows out a strong gust, then droops limp with its leaves hanging loose, side view facing right` | Push; vulnerable |
| E17-3 | Hurt and death | `Gale Wisp shudders, then its leaves come apart and drift away, side view facing right` | Hurt; death |
| E18-1 | Perch and rear | `Hail Sentry sits on its stone perch looking around, then rears its head back with beak open, side view facing right` | Idle; warning |
| E18-2 | Spit | `Hail Sentry snaps its head forward and spits a shard of ice, then lowers its head and shakes it, side view facing right` | Shot (the shard is *engine*); recovery |
| E18-3 | Hurt and death | `Hail Sentry flinches with feathers ruffled, then topples off its perch, side view facing right` | Hurt; death |
| E20-1 | Wake | `Wilted Husk droops still, then shudders awake and lifts its head, side view facing right` | Dormant; warning |
| E20-2 | Lunge | `Wilted Husk lunges forward once with its leaf arms out, then collapses open on the ground, side view facing right` | Lunge; vulnerable |
| E20-3 | Crumble | `Wilted Husk crumbles into dry petals and dust, side view facing right` | Death |
| E21-1 | Rest and rise | `Lantern Moth rests glowing softly, then flutters its wings and lifts off, side view facing right` | Idle; aware |
| E21-2 | Flight | `Lantern Moth flutters forward in gentle flight, glowing, seamless loop, side view facing right` | Scatter flight. 2 clips only; it never dies |

E-19 Hollow Shade needs no Ludo clips: it replays Qori's own frames, tinted and faded.

## Mini-bosses (4 clips each, 108 credits)

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| M1-1 | Idle and roar | `Moss Matriarch sways on all fours, then rises up and roars, side view facing right` | Idle; roar |
| M1-2 | Swipe | `Moss Matriarch rears back one clawed arm, then swipes it forward low, then returns to all fours, side view facing right` | Warning, swipe, recovery |
| M1-3 | Belly open | `Moss Matriarch sits back tired as the glowing seed in her belly shows, then hunches forward to cover it, side view facing right` | Vulnerable (the wall drop is *engine*) |
| M1-4 | Hurt and death | `Moss Matriarch flinches, then slumps down and goes still, moss spilling off her back, side view facing right` | Hurt; death |
| M2-1 | Idle and ring | `Echo Warden stands still, then its bell head rings, swinging the clapper, side view facing right` | Idle; ring (warning) |
| M2-2 | Hammer slam | `Echo Warden raises its hammer arm high, then slams it down on the ground, then lifts it, side view facing right` | Warning, slam, recovery |
| M2-3 | Bell open | `Echo Warden staggers as its bell head tilts open showing the glowing clapper, then closes, side view facing right` | Vulnerable |
| M2-4 | Hurt and death | `Echo Warden jolts as its bell cracks, then falls forward and lies still, side view facing right` | Hurt; death |
| M3-1 | Idle and grind | `Grain Golem stands grinding its millstone core slowly, then shakes and sheds a shower of grain stones, side view facing right` | Idle; shed (the platforms it sheds are *engine*) |
| M3-2 | Fist slam | `Grain Golem raises both fists, then slams them into the ground, then straightens, side view facing right` | Warning, slam, recovery |
| M3-3 | Core open | `Grain Golem kneels as its millstone core opens and glows, then closes and stands, side view facing right` | Vulnerable |
| M3-4 | Hurt and death | `Grain Golem cracks as it is hit, then breaks apart into stones, side view facing right` | Hurt; death |

## After each clip

Tell me the clip names; I check the frames, cut them, set them up and swap the stand-in in the scene. Redos go in a dated "Checked" section at the end of this file, like the Thornheart's.

## Checked

- **G4-1 (31 Oct) kept, the test clip, generated by Claude through Chrome.** Frames in `Tools/IncomingArt/Ludo/Underground/G-04 - Idle and roar/` (36 frames, 684 x 440). Idle sway 0-16, the arms thrown wide with the bloom showing (the roar) about 20-28, back to idle 32-35. The body stays the same creature throughout; the padded reference worked (nothing cropped).
- **All 58 clips plus one redo generated (31 Oct-1 Nov), by Claude through Chrome.** 531 credits in all (59 clips x 9). Chrome blocked the frame downloads after the first two, so the frames come from each clip's 6 x 6 sprite sheet on Ludo's storage, cut by `Tools/ArtRequests/LudoRun/sheets_underground.py` into `<clip folder>/frames/` (the sheet's URL is kept in `sheet_url.txt`). Review strips: `Tools/IncomingArt/Ludo/Underground/_review_0..3.jpg`.
- **G3-1 replaced by `G-03 - Idle and roar (redo)`:** the first roar turned the Warden into an upright creature with a fanged mouth. The redo prompt adds "stays low on all its legs ... keeping the same body and shell", and it keeps the body.
- **Use part of the clip:** G5-2 wing gust and G5-3 dive wind-up (the owl shrinks in the last third: use up to about frame 24); M2-2 hammer slam (Ludo grows a hammer in the hand and the body draws smaller: check scale when registering); E10-2 crawl (keeps a small piece of the ceiling bar from the reference: erase in cleanup).
- **Kept as generated:** everything else. Ludo flagged many clips "bad loop" or "out of frame"; the loops are cut by hand in export, and nothing is cropped in the review strips.

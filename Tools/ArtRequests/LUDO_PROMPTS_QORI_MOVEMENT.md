# Ludo prompts: Qori movement animations (combined, 11 clips)

Each Ludo clip is 3 seconds = 36 frames and costs 9 credits. Most moves need far fewer than 36 frames (a landing is 4-6, a take-off 3-4, a strike 6-8), so most clips below chain two or three moves that follow each other naturally. I'll cut each clip into its separate animations afterwards. 11 clips = 99 credits, which leaves about 110 credits for redos.

For every prompt: use the same Qori reference image and settings as the idle you liked, side view facing right, full body, staff in hand. Keep each clip to one continuous action, "first ..., then ...", so Ludo doesn't jump between poses.

## Order to generate (most needed first)

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| 1 | Run (redo) | `Qori sprinting forward with long strides, leaning forward, cloak streaming behind, seamless loop` | Run loop (longer steps, so the feet stop slipping at game speed) |
| 2 | Jump | `Qori crouches, jumps high, floats at the top, then falls and lands with knees bending` | Take-off, rise, apex, fall loop, landing |
| 3 | Dash and glide | `Qori dashes forward low and fast, then opens the leaf cloak like a wing and glides` | Dash, glide loop |
| 4 | Wall | `Qori slides down a wall on the right, then kicks off and leaps away to the left` | Wall-slide loop, wall jump |
| 5 | Ledge | `Qori hangs from a ledge by both hands, then pulls up and climbs onto it` | Ledge-hang loop, ledge climb |
| 6 | Start and stop | `Qori starts running from standing, then skids to a stop, face steady facing right, eyes focused, no mouth` | Run start, run stop |
| 7 | Staff attacks | `Qori strikes forward with the leaf staff, then swings it up overhead, determined eyes, face facing right, no mouth` | Forward attack, up attack |
| 8 | Air attack | `Qori jumps and stabs the leaf staff straight down, fierce eyes, face facing right, no mouth` | Down attack in the air |
| 9 | Hurt and fall | `Qori gets hit and flinches back, eyes squeezed shut, then collapses to the ground, eyes closed, no mouth` | Hurt, death |
| 10 | Look | `Qori looks up with wide curious eyes, then crouches and looks down, no mouth` | Look up, crouch |
| 11 | Rope | `Qori swings on a vine rope holding with both hands, calm eyes, face facing right, no mouth, seamless loop` | Rope swing loop |

Face notes for every prompt: Qori has **no mouth**, only eyes, small brows and a tiny nose, so the expression lives in the eyes. Keep "face facing right" so the head doesn't turn to the camera, and name one eye expression per move: focused for running, determined or fierce for attacks, squeezed shut for a hit, wide for looking. Without it, Ludo adds a smile or an open mouth and turns the face mid-move.

## Left out on purpose

- **Turn around:** the game flips Qori instantly when he changes direction. Most platformers do this, and it costs nothing.
- **Separate rise and fall:** both come out of the jump clip.

## Tips

- If credits get tight, clips 1-5 cover the core platforming. Generate those first and test them in game before spending on the rest.
- If a two-move clip comes out muddled, regenerate just that clip with only its first move. Never redo the whole set.
- Download the frames as transparent PNGs into `Tools/IncomingArt/Ludo/<clip name>/`. I'll cut, loop, steady and test each one.

## Redos (14 Oct, after the in-game test)

| Clip | Prompt | Why |
|---|---|---|
| Idle | `Qori standing still, calm breathing, one blink, no yawn, no mouth, seamless loop` | The first idle's long yawn read badly on repeat. |
| Jump | `Qori jumps up and lands, head and face steady facing right, no mouth` | The first jump turned and smiled mid-air. |
| Ledge (optional) | `Qori hangs from a ledge with both hands, then pulls up onto it, no mouth` | The first one hung from the staff with a thread. |

Add "no mouth, calm face" to every future prompt: Ludo keeps adding a mouth otherwise.

## Attack set (14 Oct): combo, up, jump, sprint and charge attacks

4 clips = 36 credits. Each chains moves that flow into each other, and I cut them apart. Use the same Qori reference and settings as before.

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| A1 | Ground combo | `Qori does a fast three-hit combo with the leaf staff: a forward swing, a backswing, then a spinning finishing strike, determined eyes, face facing right, no mouth` | Combo hit 1, hit 2, hit 3 finisher (spin) |
| A2 | Up and jump attack | `Qori swings the leaf staff straight up over his head, then jumps and does a forward flip swinging the staff in a full circle, fierce eyes, no mouth` | Up attack, jump attack (flip) |
| A3 | Sprint attack | `Qori sprints forward and lunges into a spinning staff thrust, cloak whirling, then skids to a stop, focused eyes, face facing right, no mouth` | Sprint attack (spin lunge), recovery skid |
| A4 | Charge attack | `Qori crouches and holds the leaf staff back as green leaves swirl around it, then releases a huge forward sweep, determined eyes, no mouth` | Charge start, charge hold loop, release |

How they play in game (the plan once the frames are in):
- **Combo:** press attack up to three times. Each press inside the last hit's follow-through chains the next hit, and the third hit is the spin finisher with more knockback.
- **Up attack:** Up + attack on the ground. It hits above Qori's head, for flyers and the guardian's top arm.
- **Jump attack:** attack in the air. It's the flip with a full-circle swing that hits all around him. The existing down stab (prompt 8) stays as Down + attack in the air.
- **Sprint attack:** attack while running (Shift). It's a lunge that carries him forward and covers ground.
- **Charge attack:** hold attack. The leaves gather, and releasing after the charge does a big sweep worth double damage.

If A2's flip comes out muddled, redo it alone with `Qori jumps and does a forward flip swinging the leaf staff in a full circle, no mouth`.

## Fix set (29 Oct, after the animation review)

Only the clips the review marked as missing, or that can't be fixed by trimming and timing. Everything else is fixed in playback. Use the same Qori reference and settings as the idle (side view facing right, full body, leaf staff in the right hand held forward like the idle). Add the face note to each: Qori has no mouth.

| # | Clip | Prompt | Becomes |
|---|---|---|---|
| F1 | Start, skid and turn | `Qori starts running from standing with a quick forward lean, runs a few steps, then skids to a stop with feet braced and cloak swinging forward, then turns around to face left, focused eyes, no mouth` | Run start (3-4 frames), skid (4-6), turn (2-3) |
| F2 | Hurt and death | `Qori is hit from the front and jolts backwards, eyes squeezed shut, recovers his stance, then is hit again and falls backwards to the ground and lies still, leaf staff dropping beside him, eyes closed, face facing right, no mouth` | Hurt (4-6 frames), death (12-16) |
| F3 | Air attack | `Qori in mid-air with legs tucked swings the leaf staff in a fast forward arc in front of him, a trail of green leaves following the staff, fierce eyes, face facing right, no mouth` | Air attack (about 8 frames) |
| F4 | Sprint | `Qori sprints at full speed with long bounding leaps, both feet off the ground between strides, body leaning far forward, leaf staff held low and level, cloak streaming straight back, focused eyes, face facing right, no mouth, seamless loop` | Sprint loop (8-10 frames) |
| F5 | Walk (interiors) | `Qori walks calmly at a relaxed pace with clear full steps, leaf staff held forward, gentle cloak sway, calm eyes, face facing right, no mouth, seamless loop` | Walk loop for houses (12 frames) |
| F6 | Look up and crouch | `Qori looks up with wide curious eyes and holds it, then returns to standing, then crouches low and looks down and holds it, leaf staff in hand, face facing right, no mouth` | Look up (in + hold), crouch (in + hold) |
| F7 | Ledge climb (optional) | `Qori hangs from a ledge with both hands, leaf staff held in one hand along his arm, then pulls himself up onto the ledge and stands up holding the staff forward, calm eyes, face facing right, no mouth` | Ledge climb that keeps the staff and ends in the idle pose |

Order: F2 and F1 first (they show most often), then F3 and F4. F5 only matters for the house interiors. F7 is only needed if trimming the current climb at frame 15 still looks wrong. 7 clips = 63 credits.

If a clip comes out muddled: F1 redo with only `Qori skids to a stop from a run, feet braced, cloak swinging forward, no mouth`; F2 with the hurt and the fall as separate clips. For F4, if Ludo won't lift both feet, add `like a deer bounding`.

Download to `Tools/IncomingArt/Ludo/Qori/Ludo-Qori-<clip>/` as before.

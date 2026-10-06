# Sound effects: integration handoff (6 Oct 2026)

The 136 ElevenLabs sounds (four takes each, 544 WAVs) are in the game. Every take sits in the project unchanged; the game plays the takes **in provisional use** until you audition them. **No take has been approved.** All 544 are "No decision" and unheard.

## Audition the sounds in Unity

1. Let the open editor import the new audio (focus it; about 620 WAVs import once, which takes a few minutes).
2. **Qolossal > Audio > SFX Audition** opens the window.
3. Pick a sound on the left (search, filter by bus, or tick **No Favorite yet**). The right side shows its event, hook, prompt and four takes with waveforms.
4. Listen: **1–4** play takes 1–4 (a loop sound loops; **Space** stops). A loop sound also has **▶ Seamless loop** for its crossfaded derivative. The toolbar's **Game mix / Source** switch plays a take as the game mixes it (loudness-matched, through its mixer group) or as the raw file.
5. Decide with the buttons or keys, for the take you last played: **F** Favorite, **C** Candidate, **R** Rejected, **U** back to No decision. **Up/Down** (or J/K) move to the next sound.
   - The game plays the **Favorite** take(s) only once a sound has one; until then, every take not Rejected, in rotation.
   - For a repeated sound (footsteps, hits, swings), mark several Favorites to keep variation.
   - Rejected takes stay in the project and out of the game's rotation; nothing is deleted.
6. Tuning per sound (volume, pitch spread, cooldown, max voices, positional) is on the same page. Every change saves into `Assets/Audio/Resources/SfxLibrary.asset` immediately.
7. **Export decisions** writes `Tools/Audio/SFX_AUDITION_DECISIONS.csv` (the window also counts sounds with a Favorite and takes heard).
8. In game, **Pause > Sound** steps Master, Music, Effects and Ambience (each press: -20%, from silent back to 100%).

## What's in place

| Part | Where |
|---|---|
| Source takes (byte-identical to the delivery) | `Assets/Audio/SFX/<Bus>/SFX_<id>_0N.wav` |
| Seamless-loop derivatives (sources untouched) | `Assets/Audio/SFX/Loops/SFX_<id>_0N_loop.wav` |
| Mapping manifest (machine) | `Assets/Audio/SFX/sfx_manifest.json` |
| Mapping manifest (readable): Sound ID, bus/mixer group, event, hook, status, loop, the four takes, derivatives, tuning, audit flags | `Tools/Audio/SFX_MAPPING.csv` |
| The library the game reads | `Assets/Audio/Resources/SfxLibrary.asset` |
| Mixer | `Assets/Audio/Qolossal.mixer`: Master > Music, Effects (Player, Combat, Enemy, World, Pickup, Town, Story), Ambience, UI |
| Runtime | `Assets/Scripts/Audio/`: `Sfx.cs` (API, the one player, settings), `SfxEmitter.cs` (loops on objects), `SfxLibrary.cs`, `QoriSounds.cs` |
| Editor | `Assets/Editor/Audio/`: `SfxLibraryBuilder.cs`, `SfxAuditionWindow.cs`, `SfxImportSettings.cs` |
| Tools | `Tools/Audio/audit_sfx_library.py`, `build_sfx_library.py`, `sfx_mapping.py` (the hand-written mapping), `write_script_meta.py` |
| Reports | `Tools/Audio/SFX_AUDIT.json`, `SFX_LOOP_REPORT.json` |

To change a mapping: edit `sfx_mapping.py`, run `python Tools/Audio/build_sfx_library.py`, then **Qolossal > Audio > Rebuild SFX Library**. A rebuild keeps every audition decision and your tuning. Use **Rebuild SFX Library (reset tuning from manifest)** only to discard tuning.

### How sounds play

- **One player.** `Sfx.Play("Id")` / `Sfx.Play("Id", position)` from gameplay code; one persistent "Sound Effects" object with a 24-voice pool. The soundtrack keeps its own player (`MraMusicPlayer`). No other manager was added.
- **Variation.** A random take in use, never the same take twice running, plus a small pitch spread.
- **Spam control.** A per-sound cooldown (unscaled time), a per-sound voice limit (the oldest copy is replaced), and a full pool steals the oldest voice of the least important bus (Story > UI > Player/Combat > the rest).
- **Superseding.** A sound can replace another that just started. For example, a Waymark lighting replaces the plain checkpoint touch, the guardian's collapse replaces the generic defeat, and Chart travel replaces the Chart closing.
- **2D space.** Positional sounds (enemies, the world) pan gently by screen position (at most halfway to a side) and fade out between the screen edge and 1.6 times the half-view past it. Qori's own sounds, pickups, UI and story sounds are centred. `spatialBlend` is 0 everywhere: no 3D roll-off, no Doppler.
- **Loops on objects.** `SfxEmitter` loops fade in and out (0.35 s), start at a random point (neighbours never phase), and only the nearest copies within a sound's voice limit sound. For example, of five hovering moths, two are heard.
- **Ambience.** Per scene by name prefix (the library's scene rules): each region's bed, `Amb_Cave` in every side cave, `Amb_HouseInterior` in the Qvale homes. Changing beds (into a cave, a home, the next region) is a 2.5 s crossfade; the same bed carries on. The bed ducks under the listening tree (to 45%) and under the reveal and ending (to 30%).
- **Footsteps.** From the live rig's feet (`FootNear`/`FootFar` planting after a lift), at most one per 0.3 u travelled. The surface comes from the scene rule (Moss, Stone or Wood); ground named "water"/"shallow" or "plank"/"wood" overrides it.
- **Pause.** Everything pauses with `AudioListener.pause` except the UI bus (menu sounds play while paused).
- **Story.** The Story bus refuses `Sfx.Play`. Only `Sfx.PlayStory` from `MraReveal` and `MraEnding` can start it. A reveal or ending cut short stops its sound.
- **Volumes.** `SfxSettings` (PlayerPrefs, separate from the save): Master = listener volume, Music scales `MraMusicPlayer` and the listening tree, Effects and Ambience scale their buses.

### Loudness

The takes arrived up to about 40 dB apart. Each take gets a gain toward its bus's target active level:

| Bus | Target |
|---|---|
| Combat, Story | -20 dBFS |
| Enemy, World, Pickup | -22 dBFS |
| Player, Town | -24 dBFS |
| UI | -26 dBFS |
| Ambience | -30 dBFS |
| Footsteps, dialogue blips | -28 to -30 dBFS |

The gain never lifts a peak past -1 dBFS. The effect, ambience and UI mixer groups sit at +12 dB, so per-take volumes below 1 can still lift quiet takes. The music group stays at 0 dB.

These are starting points for your ears, not a final mix.

## Mapping status (136 sounds)

| Status | Count | Sounds |
|---|---|---|
| wired | 111 | Played by a hook in the Mountain Relief game (see `SFX_MAPPING.csv` for each event and hook). |
| wired, not placed | 11 | Hooked in shared scripts that the MRA scenes don't place yet: Qori_Step_Water (pool splash), Crawler_Move, Spring_Pulse, Rock_Fall, Platform_Crumble, Platform_Reform, Breakable_Smash, SecretWall_Open, GrowthFlower_Bloom, MovingPlatform_Loop, Waterfall. |
| future | 6 | Features that don't exist: Combat_Parry, Combat_ChargeUp, Combat_ChargeRelease (no parry or charged attack), Amber_Pickup (enemies drop sap orbs, not Amber), Lantern_Light, Bell_Tower (no town requests for them). |
| no hook | 8 | No event to attach to: Quake_Small (no aftershock), Arch_Fall (MRA stirs swap art without a fall), HangingChain (no contact event), Water_Stream (no flowing-water object), Night_Hum (no night), Chart_PieceAdd (pieces join without a moment), Save (saving is silent and continuous), Story_Sprout (no opening sequence). |

### Ambiguous or open hooks: your call

- **Story_EyeOpen** plays as the careful ending begins (the waking); **Story_Ending** with its last caption. The reveal has only **Story_Reveal**, at the pull-out. Swap them if you hear the eye opening as part of the reveal.
- **Quake_Stir** plays at a knot's quake. **Knot_Wake** plays as Qori touches the knot. **Quake_Small** could become an aftershock in Qvale after a stir, if you want one.
- **Discovery** plays for a newly found cave mouth or Qvale home. **Banner_Show** plays with a region's name plate.
- **Smithy_Anvil** plays when Brannick's forge opens; **Smithy_Upgrade** on a forge purchase. **Map_Unroll** plays when Scribble's pages open; **Map_Stamp** on buying a page. Other shops use UI_Confirm and Shop_Buy.
- **Dialogue blips** play on every third typed letter. The low blip is for speakers listed in the library's low-voice speakers (Grandfather Tallow).
- **Water footsteps** need ground named "water"/"shallow". No MRA surface is named that yet.

## Loop work

- **25 loop sounds** (the CSV's 12, plus 13 ambiences).
  - **Ambiences.** The CSV's Loop column missed the ambiences. The sheet's heading said they loop, but they were generated without loop mode. They're treated as loops here.
- **Every loop take's seam was measured:**
  - **22** were clean as delivered and play as they are.
  - **78** got a crossfaded derivative: faded edges trimmed, then an equal-power crossfade of the tail into the head (2 s for beds; 0.25–0.6 s for object loops). Every derivative's wrap now joins two samples that were neighbours in the source.
  - **24** of those are marked "check by ear": an event (a drip, a click, a ripple) falls inside the crossfade, so its level differs from the audio around it. They're flagged in the audition window. Most of these sounds have a cleaner alternative take.
- **Turning a derivative off.** Each take's "Game uses the seamless loop derivative" toggle switches it off.

## Import settings

Defaults; there are no platform overrides until measured on hardware.

| Kind | Settings |
|---|---|
| One-shots under 1.2 s | Decompress On Load, ADPCM, preloaded |
| Longer one-shots | Vorbis q0.7, compressed in memory |
| Object loops | ADPCM, compressed in memory (sample-accurate looping) |
| Ambience beds and Story | Streaming Vorbis (q0.6 / q0.7) |
| Channels | Player, Combat, Enemy, World and Pickup forced to mono (the game pans them); Ambience, Story, Town and UI keep stereo |

Every type keeps the 48 kHz sample rate. The audition window plays these imported clips, so gameplay takes are heard in mono.

## Validation

### Source checks (Python, outside Unity)

- 544/544 files present, named to the CSV's "Save as" with _01–_04, and SHA-256 equal to `DOWNLOAD_RECEIPTS.jsonl`. The CSV's hash matches the delivery's record.
- All 48 kHz, 16-bit, stereo, complete sample data. Durations match the requested lengths.
- After copying into `Assets/Audio/SFX/`, every file's hash was checked again. The delivery folder was only read.
- Quality notes, per take, in the CSV and the audition window:
  - 89 takes have clipped samples in the source (from 1 to 990 samples). Gain can't repair clipping.
  - 89 takes can't reach their loudness target without pushing a peak past -1 dBFS, so they sit a little quieter.

### Unity import and runtime (Windows editor, batch mode on the scratch copy)

| Run | Result |
|---|---|
| Library build (`SfxLibraryBuilder.BuildBatch`) | 136 sounds, 544 takes in use, 0 clips missing, mixer created (Effects/Ambience/UI +12 dB, Music 0 dB) |
| **Audio checks** (`MraPlayTest.Run -mraAudioOnly`, new) | **137 passed, 0 failed** |
| Polish checks (`-mraPolishOnly`) | 90 passed, 0 failed (unchanged) |
| Full play test | **656 passed, 0 failed** (the last recorded run was 647/0) |

What the audio checks prove, by driving the real scenes (keyboard and pad where the game takes input):

- **Library and imports.** All 544 takes import; every sound has takes in use; the mixer routes all nine buses and the music; loop sounds use their derivatives. A footstep imports decompressed ADPCM mono, a bed streams in stereo, a story sound streams, and an object loop is ADPCM compressed in memory.
- **The rules.**
  - Cooldown: ten same-frame footsteps play once.
  - Voice limits: at most 3 staff hits at once.
  - Variation: 12 swings use all 4 takes, never the same one twice running.
  - Superseding: a Waymark replaces the checkpoint touch.
  - Story: a story sound is refused outside its event.
  - Buses: UI sounds route to the UI group and play through the pause; Effects at 50% halves an effect.
  - Emitters: of five hovering moths, two are heard.
- **Ambience.** The right bed and footstep surface in the Cradle, Causeway, Qvale, a home (Wood), a cave and the Summit. A bed change crossfades (caught mid-fade at 0.27 in / 0.73 out) into a looping bed.
- **Qori.**
  - Movement: walking plays footsteps, at most one per rig footfall and never closer than the cooldown. A jump, landing, dash (C), Glidecap opening and glide loop all play.
  - Combat: an attack plays its swing.
  - Thread: casting it plays the flick and the snag; letting go plays the release.
  - Health: hurt, death, then the return.
- **Weapons.** Swing and hit sounds for the Leaf Sword, Seedpod Mace (plus the heavy impact), Thorn Spear and Leaf Staff. Also the blocked blow, the pogo bounce, the sling's release and its pellet striking rock, and a sap orb's heal.
- **Enemies, one of each in place, near Qori.**
  - Grub, Shellback (walk loop), Sentinel (steps and bash), Thornwing (rear and dive), Pod Spitter (swell and spit), Gust Moth (blast), Newt (leap).
  - The guardian: awaken, arm raise and slam, hurt and collapse.
  - The defeat sound for each.
- **World.**
  - The knot: hum near a sleeping knot; wake and quake; the hum stops once it's awake.
  - Places: cave discovery and entry with the cave bed inside.
  - Chambers: winches; every light in each of 5 sequences chimes and the solved gate opens (10 gate openings heard); a pressure plate; the timed thorns warn, rise and sink.
  - Finds: a relic shrine; song shell, heart seed, Amber and lore finds.
- **Qvale.**
  - Brannick's forge: open, buy without Amber, buy, move down the list, leave.
  - Scribble's pages open.
  - Dialogue blips: small for Pip, low for Grandfather Tallow.
  - The listening tree: sit and get up.
  - A home's door, in and out (bed and wooden floor inside).
- **Menus.** Pause, move and resume; the Chart opening and closing.
- **Story.**
  - No ending sound before the ending.
  - An interrupted reveal stops its sound; the full reveal plays Story_Reveal once and completes.
  - The ending plays Story_EyeOpen and Story_Ending once each.
  - Story_Sprout never plays.

Not reached by the test (wired, each a one-line hook on an event the full play test already exercises):

- Qori: Qori_Step_Stone/Wood (only moss was walked), Qori_Land_Hard, Qori_LedgeGrab, Qori_Climb, Qori_Thread_Swing.
- Enemies: Spitter_SeedHit, Shellback_ShellCrack, Sentinel_ShieldBlock.
- Interactions: Lever_Pull (every MRA lever is a winch), Waymark_Travel, Map_Stamp.

Memory, editor: the 544 in-use clips occupy **29.0 MB** loaded: 476 in memory, plus 68 streamed clips, which hold stream buffers only. Measured with `Profiler.GetRuntimeMemorySizeLong` in the Windows editor on the scratch copy. That's an editor measure, not a player or console budget. No CPU timing of the sound system was taken (it's written not to allocate per frame once warm, but that isn't measured either).

Evidence in `Tools/Audio/evidence/`: `menu_main.png` and `menu_sound.png` (1920×1080, the pause menu with its new Sound page), and the three runs' results JSON.

Batch mode has an audio device but nobody listening: these runs prove the hooks, the rules and the routing, **not how anything sounds**. Listening is the audition.

### Devices

| Target | Status |
|---|---|
| Windows player build | not tested (no player build this round) |
| PS5 | not tested |
| Xbox Series X | not tested |
| Xbox Series S | not tested |
| Switch 2 handheld | not tested |
| Switch 2 docked | not tested |

Platform direction item 9 (eight-stream profiling and per-platform codecs) still needs hardware.

## Pending your decisions

1. **Audition** all 136 sounds: choose Favorites, reject the bad takes (start with the clipped and "check by ear" flags).
2. The mix: listen in game and adjust volumes. Footsteps, the knot hum and the ambience beds are the ones most likely to need it.
3. The ambiguous hooks above.
4. Whether to keep all 544 sources in the repository. They add 457 MB to `Assets/Audio/SFX`, and LFS will upload them when you push. Rejected takes stay out of builds but not out of the repository.

## Changed files

- **New:**
  - `Assets/Scripts/Audio/*`
  - `Assets/Editor/Audio/Sfx*.cs`
  - `Assets/Audio/SFX/**` (622 WAVs + manifest)
  - `Assets/Audio/Resources/SfxLibrary.asset`
  - `Assets/Audio/Qolossal.mixer`
  - `Assets/MountainReliefAtlas/Scripts/Testing/MraAudioTest.cs`
  - `Tools/Audio/*`
- **Hooks**, one line or a few at each event:
  - Qori, combat and health: PlayerMovement, PlayerHealth, PlayerCombatFeedback, QoriResinShot, WeaponKind.
  - Creatures: EnemyBase, Thornwing, Spitter (and its seed), Shellback, Grub, Newt, Sentinel, GustMoth, Knucklebramble, GroundCreature.
  - Effects: Fx.Glint, SapOrb.
  - Mechanics: AbilityShrine, Breakable, CrumblePlatform, FallingRock, HeartSeed, MovingPlatform, PressurePlate, SecretWall, SeedSwitch, GrowthFlower.
  - World: Waymark, LoreStone, WaterPool, WaterfallSplash, WarmSpring.
  - Town: TownShop, DialogueBox, ListeningSpot.
  - Mountain Relief: MraKnot, MraSwell, MraTimedHazard, MraGate, MraLever, MraReward, MraDiscovery, MraChamberDoor, MraChamberReturn, MraChart, MraReveal, MraEnding.
  - UI and transitions: GamePauseMenu, AreaTransition.
- **Pause menu.** It gained a **Sound** page; the panel grew from 840 to 960 px to fit six buttons.
- **QoriMovementFeedback.** Its synthesized rustles are silenced when QoriSounds is present, which is always on the Player.
  - That component only runs on the old QoriVisual, which is inactive in the Player prefab, so those rustles never played in the MRA game anyway.
- **Music.** `MraMusicPlayer` and `ListeningSpot` now route to the mixer's Music group and follow the Music volume. No track, assignment or music file changed.
- **Tests.** `MraTestDriver` is now `partial` and has `-mraAudioOnly`. `UiCapture` also captures the Sound page.

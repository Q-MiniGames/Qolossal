# Qolossal: session handoff (7 Oct 2026)

This file stands alone. A new session opened in `C:\UnityProjects\Qolossal` should need nothing from earlier chats. Read it first, then the documents in section 3.

---

## 1. The project

- **Game:** Qolossal, a 2D action-platformer in Unity **6000.3.19f1**, at `C:\UnityProjects\Qolossal`.
- **Hero:** Qori, a small leaf-cloaked creature. The starting weapon is the Leaf Staff (id `forest-staff`). The sword, mace and spear exist, but making them "found later" isn't done.
- **World:** the whole world is the body of a sleeping titan, and **that is a mystery**. Players must not know they are on a titan until the final level, where they climb onto the face and the complete map reveals the body.
  - Before that, everything must read as strange landscape: no visible face, no anatomy, no text naming the body.
  - Clues may only make sense in hindsight.
  - Example: the Summit lake and reeds stay in the terrain, never in background planes.
  - The full rules are in `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2), sections 2, 10 and 11.
- **Town hub:** Qvale, in the lap, mid-route. It has the Amber currency, enterable homes (cutaway), NPCs, a music-listening spot and an old man who warns the player not to wake "the mountain".
- **Platforms:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. **The original Switch is excluded.**
  - 60 FPS is a target, not a result.
  - Never claim console readiness without real builds and hardware tests.
  - Test Switch 2 handheld and docked, and Xbox Series S, explicitly.
  - See `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`.
- **Current playable build:** the Mountain Relief Atlas (MRA) prototype, regions MR01–MR08, with 51 scenes in `Assets/MountainReliefAtlas/`.
  - Build them with Qolossal > Mountain Relief Atlas > Build All Scenes.
  - The regions are MR01 Cradle, MR02, MR03 Terraces, MR04 Qvale (with house interiors `MRAtlas_MR04_C02..C06`), MR05, MR06, MR07 Summit and MR08.

## 2. Git

- **Branch:** `mountain-relief-atlas`. On 7 Oct the user said "commit", and the section 4 work (SFX, depth backgrounds, Batch 14 art, the footstep and glide changes) was committed on top of `c6ef1f3`. On 7 Oct it was merged into `main` (merge commit `092451f`), and `main` is now checked out. **Pushed:** the remote `origin` is `https://github.com/Q-MiniGames/Qolossal.git`, and `main` tracks `origin/main`. The first push on 7 Oct uploaded 2075 LFS objects (2.3 GB). The `mountain-relief-atlas` branch is local only, because it's fully merged.
- **Checkpoint commits, 10 Oct (the user's "commit"; not pushed):** `47e074e` audio, `c685c9d` accepted art (manifest 1192), `7f0570c` MRA code and scenes, then the docs and tools commit. `Assets/QoriRigTest/` stays uncommitted (Qori v3 on hold).
- **Git LFS** carries the music WAVs, SFX WAVs, PNGs and (from 7 Oct) WEBP evidence clips.
- `Tools/IncomingArt/` (the Codex staging and review docs) is git-ignored.
- **Codex can also remote-control this PC** (from 7 Oct). Check `git status` before work and before committing.
- **Commit rule:** commit only after the user has play-tested and said so. End commit messages with:
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`

## 3. Read first

- `CLAUDE.md` / `AGENTS.md`
- `Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md`
- `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2; sections 2, 10 and 11 hold the mystery rules)
- `Assets/MountainReliefAtlas/LEVEL_POLISH_HANDOFF.md`
- `Tools/Audio/AUDIO_INTEGRATION_HANDOFF.md`
- `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md`
- `Tools/Backgrounds/depth_prototype/README.md`
- `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_25.md` (the latest art review)

## 4. Latest committed work (7 Oct 2026)

This was committed at the user's word. The audition, mix and hook decisions below are still open. It falls into four groups.

### 4.1 Sound effects (ElevenLabs)

- **The sounds:** 136 sounds with four takes each (544 WAVs) in `Assets/Audio/SFX/<Bus>/`, byte-identical to the delivery in `OneDrive\Documents\ChatGPT\Qolossal\Audio\ElevenLabs_SFX`. There are also 78 seamless-loop derivatives in `Assets/Audio/SFX/Loops/`.
- **Runtime:**
  - `Sfx`, `SfxPlayer`, `SfxEmitter` and `SfxLibrary` are in `Assets/Scripts/Audio`.
  - `QoriSounds`, added by `PlayerMovement.Awake`, plays Qori's movement sounds.
  - The mixer is `Assets/Audio/Qolossal.mixer`, with +12 dB headroom on the effect, ambience and UI groups.
  - The library is `Assets/Audio/Resources/SfxLibrary.asset`.
  - The pause menu has a Sound page (Master, Music, Effects, Ambience).
- **Pipeline:**
  1. `Tools/Audio/sfx_mapping.py` is the source of truth: events, hooks, targets and cooldowns.
  2. `python Tools/Audio/build_sfx_library.py` writes `Assets/Audio/SFX/sfx_manifest.json`.
  3. Then Unity: Qolossal > Audio > Rebuild SFX Library (batch: `SfxLibraryBuilder.BuildBatch`).
- **Status:** 111 wired, 11 wired but not placed in MRA scenes, 6 future, 8 with no hook.
- **Audition:** Qolossal > Audio > SFX Audition. **No take is approved yet**, so all non-rejected takes play in rotation.
  - Never mark takes Favorite or Rejected for the user.
  - Reveal and ending sounds play only through `Sfx.PlayStory`.
- **Open decision:** Story_EyeOpen is placed at the start of the ending.
- **Changes on 7 Oct, after the user said the footsteps were annoying:**
  - `Qori_Step_*` (Moss, Stone, Wood, Water) target lowered from −28 to −35 dB in `sfx_mapping.py`, and the library rebuilt. Water splashes into pools are quieter too.
  - `QoriSounds.Step()` sounds every other footfall, so one per full stride. Volume drops to 0.7 at full run speed (`RunVolume`).
  - **Glide:** `QoriSounds` keeps a held `canopy` state. It opens when `IsGliding` first becomes true and stays on while Jump is held in the air, ending on landing, ledge, wall, dash or thread.
    - `Qori_Glide_Open` now plays once per opening.
    - The glide loop no longer drops out when the glide briefly lets go (an updraft or a swell's push).
    - Added `PlayerMovement.JumpHeld`.
  - If the steps are still too loud: lower the target further, or use only the softest single-hit takes (Moss 01–03, Stone 02–03).
- **Second pass, 8 Oct (uncommitted), from the user's two screen recordings:** steps still blurred into a hiss.
  - Cause: every step take is a 0.3–0.5 s shuffle of several scuffs, and Qori steps every 0.40 s walking and 0.27 s running, so the takes overlapped.
  - `QoriSounds` now plays only the first 0.2 s of a step (`Sfx.PlayShort`, fading over the last 0.06 s), 3 dB quieter (`StepVolume`).
  - Glide: the probe found one opening and no loop cuts, so the logic was right. The user then asked to remove `Qori_Glide_Loop` (8 Oct). `QoriSounds` no longer attaches it, so gliding plays only `Qori_Glide_Open`. The mapping, manifest, CSV and library mark it `no-hook`. Its takes stay in the project.
  - The user chose `Qori_Step_Water` Take 3 as the one footstep everywhere (8 Oct). `QoriSounds.StepId` is always `Qori_Step_Water`. In the library, Take 3 is now its only Favorite, and Take 2 (the user's earlier Favorite) is back to Candidate. The Moss, Stone and Wood steps are kept but unused. `WaterPool`'s splash uses the same sound.
  - Tests after both changes: audio 144 passed, 0 failed; polish 107 passed, 0 failed.
  - `SfxPlayer`'s internal `Start(...)` was renamed `Begin`, which stops Unity's "Start() can not take parameters" error.
  - New test `MovementSoundProbe` (in `-mraAudioOnly`) logs every Qori sound while walking, running and gliding.
- **The user's full SFX selection, 8 Oct (uncommitted):** the user went through every take in `Tools/Audio/SFX_SELECTOR.html` (built by `build_sfx_selector.py`) and exported `Tools/Audio/sfx_selection_2026-10-08.json`. `Tools/Audio/apply_sfx_selection.py` applied it to `SfxLibrary.asset`: 448 takes are now Rejected (never played, out of builds; the WAVs stay in the project), and 96 takes are in use.
  - 44 sounds lost every take and are now silent, including every ambience bed, `Qori_Hurt`, `Qori_Death`, `Qori_Respawn`, `Qori_Climb`, `Qori_Sit`, the thread shoot and swing, `UI_Pause`/`UI_Unpause` and the thorns. Their hooks still call them; `Sfx` refuses them quietly. The list is in the JSON and in the test log line "sounds removed entirely".
  - `MraAudioTest` was adjusted: a silent sound's refused request counts as its hook firing, the cooldown test uses the standard step, the variation test picks any sound with two or more takes in use, and the ambience crossfade is checked only while the beds have takes. **Not yet run:** the editor was open when this was applied.
  - The library was edited on disk while the editor was open; Unity must reimport it (no unsaved Sfx Audition window changes may be saved over it).
- **Second SFX selection, 10 Oct (uncommitted):** `Tools/Audio/sfx_selection_2026-10-10.json` replaced the 8 Oct one: 11 more takes Rejected and `Qori_Glide_Open` Take 3 brought back (in place of Take 2). 458 Rejected, 86 in use, 53 sounds silent. Newly silent: the footstep (`Qori_Step_Water`, so Qori has no footsteps at all), `Qori_Jump`, `Qori_LedgeGrab`, `Qori_Thread_Attach`, `Qori_Thread_Release`, `Weapon_Staff_Swing`, `Weapon_Spear_Thrust`, `Weapon_Sling_Shot`, `Spitter_Spit`. `apply_sfx_selection.py` now also restores a Rejected take marked Keep (to Unheard). `MraAudioTest` handles a removed footstep. Tests not yet run.

### 4.2 Background audit and Batch 14 backgrounds

- **Proposal:** `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md`, with Option B recommended. Evidence and mockups M1–M5 are in `Tools/Backgrounds/proposal/`.
- **Audit tool:** `MraBackgroundAudit.Run [-mraOnly MR03] [-fgMatte]`.
- **Depth parallax** is now used in **MR01 Cradle, MR02 Causeway, MR03 Terraces, MR05 Ribwood, MR06 Heights and MR07 Summit** (`MraWorldBuilder.DepthSets`, which maps each region to its Far/Mid/Near file names; a null Mid is skipped).
  - `ParallaxLayer.depth` is the distance relative to the terrain, which is 1. It sets each layer's speed (1/depth) and its zoom response, like a camera pulling back.
  - `ParallaxLayer.uvRect` draws part of a texture.
  - Far, Mid and Near are at depths 25/10/5, 10 u tall.
  - MR03 also has the mill at depth 14 (2.8 u tall), 11 foreground decor clumps at depth 0.75, and a Mill Ridge lookout zoom.
  - Rebuild one region with `MraWorldBuilder.BuildRegionsBatch -mraOnly MR03`.
  - Record comparison runs with `MraPlayTest.Run "-mraDepthCapture <tag>"`.
- **Qvale (MR04):**
  - The `BG_Qvale_VaultRim_Mid` band is at order −88.
  - `BG_Qvale_Overlook_Vista` shows only while Qori is seated, through the `SeatedVistaFade` component.
  - The MR01 and MR04 skies are now `BG_Sky_Terraces` (the dawn sky).
- **Still not fixed:**
  - MR02, MR05 and MR06's hard-edged far paintings are gone: since 7 Oct they use the Batch 15 depth sets under `BG_Sky_Terraces`.
  - The remaining Option A fixes aren't done: framing, haze, mipmaps.
  - The background strips are enlarged 1.35x at 1080p.
  - KneeTerraces is excluded (it reads as a knee).
  - Do Option A only when the user says go. The camera framing change (A6) needs their play-test.
- **MR02, MR05 and MR06** got their Batch 15 depth sets on 7 Oct (section 4.3). MR06 has no Mid until `BG_Heights_Mid` is redone.

### 4.3 Art: Codex Batch 14, Review 25 (done)

- **Delivery:** `OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch14` (PNGs in `Backgrounds/`, plus `MANIFEST.json`).
- **Accepted:** all 8 (Cradle Far/Mid/Near, Summit Far/Mid/Near, Qvale VaultRim_Mid, Qvale Overlook_Vista). They are in `Assets/Art/Codex/Backgrounds/`.
  - The accepted manifest holds **1140** files.
  - On 7 Oct all 8 were re-verified against the delivery manifest, and all 1140 accepted files were confirmed byte-identical.
- **Batch 15 is written (7 Oct), for the user to send to Codex:** `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH15_BACKGROUNDS.md` and `CODEX_PROMPT_BATCH15.md`. It holds nine new depth layers (`BG_Causeway_*` for MR02, `BG_Ribwood_*` for MR05, `BG_Heights_*` for MR06; names checked free) plus the Review 25 touch-ups below. New gameplay mattes for it are in `Tools/Backgrounds/proposal/mattes/` (`MR02_mid`, `MR05_mid`, `MR05_low`, `MR06_mid`, `MR06_encounter`). On delivery, add the three regions to `MraWorldBuilder.DepthSets`. The touch-ups are:
  - two dark smears on the `BG_Cradle_Far` skyline (x≈1925 and x≈2325, rows 585–620);
  - stair-stepped tree crowns on `BG_Cradle_Near` and `BG_Summit_Near`;
  - a few of the same steps on `BG_Cradle_Mid` and `BG_Qvale_VaultRim_Mid`.

  The delivery will be reviewed as Review 26.
- **Review 26 (7 Oct, done; nothing imported yet):** `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_26.md`. The delivery is in `Codex_v2_full\Batch15`, and all 14 hashes match.
  - The user's prompt to Codex asked for the touch-ups as `_v2` files, so they don't overwrite anything.
  - **Accepted (12):** `BG_Causeway_*`, `BG_Ribwood_*`, `BG_Heights_Far/Near`, `BG_Cradle_Far/Mid/Near_v2` and `BG_Qvale_VaultRim_Mid_v2`.
  - **To redo (Batch 15b, then Review 27):**
    - `BG_Heights_Mid`: the MR06 flying enemy disappears against it.
    - `BG_Summit_Near_v2`: the feathergrass plumes are still column blocks.
  - **Imported and wired (7 Oct, at the user's "go"; not committed, waiting for their play-test):**
    - The 12 are in `Assets/Art/Codex/Backgrounds/`. The accepted manifest now holds **1152** files, all byte-identical (0 mismatches). `build_codex_v2_manifest.py` reads `Batch15/MANIFEST.json` for their settings (144 PPU, bottom-centre pivot, Repeat U).
    - `DepthSets` now covers MR02, MR05 and MR06 (MR06 with Far and Near only), MR01 uses the Cradle `_v2` files and MR04 uses `BG_Qvale_VaultRim_Mid_v2`. The MR02, MR05 and MR06 skies are now `BG_Sky_Terraces` (`MraBuilderArt`).
    - Rebuilt MR01, MR02, MR04, MR05 and MR06. Polish check: 90 passed, 0 failed. In-engine captures: `Tools/Backgrounds/proposal/batch15_ingame_A.jpg` and `_B.jpg`.
    - The old Batch 14 originals (`BG_Cradle_*`, `BG_Qvale_VaultRim_Mid`) stay accepted but are no longer drawn.
- **Review 27 (7 Oct): Batch 15b rejected, nothing imported.** `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_27.md`.
  - `BG_Heights_Mid_v2` came back as a flat grey grade with a rectangular haze block.
  - `BG_Summit_Near_v3` has flame-shaped, pasted-looking plumes.
  - Batch 15c is requested in the review. MR06 stays on Far and Near, and MR07 on `BG_Summit_Near`.
- **Review 28 (7 Oct): Batch 15c.** `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_28.md`. Not imported yet.
  - **Accepted:** `BG_Summit_Near_v3`, the Batch 15c file (the name was reused; use this delivery's hash). The plumes are now fine drooping stems. **Imported and wired (7 Oct, at the user's "go"; not committed):** MR07's Near is now `BG_Summit_Near_v3`, MR07 was rebuilt, and the polish check passed (90, 0 failed). The accepted manifest holds **1153** files, 0 mismatches. In-engine capture: `Tools/Backgrounds/proposal/batch15c_ingame_MR07.jpg`.
  - **Heights Mid closed:** `v3` still hides the flyer, so MR06 stays on Far and Near for good.
- **Harmless note:** `verify_preservation.py` reports "1 duplicate": `MraChartArt` is both a script and an asset, both already committed.

### 4.4 Other

- `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH14_BACKGROUNDS.md` and `CODEX_PROMPT_BATCH14.md` are the Batch 14 request (already delivered).

### 4.5 Qvale house interiors (7 Oct; not committed, separate from Batch 15; waiting for the user's play-test)

The user's screenshots showed the old man's house looking stretched, the outside door and awning inside his room, and hard edges at the room's floor.

- **Measured:** the game draws every house exactly as its accepted PNG (SIFT match on the user's screenshots: x/y scale ratio 1.0000, mean pixel difference about 3/255). The stretch and the faceted rock are baked into the accepted Batch 8 W3 files: `build_w3.py` resampled each Codex painting into a fixed layout box with independent X/Y factors (`PROVENANCE.json` `resample_xy`). OldMan is 1.24 across and 1.81 up (46% taller than painted), HomeB 1.58x, HomeC 1.62x and HomeA 1.02x. Comparison: `Tools/MountainReliefAtlas/QA/QvaleInteriors/house_exteriors_painting_vs_accepted.jpg`. **Not changed:** fixing it means new art or a different house footprint, and that is the user's call.
- **Fixed (runtime only, no art changed):** `MraTownBuilder.BuildHouse`.
  - The old man's (C05) and mapmaker's (C03) rooms are framed from inside their front wall (`RoomFromPx`, measured on the cutaways), so the painted outside door and awning stay off-screen. Qori arrives and leaves at that wall.
  - Residents' Home (C04, HomeC) keeps the full view, because its awning hangs over the shelf and desk. A proper fix there needs a cutaway touch-up from Codex.
  - All five interiors: the floor is collision only, and the view's bottom sits at 0.45 u, so the green ground strip under the painted floor is gone.
  - New batch entry `MraWorldBuilder.BuildHousesBatch` rebuilds only the five interior scenes. `MraTestDriver` checks the framed rooms and uses the scene's entry landing.
- Polish check after the rebuild: 90 passed, 0 failed. Before and after frames: `Tools/MountainReliefAtlas/QA/QvaleInteriors/`.
- **Squash in-game (7 Oct, the user's pick; then "apply this fix to all stretched houses"; not committed):** `MraTownBuilder.PaintedHeight` draws every Batch 8 W3 building at x/y of its `resample_xy`, outside and inside: HomeA (herbalist) 0.9757, HomeB (mapmaker) 0.6315, HomeC (residents) 0.6164, OldMan 0.6846 and Smithy 1.1890 (it was 19% too wide, so it now stands taller). Width, doors and street spacing are unchanged, and the accepted pixels are untouched. The loft (Batch 13) already matches its painting. `MraTestDriver` now also frames the smithy (`smithy_street`). Rebuilt MR04 (the street scene also carries the uncommitted Batch 15 VaultRim wiring) and the five interiors. Polish check: 90 passed, 0 failed. Frames: `QvaleInteriors/squash/`.
  - **Next (7 Oct, recommended, not sent):** the interiors reuse the street cutaways (whole building on black), so cropping and squashing can't make them read as rooms. `Tools/ArtRequests/CODEX_PROMPT_BATCH16_INTERIORS.md` asks Codex for five full-screen `Interior_Qvale_*_Room` paintings (names checked free) with registration. On delivery, point `InteriorArt` at them and drop `RoomFromPx` and the interior use of `PaintedHeight`.
  - **Batch 16 delivered and wired (8 Oct, Review 29; not committed, waiting for the user's play-test):** `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_29.md`. All five `Interior_Qvale_*_Room` paintings were accepted (manifest **1158**, 0 mismatches) and are in `Assets/Art/Codex/Town/`. `MraTownBuilder` now draws them as the interiors: the camera is 10 u tall and fills a 16:9 view, the floor is on the 1280 px walk line, the exit is at each painted inside door, and residents stand on the registered spots. The loft's stair follows the three new treads, and the old man's sleeping loft is now climbable (one-way deck plus two ladder rungs). `RoomFromPx` and the interior `PaintedHeight` are gone; the exteriors keep `PaintedHeight`. Rebuilt the five interiors. Polish check: 97 passed, 0 failed (it now includes a ladder climb). Frames: `QvaleInteriors/batch16/`. Open, the user's call: all five rooms share one timber shell; an optional Batch 16b brief for more per-home character is at the end of Review 29.
  - **Batch 16b prompt written (8 Oct, the user asked for it; not sent):** `Tools/ArtRequests/CODEX_PROMPT_BATCH16B_INTERIOR_COLOUR.md` gives each home its own palette and ceiling as `Interior_Qvale_*_Room_v2` (names checked free), with each Batch 16 `REGISTRATION.json` frozen in X and against the walk line, so on delivery only the sprite names in `MraTownBuilder` change. Amended the same day after a friend of the user found the floor too deep: the back wall now meets the floor on the walk line, and the 160 px below is the floor seen edge-on.
  - **Batch 16b delivered; Old Man room on trial in-game (9 Oct, the user's ask; not committed, not in the accepted manifest, no review written):** `Interior_Qvale_OldMan_Room_v2.png` hash-matched `Batch16b/MANIFEST.json` (`1c9ce70c…`) and sits in `Assets/Art/Codex/Town/` next to the Batch 16 room (same import settings, new GUID). `MraTownBuilder.RoomV2` lists the rooms drawn from `_v2` art (OldMan only); remove the entry and rebuild C05 to undo. `BuildHousesBatch` now takes `-mraOnly MR04_C05`. Rebuilt C05 only. Polish: 142 passed, 0 failed. `MraTestDriver.RoomShots` writes door, resident spot and loft frames at 1080p and 720p: `QA/QvaleInteriors/batch16b/`. Findings: Qori's pale legs read on the darker floor (floor L 53 vs 74); the deep floor is unchanged because Codex received the pre-amendment prompt (no SIDE-ON FLOOR section; registration copied unchanged). The other four rooms are not imported.
  - **Umber ground and Batch 16c prompt (9 Oct; not sent):** the user picked the "dollhouse cutaway" room layout (everything on the walk line, a thin lit floor edge, ground in section below) and asked for the Qvale street ground to match the rooms (the street is too bright). Frame mock-up: `QA/QvaleInteriors/ground_mock/`. `Tools/ArtRequests/CODEX_PROMPT_BATCH16C_UMBER_GROUND.md` asks for `Qvale_Top_Strip` and `Qvale_Fill` (copies of the Causeway pair's size, rows and import settings, umber soil about #7A5A3E, kerb about #CDBB98; names checked free) and five side-on `Interior_Qvale_*_Room_v3` rooms built on the 16b paintings. On delivery, MR04 needs its own surface in `MraBuilderArt` (it borrows `Terrain/Body/Causeway/Causeway` today; MR02 must keep the Causeway pair).
  - **Batch 16c delivered and reviewed (9 Oct, `CLAUDE_REVIEW_40.md`): pass on all 7, nothing imported.** Hashes match; registration changes only the door height; the soil is #7A5A3E-ish in strip, fill and rooms. Notes: a faint mirrored cross at the fill's tile joins, the Loft's first tread painted as a bench, the OldMan ladder guide vs the painted feet. The wiring steps are at the end of the review; wait for the user's "go".
  - **Batch 16c in the game (9 Oct, at the user's "put in the game"; not committed, waiting for their play-test):** the 7 are accepted (manifest **1165**, preservation 0 mismatches; `build_codex_v2_manifest.py` now reads `Batch16c/MANIFEST.json`) and imported (VerifyAll 0 problems): rooms in `Assets/Art/Codex/Town/`, ground in `Assets/Art/Codex/Terrain/Body/Qvale/`. `MraBuilderArt` MR04 uses `Qvale_Top_Strip`/`Qvale_Fill` (MR02 keeps the Causeway pair). `MraTownBuilder.RoomVersion = "_v3"` for all five rooms, the loft included (set it to "" to go back to Batch 16). Rebuilt MR04 and the five interiors. Polish: 142 passed, 0 failed (ladder climb and loft stair pass). `MraTestDriver.RoomShots` now saves every street door and room at 1080p and 720p: `QA/QvaleInteriors/batch16c/` (sheet `batch16c_ingame_sheet.jpg`). The 16b trial file `Interior_Qvale_OldMan_Room_v2.png` is still in `Assets/Art/Codex/Town/` but unused and not accepted. **Steps fix (9 Oct, from the user's screenshot):** the Causeway cliff kit (pale corners, feet and faces) showed as ghost kerbs under each Qvale step on the umber soil, and the pale `Body_Crease_` cracks glared on it. MR04 now points `cliffKit`/`face` at `Terrain/Body/Qvale/Qvale*` names that don't exist yet (plain soil steps; a future Qvale kit under those names is picked up automatically), and `RockDressing` skips MR04. Rebuilt MR04; polish 142 passed, 0 failed. Step ends are now plain cuts. The user wants proper step art and a darker kerb: **Batch 16d prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16D_QVALE_STEPS.md` (Qvale_Top_Strip_v2 with the kerb retoned to about #B39C78, plus the seven Qvale cliff-kit pieces copying the Causeway kit's canvases and edges; names checked free). On delivery only the strip name in `MraBuilderArt` changes; the kit names are already wired. **Batch 16d delivered and reviewed (9 Oct, `CLAUDE_REVIEW_41.md`): pass on all 8, nothing imported.** Note: on the short MR04 entry treads the 4.27 u corners leave faint stone fragments under the tread above; judge in-game, clip in `MraTerrain` or keep the kit off. **In the game (9 Oct, at the user's "go"; not committed):** the 8 accepted (manifest **1173**, 0 mismatches; `build_codex_v2_manifest.py` reads `Batch16d/MANIFEST.json`) and imported (VerifyAll 0 problems). MR04 uses `Qvale_Top_Strip_v2` and the Qvale kit. Rebuilt MR04; polish 142 passed, 0 failed. Captures `QA/QvaleInteriors/batch16d/` (`stairs_nokit_vs_kit.jpg`). Seen in-game: on tall risers the corner's light stones stop in a hard horizontal line where the darker side piece starts (art, not a pixel seam: `MraTerrain.Wall` starts the side at the corner's bottom row), and faint stone bits show under the tread above. To drop the kit, point `f.cliffKit`/`f.face` at names that don't exist. **Corner crop (9 Oct, from the user's screenshot):** the 4.27 u top corner's level lip poked out under the slope above the lamp step. New `MraTerrain.cornersFollowTread` (default off; set for the Qvale family in `MraBuilderArt.Terrain`) crops each top corner to the flat tread beside its riser (at least 0.9 u, 0.45 u fade). Rebuilt MR04; polish 142 passed, 0 failed; frames `batch16d/crop_*`. **Town-step corners (9 Oct, the user: "act as a professional game designer and fix it"):** `cornersFollowTread` now runs each top corner along the tread beside it (full on a flat tread, cropped with a 0.5 u fade at the next riser, bent along a slope via a per-column `dy` in `Patch`), leaves the talus feet off, and on risers taller than the corner repeats the corner's own dressed-stone column (`townCourseL/R`, joint rows 302-478 / 249-495 measured on the 16d corners) instead of the side art. Other regions unchanged (flag off). Rebuilt MR04; polish 142 passed, 0 failed; sheet `batch16d/town_steps_sheet.jpg`. The user still saw cuts and asked for proper assets: **Batch 16e prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16E_QVALE_STEP_SET.md` (inputs in `Tools/ArtRequests/Batch16e/`, incl. `qvale_surface.json`, the built Qvale walk line and its 8 risers: 1.04 x2, 1.63-1.88 x4, 5.74, 6.25). 12 pieces: `Qvale_Top_Strip_v3` (no pale band under the kerb), `Qvale_Step_Short_L/R` (riser 1.04), `_Medium_L/R` (1.75, stretch up to 8%), `_Outer_L/R` + `_Wall_A/B/C` + `_Inner_L/R` for tall walls; every join a painted fade or clump. On delivery, `MraTerrain` needs the new assembly described in the prompt (replacing the cliff-kit path for Qvale). **Batch 16e delivered and reviewed (9 Oct, `CLAUDE_REVIEW_42.md`): pass on 6 (Strip_v3, Wall_A/B/C, Inner_L/R), redo 6 (Short, Medium, Outer L/R): their step-top grass is a raised hedge box with straight edges. Nothing imported. Batch 16f prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16F_QVALE_STEP_TOPS.md` (`_v2` of those 6, grass only). Review 42 also answers Codex: the game will bend both the upper and lower runs along the ground, moving whole pixel columns. Import all 12 together once 16f passes. **Batch 16f delivered and reviewed (9 Oct, `CLAUDE_REVIEW_43.md`): pass on all 6; the 12-piece step set is complete. Nothing imported; the wiring steps are at the end of Review 43. **In the game (9 Oct, at the user's "go"; not committed):** the 12 accepted (manifest **1185**, 0 mismatches; `build_codex_v2_manifest.py` reads `Batch16e`/`Batch16f`) and imported (VerifyAll 0 problems). New `MraTerrain` step-set path (`stepShort/Medium/Outer/Inner L/R`, `stepWalls`, `StepParts`, used when `HasStepSet`, ahead of the cliff kit): Short up to 1.3 u, Medium to 2.05 u (mesh-stretched between the walk rows), taller = wall courses + Outer + Inner; columns move whole along `GroundAlong` (upper and lower runs); the strip fades in over the pieces' fade bands (`StripAlpha`); higher steps draw over lower ones. `MraBuilderArt`: `Family.stepSet`, MR04 uses `Qvale_Top_Strip_v3` and `Qvale_Step_`, no cliff kit (the 16d pieces and `cornersFollowTread`/`townCourse` code stay but are unused in Qvale). Rebuilt MR04; polish 142 passed, 0 failed. Frames `QA/QvaleInteriors/batch16ef/` (sheet `qvale_step_set_sheet.jpg`). The user then saw the face stones slant where a step sits at the bottom of a slope (risers 3, 6 and 8: whole-column bending shears the face). **Batch 16g prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16G_QVALE_SLOPE_TOPS.md` (Short/Medium/Outer `_SlopeTop_L/R`, the slope painted into the upper run, registered walk-line points; names checked free). On delivery: `StepParts` picks a SlopeTop piece when the upper ground isn't flat and draws it unbent. **Seams (9 Oct, the user: remove the fading area between pieces):** the strip/piece crossfade (320-512 px) ghosted two kerbs. Now each tread's strip is laid out to match its step's upper run (that run is the strip's own stones at column offset 256, mirrored on R pieces; `StepSeams`, `StripU`), and each run meets the strip at a mortar joint of the strip (`stripJointsPx`, set in `MraBuilderArt`): stones blend over 6 px, grass and the moss line over 0.12 u, soil over 0.5 u; pieces are clipped at the seam. Rebuilt MR04; polish 142 passed, 0 failed; `batch16ef/seams_before_after.jpg`. **Batch 16g delivered and reviewed (9 Oct, `CLAUDE_REVIEW_44.md`): pass on all 6 SlopeTop pieces; nothing imported; wiring steps at the end of Review 44. **In the game (9 Oct, at the user's "go"; not committed):** the 6 accepted (manifest **1191**, 0 mismatches; manifest tool reads `Batch16g`) and imported (VerifyAll 0 problems). `MraTerrain`: `stepShort/Medium/OuterSlope L/R`, `short/medium/outerSlopeRise` (registered walk lines), `slopeTopRows`; `SlopedTop` picks them when the ground beside a riser's top climbs, and `Grid` bends the upper run only by what the painting doesn't already climb (nothing on Qvale's three); slope-top risers set no strip layout. Rebuilt MR04; polish 142 passed, 0 failed; `QA/QvaleInteriors/batch16g/slope_tops_sheet.jpg`.** The user then saw a colour difference: the strip's kerb stones are paler, cooler and flatter than the step pieces' (measured percentiles in the prompt). **Batch 16h prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16H_QVALE_STRIP_MATCH.md` (`Qvale_Top_Strip_v4`, v3 regraded to the pieces, same alpha, grass and joint columns; name checked free). On delivery only the strip name in `MraBuilderArt` changes. **Batch 16h delivered and reviewed (9 Oct, `CLAUDE_REVIEW_45.md`): redo.** The graded stones match the pieces (stone-only percentiles), but the under-kerb soil targets were mismeasured (moss included) and the grade turned that band into orange speckle. **Batch 16i prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH16I_QVALE_STRIP_V5.md` (`Qvale_Top_Strip_v5` = v4 rows 0-145 + v3 rows 157+, blended between). **Batch 16i delivered and reviewed (9 Oct, `CLAUDE_REVIEW_46.md`): pass; nothing imported; **in the game (9 Oct, at the user's "go"; not committed): accepted (manifest **1192**, 0 mismatches) and imported; MR04 uses `Qvale_Top_Strip_v5`; rebuilt MR04; polish 142 passed, 0 failed; `QA/QvaleInteriors/batch16i/strip_v5_before_after.jpg`.** **Grass level (10 Oct, the user's last fix):** the strip's grass stands 48 px over its walk row, the step set's 26 (16f planting at 55%), and the pieces' kerb tops sat 7 px lower. `StripAlpha` now keeps the strip's grass and moss line on over the step pieces (they draw over it) up to 0.05-0.35 u from each riser edge, and `Grid` draws pieces `KerbLift` (7 px) higher. Rebuilt MR04; polish 142 passed, 0 failed; `batch16i/grass_level_before_after.jpg`. **Kerb bottoms (10 Oct):** the user said the soil was fine (the soil darkening and the 7 px `KerbLift` were reverted; the upper runs are the strip's own rows) and pointed at the kerbs' bottom edges not lining up at the seams. Measured the pale ledge under each lower run: Short 46 px under its walk row, Medium 58, Inner 57.5, strip 56. `Grid` now moves each lower run by its offset (`ShortLowerDrop` 10 px down, `MediumLowerDrop` 2 px up, `InnerLowerDrop` 1.5 px up), easing in 0.25-0.45 u from the riser. Rebuilt MR04; polish 142 passed, 0 failed; `batch16i/kerb_bottoms_aligned.jpg`. Still visible up close: the 16h grade made the strip's ledge a little brighter than the pieces' at upper seams.
  - **Side effect for the user to judge:** the three houses are now smaller next to Qori. The doors are only a little taller than him, and the mapmaker's and old man's rooms are low. To undo, empty `PaintedHeight` and rebuild MR04 and the houses.

### 4.6 Route bar HUD (8 Oct; not committed, waiting for the user's play-test)

- **What:** a bar at the top middle of the play screen in every region scene (not in side chambers or homes): Qori's progress from the region's entry (left) to its exit (right), the metres left, and markers. `Assets/MountainReliefAtlas/Scripts/MraRouteBar.cs`, made by `MraSession.Awake`.
- **Distance:** measured along the region's route edges in order (`edges[].points`, the gameplay surface), so switchbacks count their full length; 1 unit = 1 m. Qori's place is the Chart's projection (`MraRouteTracker` / `MraRoute.Locate`). MR01 is 304 m.
- **Markers:** at their node's distance along the route. Caves: found ones with the cave icon (dimmed once the find is taken), unfound ones as the unexplored mark, `secret` caves hidden until found. Plus the shrine, the Waymark and small pips for resting places. Rebuilt when save flags change. Knots are left out, as on the Chart.
- **Art:** the accepted, previously unused `HUD_Resource_Bar` frame and `HUD_Resource_Bar_Fill` (sliced at run time; pixels and import settings untouched), via new `MraChartArt.routeFrame` / `routeFill` (set in the asset and in `MraBuilderReview.BuildChartArt`). Icons are the accepted Map_Icon set. Stand-ins: the cave icon is the chest (`Map_Icon_Secret`), rest pips are cream diamonds, and the backing plate is a plain rectangle. **Batch 17 prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH17_ROUTE_BAR.md`.
- It fades out for menus, the Chart, a knot's stir, the reveal, the ending and the listening seat. It narrows on screens less wide than 16:9.
- Polish check (now with a route-bar section): 107 passed, 0 failed. Captures: `Tools/MountainReliefAtlas/QA/RouteBar/`.

### 4.7 Reclining titan route (8 Oct; not committed, waiting for the user's play-test)

The user approved `Tools/Design/QOLOSSAL_RECLINING_TITAN_PROPOSAL.md` (map `QOLOSSAL_RECLINING_ROUTE_MAP_DESIGNER_SPOILERS.png`) and asked for the game to follow it, keeping all background art as it is. Defaults taken: drift levels over four areas (shin, over Qvale, chest, face), all optional; MR05 keeps its length with a gentler belly start.

- **How:** `Tools/MountainReliefAtlas/reclining.py` edits `mra_world.json` after conversion (`convert_spec.py` now calls it, so a re-conversion keeps it; it is idempotent: `python reclining.py`). Then `design_route.py --plots`, then the Unity rebuild. The world data carries `layout: reclining-titan-2026-10-08`.
- **Region order and links:** unchanged (MR01 to MR08). Return arrivals follow the moved exit beats.
- **Body map (designer spoilers only, never shown):** a `body` field on every region and on the caves that have a body place (`MraData`: `Region.body`, `Chamber.body`).
- **MR03 (knee, then thigh):** the climb to the Terraces knot (y 158, the kneecap) is unchanged; N09-N11 now run down the thigh (N09 300,140; N10 350,112; N11 400,92). The region is 45 u longer (size 425, camera max x 437). Root Hanger (C06, Glidecap) stays at N09, which is now under the knee (the Under-Arch in the design; it still uses the normal cave mouth).
- **MR05 (belly, then ribs):** N01-N03 are a gentler open slope; N04-N11 are 18 u lower than before. Moss Well (C03) stays at N03, the end of the belly (the Navel Well in the design; normal cave mouth).
- **MR06 (shoulder, neck dip):** N09 350,232; N10 390,224 (glide launch); N11 440,202. The glide span now crosses into the neck dip and the region ends low. The moved exit beat is (440, 202) in `design_route.MOVED`, `MraWorldBuilder.Moved` and `MraTestDriver.Pos`.
- **Floating-platform levels (drift levels), optional, placeholder layouts:** chambers of type `drift-level`, built by `Editor/MraDriftBuilder.cs` (`MraWorldBuilder.BuildDriftBatch`), each an open-sky scene with the region's sky and Far layer:
  - `MR02_P1` Stepping Stones (launch at MR02_N09; no relic; a thread anchor to the top shelf; song shell);
  - `MR03_P2` Lantern Drift (MR03_N10 Town overlook; Bloomfall pods; heart seed);
  - `MR05_P3` The Swell Isles (MR05_N09; Wind Leaf dashes, slowly bobbing stones; rare find);
  - `MR07_P4` Updraft Stair (MR07_N04 Reed bed; Glidecap glides between rising stones; Lore Stone).
  - Stones: the region kit's accepted `*_Platform_Floating_*` and `Platform_Moving_*`, the accepted `Platform_Crumble`; the launch point is the accepted `Breath_Vent_Exhale` with `FX_Breath_Current`. A fall returns Qori to the last safe stone (`MraDriftRest`, new `PlayerMovement.SetRespawn`). They show as caves on the Chart and the route bar.
- **Rebuilt:** MR02, MR03, MR05, MR06, MR07 and the four drift scenes (added to the build list). Polish check: **139 passed, 0 failed** (new parts: "reclining route" walks the thigh descent and the neck dip; "drift levels" checks each launch point, stone counts, movement, a fall and the reward; the side-chamber pass now runs `DriftTrial` too). Captures: `Tools/MountainReliefAtlas/QA/Reclining/`. Side views: `Tools/MountainReliefAtlas/QA/route_design/`.
- **Second stage (8 Oct):**
  - **MR07 as the face:** a new long bare ridge, the Prow (the nose), between the pale steps (N02) and the reeds (N03); N03-N11 are 30 u further on (region size 380, camera max x 392). The rest keeps its order, which already climbs chin (lower falls), lips (pale steps), lashes (reeds), eye (lake and seam), brow, forehead (knot), crown (lookout and reveal). `reclining.py` now also moves modules and encounter centres along x with their edge.
  - **Reveal camera:** `MraReveal` first looks back down the face (`lookBack`, `lookBackSize` 22, 3.5 s, a 0.8 s hold), then pulls out over the whole Summit (centre and size from the region's beats, set in `MraWorldBuilder.Reveal`). The stills are unchanged (`Reveal_Face_01_Brow`, then the seated final-assembly concept). `Reveal_Face_02/03` show the seated pose (a raised hand): **do not wire them in**; Batch 21 asks for reclining replacements.
  - **Ending:** unchanged in code. `Ending_Careful_01_HeadLift` and `_02_Lap` (the hand beside the lit town) still fit; Batch 21 asks for the knee lowering and the hand closing round the town.
  - Rebuilt MR07. Polish: **142 passed, 0 failed** (adds the Prow walk and the reveal's look-back). Audio: **144 passed, 0 failed**. `MraCapture` now also writes `08_reveal_lookback` and `09_reveal_pullout` for MR07. Frames: `QA/Reclining/summit_reveal_sheet.jpg`.
- **Not done yet:**
  - The Chart: the regional relief pages are still the seated design's drawings (MR03's page shows a climb where the route now descends; MR07's page has no Prow; the markers still follow the route); the post-reveal whole map is still the seated concept. Needs the Batch 18 chart painting (prompt extended).
  - The Batch 21 stills, once delivered: the reveal shows `Reveal_Reclining_01/02` instead of the final assembly concept; the ending adds `Ending_Reclining_01/02`.
  - Warm air rising off the Prow's end (a clue); P2's town effect (the sky over Qvale lit once the lanterns are recovered); the clue trail update; retiring `Proto_Palm`.
  - The full play test (`MraPlayTest.Run`, about 35 min) has not been rerun.
- **Art prompts, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH20_DRIFT_STONES.md` (drift stone kit, launch vent, Under-Arch and Navel Well mouths, drift map icon; Batch 19 is the user's Qori look-development prompt); `CODEX_PROMPT_BATCH21_RECLINING_STILLS.md` (two reveal and two ending stills); `CODEX_PROMPT_BATCH19B_QORI_LOOK.md` (8 Oct: the Qori v3 look revision after `CLAUDE_REVIEW_30.md` and the user's answers; concept only, with the cape built for a skinned-mesh rig; delivered and reviewed in `CLAUDE_REVIEW_31.md`, and the user approved the look); `CODEX_PROMPT_BATCH22_QORI_RIG_PARTS.md` (8 Oct: the Qori v3 rig parts, 14 pieces from 10 paintings plus face and hand swaps, with registration JSON and bend proofs; limbs default to 15% thinner than 19b because the user didn't choose; delivered, but failed `CLAUDE_REVIEW_32.md` on assembled proportions); `CODEX_PROMPT_BATCH22B_QORI_RIG_FIX.md` (8 Oct: the correction pass, with landmark targets measured on the 19b staff idle; delivered, and `CLAUDE_REVIEW_33.md` passes the fit, but the knee and elbow caps still show; recommended continuous skinned arm and leg paintings as Batch 22c, awaiting the user; a second independent review, `CLAUDE_REVIEW_34.md`, agrees and adds the near-ear crest cut and stray leaf fragments on the mantle PNG; the user then asked for the 22c prompt and a test rig: `CODEX_PROMPT_BATCH22C_QORI_LIMBS.md` (continuous skinned arm and leg paintings, 19b tan-brown bark, stride feet apart, mantle cleanup), and a test-only rig in the scratch copy `C:\_temp\qp\Assets\QoriRigTest` (hash-checked 22b parts, `QoriV3TestRig` poser, prefab and scene, built by `QoriRigTestBuilder.BuildAndCapture`; near ear drawn behind the head; capture `Tools/IncomingArt/Codex_v2/QoriRigTest_22b_Poses.jpg`). Copying it into the live project and the polish run were blocked by the permission check, waiting for the user); `CODEX_PROMPT_BATCH18_WORLD_MAP.md` now also asks for `Chart_Final_Reclining_DESIGNER_SPOILERS`.
- **Qori v3 is ON HOLD (user's decision, 8 Oct 2026 evening): keep the current Qori.** Driven by the original rig's movement, v3 read as the same Qori with a different cloak, so the user stopped the work and asked to park everything: `Assets/QoriRigTest/` (rig, test scenes, scripts), the Codex 22c–22f deliveries in OneDrive and the 22d–22f prompt tools stay uncommitted and unused. Don't resume, import or send more Qori v3 prompts unless the user asks. History below.
- **Qori v3 rig, 8 Oct (not committed):** Batch 22c reviewed in `CLAUDE_REVIEW_35.md` (legs pass; the arm has a seam at row 172 and a shoulder knob). The test rig is now in the real project at `Assets/QoriRigTest/` (own scene, prefab and menu Qolossal > Qori Rig Test > Build), rebuilt on the 22c parts with two-bone skinned arms and legs read from the registration; notes and checks in `CLAUDE_REVIEW_36.md` (compiles clean, draw order 17/17, polish 142 passed, 0 failed), capture `Tools/IncomingArt/Codex_v2/QoriRigTest_22c_Poses.jpg`. The in-game Qori and all scenes are untouched. **Found:** the project's current quality level (Very Low) allows one skin weight per vertex, so a SkinnedMeshRenderer hinges every bend; the rig skins on the CPU instead, and the shipped quality levels need at least two weights before production skinning. **Batch 22d prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH22D_QORI_ARM.md` (arm only, plus NearHand/FarHand draw order 8.9/1.9), with a measured template, masks and a 59-test checker in `Codex_v2_full/Batch22d_Input/` (generator `Tools/ArtRequests/Batch22d/make_batch22d_input.py`). **Batch 22d delivered and reviewed in `CLAUDE_REVIEW_37.md`: fail on the wrist only.** Checker 59/59 pass, other parts and registration as required, elbow and shoulder clean in the rig; but the flare under the wrap and the hand cover (arm rows 236–256) are smeared and grainless with a hard bottom edge over the Grip and Spread hands. Recommended next: a 22e that repaints only rows 236–256, plus checker tests for texture detail and the cover edge. The rig now runs on the 22d arm and registration (receipt updated; capture `QoriRigTest_22d_Poses.jpg`). **Batch 22e prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH22E_QORI_WRIST.md` (repaint arm rows 180–256 only: the wrap, the flare and the hand cover; rows 0–179 locked to 22d; everything else byte-identical), inputs and a 75-test checker in `Codex_v2_full/Batch22e_Input/` (generator `Tools/ArtRequests/Batch22e/make_batch22e_input.py`; fails 22d on 12 tests, passes the mock `Tools/ArtRequests/Batch22e/mock/make_mock22e.py`, which also looked clean in the rig). **Batch 22e delivered and reviewed in `CLAUDE_REVIEW_38.md`:** checker 75/75, the wrist now runs cleanly into every hand in the rig; fails only on dark, stair-stepped bark flaps beside the wrap (partly caused by the 22e inner mask demanding the old leaf-tip silhouette). Recommended: a small 22f repainting only the wrap's outer edge, after fixing the masks; or accept 22e for the in-game test (flaps are 1–2 px at game scale). The rig now runs on the 22e arm and registration, and its far-grip overlay is cut below the arm (`Generated/Qori_v3_Hand_Grip_FarFingers.asset`), so the far wrist no longer shows a hand edge; capture `QoriRigTest_22e_Poses.jpg`. **Batch 22f prompt, not sent:** `Tools/ArtRequests/CODEX_PROMPT_BATCH22F_QORI_WRAP_EDGE.md` (only the wrap's outer edge band may change from 22e; smooth antialiased silhouette, no dark band), inputs and an 85-test checker in `Codex_v2_full/Batch22f_Input/` (generator `Tools/ArtRequests/Batch22f/make_batch22f_input.py`; fails 22e on the 5 edge tests, passes the mock `Tools/ArtRequests/Batch22f/mock/make_mock22f.py`, which showed no flaps in the rig). **Batch 22f delivered and reviewed in `CLAUDE_REVIEW_39.md`: pass** (checker 85/85, no flaps or steps at 2x in all five rig poses). The Qori v3 parts now stand as the 22c parts with the 22f arm and registration; the rig runs on them (receipt updated; capture `QoriRigTest_22f_Poses.jpg`). Recommended next: an in-game import test of the rig on the real Qori in a test scene, settling the skin-weight quality level and the far-grip runtime masking. **In-game test, 8 Oct (not committed):** `Assets/QoriRigTest/QoriV3InGameTest.unity` (built by menu Qolossal > Qori Rig Test > Build In-Game Test Scene; batch `QoriV3InGameTest.Run -captureDir <dir>` with `-noquit`) is the Qori movement lab with the v3 rig on the real Player: `QoriV3OnPlayer` hides the current cutout rig's sprites (layer 30; v3 on layer 31; the old rig, weapon and thread keep running underneath) and poses the v3 rig procedurally from `PlayerMovement` (walk/run cycle, lean, bob, rise/fall, facing). Scaled to the current Qori's 1.727-unit standing height on the same feet line (rig scale 0.1054). In play mode V switches old/new. `QoriV3InGameDriver` walks, runs, jumps, glides and turns with a virtual keyboard (sets the Input System focus rules like `CameraFeelPlayTest`, restored after) and saves current/v3 close-ups and game views. Capture `Tools/IncomingArt/Codex_v2/QoriV3_InGame_Test.jpg`. Not yet on v3: the carried Leaf Staff, the glidecap, attacks, thread/rope, wall and ledge, hurt and sit poses. The lab scene and Player prefab are untouched. **Rejected by the user (8 Oct):** the procedural motion, the look and the scale didn't match. **Redone the same day:** `QoriV3OnPlayer` now puts the v3 rig inside the original rig's Root and copies every original bone angle each frame (Body, Torso, Head, ears ← EarLower/EarUpper, Cape ← Cape1, arms ← the original's camera-side "Far" limbs and the ledge reach arms when shown, legs, feet), plus the body bob/squash offset, the blink/hurt/effort face, and the original weapon moved into the matching v3 hand (order 18 back hand, 88 camera hand). Size: v3 hip-to-neck = original's (scale 0.916 in Root, 0.1341 world), feet on the original's feet line. Capture `Tools/IncomingArt/Codex_v2/QoriV3_InGame_Test2.jpg`.

## 5. Other open items

- Wire in Reveal_Face_02/03 and Ending_Careful_03 (accepted art, not yet in the game).
- Decide the loft exterior layout.
- Wire the Batch 13 input glyphs into `MraState.Prompt`.
- Chart round 2 is **on HOLD** (the user's call).
- Rerun the Windows performance profile on a quiet machine with the game window focused.
- Summit lake and reed terrain art; the slate A6 terrain clashes with the approved Summit.
- Weapon tiers and healing items; an Amber balance pass.
- Tune the hit-stop durations (HitStop.cs) after the user's play-test.
- **Reclining titan route:** approved 8 Oct and partly in the game; see section 4.7 for what's done and what's left.

## 6. Last test results (7 Oct 2026, scratch copy)

| Check | Result |
|---|---|
| Audio (`-mraAudioOnly`), after the footstep and glide changes | 137 passed, 0 failed |
| Audio (`-mraAudioOnly`), after the 8 Oct step and glide pass | 143 passed, 0 failed |
| Polish (`-mraPolishOnly`), after the 8 Oct step and glide pass | 107 passed, 0 failed |
| Polish (`-mraPolishOnly`) | 90 passed, 0 failed (6 Oct) |
| Full play test | 656 passed, 0 failed (6 Oct) |
| Accepted art preservation | 1153, 0 mismatches (7 Oct, after Batch 15c) |
| Polish (`-mraPolishOnly`), after Batch 15 wiring | 90 passed, 0 failed (7 Oct) |
| Polish (`-mraPolishOnly`), with the route bar | 107 passed, 0 failed (8 Oct) |
| Polish (`-mraPolishOnly`), reclining route and drift levels | 139 passed, 0 failed (8 Oct) |
| Polish, after the Summit Prow and the reveal look-back | 142 passed, 0 failed (8 Oct) |
| Audio, after the reveal change | 144 passed, 0 failed (8 Oct) |
| Consoles, and PC above 1080p | not tested |

Batch mode can't judge how anything sounds or looks. The user's play-test decides.

## 7. Working method

### Batch work goes to a scratch copy
The user's Unity editor is usually open on the project, so batch runs use the scratch copy `C:\_temp\qp`.

- Helper: `C:\_temp\qp_args.ps1 <Method> "<extra args>" <minutes> [-noquit]`. It robocopies `Assets` into the scratch copy, runs Unity in batch mode with `-saveFile mra_playtest.json`, and prints matching log lines. The log is `C:\_temp\qp_<Class>.log`.
- **`MraPlayTest.Run` needs `-noquit`**, or it exits before testing.
- Copy back **only** the changed scenes, assets and `.meta` files.
- New scripts need deterministic `.meta` files: `Tools/Audio/write_script_meta.py`.
- The wrapper's exit codes 2 and 3 are noise. Read the log.

### Tests

| Command | What it runs |
|---|---|
| `MraPlayTest.Run` | full play test (about 35 min) |
| `MraPlayTest.Run "-mraPolishOnly"` | polish checks (about 5 min) |
| `MraPlayTest.Run "-mraAudioOnly"` | audio checks |
| `MraPlayTest.Run "-mraEncountersOnly"` | encounter checks |
| `MraPolishCapture.Run -polishTag X` | polish captures |

### Batch-mode quirks
- Frames are uncapped, so waits must use unscaled time.
- `WaitForEndOfFrame` hangs.
- The game view is 4:3.

### Shell
Bash heredocs containing quotes break in this shell. Write edit scripts to a scratchpad `.py` file and run them.

### Codex art workflow
Codex delivers to `OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\BatchN`. The staging folder is `Tools/IncomingArt/Codex_v2`.

1. Verify the delivery hashes against its `MANIFEST.json` (`assets[].path` / `sha256`).
2. Run the global name check. The manifest is keyed by name, so a duplicate name collides.
3. Look at the proofs.
4. Accept: `python Tools/ArtImport/build_codex_v2_manifest.py accept <names>`.
5. Import on the scratch copy with `CodexArtImporter.ImportAll`, then `VerifyAll`, and copy the PNGs and `.meta` files back.
6. Check preservation: `python Tools/MountainReliefAtlas/verify_preservation.py <receipt.json>`.
7. Write `Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_N.md`. Those reviews are what the user pastes to Codex.

Never import straight from staging. Only hash-pinned, reviewed pixels reach the game.

### Music
Music comes from Suno: 12 WAVs in `Assets/Audio/Music`, played by `MraMusicPlayer`. Don't ask Codex for music.

## 8. How the user likes to work

- Show screenshots or clips where something is visual, and **wait for their play-test before committing.**
- Give short, plain answers with a recommendation, not a long list of options.
- Don't approve or reject audio takes or art picks on their behalf. Those choices are theirs.
- The user sends requests to Codex themselves. Claude writes the requests and the reviews.

## 9. Suggested first message in the new session

> Read QOLOSSAL_SESSION_HANDOFF.md and the docs it lists, then tell me the state and what's waiting on me.

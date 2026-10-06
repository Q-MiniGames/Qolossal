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

### 4.2 Background audit and Batch 14 backgrounds

- **Proposal:** `Tools/Backgrounds/BACKGROUND_ENHANCEMENT_PROPOSAL.md`, with Option B recommended. Evidence and mockups M1–M5 are in `Tools/Backgrounds/proposal/`.
- **Audit tool:** `MraBackgroundAudit.Run [-mraOnly MR03] [-fgMatte]`.
- **Depth parallax** is now used in **MR01 Cradle, MR03 Terraces and MR07 Summit** (`MraWorldBuilder.DepthSets`).
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
  - MR02, MR05 and MR06 still show the hard rectangle edge on their far paintings (only 38–53% coverage) and aren't in Direction A.
  - The remaining Option A fixes aren't done: framing, haze, mipmaps.
  - The background strips are enlarged 1.35x at 1080p.
  - KneeTerraces is excluded (it reads as a knee).
  - Do Option A only when the user says go. The camera framing change (A6) needs their play-test.
- **Next background step to offer the user:** fix MR02, MR05 and MR06, either with Option A on the existing art or with a new Codex batch.

### 4.3 Art: Codex Batch 14, Review 25 (done)

- **Delivery:** `OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch14` (PNGs in `Backgrounds/`, plus `MANIFEST.json`).
- **Accepted:** all 8 (Cradle Far/Mid/Near, Summit Far/Mid/Near, Qvale VaultRim_Mid, Qvale Overlook_Vista). They are in `Assets/Art/Codex/Backgrounds/`.
  - The accepted manifest holds **1140** files.
  - On 7 Oct all 8 were re-verified against the delivery manifest, and all 1140 accepted files were confirmed byte-identical.
- **Batch 15 touch-ups are written in `CLAUDE_REVIEW_25.md` but not sent to Codex**, and there's no Codex prompt yet:
  - two dark smears on the `BG_Cradle_Far` skyline (x≈1925 and x≈2325, rows 585–620);
  - stair-stepped tree crowns on `BG_Cradle_Near` and `BG_Summit_Near`;
  - a few of the same steps on `BG_Cradle_Mid` and `BG_Qvale_VaultRim_Mid`.

  The delivery will be reviewed as Review 26.
- **Harmless note:** `verify_preservation.py` reports "1 duplicate": `MraChartArt` is both a script and an asset, both already committed.

### 4.4 Other

- `Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH14_BACKGROUNDS.md` and `CODEX_PROMPT_BATCH14.md` are the Batch 14 request (already delivered).

## 5. Other open items

- Wire in Reveal_Face_02/03 and Ending_Careful_03 (accepted art, not yet in the game).
- Decide the loft exterior layout.
- Wire the Batch 13 input glyphs into `MraState.Prompt`.
- Chart round 2 is **on HOLD** (the user's call).
- Rerun the Windows performance profile on a quiet machine with the game window focused.
- Summit lake and reed terrain art; the slate A6 terrain clashes with the approved Summit.
- Weapon tiers and healing items; an Amber balance pass.
- Tune the hit-stop durations (HitStop.cs) after the user's play-test.

## 6. Last test results (7 Oct 2026, scratch copy)

| Check | Result |
|---|---|
| Audio (`-mraAudioOnly`), after the footstep and glide changes | 137 passed, 0 failed |
| Polish (`-mraPolishOnly`) | 90 passed, 0 failed (6 Oct) |
| Full play test | 656 passed, 0 failed (6 Oct) |
| Accepted art preservation | 1140, 0 mismatches |
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

# Qolossal: roadmap to release (10 Oct 2026)

Written from `QOLOSSAL_SESSION_HANDOFF.md`, the design docs and a check of the repo. Qori v3 is on hold and is not planned here.

## Where the game stands

**Built and playable (MRA prototype):** all eight regions MR01–MR08 in order, Qvale with five interiors, 38 side chambers, four optional drift levels, the reclining-titan route (thigh descent, neck dip, the Prow), the reveal and the careful ending, the Chart, route bar HUD, five movement abilities, combat with four weapons, Amber, music (12 Suno tracks), 86 SFX takes in use, 1192 accepted art files. Polish check 142/0, audio 144/0.

**Exists but thin:**
- Save: one JSON file with temp/backup (`Assets/Scripts/Progress/GameSave.cs`). No slots, no title-screen Continue/New Game.
- Settings: pause menu has Resume, Chart, Sound (four volumes), Quit. No video, controls, accessibility or language pages.
- Input: Input System, keyboard and gamepad hard-checked in code. No remapping, no platform glyphs (Batch 13 glyphs not wired).

**Missing:**
- Title screen / front end. The first scene in the build list is still the old `OpeningLevel`, with the A0–A2/R1 test scenes ahead of the MRA scenes.
- Localisation (no string tables), subtitles, credits.
- Platform layers: Steamworks, Xbox GDK, PS5 and Switch 2 SDKs, achievements, platform save/suspend. No dev kits or console builds yet.
- Performance: not measured on any target; quality levels allow one skin weight.
- 53 sounds are silent after the user's selection (ambience beds, hurt, death, jump, footsteps).
- Chart pages and final map still show the seated titan (Batch 18). Reveal/ending stills for the reclining pose (Batch 21). Drift-level art (Batch 20) and their layouts are placeholders.

## Milestones, in order

1. **Lock the current build.** Play-test and accept the 10 Oct commits, push to GitHub, rerun the full play test.
2. **Content complete.** Drift levels designed for real (Batch 20 art), Chart and reveal art for the reclining pose (Batches 18, 21), weapons found later, weapon tiers and healing, Amber balance, the clue trail, Summit lake and reed terrain, silent sounds replaced. Retire `Proto_Palm` and the old test scenes from the build.
3. **Game shell.** Title screen with Continue / New Game / Settings / Quit, save slots, video and accessibility settings, input remapping with device glyphs, full gamepad navigation, credits.
4. **Vertical-slice performance.** Windows build profiled at 60 FPS on a mid PC, memory and loading measured. Fix the quality levels.
5. **Polish and playtests.** Outside players (not just the user) play start to finish; fix pacing, difficulty and confusing spots. Localisation if wanted.
6. **PC (Steam) release.** Steamworks (achievements, cloud save), Steam page, trailer, demo optional. Ship PC first.
7. **Console ports.** Register with Nintendo, Microsoft (ID@Xbox) and Sony; get dev kits and Unity console modules; build, profile (Series S and Switch 2 handheld/docked explicitly), implement platform save/user/suspend rules, pass certification. Unity version stays at 6000.3.19f1 until each SDK is confirmed compatible.

## Next things to do

1. Play the current build start to finish and say "push" (or what's wrong). Everything since 7 Oct is committed but not pushed or play-tested.
2. Apply to the console developer programmes now: Nintendo Developer Portal, ID@Xbox, PlayStation Partners. Approval and kits take the longest and only you can apply.
3. Send Batch 20 (drift stones) and Batch 18 (reclining Chart) to Codex; those are the biggest art gaps left in the story.
4. Decide on the 53 silent sounds: re-generate the essential ones (jump, footstep, hurt, death, ambience) or keep them silent.
5. Ask Claude to build the title screen, save slots and settings pages (milestone 3). It needs no new art decisions to start.

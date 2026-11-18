# Route A "Keepers' Cistern" backgrounds: in the game (9 Nov 2026)

Claude installed the approved Batch 49 art (Reviews 97-105) into `Qolossal_Underground`. Nothing is committed.

## What changed

- **Art:** 64 paintings and 20 light maps, copied unchanged from the prototype revisions (newest first: 49d-r1 > 49d > 49c > 49b-r3/r2/r1), into `Assets/Art/Underground/Backgrounds/KeepersCistern/`. Four unused kit pieces were left behind (`CK_Glow_Mint`, `CK_Roots_Crawl_01/02`, `CK_Wall_Bedrock`). The shader is copied as `Qolossal/Environment/KeepersCistern`.
- **Runtime:** `Assets/Scripts/Environment/KeepersCistern/KeepersCisternBackdrop*.cs`, a port of the Editor-only `KeepersCisternPreview`, with the same 515 pieces, placements, motions (FG 110/105, rear 100, MID 88/94, FAR 50/60), room light maps and depth pass. It builds in Play only, as unsaved children. Terrain contact shadows are added 1 s after start, once the generated colliders exist.
- **Scene:** one new root object, `Route A Backdrop - Keepers' Cistern`. The scene diff is only that object. Backup: `backup/Qolossal_Underground.before_keepers.unity` (sha256 a448a21a…).
- **Switch:** `Qolossal > Backgrounds > Route A Keepers' Cistern > Use new art / Use old art`. The old cave art underneath is untouched. `Install or update` re-copies and re-wires everything.
- The Editor previews (Keepers' Cistern and Cistern Continuous) are switched off, so they don't draw a second copy.

## Texture compression: found and fixed

Unity 6.3 only block-compresses a texture with mipmaps when it is power-of-two. Every painting here is NPOT (216 px/u), so with mipmaps on it imported as **RGBA32**: 1,092 MiB on disk, 2,223 MiB in the Editor. A probe proved it (`format_probe*.txt`): 512², 1024×512, 2048² and 4096×2048 compress with mips; 432×648, 1024×576, 2160², 2304² and 3456×2592 do not; mips off gives BC7.

Fix: mipmaps off on the 64 Route A paintings (they're drawn 1:1 at 4K). All 84 textures are now **BC7: 206.7 MiB of storage** (`check.json`).

**Project-wide:** the accepted 4K surface backgrounds (for example `BG_Ribwood_Near_4K`) are also RGBA32 for the same reason. That needs its own pass.

Trade-off: without mips, outputs below 4K minify 2:1. The 1920×1080 checks in `Captures/` look clean in stills, but shimmer while scrolling is untested. Lower-tier memory can no longer come from a mip limit. It needs a platform max-size override (Switch 2: not set until the module docs are available) or smaller masters.

## Checks

- Batch install and check: 0 compile errors, 515 pieces, 0 missing paintings. 14 review cameras at 1920×1080 in `Captures/`; they match the 49d-r1 compositions. `CapturesBefore/` is the same before the mip change.
- A02 pool coping: its top edge is at about y 131, about 5 u above the shaft bottom (y 126), so outside the 4.5 u ledge band.
- **Not tested:** Play-mode traversal in the real scene (the user's play test), the soft A02 lantern-light boundary (still visible), A05 spill VFX, frame time, a Windows player build, and PS5, Xbox Series X|S and Switch 2 (handheld and docked).
- Performance note: the old cave art still renders underneath the new backing. That overdraw should be removed once the new art is accepted.

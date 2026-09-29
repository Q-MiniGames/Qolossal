# Qolossal platform direction

User-approved direction, 2026-09-29. This supersedes the earlier proposal to use the original Nintendo Switch as the minimum target.

## Supported targets

- Windows PC
- PlayStation 5
- Xbox Series X and Series S (validate both)
- Nintendo Switch 2 (validate handheld and TV/docked modes)

**Original Nintendo Switch is excluded.** Do not reduce Qolossal's art, world scope, animation, audio arrangement or gameplay to accommodate it. Switch 2 is a first-class native target using the shared game and approved visual direction. It still has its own build, platform integration, memory/performance budgets and certification; it is not assumed to have PS5 performance. No simultaneous launch, cross-save, cross-play, 4K or 120 FPS commitment is implied.

## Implementation and art rules

1. Keep one shared Unity project and platform-specific build/import profiles. Preserve the current approved Unity version until console SDK compatibility has been verified; do not upgrade just because the target list changed.
2. Target stable **60 FPS / 16.67 ms per frame**, subject to measured CPU/GPU performance on each target. This is the project's design target, not a claim that certification mandates 60 FPS or that the current game achieves it.
3. Preserve accepted source files byte-for-byte, plus existing pivots, PPU, slice rectangles, canvas registration and repeat seams. Derive compressed/downscaled runtime resources through import/build settings. Resolution changes must preserve world scale and registered state families.
4. Keep native painting masters. Do not upscale every asset to 4K or shrink every asset to an arbitrary handheld cap. Existing 1920x1080 cinematic masters remain valid; evaluate their presentation at the chosen output resolution before requesting replacements.
5. Use measured, category-specific texture limits/compression. Prioritize alpha-edge quality, UI/Chart readability and terrain continuity. Test decoded/compressed imported resources, not just PNG masters; do not invent Switch 2 compression formats, platform identifiers or SDK settings without official module documentation.
6. Gamepad navigation must cover all gameplay, Chart, menus, dialogue and settings without a mouse. Preserve PC keyboard/mouse support. Use action-based bindings, remapping and active-device/platform glyphs; verify reconnect, device switching and focus restoration. Do not bake button glyphs or localized text into generic art.
7. Validate UI, critical silhouettes, knot targets, Echo masks, subtitle space and Chart icons at actual handheld and television viewing sizes. Include a 1920x1080 composition proof and optional 1280x720 readability stress proof; 720p is a QA stress size, not a declared Switch 2 output mode. Keep text in scalable UI rather than paintings, with localization expansion and safe-area checks.
8. Retain region loading and profile scene transitions, peak memory and transparent overdraw. Measure dense terrain, fog, water, effects, guardians and Chart together. Select quality options using evidence; do not remove story or gameplay content as a quality setting.
9. Keep the D-A-B-A music motif and existing adaptive stem timing/gain relationships. Profile eight simultaneous streams for decode CPU, memory, loop continuity and transitions on hardware. Preserve MIDI/native sources; choose runtime codecs/loading per platform only after profiling.
10. Keep save data/gameplay separate from platform account, storage, achievements and lifecycle adapters. Implement and test suspend/resume, user changes, save interruption/error recovery and platform-specific behavior with the appropriate SDK.

## Gate for future work

Every new art request, integration task and delivery must carry this target list. Add a platform-readiness section to QA: tested platform/mode, actual resolution, controller coverage, CPU/GPU frame time, peak memory, texture/audio import profile and remaining untested cases. Mark unavailable hardware/SDK tests **not tested**, never pass by inference from editor screenshots.

The existing visual design, art density rules, review decisions and music approval gates remain in force. At this update Review 13 has 732 accepted PNGs; Batch 7/Review 14 art remains a separate request. Music is paused for the user's audition. This platform update does not start another art batch or resume music.

## Required release validation

Build a representative vertical slice including a dense region, traversal/combat, guardian, Chart, save/load, region transition and eight-stem audio. Profile Windows, PS5, Series X, Series S, Switch 2 handheld and Switch 2 docked separately. Obtain platform access, modules and hardware, then establish measured budgets and complete each platform's current certification checklist. Store approval and certification are not completed by this document.

## Public references

- Unity Switch 2 support: https://investors.unity.com/news/news-details/2025/Unity-to-Support-Developers-Targeting-Nintendo-Switch-2-for-Launch/default.aspx
- Unity console workflow: https://unity.com/solutions/console
- Nintendo development process: https://developer.nintendo.com/the-process
- Nintendo Switch 2 play modes: https://en-americas-support.nintendo.com/app/answers/detail/a_id/68487
- Xbox certification: https://learn.microsoft.com/en-us/xbox/gdk/docs/store/policies/console/certification-requirements

Exact non-public requirements must be verified in the approved developer portals; this is project direction, not an SDK or certification specification.

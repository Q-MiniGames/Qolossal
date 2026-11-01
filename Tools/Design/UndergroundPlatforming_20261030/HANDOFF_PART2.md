# Underground: handoff to part 2

Written 1 November 2026, at the end of the first underground thread. Read `QOLOSSAL_SESSION_HANDOFF.md` first; its top sections have the detail.

## What's committed

Committed on the local branch `claude/project-thread-yucm6c`, not pushed:

- The connected underground scene, its greybox platforming data and builder, and the map data.
- Every creature's frames and behaviour: 10 regular enemies, 3 mini-bosses, 3 guardians.
- Combat fixes, hit feel (white blink, squash, push, health bars), the one-way platform fix, Scene Finder, and the test tools.
- Codex's Route A platform kit, revision 02, in `Assets/Art/Underground/PlatformKit/A/`.

Left out on purpose:

- Other work in the same folder, such as the cave rollout tools, the other Codex art and the surface scenes. A clean checkout of this branch alone won't build, because the scene and code depend on that uncommitted work (for example `UndergroundRoom` and `RegionalSceneCatalog`).
- `backup/` and `captures/` in this folder (213 MB), which stay on disk only.

Some shared files hold other sessions' edits as well as ours, such as `PlayerMovement.cs`.

## Open questions for the user

1. **Route A style:** the user said to commit and move on but hasn't given a verdict on the look. Ask before copying it to other routes.
2. **Hit sound:** all game sounds are off (`Sfx.EffectsRemoved`). Propose first, wire nothing without an OK. My suggestion: audition one existing hit sound and one blocked-hit sound.
3. **Sluice Warden:** no bot has beaten it. The Skilled bot learned to avoid the sweep but doesn't climb the ledges, and one flood can cost up to 3 hearts, which may be too harsh. It needs a hands-on verdict.
4. **Heart plinth:** raised by the coordinator; I have no decision recorded. Check with the user. Lowering E's doorway to the Heart would need a terrain re-carve.
5. **PC04 entrance:** this room has no way in yet.
6. **Ludo creature clips:** waiting on the user's go-ahead. The Hollow Shade (E-19) and Lantern Moth (E-21) aren't placed.
7. **Enemy movement:** the user wants to adjust it after judging hit feel. Enemy hit-stun was left for that work.

## Next step

Write the prompt for the next routes' platform kits (C, D and E), once the user approves Route A's style. Follow `Tools/ArtRequests/CODEX_PROMPT_UG_ART_01_ROUTE_A_PLATFORM_KIT.md`, and add thorns and `rock`, which Route A doesn't have. The plan is at https://claude.ai/artifact/HyaPFsasLH6VpTFWR4KDQk.

## Rules learned here

- Run batch tools with Unity closed, and only one builder at a time. Codex shares this folder.
- Play-mode tests must pass `-saveFile <test>`, so the user's save isn't touched.
- Never hand-edit a `UGP Platforming` group: `UndergroundPlatformingBuilder.BuildBatch` replaces them. Art comes through `kit.json` (`Assets/Editor/World/UndergroundArtKit.cs`).
- A MonoBehaviour placed in a scene needs its own file named after the class, or Unity loses the reference.
- The bots test logic, not feel. Say so whenever reporting their results.

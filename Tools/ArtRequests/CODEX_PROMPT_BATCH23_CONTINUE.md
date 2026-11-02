Qolossal: Codex Batch 23b to 23g (natural step sets for the other six regions). The MR01 pilot is finished and in the game, and the user has play-tested it. Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch23b\ ... Batch23g\, one folder per region, as in the table in Tools\ArtRequests\CODEX_PROMPT_BATCH23_REGION_STEP_SETS.md:

- 23b MR02 Causeway
- 23c MR03 Terraces
- 23d MR05 Ribwood
- 23e MR06 Heights
- 23f MR07 Summit
- 23g MR08 Descent

Copy your context files into each folder's Integrator\Context\.

READ FIRST
1. Tools\ArtRequests\CODEX_PROMPT_BATCH23_REGION_STEP_SETS.md: the full request (canvases, registration, material, proofs, deliverables).
2. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_47.md, _48.md and _49.md: what the pilot settled. Review 49's "For 23b to 23g" section replaces the Review 48 thresholds.
3. The accepted MR01 set: Assets\Art\Codex\Terrain\Body\Body_Step_*. Use the 23a-2 walls and Inner, and the 23a-3 pieces with the corners. Match its quality, not its material.
4. The MR01 frames as they look in the game now: Tools\MountainReliefAtlas\QA\Steps23a\ (after\ and steps23a_before_after.jpg). The user play-tested these and accepted them.
5. Assets\MountainReliefAtlas\Scripts\MraTerrain.cs as it is now. Natural sets (stripDerivedRuns) already handle these, so don't solve them in the art:
   - strip layout on every tread, including slope tops and lower runs;
   - a layout switch where two steps share a tread;
   - seams kept inside short treads;
   - the strip's soil cross-fading with each piece's soil;
   - cliff-kit caps on open surface ends.
   Render your proofs with these rules (a port, as in 23a).

WHAT THE PILOT SETTLED (do it this way from the first painting)
- Paint the corner, the face and the start of the lower run as one painting. Never composite at the riser.
- The upper run and the lower run are both the region's own top strip, column for column from the riser line: the upper run unmirrored inland, the lower run mirrored away from the face. That is how the 23a-3 pieces are built, and the game lays the strip to match both.
- Give the three walls three independent silhouettes and interiors. Only rows 0 and 239 are shared.
- Keep slope-top faces upright, with only a band of about 40 px under the grass following the 0.4 slope. The rock under it is unslanted.
- List the strip's joint columns in each REGISTRATION.json (no gap over 120 px).
- Use plain names with no `_v` suffix, as the request lists (for example `Terraces_Step_Short_L`).

PER-REGION NOTES
- **MR05, MR06, MR07, MR08 (A2, A3, A6, A4):** the game draws only the top 0.5 u of these strips (down to row 156, 60 px under the walk row 96). Below that is the region's opaque `A*_Ground_Fill`. So a run must reproduce the strip only down to row 156 under its walk row, and below that blend into that region's Ground_Fill, not into the strip's lower rows. These strips are 2048 px wide; use columns 0 to 511 from the riser.
- **MR07 Summit:** some risers reach 8.97 u, so a wall stack of four or more courses must not repeat visibly. Show the tallest riser in the proofs.
- **MR02, MR03:** many steps sit under slopes (14 and 24). Show at least five real slope tops each.

CHECKS (Review 49; judge by eye first)
All are column statistics, measured on opaque rock (alpha above 200):
- **Seam:** the mean RGB step between neighbouring columns may not spike above twice the median of the 20 columns either side.
- **Detail:** the corner's mean Laplacian is 0.75 to 2 times the face's 60 px inland.
- **Gaps:** no alpha below 200 inside the rock.

Deliver the maps.

Keep every non-negotiable in the request: native or downscaled only, the name check, the preservation receipt (1209 accepted today; re-read the manifest), nothing written under Assets, the mystery rule (no anatomy, no faces in rock), and a platform section with every device test marked "not tested".

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.

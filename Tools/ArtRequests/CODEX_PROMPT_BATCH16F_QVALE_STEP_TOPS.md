Qolossal: Codex Batch 16f (Qvale step tops: replace the raised grass blocks). Start now and stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch16f\
Copy your context files into Batch16f\Integrator\Context\, as for Batch 16e.

WHY
Read Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_42.md first. Batch 16e passed on 6 of 12 pieces. The step faces are right. The problem is the planted clump on every step top. It is a tall, dense block of grass about 0.5 u above the walk line, with a flat top and a straight vertical end at the riser, so each step edge reads as a box hedge and hides Qori's feet (see CLAUDE_REVIEW_42_Pieces.png and CLAUDE_REVIEW_42_Riser01.png beside it).

WHAT TO MAKE (6 new PNGs under Terrain\Body\Qvale\, each with its REGISTRATION.json; names checked free on 9 Oct)
Qvale_Step_Short_L_v2, Qvale_Step_Short_R_v2, Qvale_Step_Medium_L_v2, Qvale_Step_Medium_R_v2, Qvale_Step_Outer_L_v2, Qvale_Step_Outer_R_v2.

Each is its Batch 16e piece with only the upper grass changed:
- Same canvas, PPU, pivot, riser column, walk rows and joint rows. The stone face, the upper kerb stones, the lower kerb and clump, and the soil are byte-identical to 16e wherever the new grass doesn't cover them.
- The upper grass is the same height and density as Qvale_Top_Strip_v3's grass line: tips no more than about 40 px above the walk line (row 160), loose, with gaps and individual blades and leaves, and the odd white flower, as on the strip. No flat top, no block, no hedge.
- At the step edge the grass and moss spill over the top stone and hang a little way down the face in a few irregular tufts and strands, not a straight curtain. The silhouette against the sky is broken everywhere. No straight vertical or horizontal edge anywhere on the piece.
- Over the last 192 px of the upper run (the join with the strip), the kerb and grass fade to alpha 0 while matching the strip's grass height, so the join can't be seen. If a covering is still needed there, use a low mat of moss over the kerb stones (below the grass tips), not a raised clump.
- Qori's feet must read on the step top at 1280 x 720.

UNCHANGED RULES
Everything in Tools\ArtRequests\CODEX_PROMPT_BATCH16E_QVALE_STEP_SET.md still applies: colours, ASSEMBLY, the fade rules and the non-negotiables. For the review's answers to your 16e questions (lower slopes, bending, x 234.5), see Review 42: the game will bend both the upper and the lower runs along the adjoining ground, moving whole pixel columns so the soil doesn't stretch. Render your proofs that way.

PROOFS
- The 16e street proofs again (the three windows and the eight risers, 1920 x 1080 and 1280 x 720), assembled from the 6 passed 16e pieces plus these 6, with both runs bent as above. Qori standing on the top of risers 1, 5 and 6, and at the foot of risers 1 and 8.
- Each new piece beside its 16e piece on a sky-blue and on a soil backdrop, at 1:1.
- A 4x crop of every step-top silhouette and every strip join.
- A difference mask per piece against its 16e version, proving only the grass area changed.

DELIVERABLES
- The 6 PNGs and REGISTRATION.json files, Batch16f\MANIFEST.json, the QA sheet, the name-check log and the preservation receipt (all 1173 accepted assets unchanged).
- REVIEW_HANDOFF.md, with a platform section that keeps source checks, imported-resource notes and device tests (all "not tested") separate.

If anything here is ambiguous or impossible, write it in the handoff instead of guessing.

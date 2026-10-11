Qolossal: Codex Batch 25a-5 (the terrace overlook: the bench moved to the parapet wall, facing the valley, with Qori larger so his head and shoulders clear the backrest). Same method as 25a-4: paint over a projected blockout and pose guide, then pass the checker. Layers only; no Unity import, no code, no music work. Stop once at the end for Claude's review.

Written 11 Oct 2026, after Review 57 (`Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_57.md`). Not sent: the user sends it. Same names as 25a-4.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a5_Input\
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch25a5\
Copy your context files into Batch25a5\Integrator\Context\, as in earlier batches. Do not write under the live project.

WHAT CHANGED FROM 25a-4
Your 25a-4 painting passed all 30 checks and the bench is right. Claude's composition was wrong: the bench stood out on empty paving, turned 45° away from the wall, and Qori at his true size showed only his head above the backrest. Now:
1. **The bench stands at the parapet**, its front 0.45 u from the wall and turned only 18° to the right of straight ahead: it faces the valley, nearly square to it, as an overlook seat does.
2. **The camera**: 6.5 u behind Qori and 0.35 u to his right, eye height 1.7 u, 7° down, horizon at y 660; 300 px per unit at Qori. All numbers in `Placement.json`.
3. **Qori is drawn 1.35x his game size** (an illustration choice, as in the concept the user picked), so his head, crest, leaf mantle and the tops of his shoulders clear the backrest. His head is turned 32° toward the river, so a sliver of cream cheek shows on the right (the guide's cream area, 5–15% of the head).

WHAT YOU PAINT
- **BenchBack, BenchFront, BenchShadow**: your 25a-4 bench, repainted over the new `Bench_Blockout_Back.png` and `Bench_Blockout_Front.png`. Same wood, waved backrest, root-twined posts and root feet, moss, leaves and light as 25a-4.
- **Qori_Body, Qori_Head, Qori_EarFar, Qori_EarNear** (and `Qori_Blink` only if the eye's edge shows): your 25a-4 Qori, repainted at the new size and angle over `Qori_Guide_*.png`. Same crest leaves, ears with green backs and a thin pale rim, mantle and cloak. The mantle and shoulders now show above the backrest: paint the leaf mantle and the clasp's edge as on the rig (`Reference_Qori_Rig_Sit_SideView.png`, your passed 25a-2 Qori).
- **Terrace**: the parapet wall moved forward to run just beyond the bench's front legs, along the base line in `Placement.json` (`parapetBaseLine`; the blue line in `Template_Preview.png`): from about (4, 1048) on the left to about (1710, 1177) on the right, so the wall runs slightly diagonally, parallel to the bench. Its top about 0.8 u above the paving (lower than the backrest top), so the valley still shows above it. Same stones, ivy and white flowers; the pot and flowers at the left edge; plain paving in front of the bench and in the bottom 10%. The wall stays behind the bench everywhere.
- Keep Sky, Clouds, Far, Mid, Mist, Tree, Lantern, LanternGlow, Foreground_01–04 and Weather byte-identical to Batch25a4.

Draw order in the game: background, Terrace, BenchShadow, BenchBack, Qori_Body, Qori_Head, Qori_EarFar, Qori_EarNear, BenchFront, foreground pieces, Weather. Pivots in `_Registration.json`: the head at the neck, each ear at its base (head ±3°, ears ±8°, no gaps).

CHECK BEFORE YOU DELIVER
Run `python check_batch25a5.py <Batch25a5_Input> <your Layers folder>`. It must report **0 failed**: the same 30 tests as 25a-4, against the new blockout and guides. The guides pass 30 of 30; your 25a-4 layers fail 13 (they are placed for the old camera). Put the full output in the handoff.

ALSO DELIVER
`Retreat_OverlookTerrace_Composite.png`, `_Composite_UIZones.png`, `_MotionProof.png`, a 1:1 crop of Qori, REVIEW_HANDOFF.md, MANIFEST.json (path, sha256, byte size and pixel size per file), a preservation check of the canonical accepted manifest (0 mismatches), a name-collision check, and a source archive with your prompts, as in earlier batches. Platform: Windows PC, PS5, Xbox Series X|S and Switch 2 handheld and docked, all "not tested"; original Switch excluded.

Then stop for Claude's review. 25b and 25c stay on hold.

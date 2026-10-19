# Codex prompt: Batch 28d, Terraces Near and Mid as side-on planes (pilot)

Written 18 Oct 2026 after Claude Review 85: Far, Sky and High from 28c are kept; Mid and Near are rejected because the brief asked for opaque aerial-looking walls. Not sent: the user sends it. The next Claude review number is re-checked before writing.

---

Qolossal: Codex Batch 28d (the Terraces Near and Mid, redesigned as side-on planes). Stop once, at the end, for Claude's review.

Project (read only): C:\UnityProjects\Qolossal
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch28d\

READ FIRST
1. Tools\IncomingArt\Codex_v2\CLAUDE_REVIEW_85.md: why 28c's Near and Mid failed. The failure is the brief's design, not your painting.
2. Your Batch28c package: keep its method (layout guide, 1:1 native sections, one drawn silhouette, cyclic wrap, depth glaze, no upscaling, full provenance), its scripts, and the kept planes. Use the Far, Sky and High byte-identical in every proof.
3. CODEX_PROMPT_BATCH28C_TERRACES_NEW_4K_METHOD.md for the rules this prompt doesn't change.

THE KEY CHANGE: SIDE VIEW, NOT AERIAL
This is a side-on platformer. Every background plane is a side elevation: slopes, cliffs, rock spires and ridges seen from the side, the way the far peaks are. No paths, scattered bushes or fields seen from above, and no ground that fills the frame like a map texture. The 28c Far is the model for the viewpoint.

MAKE (RGBA8 sRGB, 216 PPU, bottom-centre pivot, Repeat U / Clamp V)

1. **BG_Terraces_Near_4K, 3840x7000, depth 5: a tall, open plane of things that pass by as you climb.**
   - Side-on limestone spires and buttresses rising from the bottom edge at several heights, with terrace ledges carrying olive trees, cypresses and dry-stone walls, rock shelves, and a few hanging terraces.
   - **At least 50% of every 3840x2160 window, at any height, is fully transparent**, so Mid, Far and the sky show through. Spread the features: no window is empty and none is a wall. The clear gaps carry no haze or wash: alpha 0.
   - Silhouettes are natural and irregular, and differ in height and width. No regular rhythm, no repeated motif, no two features alike.
   - It is the nearest and warmest plane, with apricot light on the limestone. Contrast is held below the gameplay terrain so the route reads in front: about 70% of the terrain's contrast, with a slight haze.
   - Wrap as before. Every feature ends cleanly at its edge, with a 2 px anti-aliased silhouette and no fringes.

2. **BG_Terraces_Mid_4K, 3840x3900, depth 10: a side-on valley wall.**
   - A stepped skyline of cliffs and ridges in the top 700 px. Below it, the wall itself, seen from the side: cliff bands, terrace steps cut into the slope with olive groves on their ledges, a village clinging to a shelf, a gorge with a thin waterfall. Opaque below the skyline.
   - Softer and slightly cooler than Near, warmer than Far.

CLIMB REGISTRATION (for the proofs; Claude tunes it in game)
- Near: travel 6307 px, starting with its bottom edge at the frame's bottom.
- Mid: travel 3154 px, starting with its skyline at 40% of the frame's height from the bottom, so the valley wall is in view at the start and sinks out by about 60% of the climb.
- Far, High and Sky as in 28c.

PROOFS (4K plus 1080)
- The climb strip at 0, 15, 30, 50, 75 and 100%, with and without the MR03 gameplay matte. Qori, the ledges and the route must read clearly at every step. The sky and the farther planes must show at every step.
- A transparency map of Near: alpha per 2160 px window every 540 px, with its transparent share.
- Window contact sheets with mystery checks (full, 10%, blurred): no hands, faces, knees or other body parts in the spires; no symmetric pairs; no five parallel rounded forms.
- 3x repeats with seam numbers, alpha over black, white and magenta, and every join.

Deliverables, preservation, name check and the platform sections are as in 28c. Report your own pass or fail per plane, with numbers. If anything is ambiguous, write it in the handoff instead of guessing.

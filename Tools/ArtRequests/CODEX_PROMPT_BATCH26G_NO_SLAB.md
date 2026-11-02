Qolossal: Codex Batch 26g (a touch-up of five passed listening-retreat scenes: remove the stone slab under the bench). One layer changes per scene, in one region; everything else stays byte-identical. Do all five, then stop once for Claude's review.

Written 12 Oct 2026. The user, after trying the scenes in the game: "I need to remove this plate from all." The plate is the flat stone slab painted under the bench in the outdoor scenes. Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26_Input\ and your passed deliveries Batch26a, Batch26b, Batch26c, Batch26d and Batch26e2
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26g\<Scene>\ (CradleMeadow, TerracesRain, CausewayDusk, RibwoodGlade, HeightsClouds), each a full copy of its passed scene with only the Ground layer changed
Copy your context files into Batch26g\Integrator\Context\. Do not write under the live project.

WHAT TO PAINT
In each scene's **`Retreat_<Scene>_Ground.png`**, paint out the stone slab under the bench, inside x 180–1200, y 1000–1260 only, and continue the surrounding ground through it so the bench stands straight on that ground:
- **CradleMeadow** (26a): meadow grass and wildflowers.
- **TerracesRain** (26b): wet terrace grass.
- **CausewayDusk** (26c): the landing's paving, its joints continuing the existing paving under and around the bench, following the same perspective.
- **RibwoodGlade** (26d): mossy forest floor with a few fallen leaves.
- **HeightsClouds** (from 26e2): weathered limestone with grass tufts and heather.

The ground stays level (the same plane, `Batch26_Input\Reference\Paving_Grid_Guide.png`), with no slab edge, step, kerb or patch left. Where grass meets the root feet, let a few blades overlap the feet's lowest edge in the Ground layer's painting only if it reads naturally; the bench layers themselves are unchanged. The existing BenchShadow (a soft contact shadow) stays and must still read on the new ground; darken the ground under the seat very slightly if needed so the bench doesn't float. No new objects.

Keep every other layer, the registration and the pivots byte-identical to the passed scene (26e2 for HeightsClouds).

CHECK BEFORE YOU DELIVER (each scene)
1. `python check_batch26.py <Batch26_Input> <new scene folder>`: **0 failed** (solid ground under all four feet, actors unchanged, UI zones calm).
2. `python check_batch26g.py <passed scene folder> <new scene folder>` (in Batch26_Input): **0 failed**. Only Ground changes, only inside the region, the region is actually repainted, and stays opaque.

Put both outputs per scene in REVIEW_HANDOFF.md, with a 1:1 before/after crop of the bench's feet on the ground and the new composite per scene. MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.

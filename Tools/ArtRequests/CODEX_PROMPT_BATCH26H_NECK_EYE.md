Qolossal: Codex Batch 26h (Qori in the listening retreat: a neck, and the edge of his eye with a blink). Seven scenes, the same change in each. Stop once at the end for Claude's review.

Written 12 Oct 2026, from the user's two notes after trying all seven places in the game:
1. "There is a small empty space between the head and the body; maybe we can add a neck there?" Between his leaf crest and his shoulders the body pinches to about 29 px wide, so the head looks like it floats.
2. "Do you think in this pose we need to show half an eye or something?" Claude's answer: yes. In this lost profile we see only a sliver of cream cheek, so nothing says he is looking at the view, and he can't blink. The outer edge of his big dark eye, showing just past the curve of his cheek, makes him read as Qori gazing out, and a blink brings him to life.
Not sent: the user sends it.

Project (read only): C:\UnityProjects\Qolossal
Inputs: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26_Input\ (refreshed checkers) and the seven passed scenes:
| Scene | Passed folder |
|---|---|
| OverlookTerrace | Batch25a11 |
| OverlookNight | Batch26f |
| CradleMeadow | Batch26g\CradleMeadow |
| TerracesRain | Batch26g\TerracesRain |
| CausewayDusk | Batch26g\CausewayDusk |
| RibwoodGlade | Batch26g\RibwoodGlade |
| HeightsClouds | Batch26g\HeightsClouds (the game uses the OverlookTerrace actor colours for this one; relight it like OverlookTerrace) |
Deliver to: C:\Users\Qasim\OneDrive\Documents\ChatGPT\Qolossal\Art\Codex_v2_full\Batch26h\<Scene>\, each a full copy of its passed scene with the changes below
Copy your context files into Batch26h\Integrator\Context\. Do not write under the live project.

PAINT IT ONCE, ON THE OVERLOOK TERRACE, THEN APPLY IT TO EACH SCENE
Paint the new body and head on the approved OverlookTerrace layers first. Every scene then gets **the same shapes (the same alpha)**, with only its colours relit to that scene's light, exactly as the actors were relit in Batch 26.

1. **The neck, in `Qori_Body`.** A short, sturdy neck from the top of the leaf mantle up into the underside of the crest, the same bark brown, grain and soft olive-brown edges as his arms and shins: about 44–52 px wide where it meets the mantle, tapering slightly as it goes up under the crest. The mantle's top leaves overlap its base; the crest's lower leaves overlap its top (the head layer is drawn over the body, so paint the neck up to about y 800 and let the head cover it). In shadow under the crest, a little lighter on the river (right) side. It must not change the head's pivot (766, 794) or show a gap when the head turns ±3°.
2. **The edge of his eye, in `Qori_Head`.** On the cream cheek at the right edge of his head, paint the outer edge of his big dark eye, as on the rig (`Batch26_Input\..\Batch25a8_Input\Reference_Qori_Rig_Sit_SideView.png`): a dark crescent following the curve of the cheek, with a small highlight, only the part that shows past the cheek's curve from behind (no more than about a third of the eye). Widen the cheek a little if needed so the eye sits on it naturally. The crest, the head's outline and the ear bases stay as they are.
3. **A blink, new layer `Qori_Blink`.** The same eye closed: the cheek's cream with a soft lid line where the eye was, on its own 2560x1440 layer at (0,0), covering exactly the eye. Register it right after `Qori_Head`, with `"parent": "Qori_Head"` and the head's pivot, so it turns with the head. The game shows it for a moment every few seconds.

Keep every other layer, and the rest of each registration, byte-identical to the passed scene.

CHECK BEFORE YOU DELIVER (each scene)
1. `python check_batch26h.py <passed scene folder> <new scene folder>`: **0 failed**. Only Qori_Body and Qori_Head change and Qori_Blink is added after Qori_Head; the head outline is kept (IoU at least 0.97); Qori is at least 40 px wide from y 784 to 812 (the passed body pinches to 29); the neck is bark brown; the blink covers an eye-sized area on the cheek (40 to 700 px, inside x 770-830, y 715-795), with a dark eye under it and a cream lid on it.
2. `python check_batch26.py <Batch26_Input> <new scene folder> <Batch26h\OverlookTerrace>`: **0 failed**, so every scene shares the new OverlookTerrace actor shapes.

Put both outputs per scene in REVIEW_HANDOFF.md, with a 1:1 crop of Qori's head and neck (eye open and blinking) in each scene, the head turned -3° and +3° on the terrace, and each new composite. MANIFEST.json, a preservation check (0 mismatches) and a name-collision check as before. Platform rows all "not tested"; original Switch excluded.

Then stop for Claude's review.

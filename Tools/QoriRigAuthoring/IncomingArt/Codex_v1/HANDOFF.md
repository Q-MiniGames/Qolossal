# Qori rig art delivery — v1

Generated 2026-09-24 with the built-in image-generation tool from the existing Qori paintings. Request: Tools/QoriRigAuthoring/CODEX_ART_REQUESTS.md.

## Contents

- Qori_UpperArm.png — rounded shoulder-to-elbow bark segment.
- Qori_Forearm_Fist.png — elbow-to-fist, leaf wrist wrap, empty grip.
- Qori_Forearm_Open.png — elbow-to-open hand, leaf wrist wrap.
- Qori_Head_Neutral.png, Qori_Head_Up.png, Qori_Head_Down.png, Qori_Head_Focus.png — separate earless head views.
- Qori_Head_Blink.png — closed-eye head candidate; see limitation below.
- Qori_Ear_Upper.png, Qori_Ear_Lower.png — base on right, tip on left, for the right-facing character.
- Qori_Skirt_Flap_1.png and _2.png — cream front flaps.
- Qori_Skirt_Flap_3.png and _4.png — olive/brown back flaps.
- Qori_Cape_Panel_1.png through _3.png — separate olive back panels, attachment at top.
- Qori_Weapon_LeafSword.png, Qori_Weapon_SeedpodMace.png, Qori_Weapon_ThornSpear.png, Qori_Weapon_WhipHandle.png — individual weapons, grip left / working end right. Sickle intentionally excluded per earlier user instruction. Existing sling assets not replaced.

## Integration status and limits

These are high-resolution AUTHORING SOURCES, not exported or calibrated runtime replacements. No changes were made to parts.py, animations, rig code, scenes, prefabs, or existing art. Each file contains one independent part. Some leaf panels contain painted folds/overlaps within that part.

The generation tool does not enforce exact cross-image pixel scale. The request's common-scale requirement is NOT yet met by these raw files. Normalize in parts.py against the existing rig joint distances, not by matching canvas dimensions. Preserve Claude's working rig proportions and animation.

Existing parts.py reference at delivery: shoulder (612,622), elbow (584,694), fist (648,828) in rig pixels. Shoulder-to-elbow length is about 77.25 px; elbow-to-fist about 148.50 px. Set source joint landmarks, rotate each neutral source axis to the existing rest-axis, and scale accordingly. Open/fist forearms should share an aligned elbow and wrist before switching. Top and bottom of the opaque bounding box are NOT joint centers.

Heads need individual neck-pivot/size registration against the original atlas views. Ear bases should sit behind the skull with enough overlap for rotation. Skirt attachments belong under the belt; cape attachments under the mantle. Preserve existing weapon world size and hand grip positions after replacing atlas rects.

IMPORTANT: Qori_Head_Blink.png has visible outline/scale drift from Qori_Head_Neutral.png despite an eye-only edit request. Do NOT switch the full image directly during a blink: it would cause a head pop. Register it and use only its eyelid artwork composited onto the neutral head, or correct the blink before enabling it. This candidate is included for source artwork, not certified as an aligned animation frame.

## Validation

All 21 images opened successfully and were visually inspected. ALPHA_CHECK.json records image dimensions and an 8-pixel-grid alpha sampling check: every file contains transparent background samples and alpha=0 at the top-left corner. Painted interiors are predominantly alpha 252–253/255, so these are not hard opaque-mask exports. Preserve RGBA; inspect edges against light and dark backgrounds in the final rig import. No assembled-rig, motion, exact scale, or runtime visual validation has been performed on these new sources.

PROMPTS.md contains the generation prompt set. Original generated files were retained; this folder is a non-destructive handoff.

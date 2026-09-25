# Art requests for the Qori rig (for Codex or an image AI)

The rig currently reuses the existing paintings. The pieces below would make the biggest visual improvement, listed in priority order. Each file should be a **transparent PNG**, facing **right**, in the same painted style and at the same scale as `Assets/Resources/QoriRig/Qori_ThreeQuarter_Leg_v1.png` and `Qori_ThreeQuarter_Torso_v1.png`. Draw **one part per file**, in a straight, neutral pose, with **no background and no shadow**.

Include this in every prompt:
> Paint the whole part, including the areas that are normally hidden behind other parts. At each joint, add a rounded, overlapping end (like a paper-doll joint) so the part can rotate without showing a gap.

1. **Arms** (currently cut from the idle painting, so they're thin and low-res)
   - `Qori_UpperArm.png`: shoulder to elbow, bark texture matching the legs, rounded at both ends.
   - `Qori_Forearm_Fist.png`: elbow to closed fist, fingers wrapped around an **empty** grip (a hole where a handle goes). Include the green leaf wrist wrap.
   - `Qori_Forearm_Open.png`: the same forearm with an open hand, for falling, landing and reaching.
2. **Ears separate from the head**: the 4 head expressions *without* ears, plus `Qori_Ear_Upper.png` and `Qori_Ear_Lower.png`. The ears can then sway and lag behind the head, which adds a lot of life.
3. **Blink**: the neutral head with eyes closed (the same drawing otherwise).
4. **Skirt as 4 separate leaf flaps**, so each flap can flutter on its own when running or falling.
5. **Back cape**: 2–3 long leaf panels that hang from the shoulders behind the body (like the old `Qori_CloakPanel_*` files, but matching the three-quarter torso).
6. **Weapon art with clean alpha**: the swords on `Weapons.png` overlap each other's bounding boxes. One PNG per weapon, with the grip at the left and the blade pointing right.

When these files arrive, drop them in the project and I'll swap them into `parts.py`, then re-export. The animations and code stay the same.

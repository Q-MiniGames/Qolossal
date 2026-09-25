# Qori rig: authoring tools

These Python scripts build the parts and animations for Qori's cutout rig. They sit outside `Assets/`, so Unity ignores them.

## How it fits together

```
Assets/Resources/QoriRig/*.png  ──parts.py──▶ work/parts/*.png  (cut, scaled, pivots at joints)
Assets/Resources/QoriCloakPrototype/Qori_IdleBody_v1.png (arms)
                                    │
               rig.py   skeleton + software renderer (for previews)
               anim.py  IK (legs/arms), control → bone rotations, baking with smooth tangents
               clips.py every animation, written as poses over time
                                    │
                      export.py ──▶ Assets/Art/Characters/QoriRig/QoriRigData.json + Parts/*.png
                                    │
       Unity: Qolossal ▸ Qori Rig ▸ Build and Install on Player  (Assets/Editor/QoriRigBuilder.cs)
          → sprite pivots, Clips/*.anim, QoriRig.controller, QoriRig.prefab, installed on Player.prefab
```

## Changing an animation

1. Edit `clips.py`. Every clip is a function of time that returns "controls": body offset/lean, torso/head/skirt angles, **hand targets**, **foot targets** and the **weapon angle**. The IK solves the elbows and knees for you.
   - The walk and run cycles come from `make_gait(...)`: stride, lift, lean, bob, arm swing and weapon angle.
   - Attacks are keyposes in `FRONT_KEYS`, `UP_KEYS` and `attack_down`, on a 0–1 s timeline: 0–0.30 wind-up, 0.30–0.55 active (hits), 0.55–1.0 recovery. The game scrubs this timeline to match the combat phases exactly, so the blade you see is the blade that hits.
2. Preview with `python view2.py AttackFront 0 .2 .3 .4 .55 .8` (writes `work/k_AttackFront.png`) or `python showcase.py` (writes `work/qori_rig_preview.gif`).
3. Export with `python parts.py && python export.py`, then run **Qolossal ▸ Qori Rig ▸ Build and Install on Player** in Unity.

You can also tweak clips by hand in Unity's Animation window. A rebuild from JSON overwrites those edits, so choose one place to edit each clip.

Requires Python 3 with `numpy`, `pillow`, `scipy` and `opencv-python`.

## Replacing art

Put the new PNGs where `parts.py` reads them, or change the paths at the top of that file. Keep the same scale and orientation (facing right), then re-run the three steps above. Code and animations don't need to change.

### Codex art pack v1 (`IncomingArt/Codex_v1`)

`parts.py` fits these high-res paintings onto the rig by joint landmarks (`newart.py`), so their raw canvas size doesn't matter:

- **Arms**: `Qori_UpperArm`, `Qori_Forearm_Fist` (both arms) and `Qori_Forearm_Open` (the free hand, which stays open except while hanging). Both segments share one scale that keeps the old reach, so every clip's hand targets still fit.
- **Heads**: the 4 earless heads are fitted into the old atlas cells (same neck pivot and size). **Ears** are separate bones with a spring in `QoriAnimator` (they lift when falling, stream back when running, and flop on landings).
- **Blink**: `Qori_Head_Blink` drifts from the neutral outline, so only its closed-eye area is composited onto the neutral head (`Qori_Head_Blink.png`). `QoriAnimator` blinks every 2–5 s.
- **Weapons**: `weapons_prep.py` trims them into `Assets/Resources/Armory/Weapons/` with grip points in `Grips.txt`. `QoriArmoryFactory` uses them instead of the old sheet, at the same world length.
- **Not used yet**: `Qori_Cape_Panel_*` (single slim leaves read sparser than the layered cloak; try `QORI_NEW_CAPE=1 python parts.py`) and `Qori_Skirt_Flap_*` (the skirt is still the painted one from the torso art).

# Qolossal: asset request for Codex, Batch 10 (a long overnight run)

> **Platform direction (user decision, 29 Sep 2026):**
> - **Targets:** Windows PC, PS5, Xbox Series X|S and Nintendo Switch 2. The original Switch is excluded.
> - Read [the platform policy](../Design/QOLOSSAL_PLATFORM_DIRECTION.md).
> - Preserve accepted source bytes, and use measured platform-specific runtime settings.

The user is away overnight and wants a long, productive run. **Work through every workstream below in parallel** (one agent per workstream plus the integrator, as in Batches 6–8), and stop only once, at the end, for **Review 21**.

## Notes
- **Folder name:** "Batch9" is already taken by the Review 16 redos. Use `Batch10/` for this run's QA, handoffs and manifests.
- **Read first:**
  - `Tools/Design/QOLOSSAL_WORLD_REDESIGN_PROPOSAL.md` (v2, with sections 10–11: the user's decisions and concept picks);
  - `CLAUDE_REVIEW_14.md` to `CLAUDE_REVIEW_20.md` (the most recent direction, especially the cloak and the Leaf Staff);
  - your own `Concepts/Titan/` model sheet (Batch 8 W1, the Chalk Orchard).
- **What has changed in the game since Batch 8:**
  - Qori's cloak is now a separate `Qori_Mantle` plus re-hung panels (live in the rig).
  - **Qori starts with the Leaf Staff;** the sword, mace and spear will be found in the world.
  - Hit-stop and camera room edges are in.
  - The art manifest holds **996** accepted files.

## The rules (unchanged)
- Keep all **996** accepted files byte-identical (`Tools/ArtImport/codex_v2_accepted.json`).
- Never write under `Assets/`.
- Use deterministic post-processing, and archive native sources and exact prompts.
- Fill in the Batch 6 self-review checklist (section 2 of `QOLOSSAL_ASSET_REQUESTS_BATCH6.md`) per workstream.
- Run the **global name check** against the manifest before delivering (no name collisions).
- **The mystery rule** (redesign proposal, section 2):
  - Nothing before the final level shows or names the titan's body.
  - Backgrounds show sky and the distant valley only.
  - Body parts read as landscape at play scale.
  - Designer-only views must be labelled as spoilers.
- **The platform section in each handoff:** source checks, imported-resource notes (anything over 2048 px) and device tests marked **not tested**, kept separate. Add 1280×720 readability proofs.

---

## W1: The Wilted's cloak, matching Qori's new cloak (production, P1)
Write to: `Characters/Wilted/`. The Wilted (Qori's withered rival) shares Qori's rig, so it needs the same cloak split Qori got in Reviews 15–18.

**Reference:**
- **Qori's live set:** `Player/QoriCloak/*.png` and `QORI_CLOAK_REGISTRATION.json`.
- **The Wilted's current parts:** `Characters/Wilted/` (withered grey-brown, thorns through the leaves, a dim mint core).

**Deliver** for each of the Wilted's three states (normal, ThornArmor, Healed):

| File(s) | Constraint |
|---|---|
| `Wilted_Torso_Bare`, `Wilted_ThornArmor_Torso_Bare`, `Wilted_Healed_Torso_Bare` | the existing torsos with the shoulder leaves removed; **same canvas and pivot as `Qori_Torso` (478×425, 0.5423 / 0.0636)** |
| `Wilted_Mantle`, `Wilted_ThornArmor_Mantle`, `Wilted_Healed_Mantle` | the same canvas, pivot and leaf layout as `Qori_Mantle`, painted in the Wilted's style for that state. Whole leaves with outlined tips, overlapping in rows, flowing down and back. No cut leaves, seams or bands. **This is what Review 17 sent back for Qori.** |
| `Wilted_Cloak1..3` and `Wilted_Cloak1..3Lower` (normal), `Wilted_Healed_Cloak1..3` and `..Lower` (healed: blossoming) | the same canvases and pivots as `Qori_Cloak*`; ThornArmor reuses the normal cloak |

**Proofs:** an idle side-by-side with Qori in the live assembly for each state, the ±25° panel swings, the arm under the mantle, and the alpha checks.

## W2: Weapon shrines, so Qori can find the sword, mace and spear (production, P1)
Write to: `Props/`. At 120 px/u, in the Chalk Orchard world. They must read as old shrines that people built, never as body parts.

| ID | File(s) | Notes |
|---|---|---|
| WS-01 | `Prop_WeaponShrine_Sealed`, `Prop_WeaponShrine_Open` | a small limestone-and-root pedestal holding a weapon: about 2.2 u tall and 2.5 u wide, with its pivot at the base centre. Both states on **one shared canvas**: sealed in roots with a faint mint glow, then the roots drawn back. |
| WS-02 | `Prop_WeaponShrine_WeaponSlot` | an overlay marking where the game draws the weapon (the weapon art is drawn separately). Give the slot's centre and angle in a registration JSON. |
| WS-03 | `FX_WeaponFound_01..06` | a short reveal burst (leaves and mint motes), 6 frames, 600 px/u like the other FX |
| WS-04 | `UI_WeaponFound_Banner` | a 9-slice banner for the "Found: Thorn Spear" caption, at 100 px/u in the town UI style (`UI_TownDialogue_9Slice`) |

## W3: The Long Causeway body-terrain kit (forearm, production, P1)
Write to: `Terrain/Body/Causeway/`, `Decor/`, `Props/`.

**What the place is:** the forearm, from the wrist up toward the knee: a long, steep, curving rise with old people-built road stones and aqueduct remnants laid along it. It still reads only as landscape. Read your `Batch8_Causeway_Play_1920x1080.png` and its trace.

**Deliver everything the Cradle kit had (`Terrain/Body/` BT-01 to BT-10), re-themed:**
- `Causeway_Top_Strip`, `Causeway_Fill`, `Causeway_Fill_Shadow`, `Causeway_Cliff_Face`.
- **Tendon cords:** long taut root-tendons running along the slope, usable as rope bridges. They're horizontal repeating strips with end caps.
- **The road and aqueduct:** a worn paving strip that lies on the top strip, broken aqueduct channel pieces, and two arch ruins as props.
- `Decor_Causeway_Sheet`: 12 named pieces, with **16 px transparent margins on every slice** (Review 15's lesson).
- The causeway's warm-spring variant, and a chamber entrance.

**Proofs:** ×3 strips and 4×2 fills, the top strip on ±40° curves, and a 1920×1080 mock route (a slope, a tendon bridge, an aqueduct ruin) with Qori at 1.4 u.

## W4: The Terraces body-terrain kit (the raised knee, production, P1)
Write to: `Terrain/Body/Terraces/`, `Decor/`, `Props/`, `Backgrounds/`.

**What the place is:** the round raised knee, farmed in curved terraces held by low dry-stone walls, with one mill as a landmark. Read your `Batch8_Terraces_*` frames.

**Deliver:**
- the same core set (top strip, fills, cliff face);
- terrace-wall ledges as one-way platforms, in three lengths;
- crop rows decor (grain, beans, a scarecrow);
- `Decor_Terraces_Sheet` (12 named pieces, margins as above);
- the mill as a **background landmark layer, not decor**;
- the Terraces backgrounds (far, mid and near) showing the valley below. **No titan.**

**Proofs:** as for W3.

## W5: Concept rounds for the new world (concepts only: deliver the sheets, then they wait for the user's pick)
Write to: `Concepts/`. Two or three directions each, in the Chalk Orchard style. Spoilers must be labelled.

| ID | Concept | Brief |
|---|---|---|
| C-10 | **The new Chart (map)** | During play, each explored region is a separate parchment patch with its own odd shape, like islands on an old map: the Cradle, the Causeway, the Terraces, Qvale, the Ribwood, the Windward Heights and the Summit. Show the map with 2, 4 and 6 patches found, the patches never touching. Show **the final reveal**: the seven patches sliding together into the seated titan's silhouette. Use the frame and paper style of the accepted `Chart_Frame_9Slice`. |
| C-11 | **The face: the final level and the reveal frame** | Playable frames on the face at 10 u camera height: the cheek, the closed eye's lid cliff and lake, the lash reeds and the brow. They still read as strange land until the reveal. Plus the reveal still (1920×1080): Qori at the brow looks down and back, and the camera pulls out until the whole bowed face reads. |
| C-12 | **Title screen** | Qori on a moss-grown ridge at dawn with the valley below. **No titan:** the ridge is secretly a finger, unreadable at this framing. Leave room for the logo. |
| C-13 | **Ending stills** | Three stills: the titan lifting its head; the hand rising with Qvale safe in the lap (the careful ending); Qori and the healed Wilted on the shoulder looking out. |
| C-14 | **Quake vistas** | One 1920×1080 still per knot (seven in all): the land Qori stands on visibly shifting (ridges curling, a slope tilting, the ground breathing). Each still reads as an earthquake, **never as a body moving**. |

## W6: Small extras (production, P2)
- **`SlashStrip_Staff`:** a swing trail for the Leaf Staff, matching the format of the existing `SlashStrip_Sword` (`Assets/Resources/FX/Slash/` is the live reference; don't write there). It's a broader, leafier arc that sheds a few small leaves.
- **Upgrade-shop icons** at 100 px/u, 256×256: `Icon_Upgrade_StaffT2`, `Icon_Upgrade_SwordT2`, `Icon_Upgrade_MaceT2`, `Icon_Upgrade_SpearT2`, `Icon_Upgrade_HeartSeed`. Base each on the accepted weapon art with a glowing sap-vial accent.

---

## Delivery
- **The integrator:**
  - merges the manifests;
  - runs the 996-file preservation check and the global name check;
  - writes `REVIEW21_HANDOFF.md`, with one section per workstream, each with its filled checklist and platform section.
- **Production workstreams (W1–W4, W6)** deliver finished candidates.
- **W5** delivers concept sheets only.
- **One stop, at the end:** Review 21.

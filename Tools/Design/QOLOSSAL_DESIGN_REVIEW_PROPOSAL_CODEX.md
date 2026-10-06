# Qolossal — independent design review by Codex

**1 October 2026 · Proposal only · inspected revision `bff1251`**

No recommendation below is an approved design change. This review changes no code, scenes, source art, canonical design, music, manifests or approval states. The requested filename, `QOLOSSAL_DESIGN_REVIEW_PROPOSAL.md`, became occupied by a Claude-authored review during this inspection. This companion preserves that work rather than replacing it.

## Evidence and scope

I inspected the current world brief, platform policy, Batch 6 request, Review 13, delivery/platform handoffs, project memory, enabled scene list, the saved Grip Knot scene, world/Chart/travel/save code, movement and ability code, armory, camera, and guardian/enemy code. Older memory and the brief contain implementation notes superseded by recent commits. The source code and saved scenes take precedence for implementation status; current user decisions and reviews take precedence for direction.

**I did not run a new build, automated test, human play-test or hardware profile.** Existing automated test scripts and historical test reports are evidence that checks exist, not evidence that I reran them or that the game feels good. I did not assess the current game through a live controller session. Findings below distinguish **confirmed static evidence**, **design conflicts**, and **hypotheses requiring play-tests**.

Current foundation:

| Area | What is implemented or recorded now | What remains unproven or only planned |
|---|---|---|
| Playable structure | Four enabled scenes: A0_TestRoom, A1_Aqueduct, A2_Grove, R1_GripKnot. OpeningLevel is disabled. | A complete 28-space campaign; the proposed six-hour duration. A test gallery is not a finished opening. |
| Movement | Buffered/coyote jump, wall movement, ledges, thread, pogo, Wind Leaf dash and Glidecap glide have code. The last two have recent commits and A0 shrines. | Their combined feel, learning curve and final campaign placement. Seer's Lantern is still a design/integration task. |
| Combat | Three melee weapons plus sling; weapon-specific interactions; Knucklebramble's three-arm/exposure cycle. | A balanced campaign, eight compelling guardian encounters, difficulty curve and controller ergonomics. |
| World | Knots, persistent variants, Waymarks, Vein records, Wild destinations, Chart and a Grip example. | Useful revisits across the whole body, safe reachability through every legal progression order, room-level navigation. |
| Save | JSON temporary write, flush, replacement and backup recovery are implemented. | Platform storage/account integration; migration and interrupted multi-step progression validation. |
| Art | Review 13 records 732 accepted images and five approved guardian concepts. | Six returned images, guardian production/integration and final gameplay readability. Art acceptance does not prove an encounter works. |
| Music | Review 13 cancels W6 and places sound signatures on hold; the user is moving to Suno. | Final approved soundtrack and adaptive arrangement. Technical loudness/loop passes do not approve the rejected sound. |
| Platforms | Windows PC, PS5, Xbox Series X and S, Switch 2 handheld and docked; original Switch excluded. | Actual console builds, measured budgets, lifecycle validation and certification. 60 FPS is a target. |

Key evidence: [build scenes](C:/UnityProjects/Qolossal/ProjectSettings/EditorBuildSettings.asset), [movement](C:/UnityProjects/Qolossal/Assets/Scripts/PlayerMovement.cs), [relic definitions](C:/UnityProjects/Qolossal/Assets/Scripts/RelicAbilityDefinition.cs), [save implementation](C:/UnityProjects/Qolossal/Assets/Scripts/Progress/GameSave.cs), [Review 13](C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_13.md), [platform policy](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md).

## A. Overall verdict

**Qolossal has a strong identity and a credible prototype foundation. Its biggest risk is expanding the campaign before proving that waking the titan changes how it feels to explore.** The image of a tiny seed restoring an enormous sleeping being is immediately understandable. The hand-painted stone/root anatomy, protective palm, forked crown-mask and healed Wilted ending reinforce one another. Preserve them.

The most compelling player promise is: **“I learned this place, helped it recover, and now I can use it differently.”** That is stronger than collecting seven powers and watching seven impressive pictures. It gives players a reason to return and a specific experience to describe to friends: “The ravine I struggled through was his hand; when I healed it, his fingers became my bridge.”

The current implementation provides the beginnings of that promise: persistent state variants, a bridge, a gate, a Chart and a guardian. But the full design still risks becoming familiar platforming rooms connected by portals, with anatomy supplied mainly by the background paintings.

The biggest threats, in order:

1. **Ability timing contradicts several planned challenges.** An ability awarded after a region's guardian cannot also be the only way to defeat that guardian or cross its preceding mandatory rooms.
2. **Transformation can become confusion or busywork.** Random travel, global stirs, several currencies and revisiting old rooms multiply the player's memory burden.
3. **Uniform content quotas hide production cost.** Seven identical region structures, eight guardian encounters and numerous new puzzle families are not justified by the four-scene prototype or by a measured playtime.
4. **Interaction quality remains unvalidated.** Input exists; comfortable controls, readable combat and meaningful weapon choices still require human sessions.
5. **Art production can outrun gameplay decisions.** High-quality accepted assets should be reused, but their existence should not force repetitive encounters or additional campaign length.

I recommend keeping all seven anatomical regions provisionally, reducing the commitment to a fixed number of rooms, and proving one compact restoration loop before further campaign expansion. I would not yet authorize a wholesale world rewrite, a new combat system or a full soundtrack commission.

## B. Prioritized disposition

Costs below are relative: **S** = localized adjustment; **M** = several connected systems or one authored encounter; **L** = campaign-wide content or architecture. They are not schedule estimates: team capacity, budget and console access have not been established.

| Priority | Disposition | Subject | Proposed decision | Cost / validation |
|---|---|---|---|---|
| P0 | Improve | Ability progression | Resolve reward-before-requirement conflicts before room construction. | S design, M integration; fresh-save route test. |
| P0 | Keep / Test | Explore → restore → revisit | Make one remembered obstacle become a useful route. | M; observed return journey. |
| P0 | Improve | Navigation and Wild Veins | Guarantee safe arrival/return; distinguish known, changed and unknown routes. | M; graph and controller navigation tests. |
| P1 | Replace | Mandatory 3+1 region quota | Allocate rooms by teaching and payoff needs; retain seven region identities initially. | S planning, potentially large content savings. |
| P1 | Keep / Improve | Combat | Keep sword, heavy maul, spear and sling; test their decisions, not just damage values. | M; mixed encounter comparison. |
| P1 | Improve | Movement/input/UI | Consolidate actions and contextual prompts; test complete gamepad flow. | M; mouse-disconnected session. |
| P1 | Improve | Stirs, camera and readability | Wire presentation, expose settings, preserve readable play space. | S–M; actual-size captures plus play-test. |
| P1 | Keep / Improve | Emotional arc | Keep healed Wilted and protective titan; show one local beneficiary of restoration. | M; comprehension test without extra lore. |
| P1 | Test | Save/import/performance pipeline | Establish evidence on representative scenes and available hardware. | M initially; console work dependent on access. |
| P1 | Replace | Rejected soundtrack production | Approve a musical direction and one in-game loop before more cues. | S audition; M integration. |
| P2 | Cut / defer | Redundant reward and upgrade systems | Defer weapon tiers and Sap Vial expansion until their distinct value is demonstrated. | Saves M+ implementation/tuning. |
| P2 | Cut / defer | Global mechanical state changes | Limit new wind/light mechanics to deliberately authored locations initially. | Saves L verification burden. |
| Preserve | Keep | Accepted assets and approvals | Reuse byte-identical masters; derive runtime variants through import settings. | No repainting authorized. |

## C. Ten highest-value recommendations

### 1. Give each traversal ability before its first mandatory test

**Evidence: design conflict, high confidence.** The brief's Knot sequence grants the ability after the guardian, yet Thumb Rise teaches climbing before Grip, the Arm uses Living Thread before Reach, Crown Gardens uses glide before Bloom, and Hollowhorn explicitly asks for Bloomfall before Spring awards it. Environmental versions might resolve some cases, but the document does not consistently specify them. The Hollowhorn dependency is especially direct. This is not a claim that the present four scenes already contain a proven softlock.

Sources: [region rewards and progression](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:46), [Knot sequence](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:127), [guardians](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:226), [planned levels](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:249).

**Change:** place the region's ability at an early shrine, then teach it, combine it and use it in the guardian encounter. The Knot rewards a restored world function, a shortcut and the visible consequence, rather than being the first grant of the same ability. Grip would grant climbing early enough for Thumb Rise; restoring Grip would then bridge the remembered Crease. Retain prerequisite gates between regions.

**Benefit / tradeoff / cost:** coherent learning without special-case temporary powers; stronger distinction between personal growth and healing the titan. The boss loses its immediate new-button reward, so the transformed route must be satisfying. S design work, M changes to placement, prompts and progression triggers. Reuse shrines, abilities, knots, guardians and accepted art.

**Smallest test:** one fresh-save Palm route, including death/reload, where the player obtains climbing, practices twice, passes one combined challenge and restores Grip. Write a prerequisite table for every required exit and guardian, then validate both Grove/Knees orders. Do not let debug relic grants conceal missing prerequisites.

### 2. Make the first stir a playable return journey

**Evidence: built foundation; player impact untested.** The existing Grip example opens a gate, adds a vein and bridges A0's Crease. That is the correct direction. `StirVariant` can switch authored states without simulating a gigantic moving world. The saved Grip scene has `vista: {fileID: 0}`; its builder does not assign a vista. The code's flash also appears above the vista and remains opaque through its hold. The missing assignment is confirmed; the visual consequence of layering needs a runtime capture.

Sources: [Grip scene](C:/UnityProjects/Qolossal/Assets/Scenes/R1_GripKnot.unity:1259), [Knot builder helper](C:/UnityProjects/Qolossal/Assets/Editor/Terrain/AreaRoom.cs:259), [StirSequence](C:/UnityProjects/Qolossal/Assets/Scripts/World/StirSequence.cs:41), [StirVariant](C:/UnityProjects/Qolossal/Assets/Scripts/World/StirVariant.cs).

**Change:** before Knucklebramble, cross the Crease by a memorable, slightly awkward route. After Grip, return through the same camera composition and cross the finger bridge directly. Show a once-unreachable perch now within reach. Let the new shortcut lead toward the next goal, so revisiting is purposeful rather than a detour for a minor pickup. Use the approved vista as a brief orientation beat, not the entire payoff.

**Benefit / tradeoff / cost:** communicates the central fantasy through control and recognition. Repeated scenery is intentional, but a long return would feel padded. M level/presentation work; reuse the Crease, bridge, StirGate, poses, ghost layers and existing variant system. Confidence high on the opportunity, medium on pacing.

**Smallest test:** give five first-time players the before/after route without explaining the change. A provisional success criterion is four describing both what moved and what route became useful. Ask whether the return felt rewarding or repetitive; do not infer enjoyment from completion alone.

### 3. Keep mystery in travel; remove uncertainty about safety and progress

**Evidence: confirmed implementation limits plus design risk.** `WorldAtlas.WildDestinations` filters built scenes, charted Waymarks and present unvisited vein ends, but contains no ability-based escape validation. This does not prove today's destinations trap players; it makes safety dependent on every authored arrival. The Chart shows region reveal, level icons and travelled connections; it is not yet a detailed room/exit map. Keyboard prompts remain visible in Chart/portal UI even though a controller can open the Chart from pause.

Sources: [destination selection](C:/UnityProjects/Qolossal/Assets/Scripts/World/WorldAtlas.cs:49), [Chart](C:/UnityProjects/Qolossal/Assets/Scripts/World/ChartScreen.cs:139), [portal](C:/UnityProjects/Qolossal/Assets/Scripts/Mechanics/Portal.cs).

**Change:** require every possible Wild arrival to have a safe route to a known Waymark or a reliable return that needs no missing ability. Record ability requirements and stir-dependent exits in the route data used by validation. On the Chart, distinguish an unexplored exit, a known blocked route and a route changed by the latest stir using shapes and a small legend. Highlight the latest change until examined. Add a focused local exit diagram only if whole-body navigation remains insufficient.

Keep the approved random-every-use Wild rule for the first test. If it fails, the proposed replacement is an optional Wild excursion with a temporary guaranteed return, not random transport on the critical path. That would reopen an approved design decision and needs approval.

**Benefit / tradeoff / cost:** discovery without accidental strandings or repeated blind travel. Extra diagramming risks turning the evocative Chart into an administrative screen. M route metadata/UI work; reuse Atlas, Waymarks, map icons, ghost/pose art and travel records. High confidence on safety requirement, medium on interface detail.

**Smallest test:** enumerate reachable ability/knot states and both Act II orders against a small route graph; test every arrival and death/reload destination. Then ask players to find the newly opened route with controller only. Compare with and without the changed-route cue before building a full map editor.

### 4. Preserve seven places; stop promising four spaces in each

**Evidence: design/production judgment, high confidence on uncertainty.** The brief fixes 28 spaces and roughly six hours. It also specifies 8–12 minutes per ordinary level: 21 such levels account for approximately 168–252 minutes, with chamber, boss, travel and retry time unmeasured. Those figures are targets, not a demonstrated six-hour game. The build list has four enabled scenes, including a test room.

**Change:** give each region a traversal idea, one landmark, a restoration consequence and an emotional tone. Let its number and size of spaces follow those requirements. Some regions may be one dense loop plus a chamber; others may need a long approach. Keep the seven anatomical identities and art kits until evidence supports merging anything. Build one representative region before committing the remaining route lengths.

**Player difference:** the Eye can be a short, quiet lens-lake journey after a demanding Crown ascent rather than another three-room checklist. The Knees can support a larger mill loop if its mechanism earns the time.

**Benefit / tradeoff / cost:** pacing variety and less filler, at the price of less uniform production planning. S planning effort; reauthoring expanded content would be L, which is why the decision belongs early. Reuse all kits and landmarks; accepted assets need not all become mandatory gameplay content.

**Smallest test:** time the Palm loop and one contrasting Arm puzzle in greybox, then estimate campaign length from observed traversal, exploration and retries. Do not extrapolate only from walking speed or asset count.

### 5. Make weapons solve different combat decisions; make guardians change the arena

**Evidence: built mechanics; balance hypothesis.** The armory already distinguishes sword, maul, spear and sling. Sentinel can be pierced by spear or attacked from above/behind—a useful alternative to a strict weapon key. Knucklebramble closes after three exposure hits by default. Combined with different per-hit damage, that cap may favor heavy blows regardless of the sword's speed; actual damage windows and safety must be measured before changing it.

Sources: [armory setup](C:/UnityProjects/Qolossal/Assets/Scripts/QoriArmoryFactory.cs:12), [Sentinel defenses](C:/UnityProjects/Qolossal/Assets/Scripts/Creatures/SentinelEnemy.cs:31), [guardian exposure](C:/UnityProjects/Qolossal/Assets/Scripts/Creatures/KnucklebrambleGuardian.cs:66).

**Change:** preserve sword as forgiving coverage, maul as committed impact/stagger and spear as precise reach. Teach one choice at a time even if all are owned. Keep the sling useful for distant interactions, with limited combat dominance. Avoid solving every distinction through matching weapon icons to invulnerable targets. First compare Knucklebramble's current exposure cap with a time-based window; change it only if one weapon consistently wins without a meaningful risk.

For each guardian, require one body-function interaction in addition to avoiding attacks and striking the Knot:

| Guardian | Proposed encounter emphasis, reusing its current concept |
|---|---|
| Knucklebramble | Its slam opens a safe approach or briefly exposes a climbable route; retain simple warnings and the three-arm identity. Prototype only one arena interaction. |
| Cistern Matriarch | Reroute a visible sluice to strand it; the restored flow becomes the exit. |
| Grove Warden | Bait a charge to free a blocked airway; use dash for positioning rather than automatic damage immunity. |
| Hollowhorn | Its wheel-horn movement turns the mill/terrace geometry; pogo is already taught before the fight. |
| Brow Sentinel | Rotate or displace its shrine-door shield to reveal a route or opening; avoid merely scaling up the ordinary spear-countered Sentinel. |
| Glassmoth Queen | Reposition a small number of readable mirrors and use glide to reach a refracted opening; keep beams distinguishable from decoration. |
| Wilted | Recombine already learned movement in a short rival encounter; victory visibly frees a person, rather than awarding another damage tier. |
| Thornheart | Reuse established pulse/flow/light interactions in a short culmination. Do not introduce three unrelated final-boss subsystems. |

**Benefit / tradeoff / cost:** bosses enact bodily restoration and weapons remain choices. Each unique arena costs authoring and QA; implementing this entire table now would be L and is not recommended. One Knucklebramble experiment is M. Reuse approved rigs, attack parts, glow states, hazards and existing combat code. Confidence medium; enjoyment cannot be established statically.

**Smallest test:** three representative enemies and Knucklebramble, trying each weapon. Record hit success, damage taken, time spent waiting and reasons players switch. Look for meaningful preferences, not identical completion times. Do not add parry, stamina or an invulnerable dodge until a specific encounter problem warrants it.

### 6. Validate movement combinations and controller effort before adding verbs

**Evidence: implemented controls, untested ergonomics.** Dash/glide exist. The controller guide distributes attack, growth, thread, thread adjustment, dash, glide, sling and armory across many inputs. Armory uses View/select with right-stick cycling; sling uses hold-and-release plus right-stick aim. The default glide option opens while Jump remains held during a fall. These are not automatically bad, but accidental glide and finger workload need observation. CameraFollow already includes horizontal and falling look-ahead; do not rebuild that from scratch.

Sources: [movement parameters](C:/UnityProjects/Qolossal/Assets/Scripts/PlayerMovement.cs:71), [armory input](C:/UnityProjects/Qolossal/Assets/Scripts/QoriArmory.cs), [controller guide](C:/UnityProjects/Qolossal/Assets/Scripts/ControlsGuide.cs), [camera](C:/UnityProjects/Qolossal/Assets/Scripts/CameraFollow.cs).

**Change:** use a shared action/binding layer for gameplay, remapping and prompts. Provide contextual teaching as abilities arrive. Test hold-to-glide versus a deliberate second press or toggle option, and a run toggle versus held stick-click. Ensure attack/aim/weapon selection cannot leak through menu closure. These are proposed usability options, not mandates to change the movement's tuned values.

**Benefit / tradeoff / cost:** lowers effort without reducing traversal depth. Additional control options expand the QA matrix. M integration; reuse current movement, camera, Input System and test fixtures. Confidence high on need for testing, medium on preferred bindings.

**Smallest test:** ten minutes on gamepad with no mouse: jump → wall → thread → dash → glide, then sling, weapon selection, Chart, pause and return. Record unwanted actions and recovery failures. Repeat on keyboard/mouse. Test disconnect/reconnect and focus restoration separately from player comfort.

### 7. Treat readability as a gameplay constraint, not a final art pass

**Evidence: review findings plus code.** Review 13 returned A6 backgrounds because contrast/scale erased depth and Qori/checkpoint separation; it returned the ending because Qori was too small. Earlier review cycles already exposed weak Chart ghosts. StirSequence uses a large white flash and camera shake; fixed text and mixed-device prompts remain in parts of the UI. Source-art approval alone does not establish handheld or television readability.

**Change:** establish a playfield hierarchy: traversal edges and Qori first, threats/weak points second, interactive props third, decorative stone last. Compose one full gameplay view for each new region before producing the rest. Preserve depth using lower far-layer contrast and clear midground silhouettes. Add independent camera-shake/flash reduction and scalable captions; keep readable timing and avoid colour-only cues. Check a vista in the actual sequence, not just as a beautiful PNG.

**Player difference:** a mint Knot remains identifiable during leaves, fog and attack effects; a platform lip is visible without having to study its texture. The titan's immense background remains atmospheric instead of competing with the path.

**Benefit / tradeoff / cost:** fewer unfair misses and stronger scale. Some decorative contrast must be restrained. S–M rendering/UI work; reuse accepted masters, composition proofs, camera and contrast conventions. Do not repaint accepted art under this review. High confidence; exact thresholds require viewing tests.

**Smallest test:** 1920×1080 gameplay composition and 1280×720 readability stress captures, including damage/FX, Chart, a guardian and captions, followed by actual handheld/TV testing when available. The stress size is not a declared Switch 2 output mode. Include reduced-motion/flash settings in the same traversal test.

### 8. Give restoration a visible beneficiary; simplify rewards around it

**Evidence: narrative/reward design, not completed gameplay.** The healed Wilted and protective final palm are strong. The brief varies where the Wilted reveals itself or is first fought: Act II summary versus R5 detailed rooms and R7 guardian list. Clarify those beats before cinematics/encounters expand. The planned reward economy has seeds, shards, Sproutlings, tiers and other upgrades; the current save/health code primarily implements whole heart seeds. Seven seeds plus 14 regional shards plus seven Sproutling-return shards would yield five additional full seeds and one leftover shard if all stated rules remain. That arithmetic needs an intentional health budget.

Sources: [story acts](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:83), [rewards](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:201), [Wilted role](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md:222), [health](C:/UnityProjects/Qolossal/Assets/Scripts/PlayerHealth.cs), [save pickup counting](C:/UnityProjects/Qolossal/Assets/Scripts/Progress/GameSave.cs).

**Change:** before Grip, show one small inhabitant unable to cross or reach shelter; after Grip, its movement demonstrates the bridge's value. No extra speech is required. Establish the Wilted as a failed helper through one repeated gesture or object before the reveal; use the same visual language when freed. Resolve one canonical sequence: glimpse, confrontation, freeing, reunion. Retain the healed ending and safe inhabited terraces.

Keep permanent traversal/world changes as the primary rewards. For the slice, use one heart reward and one rescued inhabitant; defer tiers/vial expansion and do not add collectible counts to every exit. Later cap and distribute health gains intentionally. Calm defender behavior is a useful proposed restoration payoff, but the inspected Sentinel/Shellback code does not itself demonstrate that implemented state.

**Benefit / tradeoff / cost:** makes care tangible and reduces busywork. Lower numerical reward frequency may disappoint completionists; optional character discoveries can still reward them without a new currency. M animation/placement/state work; reuse Sproutling/NPC/Wilted art and existing persistent variants. Confidence medium-high on coherence, medium on audience preference.

**Smallest test:** after the slice ask “Who benefited from what you did?” and “What do you want to discover next?” If answers depend on an explanatory lore paragraph, improve the visible consequence before adding dialogue.

### 9. Expand the game only behind a measured integration gate

**Evidence: current engineering improvements plus missing validation.** Safe write-and-backup saving is already implemented; calling it a single unprotected overwrite would be obsolete. However, relic unlock and Knot wake are separate writes during a sequence. An interruption can preserve only part of the intended progression; recovery should be defined and tested. File flushes are synchronous, so stalls are a measurement question, not a confirmed defect. The latest import commit restores large source resolution; that is not proof of acceptable runtime memory.

Sources: [GameSave](C:/UnityProjects/Qolossal/Assets/Scripts/Progress/GameSave.cs:75), [ability grants](C:/UnityProjects/Qolossal/Assets/Scripts/PlayerAbilityController.cs), [StirSequence](C:/UnityProjects/Qolossal/Assets/Scripts/World/StirSequence.cs), [importer](C:/UnityProjects/Qolossal/Assets/Editor/ArtImport/CodexArtImporter.cs), [platform direction](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md).

**Change:** first build a representative Windows player and collect frame times, memory and scene-transition peaks with dense parallax, guardian, FX and Chart. Preserve full-resolution accepted masters and tune derived resources by category and measured visual quality. Keep region loading; do not introduce a seamless streaming world merely for architectural elegance. Test milestone recovery, backup fallback, checkpoint relocation after stirs and old-save migration. Put platform storage/accounts/lifecycle behind adapters using official SDKs once available.

Keep the current Unity version until module/SDK compatibility is verified. Profile each supported device/mode separately; Series S and Switch 2 are not exempt because the art is 2D. No assumed console certification or performance claim follows from this report.

**Benefit / tradeoff / cost:** finds expensive bottlenecks before multiplying them. Upfront engineering delays visible content. M for a reproducible PC gate; console integration/QA can be L and depends on access and staffing. Reuse existing tests, importer, scene boundaries and save schema. High confidence in the sequence, low confidence in any unmeasured performance prediction.

**Smallest test:** instrument one complete slice run, repeated transitions and save interruption at each progression boundary. Report CPU/GPU frame-time distribution against the 16.67 ms target, peak memory, stalls and recovery outcomes. Missing hardware results stay “not tested.” If level builders slow iteration, compare one small manually authored/serialized room with the builder workflow before replacing the production pipeline.

### 10. Approve the musical voice before designing its adaptive machinery

**Evidence: explicit user rejection and Review 13 cancellation.** Reproducible masters, LUFS compliance and equal stem lengths did not make the procedural auditions appealing. The requested references suggest interest in memorable melody, acoustic colour, spacious fantasy atmosphere and melancholy; that is an interpretation of taste, not an approved specification. D-A-B-A was chosen, but it need not be forced into every cue or retained as an obligatory kalimba timbre.

**Change:** audition three short original directions for the same Palm sequence: intimate acoustic pastoral; luminous, restrained choral/orchestral; and a more plaintive folk-led approach. Describe musical qualities rather than requesting copied melodies. Choose one in context, then finish a region loop plus a brief stir response before commissioning other tracks. No music generation is authorized by this review.

Start adaptation with exploration and tension, plus a small restoration accent. Do not assume Suno delivers perfectly synchronized stems or seamless game loops; inspect the actual exports and budget editing. Global accumulation of eight musical layers can crowd quiet regions, and the proposed heartbeat tempo must be reconciled with each cue's pulse. The platform policy's mandatory eight-stem wording now conflicts with the cancelled music direction; recommend a later policy update after approval, not an edit here.

**Benefit / tradeoff / cost:** musical identity leads technology and preserves silence. Fewer layers initially provide less continuous musical evolution. S audition effort, M editing/integration; reuse timing/QA tools and the motif as an option, not rejected audio as a sonic template. Confidence high on the approval sequence, medium on the suggested palette.

**Smallest test:** level-match the three auditions against the same 60–90 seconds of gameplay; select by fit, memorability and fatigue. Test the chosen loop for ten minutes with movement/attack sounds and one transition. Technical and creative acceptance are separate decisions. Keep delivered music untouched and all cancelled production stopped.

## D. Three bolder alternatives

These are optional departures, not additional features to stack together.

### D1. “The titan helps back” — one local response between major stirs

**Current opportunity:** restoration is mostly expressed at Knot milestones. **Alternative:** in each region, one clearly marked living structure responds to Qori: a finger supports a crossing, a tendon bends a branch, or breath raises a leaf. Make it a local authored interaction, not a universal command system. The player experiences a relationship with the body before the large restoration.

**Benefit / drawback / cost:** a more distinctive companion-like world, but ambiguity about where responses work could be frustrating. M per reusable interaction family; potentially L if every structure responds. Reuse clench art, triggers, platforms and variants. **Recommendation:** prototype one Palm response; adopt only if players understand it without repeated prompts. Confidence medium. Smallest test: one 30-second crossing with an obvious physical cause and effect.

### D2. “Restore a small connected sanctuary” — compact ecological campaign

**Current opportunity:** 28 spaces threaten to diffuse the restorative theme. **Alternative:** make fewer, denser playable hubs in which the same inhabitants and landmarks change repeatedly; remaining body regions become shorter journeys or vistas. The player remembers one community and watches it recover rather than clearing a long sequence of new rooms.

**Benefit / drawback / cost:** stronger attachment and less unique level construction, but less travel variety and substantial reopening of region structure. M redesign now, L if adopted late. Reuse all regional art selectively, NPCs and state variants. **Recommendation:** reserve as a scope fallback if the full Palm loop is expensive or weak; do not cut seven-region exploration preemptively. Confidence medium. Smallest test: revisit one hub after two mocked-up restoration states and compare interest with a new linear room.

### D3. “Travel with the breath” — traversal rhythm as the central skill

**Current opportunity:** the titan's breathing could make the world feel alive beyond visual motion. **Alternative:** a readable inhale/exhale rhythm organizes a few routes: cross in the calm phase, rise on the exhale, shelter during the strongest gust. It can support musical identity without requiring music to communicate timing.

**Benefit / drawback / cost:** a recognizable physical rhythm, but compulsory waiting and constant wind could undermine precise movement or accessibility. M prototype, L campaign retuning; reuse dash/glide, future currents and breath visuals. **Recommendation:** try as the Grove's local identity, not a global timing law. Confidence low-medium. Smallest test: a one-minute route with a safe bypass and visual/audio cues, comparing voluntary timing advantages against forced waiting.

## E. Proposed first 20–30 minutes

This is a proposed final-game onboarding sequence, not a description of A0's current gallery. It assumes recommendation 1 is approved. If players need more time, shorten or simplify encounters rather than rushing instructions.

| Time | Player experience | What it teaches or motivates |
|---|---|---|
| 0–3 min | Qori lands in moss, moves beneath a vast sheltering stone arch, jumps a small gap and activates a Waymark. A distant pulse shifts leaves. | Safety, scale, basic control and checkpoint language; no lore dump. |
| 3–6 min | One clearly warned enemy tests a sword strike or avoidance. A harmless failed crossing reveals the Crease and a stranded small inhabitant. | Combat readability and a remembered problem worth fixing. |
| 6–10 min | An early shrine grants climbing. Two short, safe surfaces teach sticking/jumping; a third combines the skill with a simple moving or folding surface. | Learn one ability through escalating use. Keep thread, dash, glide and the full controls list out of this opening. |
| 10–14 min | Climb to a viewpoint where the landscape unmistakably resolves into part of a hand; show the sleeping crown-mask in the distance. Open the Chart at a Waymark. | “I am on a body,” where the Knot lies, and one deliberately unexplored branch. |
| 14–19 min | A compact approach and Knucklebramble encounter test warning-reading and positioning. Place the retry point close. | A guardian obstructs bodily function; defeat costs time and learning, not currency. |
| 19–22 min | Qori deliberately interacts with the freed Knot; a brief readable stir follows. | Restoration is the player's choice and achievement. Current automatic proximity wake would need adjustment for this beat. |
| 22–26 min | Return to the familiar Crease. The finger bridge now carries Qori and the small inhabitant to safety. A new perch or exit is plainly visible. | The core payoff under player control; meaningful backtracking. |
| 26–30 min | Reach a view into the Arm, with flowing water and a different spatial rhythm. End at a safe Waymark with one optional curiosity. | Desire to continue. Introduce travel before random Wild travel; postpone the next ability lecture. |

The opening needs one strong reveal, one successful restoration and one reason to proceed. It does not need to demonstrate every weapon, collectible, NPC and movement feature.

## F. Minimal vertical slice

**Build one 20–30-minute Palm loop plus a tiny Arm transition room, not all of R1's originally prescribed four spaces.** Reuse A0, the Crease, Grip chamber, Knucklebramble, accepted Chart art and a small section of A1. This is proposed work only.

Include one early climbing shrine, two teaching obstacles, one optional branch, one compact guardian, the stir and changed return, one visual beneficiary, a Waymark, Chart and scene transition. Use representative visuals where readability/performance are under test. Other layout can remain greybox. Use a temporary approved-to-audition audio cue only after a direction is chosen; silence is preferable to treating rejected music as final.

Keep a separate tiny validation room for dash/glide/thread/pogo combinations and Wild destination safety. It tests future interaction risks without overloading the opening. Use debug state fixtures for alternative knot orders; do not claim those fixtures prove a complete campaign.

| Risk | Minimal evidence | Proposed exit criterion |
|---|---|---|
| Identity is only decoration | Observe five first-time players before explaining the premise. | At least four independently connect the changed route to healing the titan; record their actual words. |
| Restoration adds tedious backtracking | Observe the same before/after route. | At least four find the new crossing without coaching and regard the return as worthwhile. Treat five players as formative evidence, not statistical proof. |
| Progression breaks | Fresh-save walkthrough plus state/route checks. | No required unowned ability; both Act II order fixtures retain a safe onward/return path. |
| Boss is mostly waiting | Record exposure cycles, missed warnings, damage and weapon choice. | Players can explain the danger and their response; investigate repeated idle cycles or one universally dominant choice. |
| Controller friction | Run all gameplay and menus without a mouse. | No inaccessible action, focus loss or unintentional action after closing UI; document comfort complaints separately. |
| Stir presentation fails | Capture actual sequence and reduced-flash variant. | Vista/route change is visible, captions legible and player position safe after state swap. |
| Save interruption causes inconsistent progress | Interrupt ability/stir/checkpoint writes and reload backup/older saves. | Recover to a documented, playable milestone; no stranded checkpoint or lost required ability. |
| Visuals hide gameplay | Real game frames, not standalone art boards. | Qori, traversable edge and Knot are identifiable at intended viewing sizes and with effects active. |
| Runtime cost is too high | Standalone PC profile now; target hardware when available. | Measured frame-time/memory report with bottlenecks and tested configurations. No inferred console pass. |

After two focused iterations, decide whether to expand, simplify the structure, or revise the central interaction. Do not answer a failed restoration test by making the next region larger.

## G. Practical development sequence

### Essential before expansion

1. **Approve a small set of design changes:** ability timing, one restoration loop, flexible region lengths. Record one authoritative progression table and Wilted beat sequence. Reconcile cancelled music with old policy wording only after approval.
2. **Build the compact slice:** reuse current scenes/assets and authored variants. Wire the stir vista and confirm its rendering. Add only the local arena/return changes required to test the premise.
3. **Run human tests:** new players, keyboard/mouse and controller. Fix misunderstood routes, unfair warnings and uncomfortable actions before increasing encounter complexity.
4. **Close persistence and navigation risks:** ability/stir recovery, checkpoints after transformations, every Wild arrival, legal region orders. Keep existing automated tests and extend only where these new risks demand it.
5. **Establish the performance/platform gate:** Windows standalone first; establish console access and official module compatibility in parallel as administrative preparation. Measure available targets and state missing access explicitly. Separate source-art QA, imported-resource QA and device QA.
6. **Author one contrasting Arm loop:** flowing water and a sluice should feel different from Palm climbing. Confirm the kit/build workflow permits fast iteration before creating an editor or replacing builders.
7. **Expand by proven needs:** plan remaining regions and guardians from observed pacing and actual production throughput. Complete applicable returned art/integration tasks under their own authorization. This review starts no new batch.

If the project has one or a few generalists, unique guardian mechanics and stateful revisits will compete directly with level production: reuse mechanism families and shorten weak regions. If dedicated design, animation and engineering capacity exists, region work can overlap after interface/scale conventions and the slice are stable. Neither staffing model is assumed to be the actual team.

### Optional polish after the slice succeeds

- Additional ambient inhabitants and elaborate tending animations beyond the first beneficiary in recommendation 8.
- More elaborate stir transitions, camera flourishes and incidental titan motion beyond the readable version in recommendations 2 and 7.
- Additional weapon tiers or optional challenge rooms only if recommendation 5's encounter tests establish a need.
- Expanded musical layering only after recommendation 10's direction and loop approval.

These are extensions of the ranked recommendations, not requirements for the slice. Do not add a seamless streaming world, procedural maze, crafting economy, morality meter or general titan-command system to solve a problem the prototype has not demonstrated.

## H. Decisions that genuinely need the owner

Only the first three block the proposed slice. The rest can wait for evidence.

1. **Ability timing:** approve obtaining a region's traversal ability early, with the Knot primarily restoring the world? Recommended: yes. This changes the approved reward sequence.
2. **Campaign structure:** allow variable region lengths while preserving the seven body regions, rather than committing to exactly 28 spaces? Recommended: yes; do not promise a revised duration yet.
3. **Immediate priority:** prioritize the playable restoration slice over expanding the next full region/guardian set? Recommended: yes for gameplay production; already-authorized independent art work need not be discarded.
4. **Wild travel after testing:** if repeated randomness causes navigation frustration, may the design adopt a guaranteed-return excursion? Recommended: decide from the navigation test, preserving the current approved rule meanwhile.
5. **Musical voice:** which of the three short in-context auditions best fits Qolossal? Recommended: choose by listening before committing more tracks; do not ask the owner to choose codecs or stem architecture first.

No decision is needed to preserve accepted art, keep the healed Wilted ending, exclude original Switch, maintain the target platform list or acknowledge that console readiness has not been tested. Those directions already exist.

## Evidence register and review limitations

- Canonical creative direction: [world design](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_WORLD_DESIGN.md), especially sections 1, 3–6, 10 and 11.
- Current requests/approval: [Batch 6](C:/UnityProjects/Qolossal/Tools/ArtRequests/QOLOSSAL_ASSET_REQUESTS_BATCH6.md), [Review 13](C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/CLAUDE_REVIEW_13.md), [delivery handoff](C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/REVIEW13_HANDOFF.md). Delivery checklist passes are qualified by subsequent reviewer rejection; they are not final proof of visual quality.
- Platform direction/history: [policy](C:/UnityProjects/Qolossal/Tools/Design/QOLOSSAL_PLATFORM_DIRECTION.md), [platform handoff](C:/UnityProjects/Qolossal/Tools/IncomingArt/Codex_v2/CLAUDE_PLATFORM_HANDOFF.md). Its old save/import findings are superseded where current commits implement improvements; its music wording needs reconciliation with cancellation.
- Project memory: [build plan](C:/Users/Qasim/.claude/projects/C--UnityProjects/memory/qolossal-game-build-plan.md), [world memory](C:/Users/Qasim/.claude/projects/C--UnityProjects/memory/qolossal-world-design.md), [current memory](C:/Users/Qasim/.claude/projects/C--UnityProjects/memory/MEMORY.md). Historical “next” items were not treated as today's backlog.
- Existing test infrastructure, not rerun here: [WorldPlayTest](C:/UnityProjects/Qolossal/Assets/Editor/World/WorldPlayTest.cs), [GuardianPlayTest](C:/UnityProjects/Qolossal/Assets/Editor/World/GuardianPlayTest.cs), [MovementAbilityPlayTest](C:/UnityProjects/Qolossal/Assets/Editor/Mechanics/MovementAbilityPlayTest.cs).

There are no new measured gameplay, frame-time or human-test results in this report. Cost rankings, pacing and audience reactions are informed proposals. Platform certification specifics must come from the project's approved SDK documentation and actual device validation, not inference from PC code or source-art proofs.

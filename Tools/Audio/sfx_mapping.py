"""Qolossal sound-effect mapping: what each ElevenLabs Sound ID is for in the game.

The one hand-written table behind Assets/Audio/SFX/sfx_manifest.json and Tools/Audio/SFX_MAPPING.csv
(build_sfx_library.py writes both). Each row: event, hook, status, plus tuning overrides.

status:
  wired          - a code hook plays it in the Mountain Relief Atlas game
  wired-unplaced - hooked in a shared script the Mountain Relief Atlas scenes don't place yet
  no-hook        - nothing in the game happens that it belongs to yet
  future         - for a planned feature that isn't built
"""

# Bus for each CSV group (by its first word or two), and the bus's starting tuning.
GROUP_BUS = [
    ("Qori: movement", "Player"), ("Qori: weapons", "Combat"), ("Enemies", "Enemy"), ("Guardian", "Enemy"),
    ("World", "World"), ("Ambiences", "Ambience"), ("Pickups", "Pickup"), ("Town", "Town"), ("UI", "UI"), ("Story", "Story"),
]

# target: the active loudness each take is matched to (dBFS, see build_sfx_library.py).
BUS_DEFAULTS = {
    "Player":   dict(target=-24, volume=.85, pitch=.04, cooldown=.05, voices=2, positional=False),
    "Combat":   dict(target=-20, volume=.85, pitch=.05, cooldown=.04, voices=3, positional=False),
    "Enemy":    dict(target=-22, volume=.85, pitch=.05, cooldown=.08, voices=3, positional=True),
    "World":    dict(target=-22, volume=.85, pitch=.03, cooldown=.10, voices=2, positional=True),
    "Pickup":   dict(target=-22, volume=.85, pitch=.02, cooldown=.05, voices=2, positional=False),
    "Town":     dict(target=-24, volume=.85, pitch=.03, cooldown=.05, voices=2, positional=False),
    "UI":       dict(target=-26, volume=.80, pitch=.00, cooldown=.04, voices=2, positional=False),
    "Story":    dict(target=-20, volume=.90, pitch=.00, cooldown=1.0, voices=1, positional=False),
    "Ambience": dict(target=-30, volume=.75, pitch=.00, cooldown=.50, voices=1, positional=False),
}

# Sounds that must loop though the CSV's Loop column doesn't say so (the ambience heading said
# "all loop", but the CSV was generated from the rows only).
EXTRA_LOOPS = {"Amb_Cradle", "Amb_Causeway", "Amb_Terraces", "Amb_Qvale", "Amb_Ribwood", "Amb_Heights", "Amb_Summit",
               "Amb_Descent", "Amb_Cave", "Amb_HouseInterior", "Water_Stream", "Waterfall", "Night_Hum"}

W, U, N, F = "wired", "wired-unplaced", "no-hook", "future"

# id: (event, hook, status, overrides)
MAP = {
    # ---- Qori: movement
    "Qori_Step_Moss":   ("Footstep on moss/soil (scene surface rule Moss)", "QoriMovementFeedback: rig footstep", W, dict(target=-35, cooldown=.08, pitch=.06)),
    "Qori_Step_Stone":  ("Footstep on stone (scene surface rule Stone)", "QoriMovementFeedback: rig footstep", W, dict(target=-35, cooldown=.08, pitch=.06)),
    "Qori_Step_Wood":   ("Footstep on wood (house interiors)", "QoriMovementFeedback: rig footstep", W, dict(target=-35, cooldown=.08, pitch=.06)),
    "Qori_Step_Water":  ("Splashing into a pool; footsteps on a Water surface", "WaterPool splash; QoriMovementFeedback (ground named water/shallow)", U, dict(target=-35, cooldown=.08, pitch=.06)),
    "Qori_Jump":        ("Jump, wall jump or flower boost leaves the ground", "QoriMovementFeedback: PlayerMovement.LaunchVersion", W, {}),
    "Qori_Land_Soft":   ("Landing from a short drop", "QoriMovementFeedback: PlayerMovement.LandingVersion", W, dict(target=-26)),
    "Qori_Land_Hard":   ("Landing from a long fall", "QoriMovementFeedback: PlayerMovement.LandingVersion", W, {}),
    "Qori_LedgeGrab":   ("Catches a ledge", "QoriMovementFeedback: IsLedgeHanging starts", W, {}),
    "Qori_Climb":       ("Climbs up from a ledge", "QoriMovementFeedback: IsLedgeClimbing starts", W, {}),
    "Qori_Dash":        ("Wind Leaf dash", "QoriMovementFeedback: PlayerMovement.DashVersion", W, {}),
    "Qori_Glide_Open":  ("Glidecap opens", "QoriMovementFeedback: IsGliding starts", W, {}),
    "Qori_Glide_Loop":  ("While gliding", "QoriMovementFeedback: SfxEmitter on Qori while IsGliding", W, dict(target=-30, voices=1)),
    "Qori_Thread_Shoot": ("Thread flicks out to an anchor", "QoriMovementFeedback: PlayerThread.AttachmentVersion", W, {}),
    "Qori_Thread_Attach": ("Thread snags (just after the flick)", "QoriMovementFeedback: AttachmentVersion + 0.08 s", W, {}),
    "Qori_Thread_Swing": ("Swinging fast through the bottom of the arc", "QoriMovementFeedback: attached, speed > 5, rising", W, dict(cooldown=.8, voices=1)),
    "Qori_Thread_Release": ("Lets go of the thread", "QoriMovementFeedback: PlayerThread.ReleaseVersion", W, {}),
    "Qori_Hurt":        ("Loses a heart and survives", "PlayerHealth.TakeDamage", W, dict(cooldown=.3)),
    "Qori_Death":       ("Loses the last heart", "PlayerHealth.TakeDamage", W, dict(voices=1, cooldown=1)),
    "Qori_Respawn":     ("Back at the checkpoint after a death, fall or deep water", "QoriMovementFeedback: PlayerMovement.RespawnVersion", W, dict(voices=1, cooldown=1)),
    "Qori_Sit":         ("Sits on the listening bench", "ListeningSpot.Sit", W, {}),
    # ---- weapons and combat
    "Weapon_Staff_Swing": ("Leaf Staff attack starts", "PlayerCombatFeedback.Started (weapon kind Staff)", W, {}),
    "Weapon_Staff_Hit":   ("Leaf Staff hit lands", "PlayerCombatFeedback.Hit (Damaged, Staff)", W, {}),
    "Weapon_Reedblade_Swing": ("Leaf Sword / reed blade attack starts", "PlayerCombatFeedback.Started (Sword)", W, {}),
    "Weapon_Reedblade_Hit":   ("Sword hit lands", "PlayerCombatFeedback.Hit (Damaged, Sword)", W, {}),
    "Weapon_Mace_Swing":  ("Seedpod Mace attack starts", "PlayerCombatFeedback.Started (Mace)", W, {}),
    "Weapon_Mace_Hit":    ("Mace hit lands (and cracks a Shellback's shell)", "PlayerCombatFeedback.Hit (Damaged, Mace)", W, {}),
    "Weapon_Spear_Thrust": ("Thorn Spear attack starts", "PlayerCombatFeedback.Started (Spear)", W, {}),
    "Weapon_Spear_Hit":   ("Spear hit lands", "PlayerCombatFeedback.Hit (Damaged, Spear)", W, {}),
    "Weapon_Sling_Shot":  ("Resin sling releases a pellet", "QoriResinShot.Launch", W, {}),
    "Weapon_Sling_Impact": ("Sling pellet hits a target or rock", "QoriResinShot (hit)", W, {}),
    "Combat_Pogo_Bounce": ("Downward strike bounces Qori (pods, grubs)", "PlayerCombatFeedback.Hit (CanPogo)", W, {}),
    "Combat_Blocked":     ("A blow glances off a shell, a guardian or a seed switch", "PlayerCombatFeedback.Hit (Blocked, not a Sentinel); QoriResinShot", W, dict(cooldown=.12)),
    "Combat_Parry":       ("A parry", "none: no parry exists", F, {}),
    "Combat_ChargeUp":    ("Charging a heavy attack", "none: no charged attack exists", F, {}),
    "Combat_ChargeRelease": ("Releasing a charged attack", "none: no charged attack exists", F, {}),
    "Combat_HitStop_Heavy": ("Weight under a mace hit's freeze frame", "PlayerCombatFeedback.Hit (Damaged, Mace)", W, dict(target=-24)),
    "Combat_EnemyDefeat": ("An enemy is defeated", "EnemyBase.Die", W, dict(positional=True)),
    # ---- enemies
    "Crawler_Move":     ("Crawler skitters while patrolling", "GroundCreature: SfxEmitter while moving", U, dict(target=-28, voices=2)),
    "Thornwing_Hover":  ("Thornwing hovering", "ThornwingEnemy: SfxEmitter while alive", W, dict(target=-28, voices=2)),
    "Thornwing_Rear":   ("Thornwing rears up (telegraph)", "ThornwingEnemy.Enter(Telegraph)", W, dict(supersedes=["Enemy_Telegraph"])),
    "Thornwing_Dive":   ("Thornwing dives", "ThornwingEnemy.Enter(Dive)", W, {}),
    "Spitter_Swell":    ("Pod Spitter swells to shoot", "SpitterEnemy: swell starts", W, dict(supersedes=["Enemy_Telegraph"])),
    "Spitter_Spit":     ("Pod Spitter spits a seed", "SpitterEnemy.Spit", W, {}),
    "Spitter_SeedHit":  ("A seed breaks on rock or a weapon", "SpitterSeed", W, {}),
    "Shellback_Walk":   ("Shellback plodding", "ShellbackEnemy: SfxEmitter while walking", W, dict(target=-28, voices=2)),
    "Shellback_ShellCrack": ("The mace cracks a Shellback's shell", "ShellbackEnemy.ReceiveCombatHit", W, {}),
    "Grub_Burrow":      ("Burrow Grub tunnelling toward Qori", "GrubEnemy: SfxEmitter in Tunnel/Telegraph", W, dict(target=-26, voices=2)),
    "Grub_Erupt":       ("Burrow Grub erupts", "GrubEnemy.Enter(Rise)", W, {}),
    "Newt_Ripple":      ("Ripple Newt under the water", "NewtEnemy: SfxEmitter while Submerged", W, dict(target=-30, voices=2)),
    "Newt_Leap":        ("Ripple Newt leaps out (and back in, quieter)", "NewtEnemy.Enter(Leap/Submerged)", W, {}),
    "Sentinel_Step":    ("Bark Sentinel steps", "SentinelEnemy: every 0.55 s while moving", W, dict(target=-26, cooldown=.3)),
    "Sentinel_ShieldBlock": ("A blow hits the bark shield", "PlayerCombatFeedback.Hit (Blocked, Sentinel)", W, dict(cooldown=.12)),
    "Sentinel_Bash":    ("Bark Sentinel bashes", "SentinelEnemy.Enter(Bash)", W, {}),
    "GustMoth_Wings":   ("Gust Moth hovering", "GustMothEnemy: SfxEmitter while alive", W, dict(target=-28, voices=2)),
    "GustMoth_Blast":   ("Gust Moth beats a wind blast", "GustMothEnemy.Blast", W, {}),
    "Enemy_Telegraph":  ("Any enemy's warning glint", "Fx.Glint (enemy telegraphs only)", W, dict(cooldown=.15)),
    # ---- guardian
    "Guardian_Awaken":  ("Knucklebramble wakes when Qori first comes near", "KnucklebrambleGuardian.Enter (Dormant to Pause, once)", W, dict(voices=1, cooldown=5)),
    "Guardian_ArmRaise": ("A thorn-arm rises (telegraph)", "KnucklebrambleGuardian.Enter(Telegraph)", W, dict(supersedes=["Enemy_Telegraph"])),
    "Guardian_ArmSlam": ("A thorn-arm slams", "KnucklebrambleGuardian: slam impact", W, {}),
    "Guardian_Hurt":    ("The exposed core is struck", "KnucklebrambleGuardian.ReceiveCombatHit", W, {}),
    "Guardian_Defeat":  ("Knucklebramble collapses", "KnucklebrambleGuardian.Die", W, dict(voices=1, supersedes=["Combat_EnemyDefeat"])),
    # ---- world
    "Knot_Idle":        ("A sleeping knot hums", "MraKnot: SfxEmitter while not awake", W, dict(target=-30, voices=1)),
    "Knot_Wake":        ("Qori touches a knot", "MraKnot.Quake start", W, dict(voices=1, cooldown=2)),
    "Quake_Stir":       ("The land shakes after a knot wakes", "MraKnot.Quake shake", W, dict(voices=1, cooldown=2, positional=False)),
    "Quake_Small":      ("A short tremor", "none: no aftershock event", N, {}),
    "Swell_Loop":       ("The Ribwood's swell ledge heaving", "MraSwell: SfxEmitter", W, dict(target=-28, voices=1)),
    "Spring_Pulse":     ("A warm spring pulsing", "WarmSpring: SfxEmitter", U, dict(target=-30, voices=2)),
    "Arch_Fall":        ("A great arch falls across a ravine", "none: the MRA stirs swap art without a fall", N, {}),
    "Rock_Fall":        ("A falling rock", "FallingRock", U, {}),
    "Platform_Crumble": ("A fragile ledge starts to crumble", "CrumblePlatform", U, {}),
    "Platform_Reform":  ("A crumbled ledge re-forms", "CrumblePlatform", U, {}),
    "Thorns_Warn":      ("Timed thorns shiver before rising", "MraTimedHazard: warn phase starts", W, dict(target=-28, cooldown=.2)),
    "Thorns_Rise":      ("Timed thorns stab up", "MraTimedHazard: up phase starts", W, dict(cooldown=.2)),
    "Thorns_Sink":      ("Timed thorns sink", "MraTimedHazard: down phase starts", W, dict(target=-28, cooldown=.2)),
    "Breakable_Smash":  ("A brittle wall breaks", "Breakable", U, {}),
    "SecretWall_Open":  ("A hidden wall crumbles away", "SecretWall", U, {}),
    "Gate_Open":        ("A gate opens (heard wherever it is: a puzzle's answer)", "MraGate: IsOpen becomes true after the scene starts", W, dict(positional=False)),
    "Gate_Close":       ("A gate closes", "MraGate: IsOpen becomes false after the scene starts", W, dict(positional=False)),
    "Lever_Pull":       ("A lever is pulled", "MraLever.Use (no wheel)", W, {}),
    "Wheel_Turn":       ("A sluice/winch wheel is turned", "MraLever.Use (with MraWheelSpin)", W, {}),
    "PressurePlate_Down": ("A pressure plate sinks", "PressurePlate", W, {}),
    "SeedSwitch_Hit":   ("A seed switch or a sequence light is struck", "SeedSwitch, MraSequence.Press", W, {}),
    "GrowthFlower_Bloom": ("A growth flower blooms", "GrowthFlower", U, {}),
    "MovingPlatform_Loop": ("A moving platform slides", "MovingPlatform: SfxEmitter", U, dict(target=-30, voices=2)),
    "HangingChain":     ("Qori brushes hanging chains", "none: HangingChains has no contact event", N, dict(cooldown=.6)),
    # ---- ambience
    "Amb_Cradle":       ("MR01 Cradle bed", "SfxPlayer scene rule MRAtlas_MR01", W, {}),
    "Amb_Causeway":     ("MR02 Causeway bed", "SfxPlayer scene rule MRAtlas_MR02", W, {}),
    "Amb_Terraces":     ("MR03 Terraces bed", "SfxPlayer scene rule MRAtlas_MR03", W, {}),
    "Amb_Qvale":        ("MR04 Qvale bed", "SfxPlayer scene rule MRAtlas_MR04_Qvale", W, {}),
    "Amb_Ribwood":      ("MR05 Ribwood bed", "SfxPlayer scene rule MRAtlas_MR05", W, {}),
    "Amb_Heights":      ("MR06 Heights bed", "SfxPlayer scene rule MRAtlas_MR06", W, {}),
    "Amb_Summit":       ("MR07 Summit bed", "SfxPlayer scene rule MRAtlas_MR07", W, {}),
    "Amb_Descent":      ("MR08 Descent bed", "SfxPlayer scene rule MRAtlas_MR08", W, {}),
    "Amb_Cave":         ("Side caves (all regions)", "SfxPlayer scene rule MRAtlas_MRxx_Cyy", W, {}),
    "Amb_HouseInterior": ("Qvale home interiors", "SfxPlayer scene rule MRAtlas_MR04_C02..C06", W, {}),
    "Water_Stream":     ("Near a stream", "none: no flowing-water object (pools are still)", N, dict(positional=True, voices=2, target=-30)),
    "Waterfall":        ("Near a waterfall", "WaterfallSplash: SfxEmitter", U, dict(positional=True, voices=2, target=-28)),
    "Night_Hum":        ("Quiet night hum", "none: the game has no night scene", N, {}),
    # ---- pickups and progress
    "Amber_Pickup":     ("A small piece of Amber", "none: enemies drop sap orbs, not Amber", F, dict(cooldown=.03, voices=4, pitch=.08)),
    "Amber_PickupBig":  ("A chamber's Amber find", "MraReward (Amber)", W, {}),
    "HeartSeed_Get":    ("A heart seed (chamber, pickup or shop)", "MraReward (HeartSeed), HeartSeed, TownShop", W, dict(supersedes=["Shop_Buy", "Smithy_Upgrade"])),
    "Heal":             ("A sap orb restores a heart", "SapOrb", W, {}),
    "Relic_Get":        ("A shrine's relic flies into Qori", "AbilityShrine.Collect", W, {}),
    "SongShell_Get":    ("A song shell is found", "MraReward (SongShell)", W, {}),
    "Waymark_Light":    ("A Waymark charts its level", "Waymark: first touch", W, dict(supersedes=["Checkpoint_Touch"])),
    "Waymark_Travel":   ("Fast travel from the Chart", "MraChart.TryTravel", W, dict(supersedes=["Chart_Close"])),
    "Checkpoint_Touch": ("A new checkpoint becomes Qori's", "PlayerMovement.ActivateCheckpoint (a change)", W, {}),
    "LoreStone_Read":   ("A lore stone, find or reflection", "MraReward (Lore/Find/Reflection), LoreStone", W, {}),
    "Discovery":        ("A place found (cave mouth, Qvale home)", "MraChamberDoor, MraDiscovery: new discovered flag", W, dict(cooldown=1)),
    "Door_Enter":       ("Into a Qvale home", "MraChamberDoor.Enter (a house)", W, {}),
    "Door_Exit":        ("Out of a Qvale home", "MraChamberReturn (from a house)", W, {}),
    "Cave_Enter":       ("Into a side cave", "MraChamberDoor.Enter (a cave)", W, {}),
    # ---- town
    "Smithy_Anvil":     ("Brannick's forge opens", "TownShop.Open (forge)", W, {}),
    "Smithy_Upgrade":   ("A forge purchase", "TownShop.Buy (forge)", W, {}),
    "Shop_Buy":         ("Any other purchase", "TownShop.Buy", W, {}),
    "Shop_Cannot":      ("Not enough Amber", "TownShop.Buy (denied)", W, dict(cooldown=.3)),
    "Map_Unroll":       ("Scribble's pages open", "TownShop.Open (pages)", W, {}),
    "Map_Stamp":        ("A page bought from Scribble", "TownShop.Buy (pages)", W, dict(supersedes=["Shop_Buy"])),
    "Lantern_Light":    ("A town request lights the lanterns", "none: no lantern request yet", F, {}),
    "Bell_Tower":       ("The bell request", "none: no bell request yet", F, {}),
    "Dialogue_Blip_Small": ("Dialogue text typing", "DialogueBox typing (every 3rd letter)", W, dict(target=-30, cooldown=.05, voices=1, pitch=.06)),
    "Dialogue_Blip_Low":   ("An old speaker's text typing", "DialogueBox typing (low-voice speakers)", W, dict(target=-30, cooldown=.06, voices=1, pitch=.05)),
    # ---- UI and Chart
    "UI_Move":          ("Menu selection moves", "SfxPlayer (uGUI selection), TownShop, ListeningSpot", W, dict(target=-28, cooldown=.05)),
    "UI_Confirm":       ("Menu choice", "GamePauseMenu buttons (incl. Sound page), ListeningSpot, TownShop.Open (other shops)", W, {}),
    "UI_Back":          ("Back / leave a menu", "GamePauseMenu Back/Cancel/Esc, TownShop.Close, ListeningSpot.Leave", W, {}),
    "UI_Pause":         ("Game pauses", "GamePauseMenu.Pause", W, {}),
    "UI_Unpause":       ("Game resumes", "GamePauseMenu.Resume", W, {}),
    "Chart_Open":       ("The Chart opens", "MraChart.Show", W, {}),
    "Chart_Close":      ("The Chart closes", "MraChart.Hide", W, {}),
    "Chart_PieceAdd":   ("A map piece joins the Chart", "none: pieces join without a moment of their own", N, {}),
    "Save":             ("Game saved", "none: saving is continuous and silent", N, {}),
    "Banner_Show":      ("A region's name plate appears", "AreaTransition (name shown)", W, {}),
    # ---- story
    "Story_Sprout":     ("Qori sprouts (opening)", "none: no opening sequence exists", N, {}),
    "Story_Reveal":     ("The reveal's pull-out on the Summit ledge", "MraReveal.Run (Sfx.PlayStory)", W, {}),
    "Story_EyeOpen":    ("The careful ending begins (the waking)", "MraEnding.Run start (Sfx.PlayStory)", W, {}),
    "Story_Ending":     ("The careful ending's last caption", "MraEnding.Run caption (Sfx.PlayStory)", W, {}),
}

# Scene rules: ambience and footstep surface by scene-name prefix (longest wins).
SCENES = [
    ("MRAtlas_MR01", "Amb_Cradle", "Moss"),
    ("MRAtlas_MR02", "Amb_Causeway", "Stone"),
    ("MRAtlas_MR03", "Amb_Terraces", "Moss"),
    ("MRAtlas_MR04", "Amb_Qvale", "Stone"),
    ("MRAtlas_MR05", "Amb_Ribwood", "Moss"),
    ("MRAtlas_MR06", "Amb_Heights", "Stone"),
    ("MRAtlas_MR07", "Amb_Summit", "Moss"),
    ("MRAtlas_MR08", "Amb_Descent", "Stone"),
] + [(f"MRAtlas_MR0{r}_C0{c}", "Amb_Cave", "Stone") for r in range(1, 9) if r != 4 for c in range(1, 7)] \
  + [(f"MRAtlas_MR04_C0{c}", "Amb_HouseInterior", "Wood") for c in range(2, 7)]

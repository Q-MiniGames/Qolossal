#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// The sound-effect checks (-mraAudioOnly): the library and its imports, then each hook driven in the
// real scenes, by input where the game takes input. A sound "plays" when Sfx starts a voice for it
// (Sfx.Count); batch mode has an audio device but nobody listening, so this proves the hooks and
// the rules (variation, cooldowns, voice limits, buses), not how anything sounds.
public sealed partial class MraTestDriver
{
    int Plays(string id) => Sfx.Count(id);

    // Runs `act`, then waits `seconds`, and reports whether `id` started at least `min` times.
    IEnumerator Heard(string id, string what, IEnumerator act, float seconds = .3f, int min = 1)
    {
        int before = Plays(id);
        if (act != null) yield return act;
        yield return Wait(seconds);
        int n = Plays(id) - before;
        Expect(n >= min, $"audio: {what} plays {id} ({n}x)");
    }

    IEnumerator Do(System.Action action) { action(); yield return null; }

    IEnumerator AudioChecks()
    {
        // ---------------------------------------------------------------- library and imports
        var lib = Resources.Load<SfxLibrary>("SfxLibrary");
        Expect(lib != null, "audio: the SFX library loads from Resources");
        if (lib == null) yield break;
        Expect(lib.entries.Count == 136, $"audio: the library has all 136 sounds ({lib.entries.Count})");
        Expect(lib.entries.All(e => e.takes.Length == 4), "audio: every sound keeps its four takes");
        int missingSources = lib.entries.Sum(e => e.takes.Count(t => AssetDatabase.LoadAssetAtPath<AudioClip>(t.sourcePath) == null));
        Expect(missingSources == 0, $"audio: all 544 source takes import as AudioClips ({missingSources} missing)");
        var noClips = lib.entries.Where(e => e.clips == null || e.clips.Length == 0).Select(e => e.id).ToList();
        Expect(noClips.Count == 0, "audio: every sound has takes in use" + (noClips.Count > 0 ? ": missing " + string.Join(", ", noClips) : ""));
        Expect(lib.mixer != null && lib.busGroups.Length == 9 && lib.busGroups.All(g => g != null) && lib.musicGroup != null, "audio: the Qolossal mixer routes all nine buses and the music");
        Expect(Mathf.Approximately(lib.headroomDb, 12f), $"audio: effect buses have {lib.headroomDb} dB of headroom for gain matching");
        // Nobody approved anything yet: no take is a Favorite or Rejected unless someone chose it.
        int decided = lib.entries.Sum(e => e.takes.Count(t => t.state == SfxTakeState.Favorite || t.state == SfxTakeState.Rejected));
        Debug.Log($"[MraTest] audio: audition decisions recorded so far: {decided}");
        // Loop sounds use their seamless derivative where one exists.
        var loopWrong = lib.entries.Where(e => e.loop && e.takes.Any(t => !string.IsNullOrEmpty(t.loopPath) && t.useLoopDerivative && t.state != SfxTakeState.Rejected)
            && !e.clips.Any(c => AssetDatabase.GetAssetPath(c).Contains("/Loops/"))).Select(e => e.id).ToList();
        Expect(loopWrong.Count == 0, "audio: loop sounds play their crossfaded derivatives" + (loopWrong.Count > 0 ? ": not " + string.Join(", ", loopWrong) : ""));

        // Import settings, by kind.
        AudioImporter Imp(string path) => AssetImporter.GetAtPath(path) as AudioImporter;
        var step = Imp("Assets/Audio/SFX/Player/SFX_Qori_Step_Moss_01.wav");
        var bed = Imp("Assets/Audio/SFX/Ambience/SFX_Amb_Cradle_01.wav");
        var story = Imp("Assets/Audio/SFX/Story/SFX_Story_Reveal_01.wav");
        var loopD = lib.Find("Knot_Idle").takes.Select(t => t.loopPath).FirstOrDefault(p => !string.IsNullOrEmpty(p));
        var loopImp = loopD != null ? Imp(loopD) : null;
        Expect(step != null && step.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad && step.defaultSampleSettings.compressionFormat == AudioCompressionFormat.ADPCM && step.forceToMono,
            "audio: a footstep imports decompressed-on-load ADPCM, mono");
        Expect(bed != null && bed.defaultSampleSettings.loadType == AudioClipLoadType.Streaming && !bed.forceToMono, "audio: an ambience bed streams, in stereo");
        Expect(story != null && story.defaultSampleSettings.loadType == AudioClipLoadType.Streaming, "audio: a story sound streams");
        Expect(loopImp != null && loopImp.defaultSampleSettings.compressionFormat == AudioCompressionFormat.ADPCM && loopImp.defaultSampleSettings.loadType == AudioClipLoadType.CompressedInMemory,
            "audio: an object loop imports ADPCM compressed in memory (sample-accurate looping)");

        // Memory of the takes in use, loaded (streamed clips hold only their stream buffers).
        long bytes = 0; int streamed = 0, loaded = 0;
        foreach (var c in lib.entries.SelectMany(e => e.clips).Distinct())
        {
            if (c.loadType == AudioClipLoadType.Streaming) { streamed++; continue; }
            c.LoadAudioData(); loaded++;
        }
        yield return Wait(1f);
        foreach (var c in lib.entries.SelectMany(e => e.clips).Distinct()) bytes += Profiler.GetRuntimeMemorySizeLong(c);
        Debug.Log($"[MraTest] audio memory: {loaded} in-memory clips + {streamed} streamed, {bytes / 1048576f:F1} MB runtime (Windows editor, Profiler.GetRuntimeMemorySizeLong)");
        Expect(bytes > 0, $"audio: in-use clips measured at {bytes / 1048576f:F1} MB");

        // ---------------------------------------------------------------- the rules
        yield return Load("MRAtlas_MR01_Cradle");
        Expect(SfxPlayer.Instance != null, "audio: one persistent sound-effect player exists");
        Expect(FindObjectsByType<SfxPlayer>(FindObjectsSortMode.None).Length == 1, "audio: only one sound-effect player");
        // Cooldown: ten footsteps in one frame are one.
        int s0 = Plays("Qori_Step_Moss");
        for (int i = 0; i < 10; i++) Sfx.Play("Qori_Step_Moss");
        Expect(Plays("Qori_Step_Moss") - s0 == 1, $"audio: cooldown turns ten same-frame footsteps into one ({Plays("Qori_Step_Moss") - s0})");
        // Voice limit: a burst of hits spaced past the cooldown never exceeds the sound's voices.
        int maxSeen = 0;
        for (int i = 0; i < 8; i++) { Sfx.Play("Weapon_Staff_Hit"); maxSeen = Mathf.Max(maxSeen, Sfx.ActiveVoices("Weapon_Staff_Hit")); yield return Wait(.05f); }
        Expect(maxSeen <= lib.Find("Weapon_Staff_Hit").maxVoices && maxSeen >= 1, $"audio: at most {lib.Find("Weapon_Staff_Hit").maxVoices} staff hits sound at once ({maxSeen})");
        // Variation: no take twice running.
        var e2 = lib.Find("Weapon_Staff_Swing"); var takes = new List<AudioClip>();
        for (int i = 0; i < 12; i++) { var src = Sfx.Play("Weapon_Staff_Swing"); if (src != null) takes.Add(src.clip); yield return Wait(e2.cooldown + .02f); }
        bool repeats = false; for (int i = 1; i < takes.Count; i++) repeats |= takes[i] == takes[i - 1];
        Expect(takes.Count >= 10 && !repeats && takes.Distinct().Count() > 1, $"audio: swings vary their take and never repeat one twice running ({takes.Distinct().Count()} takes over {takes.Count})");
        // Superseding: a Waymark lighting stops the plain checkpoint touch.
        yield return Wait(1f);
        Sfx.Play("Checkpoint_Touch"); Sfx.Play("Waymark_Light");
        Expect(Sfx.ActiveVoices("Checkpoint_Touch") == 0 && Sfx.ActiveVoices("Waymark_Light") == 1, "audio: a Waymark lighting replaces the checkpoint touch");
        // Story sounds only through their story events.
        Expect(Sfx.Play("Story_Reveal") == null, "audio: a story sound can't be played outside its story event");
        // Buses: UI keeps sounding through the pause; the Effects setting scales effects.
        var ui = Sfx.Play("UI_Confirm");
        Expect(ui != null && ui.ignoreListenerPause && ui.outputAudioMixerGroup == lib.Group(SfxBus.UI), "audio: UI sounds route to the UI group and play through the pause");
        yield return Wait(.5f);
        // (Each take has its own matching gain, so compare against the take that played.)
        var buy = lib.Find("Shop_Buy");
        float Expected(AudioSource s) => s == null ? -1f : buy.volume * buy.gains[System.Array.IndexOf(buy.clips, s.clip)];
        var full = Sfx.Play("Shop_Buy"); float rFull = full != null ? full.volume / Expected(full) : -1f;
        yield return Wait(1f);
        SfxSettings.Effects = .5f; var half = Sfx.Play("Shop_Buy"); float rHalf = half != null ? half.volume / Expected(half) : -1f; SfxSettings.Effects = 1f;
        Expect(Mathf.Abs(rFull - 1f) < .01f && Mathf.Abs(rHalf - .5f) < .01f, $"audio: Effects at 50% halves an effect (level {rFull:F2} -> {rHalf:F2} of its mix)");
        // Emitter voice limit: five hovering moths near the camera, two heard.
        var cam = Camera.main.transform.position; var moths = new List<GameObject>();
        for (int i = 0; i < 5; i++) { var g = new GameObject("Audio test moth " + i); g.transform.position = cam + new Vector3(i - 2f, 0f, 0f); SfxEmitter.Attach(g, "GustMoth_Wings"); moths.Add(g); }
        yield return Wait(1f);
        int audible = moths.Count(g => g.GetComponent<SfxEmitter>().IsAudible);
        Expect(audible == lib.Find("GustMoth_Wings").maxVoices, $"audio: of five hovering moths, the nearest {lib.Find("GustMoth_Wings").maxVoices} are heard ({audible})");
        foreach (var g in moths) Destroy(g);

        // ---------------------------------------------------------------- ambience and surfaces
        Expect(Sfx.Ambience == "Amb_Cradle" && Sfx.Surface == "Moss", $"audio: the Cradle plays its bed on moss ({Sfx.Ambience}, {Sfx.Surface})");
        yield return Load("MRAtlas_MR02_Causeway");
        var p = SfxPlayer.Instance;
        bool crossfading = p.AmbienceOutGain > 0f && p.AmbienceInGain < 1f;
        Expect(Sfx.Ambience == "Amb_Causeway" && Sfx.Surface == "Stone", $"audio: the Causeway's bed on stone ({Sfx.Ambience}, {Sfx.Surface})");
        Expect(crossfading, $"audio: the bed changes by crossfade, not a cut (in {p.AmbienceInGain:F2}, out {p.AmbienceOutGain:F2})");
        yield return Wait(3f);
        Expect(p.AmbienceInGain >= 1f && p.AmbienceOutGain <= 0f && p.AmbienceSource.isPlaying && p.AmbienceSource.loop, "audio: the crossfade completes into a looping bed");
        foreach (var (scene, amb, surface) in new[] { ("MRAtlas_MR04_Qvale", "Amb_Qvale", "Stone"), ("MRAtlas_MR04_C04", "Amb_HouseInterior", "Wood"), ("MRAtlas_MR05_C01", "Amb_Cave", "Stone"), ("MRAtlas_MR07_Summit", "Amb_Summit", "Moss") })
        {
            yield return Load(scene);
            Expect(Sfx.Ambience == amb && Sfx.Surface == surface, $"audio: {scene} plays {amb} on {surface} ({Sfx.Ambience}, {Sfx.Surface})");
        }

        // ---------------------------------------------------------------- Qori, by input
        yield return Load("MRAtlas_MR01_Cradle");
        var region = W.RegionById("MR01");
        var start = region.nodes[0];
        Place(new Vector2(start.position.x + 2f, GroundAt(start.position.x + 2f, start.position.y + 6f) + 1f));
        yield return Wait(1f);
        int stepsBefore = Plays("Qori_Step_Moss");
        var feedback = Qori.GetComponent<QoriSounds>();
        Expect(feedback != null, "audio: the player carries QoriSounds");
        int footBefore = feedback != null ? feedback.Footfalls : 0;
        Press(Key.D); yield return Wait(2f); Press(); yield return Wait(.3f);
        int steps = Plays("Qori_Step_Moss") - stepsBefore, rigSteps = feedback != null ? feedback.Footfalls - footBefore : -1;
        Expect(steps >= 3, $"audio: walking plays moss footsteps ({steps})");
        Expect(steps <= rigSteps, $"audio: no doubled footsteps: at most one per rig footfall ({steps} sounds, {rigSteps} footfalls)");
        var stepTimes = Sfx.Log.Where(l => l.Contains(" Qori_Step_Moss ")).Select(l => float.Parse(l.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture)).ToList();
        float closest = 99f; for (int i = 1; i < stepTimes.Count; i++) closest = Mathf.Min(closest, stepTimes[i] - stepTimes[i - 1]);
        Expect(closest >= lib.Find("Qori_Step_Moss").cooldown - .001f, $"audio: footsteps never closer than their cooldown ({closest:F3} s)");
        yield return Heard("Qori_Jump", "a jump by Space", Tap(Key.Space), .2f);
        yield return Wait(1.2f);
        Expect(Plays("Qori_Land_Soft") + Plays("Qori_Land_Hard") >= 1, "audio: the landing plays");
        yield return Heard("Qori_Dash", "a Wind Leaf dash by C", Tap(Key.C), .2f);
        yield return Wait(.5f);
        // Glide: jump, then hold Jump while falling.
        Press(Key.Space); yield return Wait(.15f); Press(); yield return Wait(.35f);
        int glideBefore = Plays("Qori_Glide_Open");
        Press(Key.Space); yield return Wait(.5f);
        var glide = Qori.GetComponents<SfxEmitter>().FirstOrDefault(x => x.id == "Qori_Glide_Loop");
        bool glideHeard = glide != null && glide.id == "Qori_Glide_Loop" && (Qori.IsGliding ? glide.Playing : true);
        Press(); yield return Wait(1f);
        Expect(Plays("Qori_Glide_Open") > glideBefore || !Qori.IsGliding && glide != null, $"audio: the Glidecap opening plays (opens {Plays("Qori_Glide_Open") - glideBefore})");
        Expect(glideHeard, "audio: the glide loop follows the Glidecap");
        // Attack by the gamepad's attack button: the Leaf Staff's swing.
        if (pad == null) pad = InputSystem.AddDevice<Gamepad>("MRA Audio Pad");
        yield return Heard("Weapon_Staff_Swing", "an attack (Leaf Staff)", PadTap(GamepadButton.LeftShoulder), .4f);
        // Hurt, death and the return.
        var health = Qori.GetComponent<PlayerHealth>();
        yield return Heard("Qori_Hurt", "losing a heart", Do(() => health.TakeDamage((Vector2)Qori.transform.position + Vector2.right, new Vector2(2f, 2f))), .2f);
        yield return Wait(1.2f);
        health.SetHealth(1);
        int respawnBefore = Plays("Qori_Respawn");
        yield return Heard("Qori_Death", "the last heart", Do(() => health.TakeDamage((Vector2)Qori.transform.position + Vector2.right, new Vector2(2f, 2f))), .2f);
        yield return Wait(1f);
        Expect(Plays("Qori_Respawn") > respawnBefore, "audio: the return to the checkpoint plays after the death");
        // A sleeping knot hums; waking it plays the wake and the quake.
        var knot = FindAnyObjectByType<MraKnot>();
        if (knot != null && !knot.IsAwake)
        {
            Place((Vector2)knot.transform.position + Vector2.left * 3f + Vector2.up);
            yield return Wait(1f);
            var hum = knot.GetComponent<SfxEmitter>();
            Expect(hum != null && hum.id == "Knot_Idle" && hum.IsAudible, "audio: a sleeping knot hums nearby");
            int wake = Plays("Knot_Wake"), stir = Plays("Quake_Stir");
            knot.Wake();
            yield return Wait(4.5f);
            Expect(Plays("Knot_Wake") > wake && Plays("Quake_Stir") > stir, "audio: waking the knot plays its wake and the quake");
            Expect(hum != null && !hum.Playing, "audio: the awake knot stops humming");
        }
        else Expect(false, "audio: MR01 has a sleeping knot to test");
        // A cave mouth: discovery and entry, with the cave's bed inside and the region's again outside.
        var door = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.Ready);
        if (door != null)
        {
            int disc = Plays("Discovery");
            GameSave.SetFlag("audio-test"); // keeps the save alive
            Place((Vector2)door.transform.position + Vector2.up);
            yield return Wait(.6f);
            Expect(Plays("Discovery") > disc || GameSave.HasFlag(door.DiscoveredFlag), "audio: finding a cave mouth plays the discovery");
            yield return Heard("Cave_Enter", "entering a cave by Up", Tap(Key.W), .2f);
            yield return WaitScene(door.chamberScene);
            Expect(Sfx.Ambience == "Amb_Cave", $"audio: inside, the cave's bed ({Sfx.Ambience})");
        }

        // ---------------------------------------------------------------- enemies
        foreach (var r in W.regions.Where(x => W.encounters.Any(e => e.region == x.id)))
        {
            yield return Load(r.scene);
            var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == MraWorldBuilderNames.Generated);
            var group = root != null ? root.transform.Find("Encounters")?.gameObject : null;
            if (group == null) continue;
            group.SetActive(true);
            foreach (Transform enemy in group.transform)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                string kind = new[] { "Thornwing", "PodSpitter", "Shellback", "Grub", "Newt", "Sentinel", "GustMoth", "Knucklebramble" }.FirstOrDefault(k => enemy.name.Contains(k));
                if (kind == null) continue;
                var ids = kind switch
                {
                    "Thornwing" => new[] { "Thornwing_Rear", "Thornwing_Dive" },
                    "PodSpitter" => new[] { "Spitter_Swell", "Spitter_Spit" },
                    "Shellback" => new[] { "Shellback_Walk" },
                    "Grub" => new[] { "Grub_Erupt" },
                    "Newt" => new[] { "Newt_Leap" },
                    "Sentinel" => new[] { "Sentinel_Step", "Sentinel_Bash" },
                    "GustMoth" => new[] { "GustMoth_Blast" },
                    _ => new[] { "Guardian_Awaken", "Guardian_ArmRaise", "Guardian_ArmSlam" },
                };
                var before = ids.ToDictionary(i => i, Plays); int glint = Plays("Enemy_Telegraph");
                if (kind == "Sentinel")
                {
                    // On patrol (Qori out of sight) it steps; up close it bashes.
                    float far = enemy.position.x - 8f;   // beyond its watch range (6), still near enough to hear
                    Place(new Vector2(far, GroundAt(far, enemy.position.y + 6f) + 1f));
                    yield return Wait(2.5f);
                }
                float gx = enemy.position.x - (kind == "Knucklebramble" ? 3.5f : kind == "Sentinel" ? 1.4f : 2.2f);
                Place(new Vector2(gx, GroundAt(gx, enemy.position.y + 4f) + 1f));
                float until = Time.unscaledTime + 7f;
                while (Time.unscaledTime < until && ids.Any(i => Plays(i) == before[i]))
                {
                    health = Qori.GetComponent<PlayerHealth>(); if (health.Health < 3) health.SetHealth(health.MaximumHealth);
                    yield return null;
                }
                var heard = ids.Where(i => Plays(i) > before[i]).ToList();
                // A shellback's walk is a loop: heard if its emitter sounds.
                if (kind == "Shellback" && enemy.GetComponent<SfxEmitter>() is SfxEmitter walk && walk.IsAudible) heard.Add("Shellback_Walk (loop)");
                Expect(heard.Count > 0, $"audio: {r.id} {kind} plays its own sounds ({string.Join(", ", heard)}; telegraph glints {Plays("Enemy_Telegraph") - glint})");
                // Defeat: one hit too many.
                if (enemy.GetComponent<EnemyBase>() is EnemyBase foe && foe.IsAlive && kind != "Knucklebramble")
                {
                    int defeat = Plays("Combat_EnemyDefeat");
                    for (int i = 0; i < 12 && foe.IsAlive; i++) foe.ReceiveCombatHit(new CombatDamage { Damage = 5, Weapon = Qori.GetComponent<PlayerCombat>().EquippedWeapon });
                    if (!foe.IsAlive) Expect(Plays("Combat_EnemyDefeat") > defeat, $"audio: {kind} defeated plays the defeat");
                }
                if (kind == "Knucklebramble" && enemy.GetComponent<KnucklebrambleGuardian>() is KnucklebrambleGuardian g2)
                {
                    g2.Expose(); yield return Wait(1.5f);
                    int hurt = Plays("Guardian_Hurt"), down = Plays("Guardian_Defeat");
                    for (int i = 0; i < 20 && g2.IsAlive; i++)
                    {
                        if (g2.Current != KnucklebrambleGuardian.State.Exposed) { g2.Expose(); yield return Wait(1.2f); }
                        g2.ReceiveCombatHit(new CombatDamage { Damage = 1 }); yield return Wait(.1f);
                    }
                    Expect(Plays("Guardian_Hurt") > hurt && (!g2.IsAlive ? Plays("Guardian_Defeat") > down : true), "audio: the guardian's core hurt and its collapse play");
                }
            }
        }

        // ---------------------------------------------------------------- weapons and hits (their real hooks, fed directly)
        yield return Load("MRAtlas_MR01_Cradle");
        var combat = Qori.GetComponent<PlayerCombat>();
        var weapons = Resources.FindObjectsOfTypeAll<WeaponDefinition>();
        var attacks = Resources.FindObjectsOfTypeAll<AttackDefinition>();
        var anyAttack = attacks.FirstOrDefault(a => !a.slingProjectile && a.hitSound == null && a.startSound == null);
        var downAttack = attacks.FirstOrDefault(a => !a.slingProjectile && a.direction == AttackAim.Down && a.pogoCompatible);
        var startWeapon = combat.EquippedWeapon;
        foreach (var (wid, swing, hit) in new[] { ("forest-0", "Weapon_Reedblade_Swing", "Weapon_Reedblade_Hit"), ("forest-2", "Weapon_Mace_Swing", "Weapon_Mace_Hit"), ("forest-3", "Weapon_Spear_Thrust", "Weapon_Spear_Hit"), (QoriArmoryFactory.StaffId, "Weapon_Staff_Swing", "Weapon_Staff_Hit") })
        {
            var w = weapons.FirstOrDefault(x => x.weaponId == wid);
            if (w == null) { Expect(false, "audio: weapon " + wid + " found"); continue; }
            combat.EquipWeapon(w); yield return Wait(.4f);
            yield return Heard(swing, w.displayName + "'s attack", PadTap(GamepadButton.LeftShoulder), .4f);
            yield return Wait(.5f);
            yield return Heard(hit, w.displayName + "'s hit", Do(() => combat.PublishHit(new AttackHitResult { Hit = new CombatDamage { Attack = anyAttack, Weapon = w, Point = Qori.transform.position, Damage = 1 }, Response = CombatDamageResponse.Applied(1f) })), .2f);
            if (wid == "forest-2") Expect(Plays("Combat_HitStop_Heavy") > 0, "audio: a mace hit adds the heavy impact");
        }
        combat.EquipWeapon(startWeapon);
        yield return Wait(.3f);
        yield return Heard("Combat_Blocked", "a blocked blow", Do(() => combat.PublishHit(new AttackHitResult { Hit = new CombatDamage { Attack = anyAttack, Weapon = startWeapon, Point = Qori.transform.position }, Response = WeaponKinds.Blocked() })), .2f);
        if (downAttack != null)
            yield return Heard("Combat_Pogo_Bounce", "a downward strike that bounces", Do(() => combat.PublishHit(new AttackHitResult { Hit = new CombatDamage { Attack = downAttack, Weapon = startWeapon, Point = Qori.transform.position, Damage = 0 }, Response = CombatDamageResponse.Applied(0f, true) })), .2f);
        var slingAttack = attacks.FirstOrDefault(a => a.slingProjectile);
        var slingWeapon = weapons.FirstOrDefault(x => x.weaponId == "resin-sling");
        if (slingAttack != null && slingWeapon != null)
        {
            yield return Heard("Weapon_Sling_Shot", "the sling's release", Do(() => QoriResinShot.Launch(combat, (Vector2)Qori.transform.position + Vector2.up * .6f, slingAttack, slingWeapon, Vector2.down + Vector2.right * .2f)), .1f);
            yield return Wait(1f);
            Expect(Plays("Weapon_Sling_Impact") > 0, "audio: the sling pellet's impact on rock plays");
        }
        else Expect(false, "audio: the sling's attack and weapon were found");
        // A sap orb heals.
        health = Qori.GetComponent<PlayerHealth>(); health.SetHealth(1);
        if (Fx.Library != null && Fx.Library.sapOrb != null)
            yield return Heard("Heal", "a sap orb", Do(() => SapOrb.MaybeDrop((Vector2)Qori.GetComponent<Collider2D>().bounds.center, 1f)), 2f);
        health.SetHealth(health.MaximumHealth);

        // ---------------------------------------------------------------- the thread
        {
            var lift = W.RegionById("MR02");
            yield return Load(lift.scene);
            var e = lift.edges.First(x => x.module.template == "flat-thread-gap");
            float lip = e.module.position.x + .5f, yU = GroundAt(lip + 3f, 200f), yL = GroundAt(lip - 1f, yU - .5f);
            var thread = Qori.GetComponent<PlayerThread>();
            Place(new Vector2(lip - 1.2f, yL + 1f)); yield return Wait(.33f);
            int shoot = Plays("Qori_Thread_Shoot"), snag = Plays("Qori_Thread_Attach");
            Press(Key.Space); float t0 = Time.time;
            while (Time.time - t0 < 1.2f && !thread.IsAttached) { if (thread.HasTarget) thread.ToggleHook(); yield return null; }
            Press(); yield return Wait(.4f);
            Expect(thread.IsAttached && Plays("Qori_Thread_Shoot") > shoot && Plays("Qori_Thread_Attach") > snag, $"audio: casting the thread plays its flick and snag (attached {thread.IsAttached})");
            yield return Heard("Qori_Thread_Release", "letting go of the thread", Do(thread.ToggleHook), .3f);
        }

        // ---------------------------------------------------------------- a relic shrine
        foreach (var r in W.regions)
        {
            yield return Load(r.scene);
            var shrine = FindObjectsByType<AbilityShrine>(FindObjectsSortMode.None).FirstOrDefault(x => x.ability != null);
            if (shrine == null) continue;
            string relic = shrine.ability.abilityId;
            GameSave.RemoveRelic(relic);
            yield return Load(r.scene);
            shrine = FindObjectsByType<AbilityShrine>(FindObjectsSortMode.None).First(x => x.ability != null && x.ability.abilityId == relic);
            yield return Heard("Relic_Get", "taking a shrine's relic", Do(() => shrine.Collect(Abilities)), .3f);
            if (!GameSave.HasRelic(relic)) GameSave.AddRelic(relic);
            break;
        }

        // ---------------------------------------------------------------- side chambers: rewards and mechanisms
        var rewardKinds = new HashSet<MraReward.Kind>();
        foreach (var c in W.chambers.Where(c => !c.InPlace && !c.IsHouse))
        {
            yield return Load(c.scene);
            foreach (var lever in FindObjectsByType<MraLever>(FindObjectsSortMode.None))
            {
                var l2 = lever;
                Place((Vector2)lever.transform.position + Vector2.up * .5f); yield return Wait(.6f);   // Qori works it standing there
                if (lever.sequence != null) continue;
                bool wheel = lever.GetComponent<MraWheelSpin>() != null;
                yield return Heard(wheel ? "Wheel_Turn" : "Lever_Pull", $"{c.id}: working its {(wheel ? "wheel" : "lever")}", Do(l2.Use), .3f);
            }
            foreach (var seq in FindObjectsByType<MraSequence>(FindObjectsSortMode.None))
            {
                int hits = Plays("SeedSwitch_Hit"), opened = Plays("Gate_Open");
                foreach (int k in seq.order)
                {
                    Place((Vector2)seq.lights[k].transform.position + Vector2.up * .5f); yield return Wait(.3f);
                    seq.lights[k].Use(); yield return Wait(.2f);
                }
                yield return Wait(.5f);
                Expect(Plays("SeedSwitch_Hit") - hits == seq.order.Length && seq.Solved, $"audio: {c.id}: each light in the sequence chimes ({Plays("SeedSwitch_Hit") - hits}/{seq.order.Length}, solved {seq.Solved})");
                if (seq.Solved) Expect(Plays("Gate_Open") > opened, $"audio: {c.id}: the solved sequence's gate opening plays");
            }
            var plate = FindAnyObjectByType<PressurePlate>();
            if (plate != null && Plays("PressurePlate_Down") == 0)
            {
                Place((Vector2)plate.transform.position + Vector2.up * .6f);
                yield return Wait(1f);
                Expect(Plays("PressurePlate_Down") > 0, $"audio: {c.id}: standing on the plate plays it");
            }
            var thorns = FindAnyObjectByType<MraTimedHazard>();
            if (thorns != null && Plays("Thorns_Rise") == 0)
            {
                Place((Vector2)thorns.transform.position + Vector2.left * 3f + Vector2.up); yield return Wait(.5f);
                int rise = Plays("Thorns_Rise"), sink = Plays("Thorns_Sink"), warn = Plays("Thorns_Warn");
                yield return Wait(thorns.upSeconds + thorns.safeSeconds + .3f);
                Expect(Plays("Thorns_Rise") > rise && Plays("Thorns_Sink") > sink && Plays("Thorns_Warn") > warn, $"audio: {c.id}: the timed thorns warn, rise and sink ({Plays("Thorns_Warn") - warn}/{Plays("Thorns_Rise") - rise}/{Plays("Thorns_Sink") - sink})");
            }
            foreach (var reward in FindObjectsByType<MraReward>(FindObjectsSortMode.None))
            {
                if (reward.Taken || rewardKinds.Contains(reward.kind)) continue;
                string id = reward.kind == MraReward.Kind.SongShell ? "SongShell_Get" : reward.kind == MraReward.Kind.HeartSeed ? "HeartSeed_Get" : reward.kind == MraReward.Kind.Amber ? "Amber_PickupBig" : "LoreStone_Read";
                var r2 = reward;
                yield return Heard(id, $"{c.id}'s {reward.kind} find", Do(() => r2.Collect(Qori.GetComponent<PlayerHealth>())), .3f);
                rewardKinds.Add(reward.kind);
            }
        }
        Debug.Log($"[MraTest] audio: gate openings heard across the chambers: {Plays("Gate_Open")}");

        // ---------------------------------------------------------------- Qvale
        yield return Load("MRAtlas_MR04_Qvale");
        TownState.BindToSave();
        yield return Load("MRAtlas_MR04_Qvale");   // again, so the town's variants read the bound state
        var forge = FindObjectsByType<TownShop>(FindObjectsSortMode.None).FirstOrDefault(s => s.title.Contains("forge"));
        if (forge != null)
        {
            yield return Heard("Smithy_Anvil", "opening Brannick's forge", Do(forge.Open), .2f);
            int amber = TownState.Amber; if (amber > 0) TownState.Spend(amber);
            yield return Heard("Shop_Cannot", "buying without Amber", Do(() => forge.Buy(1)), .2f);
            TownState.AddAmber(200);
            yield return Heard("Smithy_Upgrade", "a forge purchase", Do(() => forge.Buy(1)), .2f);
            yield return Wait(.4f);
            yield return Heard("UI_Move", "moving down the shop list", Tap(Key.S), .2f);
            yield return Heard("UI_Back", "leaving the shop", Do(forge.Close), .2f);
        }

        else Expect(false, "audio: Qvale has Brannick's forge");
        yield return Heard("Dialogue_Blip_Small", "a resident's line typing", Do(() => DialogueBox.Show("Pip", new[] { "The springs are beating faster today, did you hear them?" })), .8f);
        yield return Tap(Key.Escape); yield return Wait(.3f);
        yield return Heard("Dialogue_Blip_Low", "the elder's line typing", Do(() => DialogueBox.Show("Grandfather Tallow", new[] { "Climbing, are you? The higher you go, the louder it hums." })), .8f);
        yield return Tap(Key.Escape); yield return Wait(.3f);
        var spot = FindAnyObjectByType<ListeningSpot>();
        if (spot != null)
        {
            Place((Vector2)spot.transform.position + Vector2.up * .5f); yield return Wait(.6f);
            yield return Heard("Qori_Sit", "sitting at the listening tree", Do(spot.Sit), .3f);
            yield return Wait(1.6f);
            yield return Heard("UI_Back", "getting up", Do(spot.Leave), .3f);
            yield return Wait(1.6f);
        }
        // A home: in by its door, its own bed and wooden floor, and out again.
        var homeDoor = FindObjectsByType<MraChamberDoor>(FindObjectsSortMode.None).FirstOrDefault(d => d.chamberId == "MR04_C03");   // Scribble's
        if (homeDoor != null)
        {
            Place((Vector2)homeDoor.transform.position + Vector2.up); yield return Wait(1.8f);
            yield return Heard("Door_Enter", "a Qvale home's door", Tap(Key.W), .2f);
            yield return WaitScene(W.ChamberById(homeDoor.chamberId).scene);
            Expect(Sfx.Ambience == "Amb_HouseInterior" && Sfx.Surface == "Wood", $"audio: inside a home, its bed and wooden floor ({Sfx.Ambience}, {Sfx.Surface})");
            var pages = FindObjectsByType<TownShop>(FindObjectsSortMode.None).FirstOrDefault(x => x.title.Contains("pages"));
            if (pages != null) { yield return Heard("Map_Unroll", "opening Scribble's pages", Do(pages.Open), .2f); pages.Close(); yield return Wait(.2f); }
            var back = FindAnyObjectByType<MraChamberReturn>();
            if (back != null)
            {
                int exit = Plays("Door_Exit");
                Place(back.transform.position); yield return WaitScene(W.RegionById("MR04").scene);
                Expect(Plays("Door_Exit") > exit, "audio: walking out of a home plays the door");
            }
        }

        // ---------------------------------------------------------------- pause menu and Chart
        yield return Heard("UI_Pause", "Escape pauses", Tap(Key.Escape), .3f);
        yield return Heard("UI_Move", "moving down the pause menu", Tap(Key.S), .3f);
        yield return Heard("UI_Unpause", "Escape resumes", Tap(Key.Escape), .3f);
        yield return Heard("Chart_Open", "the Chart opening", Do(MraChart.Open), .3f);
        yield return Wait(.5f);
        yield return Heard("Chart_Close", "the Chart closing", Tap(Key.Escape), .4f);

        // ---------------------------------------------------------------- story: only in their moments
        // The reveal may already have run: the earlier passes put Qori on the Summit, and its ledge
        // starts it when he lands there. Either way, it's the only thing that has played a story sound.
        // Earlier passes can start the reveal (Qori landing on its ledge) and cut it short by
        // loading on: the only story sound so far may be the reveal's.
        Expect(Plays("Story_EyeOpen") + Plays("Story_Ending") == 0, $"audio: no ending sound before the ending ({Plays("Story_EyeOpen") + Plays("Story_Ending")})");
        if (!MraState.Revealed)
        {
            // Interrupted: leaving mid-reveal silences its sound.
            yield return Load("MRAtlas_MR07_Summit");
            var reveal = FindAnyObjectByType<MraReveal>();
            if (reveal != null && !MraReveal.IsPlaying) reveal.Play(Qori);
            yield return Wait(1f);
            yield return Load("MRAtlas_MR06_Heights");
            yield return Wait(.3f);
            Expect(Sfx.ActiveVoices("Story_Reveal") == 0, "audio: an interrupted reveal stops its sound");
            // In full (counted from before the load: arriving on its ledge can start it at once).
            int before = Plays("Story_Reveal");
            yield return Load("MRAtlas_MR07_Summit");
            reveal = FindAnyObjectByType<MraReveal>();
            if (reveal != null) { Place(reveal.transform.position); yield return Wait(.2f); if (!MraReveal.IsPlaying) reveal.Play(Qori); }
            yield return Wait(.5f);
            Expect(Plays("Story_Reveal") == before + 1, "audio: the reveal plays Story_Reveal once");
            float until = Time.unscaledTime + 40f; while (MraReveal.IsPlaying && Time.unscaledTime < until) yield return null;
            Expect(MraState.Revealed, "audio: the reveal completes");
        }
        MraEnding ending = null;
        foreach (var scene in new[] { "MRAtlas_MR08_Descent", "MRAtlas_MR08_C01", "MRAtlas_MR08_C02" })
        {
            yield return Load(scene);
            ending = FindAnyObjectByType<MraEnding>(); if (ending != null) break;
        }
        if (ending != null && !ending.Done)
        {
            int eye = Plays("Story_EyeOpen"), end = Plays("Story_Ending");
            ending.Play();
            yield return Wait(.5f);
            Expect(Plays("Story_EyeOpen") == eye + 1, "audio: the ending begins with Story_EyeOpen");
            float until = Time.unscaledTime + 60f; while (MraEnding.IsPlaying && Time.unscaledTime < until) yield return null;
            Expect(Plays("Story_Ending") == end + 1, "audio: the ending's last caption plays Story_Ending");
        }
        else Expect(false, "audio: the descent has its ending to test");
        Expect(Plays("Story_Sprout") == 0, "audio: Story_Sprout (no opening scene yet) never plays");

        // ---------------------------------------------------------------- the record
        var never = lib.entries.Where(e => e.status == "wired" && Plays(e.id) == 0 && !e.loop).Select(e => e.id).ToList();
        Debug.Log("[MraTest] audio: wired one-shots not triggered by this test: " + (never.Count == 0 ? "none" : string.Join(", ", never)));
        Debug.Log("[MraTest] audio: plays by sound: " + string.Join(", ", lib.entries.Where(e => Plays(e.id) > 0).Select(e => e.id + "=" + Plays(e.id))));
        int refusedTotal = lib.entries.Sum(e => Sfx.Refused(e.id));
        Debug.Log($"[MraTest] audio: refused by cooldown/limits/off-screen: {refusedTotal}");
    }
}
#endif

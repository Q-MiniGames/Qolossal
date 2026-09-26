using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch check of the A0 test room's mechanics without play mode: weapon rules for the
// breakables, the sling switch, the pressure plate, the moving raft carrying Qori, and the
// ability shrines and the relic locks.
// Usage: -executeMethod MechanicsCheck.Run
public static class MechanicsCheck
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static int failures;

    static void Expect(bool ok, string what)
    {
        if (!ok) failures++;
        Debug.Log($"[MechanicsCheck] {(ok ? "PASS" : "FAIL")} {what}");
    }

    static CombatDamage Hit(string weaponId, AttackAim aim = AttackAim.Front, bool sling = false)
    {
        var weapon = ScriptableObject.CreateInstance<WeaponDefinition>(); weapon.weaponId = weaponId;
        var attack = ScriptableObject.CreateInstance<AttackDefinition>(); attack.direction = aim; attack.slingProjectile = sling;
        return new CombatDamage { Weapon = weapon, Attack = attack, Damage = 1f, Direction = Vector2.right };
    }

    static T Find<T>(string name) where T : Component =>
        Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.name == name);

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        foreach (var b in Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) b.Rebuild();
        foreach (var p in Object.FindObjectsByType<TerrainPiece>(FindObjectsSortMode.None)) p.Rebuild();
        failures = 0;

        var rubble = Find<Breakable>("Rubble Barrier (mace)");
        Expect(rubble.ReceiveCombatHit(Hit("forest-0")).Disposition == CombatHitDisposition.Blocked && !rubble.IsBroken, "sword bounces off the rubble wall");
        Expect(rubble.ReceiveCombatHit(Hit("forest-2")).Disposition == CombatHitDisposition.Damaged && rubble.IsBroken && !rubble.solid.enabled, "mace breaks the rubble wall");

        var curtain = Find<Breakable>("Thorn Curtain (sword)");
        Expect(curtain.ReceiveCombatHit(Hit("forest-2")).Disposition == CombatHitDisposition.Blocked, "mace bounces off the thorn curtain");
        Expect(curtain.ReceiveCombatHit(Hit("forest-0")).Disposition == CombatHitDisposition.Damaged && curtain.IsBroken
               && curtain.intact.sprite == curtain.brokenSprite && curtain.intact.enabled, "sword cuts the curtain, leaving the wilted one");

        var floor = Find<Breakable>("Weak Floor (down attack)");
        Expect(floor.ReceiveCombatHit(Hit("forest-2")).Disposition == CombatHitDisposition.Blocked, "a sideways mace hit doesn't break the weak floor");
        var down = floor.ReceiveCombatHit(Hit("forest-0", AttackAim.Down));
        Expect(down.Disposition == CombatHitDisposition.Damaged && down.SupportsPogo && floor.IsBroken, "a downward hit breaks the weak floor and allows a pogo bounce");

        var seed = Find<SeedSwitch>("Seed Switch (sling)");
        var slingGate = Find<RootGate>("Root Gate (sling)");
        Expect(seed.ReceiveCombatHit(Hit("forest-0")).Disposition == CombatHitDisposition.Blocked && !slingGate.IsOpen, "the sword doesn't trigger the seed switch");
        Expect(seed.ReceiveCombatHit(Hit("resin-sling", sling: true)).Disposition == CombatHitDisposition.Damaged && slingGate.IsOpen
               && seed.image.sprite == seed.on, "a sling shot opens the seed switch and its gate");

        // Physics: plate and raft, stepping FixedUpdate by hand.
        Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            var body = player.GetComponent<Rigidbody2D>();
            typeof(PlayerMovement).GetMethod("Awake", Any).Invoke(player, null);
            var fixedUpdate = typeof(PlayerMovement).GetMethod("FixedUpdate", Any);
            float extent = player.GetComponent<BoxCollider2D>().size.y * player.transform.lossyScale.y * .5f;

            var plate = Find<PressurePlate>("Pressure Plate");
            var plateGate = Find<RootGate>("Root Gate (plate)");
            var plateStep = typeof(PressurePlate).GetMethod("FixedUpdate", Any);
            body.position = new Vector2(114f, 9f + extent + .05f); body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            for (int i = 0; i < 20; i++) { fixedUpdate.Invoke(player, null); Physics2D.Simulate(Time.fixedDeltaTime); plateStep.Invoke(plate, null); }
            Expect(plate.IsPressed && plateGate.IsOpen, "standing on the plate opens its gate");
            body.position = new Vector2(110f, 12f); Physics2D.SyncTransforms();
            plateStep.Invoke(plate, null);
            Expect(!plate.IsPressed && !plateGate.IsOpen, "stepping off the plate closes its gate");

            var raft = Find<MovingPlatform>("Moving Raft");
            var raftBody = raft.GetComponent<Rigidbody2D>();
            typeof(MovingPlatform).GetMethod("Awake", Any).Invoke(raft, null);
            var raftStep = typeof(MovingPlatform).GetMethod("FixedUpdate", Any);
            body.position = new Vector2(136.5f, 9f + extent + .05f); body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
            for (int i = 0; i < 10; i++) { fixedUpdate.Invoke(player, null); Physics2D.Simulate(Time.fixedDeltaTime); }
            float playerStart = body.position.x, raftStart = raftBody.position.x;
            float maxOwnSpeed = 0f;
            for (int i = 0; i < 100; i++)
            {
                raftStep.Invoke(raft, null); fixedUpdate.Invoke(player, null); Physics2D.Simulate(Time.fixedDeltaTime);
                if (i > 5) maxOwnSpeed = Mathf.Max(maxOwnSpeed, Mathf.Abs(player.ObservedVelocity.x));
            }
            Expect(maxOwnSpeed < .3f, $"riding the raft doesn't count as walking (Qori's own speed at most {maxOwnSpeed:F2} u/s)");
            float raftMoved = raftBody.position.x - raftStart, playerMoved = body.position.x - playerStart;
            Expect(raftMoved > 1f && Mathf.Abs(playerMoved - raftMoved) < .15f && player.IsGrounded,
                   $"the raft carries Qori (raft moved {raftMoved:F2}, Qori {playerMoved:F2}, grounded {player.IsGrounded})");

            // Relics: Qori starts without them here; the Climbing Moss shrine unlocks the wall cling.
            var abilities = player.GetComponent<PlayerAbilityController>();
            if (abilities == null) abilities = player.gameObject.AddComponent<PlayerAbilityController>();
            var shrines = Object.FindObjectsByType<AbilityShrine>(FindObjectsSortMode.None);
            Expect(shrines.Length == 3 && shrines.All(s => s.ability != null && s.ability.relic != null && s.ability.icon != null && s.relic.sprite == s.ability.relic),
                   "three ability shrines, each with its relic art and HUD icon");
            Expect(new[] { Relics.LivingThread, Relics.ClimbingMoss, Relics.Bloomfall }.All(id => !abilities.HasAbility(id)), "Qori starts the test room with no relics");
            Expect(!player.GetComponent<PlayerThread>().Unlocked, "the thread is locked before the Living Thread relic");
            float halfWidth = player.GetComponent<BoxCollider2D>().size.x * Mathf.Abs(player.transform.lossyScale.x) * .5f;
            bool SlidesOnColumn()
            {
                body.position = new Vector2(54f - halfWidth - .01f, 1.5f); body.linearVelocity = new Vector2(0f, -1f); Physics2D.SyncTransforms();
                bool slid = false;
                for (int i = 0; i < 12; i++) { fixedUpdate.Invoke(player, null); Physics2D.Simulate(Time.fixedDeltaTime); slid |= player.IsWallSliding; }
                return slid;
            }
            Expect(!SlidesOnColumn(), "without Climbing Moss Qori doesn't slide on the climbable column");
            var moss = shrines.First(s => s.ability.abilityId == Relics.ClimbingMoss);
            moss.Collect(abilities);
            Expect(moss.IsTaken && abilities.HasAbility(Relics.ClimbingMoss) && !abilities.HasAbility(Relics.LivingThread), "the moss shrine unlocks Climbing Moss only");
            Expect(SlidesOnColumn(), "with Climbing Moss Qori slides on the climbable column");
            Expect(shrines.First(s => s.ability.abilityId == Relics.Bloomfall).ability is PogoAbilityDefinition, "Bloomfall is the pogo bounce");
        }
        finally { Physics2D.simulationMode = SimulationMode2D.FixedUpdate; }

        Debug.Log($"[MechanicsCheck] finished with {failures} failure(s)");
    }
}

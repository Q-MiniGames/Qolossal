using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Real Play-mode check of A2 (batch): the Bark Sentinel's shield rule (the sword bounces off the
// front, the spear pierces it, a blow from behind or above lands), its bash hurting Qori, the log
// carrying him over the resin pit, the heart seed behind the false wall, and the fireflies. The
// player's own save file is set aside and restored.
// Usage (no -quit): -executeMethod A2PlayTest.Run
[InitializeOnLoad]
public static class A2PlayTest
{
    const string Key = "Qolossal.A2PlayTest";
    static string Backup => GameSave.FilePath + ".a2test";
    static int stage, failures, errors, hearts; static float stageAt, logStartX;

    static A2PlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[A2Test] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A2_Grove.unity");
        if (File.Exists(GameSave.FilePath)) File.Copy(GameSave.FilePath, Backup, true);
        GameSave.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[A2Test] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => Object.FindFirstObjectByType<PlayerMovement>();
    static void Place(Vector2 at) { var body = Qori.GetComponent<Rigidbody2D>(); body.position = at; body.linearVelocity = Vector2.zero; }
    static float GroundAt(float x, float fromY = 40f) => Physics2D.Raycast(new Vector2(x, fromY), Vector2.down, 80f, LayerMask.GetMask("Ground")).point.y;
    static SentinelEnemy Sentinel => Object.FindObjectsByType<SentinelEnemy>(FindObjectsSortMode.None).OrderBy(s => s.transform.position.x).First();

    // A blow from Qori with `weaponId`, travelling along `direction`.
    static CombatDamage Hit(string weaponId, Vector2 direction, AttackAim aim = AttackAim.Front)
    {
        var weapon = ScriptableObject.CreateInstance<WeaponDefinition>(); weapon.weaponId = weaponId;
        var attack = ScriptableObject.CreateInstance<AttackDefinition>(); attack.direction = aim;
        return new CombatDamage { Weapon = weapon, Attack = attack, Damage = 1f, Direction = direction };
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;
        switch (stage)
        {
            case 0 when t > 1f:
            {
                Expect(GameArea.InScene != null && GameArea.InScene.displayName == "A2 Ancient Grove", "A2 is a game area named Ancient Grove");
                Expect(Object.FindObjectsByType<AmbientMotes>(FindObjectsSortMode.None).Length == 3, "fireflies drift in the grove floor, resin pit and arena");
                // Stand in front of (east of) the first Sentinel so it turns its shield toward Qori.
                Place(new Vector2(Sentinel.transform.position.x + 3.5f, GroundAt(Sentinel.transform.position.x + 3.5f) + 1f));
                stage = 1; stageAt = t;
                break;
            }
            case 1 when t - stageAt > 1.2f:
            {
                var s = Sentinel; float f = s.Facing;
                Expect(f > 0f, "the Sentinel turns its shield toward Qori");
                Vector2 intoShield = new Vector2(-f, 0f), intoBack = new Vector2(f, 0f);
                Expect(s.ReceiveCombatHit(Hit("forest-0", intoShield)).Disposition == CombatHitDisposition.Blocked, "the sword bounces off its shield");
                Expect(s.ReceiveCombatHit(Hit("forest-2", intoShield)).Disposition == CombatHitDisposition.Blocked, "so does the mace");
                Expect(s.ReceiveCombatHit(Hit("forest-3", intoShield)).Disposition == CombatHitDisposition.Damaged, "the spear pierces the shield");
                Expect(s.ReceiveCombatHit(Hit("forest-0", intoBack)).Disposition == CombatHitDisposition.Damaged, "a blow from behind lands");
                Expect(s.ReceiveCombatHit(Hit("forest-0", Vector2.down, AttackAim.Down)).Disposition == CombatHitDisposition.Damaged, "a strike from above lands");
                // Now walk right up to it and let it bash.
                hearts = Qori.GetComponent<PlayerHealth>().Health;
                Place(new Vector2(s.transform.position.x + 1.4f, GroundAt(s.transform.position.x + 1.4f) + 1f));
                stage = 2; stageAt = t;
                break;
            }
            case 2 when t - stageAt > 2.5f:
            {
                Expect(Qori.GetComponent<PlayerHealth>().Health < hearts, $"its shield bash costs Qori a heart ({hearts} -> {Qori.GetComponent<PlayerHealth>().Health})");
                // Stand on the floating log (the Thornwing over the pit is sent away, so it can't knock him off).
                foreach (var tw in Object.FindObjectsByType<ThornwingEnemy>(FindObjectsSortMode.None)) tw.gameObject.SetActive(false);
                var log = Object.FindFirstObjectByType<MovingPlatform>();
                logStartX = log.transform.position.x;
                Place(new Vector2(logStartX, log.transform.position.y + 1.2f));
                stage = 3; stageAt = t;
                break;
            }
            case 3 when t - stageAt > 2.5f:
            {
                var log = Object.FindFirstObjectByType<MovingPlatform>();
                float moved = log.transform.position.x - logStartX, qori = Qori.transform.position.x - logStartX;
                Expect(Mathf.Abs(moved) > 2f && Mathf.Abs(qori - moved) < .6f && Qori.IsGrounded, $"the log carries Qori over the resin pit (log {moved:F1}, Qori {qori:F1})");
                var seed = Object.FindFirstObjectByType<HeartSeed>();
                hearts = Qori.GetComponent<PlayerHealth>().MaximumHealth;
                Place(new Vector2(153.5f, GroundAt(153.5f, 24f) + 1f));
                stage = 4; stageAt = t;
                break;
            }
            case 4 when t - stageAt > 1.2f:
            {
                var wall = Object.FindFirstObjectByType<SecretWall>();
                Expect(wall.Revealed && wall.overlay.color.a < .5f, $"the canopy's false wall fades while Qori is inside it (alpha {wall.overlay.color.a:F2})");
                Place(Object.FindFirstObjectByType<HeartSeed>().transform.position + Vector3.up * .3f);
                stage = 5; stageAt = t;
                break;
            }
            case 5 when t - stageAt > .8f:
            {
                var health = Qori.GetComponent<PlayerHealth>();
                Expect(health.MaximumHealth == hearts + 1 && GameSave.HasPickup("a2-heartseed"), $"the A2 heart seed adds a heart ({hearts} -> {health.MaximumHealth})");
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[A2Test] finished with {failures} failure(s)");
                GameSave.Clear();
                if (File.Exists(Backup)) { File.Copy(Backup, GameSave.FilePath, true); File.Delete(Backup); }
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                break;
            }
        }
    }
}

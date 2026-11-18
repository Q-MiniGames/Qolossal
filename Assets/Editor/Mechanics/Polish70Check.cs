using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;

// Polish gate 70% checks, in the real game (3 Nov). Logic only: feel is the user's to judge.
// Check: -executeMethod Polish70Check.Run -saveFile polish70.json -captureDir <dir> (no -quit)
//   no weapon menu inputs, swings hit along the drawn staff, the hurt pose and flash, the death beat.
// Memory: -executeMethod Polish70Check.FrameMemory -captureDir <dir> -quit
//   what Qori's and the creatures' frames really cost as imported, per set and per texture format.
public static class Polish70Check
{
    const string StateKey = "Qolossal.Polish70Check";
    public static string Dir => SessionState.GetString(StateKey, "");

    static string CaptureDir()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/Polish70";
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void Run()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-saveFile") < 0) { Debug.LogError("Polish70Check needs -saveFile"); EditorApplication.Exit(1); return; }
        SessionState.SetString(StateKey, CaptureDir());
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + "MRAtlas_MR01_Cradle.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Resume()
    {
        if (string.IsNullOrEmpty(Dir)) return;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(Dir))
                new GameObject("Polish70 driver").AddComponent<Driver>();
        };
    }

    public sealed class Dummy : MonoBehaviour, ICombatDamageReceiver
    {
        public int hits;
        public CombatDamageResponse ReceiveCombatHit(CombatDamage hit) { hits++; return CombatDamageResponse.Applied(hit.Damage); }
    }

    sealed class Driver : MonoBehaviour
    {
        int failures, errors;
        readonly List<string> report = new List<string>();
        void Say(string s) { report.Add(s); Debug.Log("[P70] " + s); }
        void Expect(bool ok, string what) { if (!ok) failures++; Say((ok ? "PASS " : "FAIL ") + what); }

        IEnumerator Start()
        {
            Application.logMessageReceived += (m, st, t) => { if (t == LogType.Exception || t == LogType.Error) { errors++; Debug.Log("[P70] runtime error: " + m); } };
            float until = Time.realtimeSinceStartup + 20f;
            QoriWalkFramesOnPlayer frames = null;
            while ((frames = QoriWalkFramesOnPlayer.Active) == null && Time.realtimeSinceStartup < until) yield return null;
            var qori = FindAnyObjectByType<PlayerMovement>();
            Expect(frames != null && qori != null, "Qori wears his frames");
            if (frames == null || qori == null) { Finish(); yield break; }
            yield return new WaitForSeconds(.6f);
            var armory = qori.GetComponent<QoriArmory>(); var combat = qori.GetComponent<PlayerCombat>(); var health = qori.GetComponent<PlayerHealth>();
            Expect(combat.EquippedWeapon.weaponId == QoriArmoryFactory.StaffId, "Qori holds the Leaf Staff");

            // The menu's old inputs.
            var keyboard = InputSystem.AddDevice<Keyboard>("P70 keyboard"); var pad = InputSystem.AddDevice<Gamepad>("P70 pad");
            int selected = armory.Selected;
            foreach (var key in new[] { Key.Tab, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 })
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
                Expect(armory.Selected == selected && !QoriArmory.BlocksAttackInput, key + " opens and selects nothing");
            }
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.RightStick)); yield return null; yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState()); yield return null; yield return null;
            Expect(armory.Selected == selected && !QoriArmory.BlocksAttackInput, "right-stick click opens and selects nothing");
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(pad);

            // Each marked swing: how far the hit line is from the old rig's, and does a target at the drawn tip get hit?
            CombatDebugOverlay.Showing = true;
            var front = combat.EquippedWeapon.moveSet.Find(CombatMoveSlot.Front);
            var moves = new List<AttackDefinition> { front };
            if (front.followUps != null && front.followUps.Length > 0) moves.Add(front.followUps[0]);
            moves.Add(combat.EquippedWeapon.moveSet.Find(CombatMoveSlot.Upper));
            foreach (var move in moves)
            {
                yield return new WaitForSeconds(1f);
                Expect(frames.DrawsStaff(move), move.attackId + ": its staff is marked");
                float dir = qori.FacingDirection;
                var aim = move.direction;
                if (!combat.RequestAttack(new AttackRequest(aim, dir, CombatMoveSlot.Front), move)) { Expect(false, move.attackId + " starts"); continue; }
                float worst = 0f, sum = 0f; int n = 0; bool shot = false; Dummy dummy = null; Vector2 furthest = qori.transform.position;
                QoriAnimator rig = qori.GetComponentInChildren<QoriAnimator>();
                float started = Time.realtimeSinceStartup + .5f;   // a request starts on the combat script's next update
                while (combat.CurrentAttack != move && Time.realtimeSinceStartup < started) yield return null;
                while (combat.CurrentAttack == move)
                {
                    if (combat.Phase == AttackPhase.Active && frames.TryStaffLine(move, out Vector2 hand, out Vector2 tip))
                    {
                        float off = Vector2.Distance(tip, rig.WeaponTip.position);
                        worst = Mathf.Max(worst, off); sum += off; n++;
                        if ((tip - (Vector2)qori.transform.position).sqrMagnitude > (furthest - (Vector2)qori.transform.position).sqrMagnitude) furthest = tip;
                        if (dummy == null)
                        {
                            var go = new GameObject("P70 target"); go.transform.position = tip;
                            go.AddComponent<BoxCollider2D>().isTrigger = true; go.GetComponent<BoxCollider2D>().size = Vector2.one * .3f;
                            dummy = go.AddComponent<Dummy>();
                        }
                        if (!shot && combat.PhaseProgress > .35f) { shot = true; Capture(qori, move.attackId); }
                    }
                    yield return null;
                }
                Say($"{move.attackId}: drawn tip against the rig's tip: mean {(n > 0 ? sum / n : 0f):0.00} u, worst {worst:0.00} u; reach {Vector2.Distance(furthest, qori.transform.position):0.00} u");
                Expect(dummy != null && dummy.hits > 0, move.attackId + " hits a target at the drawn tip");
                if (dummy != null) Destroy(dummy.gameObject);
            }
            CombatDebugOverlay.Showing = false;

            // Hurt: the pose and flash on the frames.
            yield return new WaitForSeconds(1.2f);
            health.TakeDamage((Vector2)qori.transform.position + Vector2.left, new Vector2(3f, 3f));
            yield return null; yield return null;
            Expect(frames.Current == frames.jumpFall, "a hit shows the hurt pose");
            float flashEnd = Time.realtimeSinceStartup + .5f; bool flashed = false;
            var view = GameObject.Find("Young Adventurer frames (test)")?.GetComponent<SpriteRenderer>();
            while (Time.realtimeSinceStartup < flashEnd) { flashed |= view != null && view.color.a < .9f; yield return null; }
            Expect(flashed, "a hit flashes on the frames");

            // Death: down, a fade, back at the checkpoint with full hearts.
            yield return new WaitForSeconds(1.2f);
            health.SetHealth(1);
            Vector2 died = qori.transform.position;
            health.TakeDamage(died + Vector2.left, new Vector2(3f, 3f));
            yield return null;
            Expect(health.IsDying, "the last heart starts the death beat");
            Expect(GamePauseMenu.BlocksGameplayInput, "no control during it");
            yield return new WaitForSeconds(.3f);
            Expect(frames.Current == frames.evade, "Qori is down during it");
            until = Time.realtimeSinceStartup + 3f;
            while (health.IsDying && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSecondsRealtime(.4f);
            Expect(!health.IsDying && !GamePauseMenu.BlocksGameplayInput && health.Health == health.MaximumHealth, "he's back with full hearts and control");
            Finish();
        }

        void Capture(PlayerMovement qori, string name)
        {
            var camera = Camera.main; if (camera == null) return;
            Vector3 keep = camera.transform.position; float size = camera.orthographicSize;
            camera.transform.position = new Vector3(qori.transform.position.x, qori.transform.position.y + .6f, keep.z); camera.orthographicSize = 2.2f;
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(Dir, "hit_" + name.Replace(' ', '_') + ".png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; camera.transform.position = keep; camera.orthographicSize = size;
            Destroy(read); target.Release();
        }

        void Finish()
        {
            Say($"failures {failures}, runtime errors {errors}");
            File.WriteAllLines(Path.Combine(Dir, "polish70_results.txt"), report);
            SessionState.EraseString(StateKey);
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(failures + errors > 0 ? 1 : 0);
        }
    }

    // Danger zones in the underground, for the user to compare: -executeMethod Polish70Check.Zones -saveFile polish70_z.json -captureDir <dir> (no -quit)
    public static void Zones()
    {
        if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-saveFile") < 0) { Debug.LogError("Polish70Check needs -saveFile"); EditorApplication.Exit(1); return; }
        SessionState.SetString(ZonesKey, CaptureDir());
        EditorSceneManager.OpenScene(RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    const string ZonesKey = "Qolossal.Polish70Zones";

    [InitializeOnLoadMethod]
    static void ResumeZones()
    {
        if (string.IsNullOrEmpty(SessionState.GetString(ZonesKey, ""))) return;
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode) new GameObject("Polish70 zones").AddComponent<ZoneDriver>();
        };
    }

    sealed class ZoneDriver : MonoBehaviour
    {
        IEnumerator Start()
        {
            string dir = SessionState.GetString(ZonesKey, "");
            yield return new WaitForSeconds(2f);
            AttackZoneTelegraph.Mode = AttackZoneTelegraph.Level.All;
            var qori = FindAnyObjectByType<PlayerMovement>();
            foreach (var design in new[] { "G-03", "G-04", "E-13", "E-11" })
            {
                var enemy = FindObjectsByType<EnemyBase>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(e => e.name.Contains(design));
                if (enemy == null) { Debug.Log("[P70] no " + design); continue; }
                enemy.gameObject.SetActive(true);
                qori.PlaceAt((Vector2)enemy.transform.position + new Vector2(design[0] == 'G' ? -8f : -2.5f, .5f), 1f, false);
                bool warned = false, struck = false;
                float until = Time.realtimeSinceStartup + 25f;
                while (Time.realtimeSinceStartup < until && !(warned && struck))
                {
                    var zones = enemy.GetComponentsInChildren<AttackZoneTelegraph>(true);
                    var shown = zones.SelectMany(z => z.GetComponentsInChildren<SpriteRenderer>()).Where(r => r.enabled).ToArray();
                    string state = enemy is UndergroundGuardian g ? g.Current.ToString() : enemy is UndergroundEnemy u ? u.State : "";
                    if (!warned && state == "Warn" && shown.Length > 1 && shown[0].color.a > .07f) { warned = true; Shot(dir, design + "_warn", enemy, qori); Debug.Log("[P70] " + design + " warned: zone " + shown[0].transform.localScale); }
                    if (!struck && (state == "Strike" || state == "Attack") && shown.Length > 0) { struck = true; Shot(dir, design + "_strike", enemy, qori); Debug.Log("[P70] " + design + " strike shown"); }
                    yield return null;
                }
                Debug.Log($"[P70] {design}: warned {warned}, strike {struck}");
            }
            PlayerPrefs.DeleteKey("Qolossal.AttackZones");
            SessionState.EraseString(ZonesKey);
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(0);
        }

        static void Shot(string dir, string name, EnemyBase enemy, PlayerMovement qori)
        {
            var camera = Camera.main; if (camera == null) return;
            Vector3 keep = camera.transform.position; float size = camera.orthographicSize;
            Vector2 mid = ((Vector2)enemy.transform.position + (Vector2)qori.transform.position) * .5f;
            camera.transform.position = new Vector3(mid.x, mid.y + 1.5f, keep.z); camera.orthographicSize = name[0] == 'G' ? 7f : 3.5f;
            var target = new RenderTexture(1280, 720, 24); camera.targetTexture = target; camera.Render();
            RenderTexture.active = target;
            var read = new Texture2D(1280, 720, TextureFormat.RGB24, false); read.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(dir, "zone_" + name + ".png"), read.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; camera.transform.position = keep; camera.orthographicSize = size;
            Destroy(read); target.Release();
        }
    }

    public static void FrameMemory()
    {
        var sets = new (string name, string path)[]
        {
            ("Qori", "Assets/Resources/LudoFrames/LudoQoriFrames.prefab"),
            ("Surface enemies", "Assets/Resources/LudoFrames/LudoEnemyFrames.prefab"),
            ("Underground creatures", "Assets/Resources/LudoFrames/UndergroundCreatureFrames.asset"),
        };
        var lines = new List<string>();
        long all = 0;
        foreach (var (name, path) in sets)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            var textures = EditorUtility.CollectDependencies(new[] { asset }).OfType<Texture2D>().Distinct().ToArray();
            var byFormat = new Dictionary<TextureFormat, (int count, long bytes)>();
            long total = 0;
            foreach (var t in textures)
            {
                long b = Profiler.GetRuntimeMemorySizeLong(t); total += b;
                byFormat.TryGetValue(t.format, out var row);
                byFormat[t.format] = (row.count + 1, row.bytes + b);
            }
            all += total;
            lines.Add($"{name}: {textures.Length} textures, {total / 1048576} MiB in the editor");
            foreach (var kv in byFormat.OrderByDescending(k => k.Value.bytes)) lines.Add($"    {kv.Key}: {kv.Value.count} textures, {kv.Value.bytes / 1048576} MiB");
            EditorUtility.UnloadUnusedAssetsImmediate();
        }
        lines.Add($"All: {all / 1048576} MiB");
        foreach (var l in lines) Debug.Log("[P70 memory] " + l);
        File.WriteAllLines(Path.Combine(CaptureDir(), "frame_memory.txt"), lines);
    }
}

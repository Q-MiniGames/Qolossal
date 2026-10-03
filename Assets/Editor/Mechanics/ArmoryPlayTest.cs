using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Real Play-mode check of the armory (batch): Qori carries three weapons (the Leaf Sword, the
// Seedpod Mace and the Thorn Spear, with their permanent ids), each can be equipped and swung,
// and the mace is drawn bigger than the sword. Renders Qori at rest and mid-swing with each one
// to <-captureDir> (default Temp/ArmoryCaptures) for review.
// Usage (no -quit): -executeMethod ArmoryPlayTest.Run [-captureDir <folder>]
[InitializeOnLoad]
public static class ArmoryPlayTest
{
    const string Key = "Qolossal.ArmoryPlayTest";
    static int stage, weapon, failures, errors; static float stageAt;

    static ArmoryPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        Application.logMessageReceived += (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) { errors++; Debug.Log("[ArmoryTest] runtime error: " + m); } };
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/A0_TestRoom.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    static void Expect(bool ok, string what) { if (!ok) failures++; Debug.Log($"[ArmoryTest] {(ok ? "PASS" : "FAIL")} {what}"); }
    static PlayerMovement Qori => UnityEngine.Object.FindFirstObjectByType<PlayerMovement>();

    static string Folder
    {
        get
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-captureDir");
            string folder = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Temp/ArmoryCaptures";
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    // Renders a close view of Qori through the scene camera.
    static void Shot(string name)
    {
        var camera = Camera.main;
        Vector3 keep = camera.transform.position; float size = camera.orthographicSize;
        camera.transform.position = new Vector3(Qori.transform.position.x + .8f, Qori.transform.position.y + .6f, keep.z);
        camera.orthographicSize = 2.2f;
        var target = new RenderTexture(1200, 900, 24);
        camera.targetTexture = target; camera.Render();
        RenderTexture.active = target;
        var read = new Texture2D(1200, 900, TextureFormat.RGB24, false);
        read.ReadPixels(new Rect(0, 0, 1200, 900), 0, 0); read.Apply();
        File.WriteAllBytes(Path.Combine(Folder, name + ".png"), read.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        camera.transform.position = keep; camera.orthographicSize = size;
    }

    static MeshRenderer Ribbon(PlayerMovement qori) => qori.transform.Find("Reedblade Slash Arc")?.GetComponent<MeshRenderer>();

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.realtimeSinceStartup;
        var armory = Qori != null ? Qori.GetComponent<QoriArmory>() : null;
        var combat = Qori != null ? Qori.GetComponent<PlayerCombat>() : null;
        switch (stage)
        {
            case 0 when Time.time > 1.2f && armory != null && armory.Weapons != null:
            {
                var ids = armory.Weapons.Select(w => w.weaponId).ToArray();
                Expect(ids.SequenceEqual(new[] { "forest-0", "forest-2", "forest-3", "forest-staff" }), $"four weapons: sword, mace, spear, staff ({string.Join(", ", ids)})");
                Expect(armory.Weapons.Select(w => w.displayName).SequenceEqual(new[] { "Leaf Sword", "Seedpod Mace", "Thorn Spear", "Leaf Staff" }), "named Leaf Sword, Seedpod Mace, Thorn Spear, Leaf Staff");
                Expect(!armory.Select(4), "there is no fifth weapon to select");
                Expect(combat.EquippedWeapon == armory.Weapons[3] && armory.Selected == 3, $"Qori starts with the Leaf Staff ({combat.EquippedWeapon?.displayName})");
                float sword = armory.Weapons[0].weaponArtwork.bounds.size.x * armory.Weapons[0].artworkScale;
                float mace = armory.Weapons[1].weaponArtwork.bounds.size.x * armory.Weapons[1].artworkScale;
                float staff = armory.Weapons[3].weaponArtwork.bounds.size.x * armory.Weapons[3].artworkScale;
                Expect(mace > sword, $"the mace is drawn longer than the sword ({mace:F2} vs {sword:F2})");
                Expect(Mathf.Abs(staff - sword) < .03f * sword, $"the staff is drawn the sword's length ({staff:F2} vs {sword:F2})");
                Expect(armory.SlingWeapon != null && armory.SlingWeapon.weaponId == "resin-sling", "the resin sling is still carried");
                // Stand Qori on the start ground, clear of enemies.
                var body = Qori.GetComponent<Rigidbody2D>(); body.position = new Vector2(-16f, .4f); body.linearVelocity = Vector2.zero;
                weapon = 0; stage = 1; stageAt = t;
                break;
            }
            case 1 when t - stageAt > (weapon == 0 ? 2f : .5f):   // the first time, let Qori land and the frame rate settle
                Expect(armory.Select(weapon) && combat.EquippedWeapon == armory.Weapons[weapon], $"{armory.Weapons[weapon].displayName} can be equipped");
                stage = 2; stageAt = t;
                break;
            case 2 when t - stageAt > .4f:
                Shot($"{weapon + 1}_{armory.Weapons[weapon].displayName.Replace(' ', '_')}_rest");
                Expect(combat.RequestAttack(new AttackRequest(AttackAim.Front, Qori.FacingDirection, CombatMoveSlot.Front)), $"{armory.Weapons[weapon].displayName} swings");
                stage = 3; stageAt = t;
                break;
            // Wait for a frame where the trail shows (the first swing can run at a very low frame
            // rate in batch mode, where each frame outlives the trail's samples), then capture it.
            case 3 when !combat.IsAttackPoseActive && t - stageAt > .3f && t - stageAt < .6f:
                // A swing that didn't start (a landing or the first slow frames can swallow it): ask again.
                combat.RequestAttack(new AttackRequest(AttackAim.Front, Qori.FacingDirection, CombatMoveSlot.Front));
                stageAt -= .3f;
                break;
            case 3 when combat.Phase == AttackPhase.Active && combat.PhaseProgress > .5f && Ribbon(Qori) != null && Ribbon(Qori).enabled || t - stageAt > 2f:
                Shot($"{weapon + 1}_{armory.Weapons[weapon].displayName.Replace(' ', '_')}_swing");
                var ribbon = Ribbon(Qori);
                Expect(ribbon != null && ribbon.enabled, $"{armory.Weapons[weapon].displayName}'s slash trail shows during the swing");
                if (ribbon != null)
                {
                    var block = new MaterialPropertyBlock(); ribbon.GetPropertyBlock(block);
                    var texture = block.GetTexture("_MainTex");
                    string expected = weapon == 1 ? "SlashStrip_Heavy" : weapon == 2 ? "SlashStrip_Thrust" : "SlashStrip_Sword";
                    Expect(texture != null && texture.name == expected, $"and wears the painted {expected} ({(texture != null ? texture.name : "none")})");
                    Debug.Log($"[ArmoryTest] trail bounds {ribbon.bounds.size.x:F2} x {ribbon.bounds.size.y:F2} u at swing progress {combat.PhaseProgress:F2}");
                }
                stage = 4; stageAt = t;
                break;
            case 4 when !combat.IsAttackPoseActive && t - stageAt > .3f || stage == 4 && t - stageAt > 3f:
                if (++weapon < armory.Weapons.Length) { stage = 1; stageAt = t; break; }
                Expect(errors == 0, $"no runtime errors ({errors})");
                Debug.Log($"[ArmoryTest] finished with {failures} failure(s); captures in {Path.GetFullPath(Folder)}");
                SessionState.SetBool(Key, false);
                EditorApplication.update -= Tick;
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                break;
        }
    }
}

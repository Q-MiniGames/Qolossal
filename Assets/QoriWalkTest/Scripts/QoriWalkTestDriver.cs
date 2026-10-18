using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Test-only: a scripted run of the QoriWalkTest scene on a virtual keyboard (idle, walk, run, jump, walk left), with a
// close-up of the Player at each moment (painted walk, and the current Qori from the same moment) and of the demo
// walker. Started by QoriWalkTestBuilder.Run in batch mode; Done tells the editor to quit.
public sealed class QoriWalkTestDriver : MonoBehaviour
{
    public static bool Done;
    public static string OutDir = "Temp/QoriWalkTest";
    readonly StringBuilder log = new StringBuilder("moment,time,x,y,vx,grounded,facing,showing,frame,feetDx\n");
    QoriWalkFramesOnPlayer walk;
    Keyboard keyboard;
    int shot;
    Mouse mouse;
    float feetRef = float.NaN;

    IEnumerator Start()
    {
        Done = false;
        Directory.CreateDirectory(OutDir);
        while ((walk = FindAnyObjectByType<QoriWalkFramesOnPlayer>()) == null || walk.Current == null) yield return null;
        var focusRule = InputSystem.settings.editorInputBehaviorInPlayMode;
        var backgroundRule = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>("QoriWalkTestKeyboard");
        if (Camera.main != null) Camera.main.aspect = 16f / 9f;

        yield return Hold(1.2f, new float[0], "idle");
        yield return Hold(.9f, new[] { .5f, .58f, .66f, .74f, .82f }, "run", Key.D);
        yield return Hold(.5f, new[] { .45f }, "stop");
        yield return Hold(1.6f, new[] { .8f, .87f, .94f, 1.01f, 1.08f, 1.15f }, "walk_left", Key.A);
        yield return Hold(.6f, new[] { .5f }, "idle_left");
        yield return Hold(.1f, new float[0], "jump", Key.Space);
        yield return Hold(.8f, new[] { .3f }, "air", Key.Space);
        yield return Hold(.8f, new[] { .6f }, "landed");
        // the Leaf Staff's attack set (Ludo A1-A4), on a virtual mouse (attack is the left button)
        mouse = InputSystem.AddDevice<Mouse>("QoriWalkTestMouse");
        yield return Attack(1.4f, new[] { .1f, .25f, .45f, .62f, .78f, .95f, 1.15f }, "combo", new[] { 0f, .3f, .55f });
        yield return Attack(.8f, new[] { .1f, .25f, .45f }, "up", new[] { 0f }, false, Key.W);
        // the double jump: jump, let go, jump again in the air (the flip), then hold it to glide down
        yield return Hold(.15f, new float[0], "jump2", Key.Space);
        yield return Hold(.12f, new float[0], "jump2_release");
        yield return Hold(1.4f, new[] { .04f, .12f, .2f, .28f, .36f, .6f, 1.1f }, "doublejump", Key.Space);
        yield return Hold(1.2f, new float[0], "landed2");
        yield return Hold(.7f, new float[0], "runup", Key.D);
        yield return Attack(.8f, new[] { .08f, .2f, .35f, .55f }, "sprint", new[] { 0f }, false, Key.D);
        yield return Hold(.6f, new float[0], "settle");
        yield return Attack(2.2f, new[] { .5f, 1.1f, 1.5f, 1.85f, 2.0f, 2.15f }, "charge", new[] { 0f }, true);
        yield return Hold(.6f, new[] { .1f, .3f }, "release");
        InputSystem.RemoveDevice(mouse);
        File.WriteAllText(Path.Combine(OutDir, "QoriWalkTest_Log.csv"), log.ToString());
        Debug.Log("[QoriWalkTest] " + shot + " captures in " + OutDir + "; current Qori " + walk.CurrentQoriHeight.ToString("0.000") + " u tall; game walk " + walk.Player.WalkSpeed + " / run " + walk.Player.RunSpeed + " u/s");
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.editorInputBehaviorInPlayMode = focusRule;
        InputSystem.settings.backgroundBehavior = backgroundRule;
        Done = true;
    }

    IEnumerator Hold(float seconds, float[] captureAt, string label, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        float t = 0f; int next = 0;
        while (t < seconds)
        {
            yield return null;
            t += Time.deltaTime;
            if (next < captureAt.Length && t >= captureAt[next]) { Capture(label + "_" + next); next++; }
        }
        if (captureAt.Length == 0) Capture(label);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
    }

    // Clicks attack at the given moments (holding it down for the whole stretch if hold), with keys held throughout.
    IEnumerator Attack(float seconds, float[] captureAt, string label, float[] clicks, bool hold = false, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        float t = 0f; int next = 0, click = 0; bool down = false;
        while (t < seconds)
        {
            bool want = hold || (click < clicks.Length && t >= clicks[click] && !down);
            if (want != down) { InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left, want)); down = want; if (want && !hold) click++; }
            yield return null;
            t += Time.deltaTime;
            if (!hold && down) { InputSystem.QueueStateEvent(mouse, new MouseState()); down = false; }
            if (next < captureAt.Length && t >= captureAt[next])
            {
                var combat = walk.Player.GetComponent<PlayerCombat>();
                log.AppendLine(string.Join(",", label + "_" + next, Time.time.ToString("0.000"), walk.Player.transform.position.x.ToString("0.000"), walk.Player.transform.position.y.ToString("0.000"),
                    walk.Player.ObservedVelocity.x.ToString("0.00"), walk.Player.IsGrounded, walk.Player.FacingDirection, walk.Current.label, walk.Frame + 1,
                    combat.CurrentAttack != null ? combat.CurrentAttack.attackId + " " + combat.Phase : combat.IsCharging ? "charging " + combat.ChargeElapsed.ToString("0.00") : "-"));
                CaptureAt(label + "_" + next, walk.Player.transform.position, false);
                next++;
            }
        }
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
    }

    void Capture(string name)
    {
        var p = walk.Player;
        log.AppendLine(string.Join(",", name, Time.time.ToString("0.000"), p.transform.position.x.ToString("0.000"), p.transform.position.y.ToString("0.000"),
            p.ObservedVelocity.x.ToString("0.00"), p.IsGrounded, p.FacingDirection, walk.FramesVisible ? walk.Current.label : "rig", walk.Frame + 1, ""));
        CaptureAt(name, p.transform.position, true);
    }

    void CaptureAt(string name, Vector3 at, bool both)
    {
        var cam = Camera.main;
        if (cam == null) return;
        ++shot;
        var keepPos = cam.transform.position; float keepSize = cam.orthographicSize; int keepMask = cam.cullingMask;
        cam.transform.position = new Vector3(at.x, at.y + walk.CurrentQoriHeight * .45f, keepPos.z);
        cam.orthographicSize = walk.CurrentQoriHeight * .8f;
        int passes = both ? 2 : 1;
        for (int i = 0; i < passes; i++)
        {
            int all = keepMask | (1 << QoriWalkFramesOnPlayer.OldLayer) | (1 << QoriWalkFramesOnPlayer.NewLayer);
            cam.cullingMask = i == 0 ? (walk.FramesVisible || !both ? all & ~(1 << QoriWalkFramesOnPlayer.OldLayer) : all & ~(1 << QoriWalkFramesOnPlayer.NewLayer))
                                     : all & ~(1 << QoriWalkFramesOnPlayer.NewLayer);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt; cam.aspect = 16f / 9f; cam.Render();
            RenderTexture.active = rt;
            var img = new Texture2D(720, 720, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(280, 0, 720, 720), 0, 0); img.Apply();
            RenderTexture.active = null; cam.targetTexture = null; rt.Release(); Destroy(rt);
            File.WriteAllBytes(Path.Combine(OutDir, shot.ToString("00") + "_" + name + (i == 0 ? "_shown" : "_currentQori") + ".png"), img.EncodeToPNG());
            Destroy(img);
        }
        cam.transform.position = keepPos; cam.orthographicSize = keepSize; cam.cullingMask = keepMask;
    }
}

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// A recorded run for comparing background treatments (-mraDepthCapture <tag>): in the Terraces, Qori
// runs by keyboard input from 24 u before Mill Ridge to 16 u past it, climbing on the way, through a
// lookout zoom at the ridge (added for the recording if the scene has none, so every treatment sees
// the same zoom). Game time is fixed at 30 frames per second (Time.captureFramerate), and each frame
// is rendered at 1280x720 into <captureDir>/polish_frames/depth_<tag>/. The camera's own record
// (position and size per frame) goes to depth_<tag>/camera.csv. A tag "name@x" starts at x instead
// and runs 70 u (no lookout added).
public sealed partial class MraTestDriver
{
    IEnumerator DepthCapture(string tag)
    {
        yield return Load("MRAtlas_MR03_Terraces");
        var r = W.RegionById("MR03");
        var ridge = r.NodeById("MR03_N07");
        Vector2 n7 = ridge.spawn;
        if (tag.IndexOf('@') < 0 && FindObjectsByType<VistaZone>(FindObjectsSortMode.None).All(v => Mathf.Abs(v.transform.position.x - ridge.position.x) > 8f))
        {
            var vista = new GameObject("Lookout vista (recording)").AddComponent<VistaZone>();
            vista.transform.position = ridge.position + new Vector2(0f, 2f);
            vista.GetComponent<BoxCollider2D>().size = new Vector2(Mathf.Max(8f, ridge.padWidth), 4f);
            vista.size = 7f; vista.lift = 1.5f;
        }
        float startX = n7.x - 24f, endX = n7.x + 16f;
        int atSign = tag.IndexOf('@');
        if (atSign > 0 && float.TryParse(tag.Substring(atSign + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float from))
        {
            startX = from; endX = from + 70f; tag = tag.Substring(0, atSign) + "_" + Mathf.RoundToInt(from);
        }
        Place(new Vector2(startX, GroundAt(startX, n7.y + 30f) + 1f));
        yield return Wait(1.5f);
        string folder = Path.Combine(captureDir, "polish_frames", "depth_" + tag);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        Directory.CreateDirectory(folder);
        var rows = new List<string> { "frame,camera_x,camera_y,size,qori_x,qori_y" };
        Time.captureFramerate = 30;
        float jumpUntil = -1f, stall = 0f;
        for (int i = 0; i < 480 && Body.position.x < endX; i++)
        {
            var q = Qori; bool held = q.IsGrounded || q.IsLedgeHanging || q.IsWallSliding;
            stall = held && Mathf.Abs(Body.linearVelocity.x) < .6f ? stall + Time.deltaTime : 0f;
            if (stall > .12f && Time.time > jumpUntil) { jumpUntil = Time.time + .42f; stall = 0f; }
            var keys = new List<Key> { Key.D };
            if (Time.time < jumpUntil) keys.Add(Key.Space);
            if (q.IsLedgeHanging) keys.Add(Key.W);
            Press(keys.ToArray());
            yield return null;
            Frame($"depth_{tag}/f{i:0000}");
            var cam = Camera.main;
            rows.Add($"{i},{cam.transform.position.x:F3},{cam.transform.position.y:F3},{cam.orthographicSize:F3},{Body.position.x:F3},{Body.position.y:F3}");
        }
        Press();
        Time.captureFramerate = 0;
        File.WriteAllLines(Path.Combine(folder, "camera.csv"), rows);
        Expect(rows.Count > 60, $"depth capture: {rows.Count - 1} frames recorded ({tag})");
        Expect(Body.position.x >= endX - 1f, $"depth capture: Qori reached past Mill Ridge (x {Body.position.x:F1}, target {endX:F1})");
    }
}
#endif

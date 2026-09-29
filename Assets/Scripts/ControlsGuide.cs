using UnityEngine;

[DisallowMultipleComponent]
public sealed class ControlsGuide : MonoBehaviour
{
    private GUIStyle headingStyle;
    private GUIStyle rowStyle;
    private static readonly string[] keyboardRows =
    {
        "Move: A / D or arrow keys",
        "Run: Hold Shift while moving",
        "Jump / flower boost: Space",
        "Wall: Space climbs; away + Space jumps off",
        "Ledge: Space / Up climbs; Down / away drops",
        "Dash (Wind Leaf): C",
        "Glide (Glidecap): hold Space while falling",
        "Attack: Left mouse; W / Up aims overhead",
        "Armory: Tab / 1-4 | Hold Q: aim; release: fire",
        "Hook target: Nearest highlighted anchor",
        "Hook / release: F or right mouse",
        "Shorten / extend thread: W / S",
        "Grow temporary platform: E",
        "Pause: Escape"
    };
    private static readonly string[] controllerRows =
    {
        "Move: Left stick",
        "Run: Hold left-stick click (L3)",
        "Jump / flower boost: Bottom face button",
        "Wall: Jump climbs; away + Jump pushes off",
        "Ledge: Jump / Up climbs; Down / away drops",
        "Dash (Wind Leaf): Right face button",
        "Glide (Glidecap): hold Jump while falling",
        "Attack: Left shoulder; Up aims overhead",
        "Armory: View + right stick | Hold X: aim; release: fire",
        "Hook target: Nearest highlighted anchor",
        "Hook / release: Press right trigger",
        "Shorten: Left trigger | Extend: D-pad down",
        "Grow temporary platform: Right shoulder",
        "Pause: Menu / Start"
    };

    private void OnGUI()
    {
        if (GamePauseMenu.IsPaused) return;
        if (headingStyle == null)
        {
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        }

        // Scale the guide on smaller windows while retaining a consistent
        // bottom-left position. This is display-only, so clicks cannot trigger
        // guide buttons and gameplay attacks at the same time.
        float scale = Mathf.Min(1f, Screen.width / 720f, Screen.height / 480f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float height=46f+keyboardRows.Length*20f;
        float y = Screen.height / scale - height - 16f;
        GUI.Box(new Rect(16f, y, 680f, height), GUIContent.none);
        GUI.Label(new Rect(28f, y + 8f, 320f, 24f), "Keyboard + mouse", headingStyle);
        GUI.Label(new Rect(364f, y + 8f, 320f, 24f), "Controller", headingStyle);
        for (int i = 0; i < keyboardRows.Length; i++)
        {
            float rowY = y + 34f + i * 20f;
            GUI.Label(new Rect(28f, rowY, 330f, 22f), keyboardRows[i], rowStyle);
            GUI.Label(new Rect(364f, rowY, 330f, 22f), controllerRows[i], rowStyle);
        }
        GUI.matrix = previousMatrix;
    }
}

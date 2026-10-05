using UnityEngine;

// The Mountain Relief Atlas route's saved predicates, as the design data writes them:
// "relic:<id>" (GameSave relics), "knot:<legacy id>" (GameSave knots: the existing grip, reach, ...)
// and "flag:<mra id>" or a bare "mra:..." id (GameSave flags). A list holds when every entry does.
public static class MraState
{
    public const string RevealFlag = "mra:reveal-complete", EndingFlag = "mra:careful-ending";

    public static bool Holds(string predicate)
    {
        if (string.IsNullOrEmpty(predicate)) return true;
        if (predicate.StartsWith("relic:")) return GameSave.HasRelic(predicate.Substring(6));
        if (predicate.StartsWith("knot:")) return GameSave.IsKnotAwake(predicate.Substring(5));
        if (predicate.StartsWith("flag:")) return GameSave.HasFlag(predicate.Substring(5));
        return GameSave.HasFlag(predicate);
    }

    public static bool Holds(string[] predicates)
    {
        if (predicates == null) return true;
        foreach (string p in predicates) if (!Holds(p)) return false;
        return true;
    }

    // What Qori is told he lacks, by its innocent name ("Living Thread").
    public static string Describe(string predicate)
    {
        if (string.IsNullOrEmpty(predicate)) return "";
        if (predicate.StartsWith("relic:"))
        {
            string id = predicate.Substring(6);
            foreach (var relic in Relics.All) if (relic != null && relic.abilityId == id) return relic.displayName;
            return id;
        }
        return "something not yet found";
    }

    public static string VisitedFlag(string region) => "mra:" + region + ":visited";
    public static string EdgeFlag(string edge) => "mra:" + edge + ":seen";
    public static string PageFlag(string region) => "chart-page:" + region;   // a TownState flag, bought from Scribble
    public static bool Revealed => GameSave.HasFlag(RevealFlag);

    public static bool IsQori(Collider2D other) =>
        other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null;
    public static PlayerMovement QoriOf(Collider2D other) =>
        other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;

    // A prompt over a point in the world, on a dark plate so it reads on bright sky and pale rock.
    // "▲" in the text marks the Up action: it's drawn as a key cap for the device in use (W on the
    // keyboard, the D-pad on a gamepad), switching as soon as the other device is used. The caps are
    // text, not platform glyph art (a follow-up: per-platform button art).
    public static void Prompt(Vector3 world, string text, bool bright = true)
    {
        var view = Camera.main; if (view == null) return;
        Vector3 p = view.WorldToScreenPoint(world);
        if (p.z < 0f) return;
        TrackDevice();
        int size = Mathf.Max(14, Mathf.RoundToInt(Screen.height * .024f));
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontSize = size, fontStyle = FontStyle.Bold, wordWrap = false };
        style.normal.textColor = bright ? new Color(1f, .93f, .72f) : new Color(1f, 1f, 1f, .8f);
        var capStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(size * .8f) };
        capStyle.normal.textColor = new Color(.16f, .13f, .1f);
        string cap = padMode ? "D-pad Up" : "W";
        string[] lines = text.Split('\n');
        float lineH = size * 1.55f, pad = size * .5f;
        // Measure each line: [cap] text.
        var widths = new float[lines.Length];
        float width = 0f;
        for (int i = 0; i < lines.Length; i++)
        {
            string body = lines[i].Replace("▲", "").Trim();
            float w = style.CalcSize(new GUIContent(body)).x;
            if (lines[i].Contains("▲")) w += capStyle.CalcSize(new GUIContent(cap)).x + size * 1.1f;
            widths[i] = w; width = Mathf.Max(width, w);
        }
        float plateW = width + pad * 2f, plateH = lineH * lines.Length + pad * .6f;
        float left = p.x - plateW * .5f, top = Screen.height - p.y - plateH;
        Color was = GUI.color;
        GUI.color = new Color(.1f, .08f, .06f, bright ? .72f : .55f);
        GUI.DrawTexture(new Rect(left, top, plateW, plateH), Texture2D.whiteTexture);
        GUI.color = was;
        for (int i = 0; i < lines.Length; i++)
        {
            float x = p.x - widths[i] * .5f, y = top + pad * .3f + lineH * i;
            string body = lines[i].Replace("▲", "").Trim();
            if (lines[i].Contains("▲"))
            {
                float capW = capStyle.CalcSize(new GUIContent(cap)).x + size * .6f;
                var capRect = new Rect(x, y + lineH * .14f, capW, lineH * .72f);
                GUI.color = new Color(1f, .95f, .82f, bright ? 1f : .7f);
                GUI.DrawTexture(capRect, Texture2D.whiteTexture);
                GUI.color = was;
                GUI.Label(capRect, cap, capStyle);
                x += capW + size * .5f;
            }
            GUI.Label(new Rect(x, y, widths[i] + 4f, lineH), body, style);
        }
    }

    static bool padMode;
    static void TrackDevice()
    {
        var pad = UnityEngine.InputSystem.Gamepad.current; var keys = UnityEngine.InputSystem.Keyboard.current;
        if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > .09f || pad.dpad.ReadValue().sqrMagnitude > .09f || pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.rightTrigger.isPressed)) padMode = true;
        if (keys != null && keys.anyKey.isPressed) padMode = false;
    }

    public static bool Busy => ModalUi.IsOpen || GamePauseMenu.IsPaused || AreaTransition.IsTransitioning || MraChart.IsOpen;
}

using UnityEngine;
using UnityEngine.UI;

// Qvale prototype HUD: the Amber counter in the top-right corner (it shows itself when Amber
// changes, and stays in town), plus short messages (a find, a caption) across the top.
public sealed class TownHud : MonoBehaviour
{
    public static TownHud Instance { get; private set; }
    [Tooltip("Keep the counter on screen (in town), rather than only after a change.")] public bool alwaysShowAmber = true;

    Canvas canvas; Text amberText, toastText, captionText, subText; Image gem;
    float amberShownAt = -100f, toastAt = -100f, captionAt = -100f, toastSeconds = 4f;
    int lastAmber = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => Instance = null;

    public static TownHud Ensure() => Instance != null ? Instance : new GameObject("Town HUD").AddComponent<TownHud>();

    void Awake()
    {
        Instance = this;
        canvas = GameHud.CreateCanvas("Town HUD Canvas", 20);
        canvas.transform.SetParent(transform, false);
        gem = GameHud.AddImage(canvas.transform, "Amber Gem", null, new Vector2(1f, 1f), new Vector2(-220f, -60f), new Vector2(34f, 42f));
        gem.color = TownUi.AmberColor; gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        gem.rectTransform.sizeDelta = new Vector2(32f, 32f);
        amberText = TownUi.Label(canvas.transform, "Amber", new Vector2(1f, 1f), new Vector2(-120f, -60f), new Vector2(160f, 50f), 38, TextAnchor.MiddleLeft, TownUi.Ink);
        toastText = TownUi.Label(canvas.transform, "Toast", new Vector2(.5f, 1f), new Vector2(0f, -170f), new Vector2(1300f, 110f), 34, TextAnchor.MiddleCenter, TownUi.Ink);
        captionText = TownUi.Label(canvas.transform, "Caption", new Vector2(.5f, .5f), new Vector2(0f, 200f), new Vector2(1400f, 90f), 62, TextAnchor.MiddleCenter, Color.white);
        subText = TownUi.Label(canvas.transform, "Caption Sub", new Vector2(.5f, .5f), new Vector2(0f, 130f), new Vector2(1400f, 60f), 34, TextAnchor.MiddleCenter, TownUi.Mint);
        captionText.fontStyle = FontStyle.Bold; subText.fontStyle = FontStyle.Italic;
        TownState.Changed += OnChanged;
        OnChanged();
    }

    void OnDestroy() { TownState.Changed -= OnChanged; if (Instance == this) Instance = null; }

    void OnChanged()
    {
        if (TownState.Amber != lastAmber) { lastAmber = TownState.Amber; amberShownAt = Time.time; }
        amberText.text = TownState.Amber.ToString();
    }

    public static void Toast(string message, float seconds = 4f)
    {
        var hud = Ensure(); hud.toastText.text = message; hud.toastAt = Time.time; hud.toastSeconds = seconds;
    }

    public static void Caption(string title, string sub)
    {
        var hud = Ensure(); hud.captionText.text = title; hud.subText.text = sub; hud.captionAt = Time.time;
    }

    static float Fade(float since, float hold) => since < .3f ? since / .3f : since < hold ? 1f : Mathf.Clamp01(1f - (since - hold) / .8f);

    void Update()
    {
        float amberAlpha = alwaysShowAmber ? 1f : Fade(Time.time - amberShownAt, 3f);
        bool modal = ListeningSpot.IsSitting;   // the screen goes quiet while listening
        amberText.color = new Color(TownUi.Ink.r, TownUi.Ink.g, TownUi.Ink.b, modal ? 0f : amberAlpha);
        gem.color = new Color(TownUi.AmberColor.r, TownUi.AmberColor.g, TownUi.AmberColor.b, modal ? 0f : amberAlpha);
        float pulse = 1f + .25f * Mathf.Max(0f, 1f - (Time.time - amberShownAt) / .25f);
        gem.rectTransform.localScale = Vector3.one * pulse;
        toastText.color = new Color(TownUi.Ink.r, TownUi.Ink.g, TownUi.Ink.b, Fade(Time.time - toastAt, toastSeconds));
        float c = Fade(Time.time - captionAt, 3.2f);
        captionText.color = new Color(1f, 1f, 1f, c); subText.color = new Color(TownUi.Mint.r, TownUi.Mint.g, TownUi.Mint.b, c);
    }
}

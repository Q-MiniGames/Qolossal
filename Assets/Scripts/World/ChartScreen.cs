using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The Chart: the map of the titan (world design 3.2). A parchment painting of the whole body
// under painted moss and mist. A region's mist clears once any of its levels is charted (at its
// Waymark); a woken knot shows its body part in the new pose, with the faint contour of the
// sleeping pose beside it. Travelled veins are drawn as glowing root lines between levels, and
// icons mark the charted levels, the knots, untamed Wild Veins and where Qori is.
// M (or the pause menu's Chart button) opens it; M, Esc or B closes it. The game is paused
// while it's open. It reads the saved game and the world atlas, so it's rebuilt on every opening.
[DefaultExecutionOrder(-1100), DisallowMultipleComponent]
public sealed class ChartScreen : MonoBehaviour
{
    const float MapSize = 820f, FrameMargin = 70f, MapY = -12f, IconSize = 46f, VeinWidth = 18f;
    static readonly Color Ink = new Color(.93f, .88f, .76f), Mint = new Color(.72f, 1f, .88f), Sepia = new Color(.24f, .15f, .08f), Parchment = new Color(.96f, .92f, .8f, .85f);

    public static bool IsOpen { get; private set; }
    static int closedFrame = -1;
    // The pause menu ignores Esc on the frame the Chart closed with it.
    public static bool BlocksPause => IsOpen || Time.frameCount == closedFrame;
    public static ChartScreen Instance { get; private set; }
    // A scene's own map, opened by the pause menu's Chart button where this Chart doesn't exist.
    public static System.Action OpenReplacement;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsOpen = false; closedFrame = -1; Instance = null; OpenReplacement = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    // Every game area gets a Chart (sandbox and lab scenes don't).
    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != null || GameArea.InScene == null || !GameArea.InScene.legacyChart) return;
        var skin = UiSkin.Load();
        if (skin == null || skin.chartBase == null || WorldAtlas.Load() == null) return;
        new GameObject("Chart").AddComponent<ChartScreen>();
    }

    Canvas canvas; RectTransform map; Image qori;
    float previousTimeScale = 1f;
    readonly List<Image> pulsing = new List<Image>();

    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) { Instance = null; if (IsOpen) Close(); } }

    // Keyboard only; on a gamepad the Chart opens from the pause menu.
    static bool TogglePressed() => Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame;

    static bool BackPressed()
    {
        var key = Keyboard.current; var pad = Gamepad.current;
        return key != null && key.escapeKey.wasPressedThisFrame || pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
    }

    void Update()
    {
        if (IsOpen)
        {
            if (TogglePressed() || BackPressed()) Close();
            else Animate();
            return;
        }
        if (TogglePressed() && !GamePauseMenu.IsPaused && !AreaTransition.IsTransitioning && !StirSequence.IsPlaying) Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        previousTimeScale = Time.timeScale; Time.timeScale = 0f;
        Build();
        canvas.gameObject.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false; closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- building

    static RectTransform Rect(Transform parent, string name, Vector2 size, Vector2 position = default)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = size; rt.anchoredPosition = position;
        return rt;
    }

    static Image Picture(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position = default)
    {
        var image = Rect(parent, name, size, position).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.raycastTarget = false; image.preserveAspect = false;
        return image;
    }

    // Screen text is light with a dark shadow; text on the map is sepia ink with a parchment halo.
    static Text Label(Transform parent, string text, Vector2 position, int size, Color color, float width = 700f, bool onMap = false)
    {
        var rt = Rect(parent, "Label", new Vector2(width, size * 1.8f), position);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = UiSkin.Font; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = color;
        t.text = text; t.raycastTarget = false;
        if (onMap)
        {
            t.fontStyle = FontStyle.Bold;
            var halo = rt.gameObject.AddComponent<Outline>(); halo.effectColor = Parchment; halo.effectDistance = new Vector2(2f, -2f);
        }
        else
        {
            var shadow = rt.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .75f); shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return t;
    }

    // Map position (0-1 from the bottom-left) to the map rect's local position.
    static Vector2 OnMap(Vector2 p) => (p - new Vector2(.5f, .5f)) * MapSize;

    static int RegionIndex(string region) =>
        region != null && region.Length == 2 && region[0] == 'R' && char.IsDigit(region[1]) ? region[1] - '1' : -1;

    void Build()
    {
        var skin = UiSkin.Load(); var atlas = WorldAtlas.Load();
        if (canvas == null)
        {
            canvas = GameHud.CreateCanvas("Chart Canvas", 70);
            canvas.transform.SetParent(transform, false);
        }
        foreach (Transform child in canvas.transform) Destroy(child.gameObject);
        pulsing.Clear();

        var dim = Picture(canvas.transform, "Dim", null, new Vector2(4000f, 4000f));
        dim.color = new Color(.05f, .06f, .05f, .82f);
        var frame = Picture(canvas.transform, "Frame", skin.chartFrame, Vector2.one * (MapSize + FrameMargin * 2f), new Vector2(0f, MapY));
        frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 2.4f;
        map = Rect(canvas.transform, "Map", Vector2.one * MapSize, new Vector2(0f, MapY));
        // The frame's vine sits over the map's edge.
        frame.transform.SetAsLastSibling();

        // Which regions are charted: any of their levels charted at its Waymark.
        var revealed = new bool[7];
        int charted = 0, total = 0;
        foreach (var level in atlas.levels)
        {
            total++;
            if (!GameSave.IsCharted(level.levelId)) continue;
            charted++;
            int r = RegionIndex(level.region);
            if (r >= 0) revealed[r] = true;
        }

        Picture(map, "Chart", skin.chartBase, Vector2.one * MapSize);
        Picture(map, "Mist", skin.chartFog, Vector2.one * MapSize);
        // Each charted region shows the map through its own mask, above the mist.
        for (int r = 0; r < 7; r++)
        {
            if (!revealed[r] || skin.chartRegions[r] == null) continue;
            var mask = Picture(map, $"Charted R{r + 1}", skin.chartRegions[r], Vector2.one * MapSize);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            Picture(mask.transform, "Chart", skin.chartBase, Vector2.one * MapSize);
        }
        // Woken knots: the body part in its new pose, and the old contour beside it.
        int awake = 0;
        for (int r = 0; r < Knots.All.Length; r++)
        {
            if (!GameSave.IsKnotAwake(Knots.All[r])) continue;
            awake++;
            if (r < skin.chartStirPoses.Length && skin.chartStirPoses[r] != null) Picture(map, $"Stir R{r + 1}", skin.chartStirPoses[r], Vector2.one * MapSize);
            if (r < skin.chartStirGhosts.Length && skin.chartStirGhosts[r] != null) Picture(map, $"Old pose R{r + 1}", skin.chartStirGhosts[r], Vector2.one * MapSize);
        }

        // Travelled veins, as root lines between the levels at either end.
        var levelOfPortal = new Dictionary<string, WorldAtlas.Level>();
        foreach (var level in atlas.levels) foreach (var end in level.veins) levelOfPortal[end.portalId] = level;
        foreach (string vein in GameSave.Veins)
        {
            string[] ends = vein.Split('|');
            if (ends.Length != 2 || !levelOfPortal.TryGetValue(ends[0], out var a) || !levelOfPortal.TryGetValue(ends[1], out var b) || a == b) continue;
            VeinLine(skin, OnMap(a.chartPosition), OnMap(b.chartPosition));
        }
        // A Wild Vein that settled goes to a Waymark rather than a portal: its line runs to that level.
        foreach (var level in atlas.levels)
            foreach (var end in level.veins)
                if (end.wild && GameSave.IsKnotAwake(end.settlesWith))
                    foreach (string vein in GameSave.Veins)
                        if (vein.StartsWith(end.portalId + "|") || vein.EndsWith("|" + end.portalId))
                        {
                            string other = vein.StartsWith(end.portalId + "|") ? vein.Substring(end.portalId.Length + 1) : vein.Substring(0, vein.Length - end.portalId.Length - 1);
                            var target = atlas.levels.Find(l => l.waymark == other);
                            if (target != null && target != level) VeinLine(skin, OnMap(level.chartPosition), OnMap(target.chartPosition));
                        }

        // Charted levels: their Waymark (or knot), name, and any Wild Vein still untamed.
        var here = atlas.LevelOf(SceneManager.GetActiveScene().name);
        foreach (var level in atlas.levels)
        {
            bool known = GameSave.IsCharted(level.levelId);
            if (!known && level != here) continue;
            Vector2 at = OnMap(level.chartPosition);
            Sprite icon = !string.IsNullOrEmpty(level.knot) ? (GameSave.IsKnotAwake(level.knot) ? skin.mapKnotAwake : skin.mapKnotDormant)
                : known ? skin.mapWaymark : skin.mapUnexplored;
            var image = Picture(map, "Icon " + level.levelId, icon, Vector2.one * IconSize, at);
            image.preserveAspect = true;
            if (!string.IsNullOrEmpty(level.knot) && !GameSave.IsKnotAwake(level.knot)) pulsing.Add(image);
            if (known) Label(map, level.displayName, at + new Vector2(0f, -IconSize * .85f), 18, Sepia, 280f, onMap: true);
            foreach (var end in level.veins)
                if (end.wild && end.Present && !GameSave.IsKnotAwake(end.settlesWith) && skin.mapWildVein != null)
                {
                    var wild = Picture(map, "Wild Vein " + end.portalId, skin.mapWildVein, Vector2.one * IconSize * .75f, at + new Vector2(IconSize * .9f, IconSize * .2f));
                    wild.preserveAspect = true; pulsing.Add(wild);
                }
        }
        qori = null;
        if (here != null && skin.mapQori != null)
        {
            qori = Picture(map, "Qori", skin.mapQori, Vector2.one * IconSize * 1.2f, OnMap(here.chartPosition) + new Vector2(0f, IconSize));
            qori.preserveAspect = true;
        }

        float top = MapY + MapSize * .5f + FrameMargin, bottom = MapY - MapSize * .5f - FrameMargin;
        Label(canvas.transform, "The Qolossal", new Vector2(0f, top + 24f), 36, Ink);
        Label(canvas.transform, $"{charted} of {total} places charted   ·   {awake} of {Knots.All.Length} knots awake   ·   M / Esc: close",
            new Vector2(0f, bottom - 22f), 20, Mint, 1200f);
        frame.transform.SetAsLastSibling();
    }

    void VeinLine(UiSkin skin, Vector2 from, Vector2 to)
    {
        if (skin.chartVeinLine == null) return;
        Vector2 d = to - from;
        var line = Picture(map, "Vein", skin.chartVeinLine, new Vector2(d.magnitude, VeinWidth));
        line.type = Image.Type.Tiled; line.pixelsPerUnitMultiplier = 64f / VeinWidth;
        var rt = line.rectTransform;
        rt.pivot = new Vector2(0f, .5f); rt.anchoredPosition = from;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    void Animate()
    {
        float t = Time.unscaledTime;
        foreach (var image in pulsing) if (image != null) image.color = new Color(1f, 1f, 1f, .65f + .35f * Mathf.Sin(t * 3f));
        if (qori != null) qori.rectTransform.localScale = Vector3.one * (1f + .08f * Mathf.Sin(t * 2.4f));
    }
}

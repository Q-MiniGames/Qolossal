using System.Collections.Generic;
using Mra;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The regional Chart (Mountain Relief Atlas): one unlocked region's relief drawing at a time, with
// its own navigation layer on top: the paths Qori has walked (or a bought page's), his position
// (updated as he moves, projected along the route), the lit Waymark, shrines whose relic he holds,
// and side chambers only once discovered. There is no whole-world view before the reveal; after
// it, the final assembly joins the list.
// Keyboard and mouse: M opens and closes (Esc too); WASD / arrows or drag pan; the wheel or Z / X
// zoom; Q / E or PageUp / PageDown switch region; R centres on Qori; Enter travels. Gamepad: View
// opens and closes (B too); left stick pans; triggers zoom; shoulders switch region; Y centres;
// A travels. Hints show the active device's buttons. The game is paused while it's open; zoom and
// position are kept per region across closing, reopening and Waymark travel.
[DisallowMultipleComponent]
public sealed class MraChart : MonoBehaviour
{
    // 1920x1080 reference layout: the map, the region list and legend, the title and action rows.
    const float MapW = 1480f, MapH = 900f, MapX = -180f, MapY = -30f, ListX = 790f;
    const float MinZoom = 1f, MaxZoom = 2.6f, Stroke = 6f, Halo = 2f, MarkerSize = 48f;
    static readonly Color Parchment = new Color32(0xF5, 0xEC, 0xD4, 0xFF), Ink = new Color32(0x4B, 0x51, 0x30, 0xFF),
        Route = new Color32(0x5F, 0x63, 0x38, 0xFF), Amber = new Color32(0xD7, 0x98, 0x35, 0xFF), Dim = new Color(0f, 0f, 0f, .72f);

    public static bool IsOpen { get; private set; }
    static MraChart instance;
    static int closedFrame = -10;
    // Remembered across openings and scene loads.
    static readonly Dictionary<string, Vector2> panOf = new Dictionary<string, Vector2>();
    static readonly Dictionary<string, float> zoomOf = new Dictionary<string, float>();
    static string viewing;
    static bool padMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsOpen = false; instance = null; closedFrame = -10; panOf.Clear(); zoomOf.Clear(); viewing = null; padMode = false; }

    public const string WholeClimb = "ALL";
    Canvas canvas; RectTransform viewport, content, overlay; Image mapImage; Text title, list, hints, legend;
    float previousTimeScale = 1f;
    readonly List<string> choices = new List<string>();
    public string Viewing => viewing;
    public float Zoom => zoomOf.TryGetValue(viewing ?? "", out float z) ? z : MinZoom;
    public Vector2 Pan => panOf.TryGetValue(viewing ?? "", out Vector2 p) ? p : Vector2.zero;
    public RectTransform QoriMarker { get; private set; }
    public Vector2 QoriUv { get; private set; }
    public int RouteSegmentsShown { get; private set; }
    public int CavesShown { get; private set; }

    public static MraChart Instance => instance;
    public static MraChart Ensure() => instance != null ? instance : instance = new GameObject("MRA Chart").AddComponent<MraChart>();
    public static void Open() => Ensure().Show();

    void Awake() { instance = this; Build(); canvas.enabled = false; }
    void OnDestroy() { if (instance == this) { if (IsOpen) Hide(); instance = null; } }

    // ---------------------------------------------------------------- input

    static Keyboard Key => Keyboard.current;
    static Gamepad Pad => Gamepad.current;
    static Mouse Pointer => Mouse.current;

    static bool TogglePressed() => Key != null && Key.mKey.wasPressedThisFrame || Pad != null && Pad.selectButton.wasPressedThisFrame;
    static bool BackPressed() => Key != null && (Key.escapeKey.wasPressedThisFrame || Key.backspaceKey.wasPressedThisFrame)
        || Pad != null && (Pad.buttonEast.wasPressedThisFrame || Pad.startButton.wasPressedThisFrame);

    static void TrackDevice()
    {
        if (Pad != null && (Pad.wasUpdatedThisFrame && (Pad.leftStick.ReadValue().sqrMagnitude > .04f || AnyPadButton()))) padMode = true;
        else if (Key != null && Key.anyKey.wasPressedThisFrame || Pointer != null && (Pointer.delta.ReadValue().sqrMagnitude > 4f || Pointer.leftButton.wasPressedThisFrame)) padMode = false;
    }
    static bool AnyPadButton() => Pad.buttonSouth.isPressed || Pad.buttonEast.isPressed || Pad.buttonNorth.isPressed || Pad.buttonWest.isPressed
        || Pad.leftShoulder.isPressed || Pad.rightShoulder.isPressed || Pad.leftTrigger.isPressed || Pad.rightTrigger.isPressed || Pad.selectButton.isPressed || Pad.dpad.ReadValue().sqrMagnitude > 0f;

    void Update()
    {
        TrackDevice();
        if (!IsOpen)
        {
            if (TogglePressed() && Time.frameCount > closedFrame + 1 && !MraState.Busy && !MraKnot.IsPlaying && MraSession.Current != null && !MraSession.Current.Blocked) Show();
            return;
        }
        if (TogglePressed() || BackPressed()) { Hide(); return; }

        float dt = Time.unscaledDeltaTime;
        Vector2 move = Vector2.zero;
        if (Pad != null) move += Pad.leftStick.ReadValue();
        if (Key != null)
        {
            if (Key.aKey.isPressed || Key.leftArrowKey.isPressed) move.x -= 1f;
            if (Key.dKey.isPressed || Key.rightArrowKey.isPressed) move.x += 1f;
            if (Key.sKey.isPressed || Key.downArrowKey.isPressed) move.y -= 1f;
            if (Key.wKey.isPressed || Key.upArrowKey.isPressed) move.y += 1f;
        }
        Vector2 pan = Pan - move * 700f * dt;
        if (Pointer != null && Pointer.leftButton.isPressed) pan += Pointer.delta.ReadValue() * (1080f / Mathf.Max(1, Screen.height));

        float zoom = Zoom, zoomIn = 0f;
        if (Pad != null) zoomIn += Pad.rightTrigger.ReadValue() - Pad.leftTrigger.ReadValue();
        if (Key != null) { if (Key.xKey.isPressed || Key.equalsKey.isPressed) zoomIn += 1f; if (Key.zKey.isPressed || Key.minusKey.isPressed) zoomIn -= 1f; }
        zoom *= Mathf.Pow(2f, zoomIn * 1.4f * dt);
        if (Pointer != null) zoom *= Mathf.Pow(1.15f, Pointer.scroll.ReadValue().y / 120f);

        int step = 0;
        if (Key != null && (Key.eKey.wasPressedThisFrame || Key.pageDownKey.wasPressedThisFrame)) step++;
        if (Key != null && (Key.qKey.wasPressedThisFrame || Key.pageUpKey.wasPressedThisFrame)) step--;
        if (Pad != null && Pad.rightShoulder.wasPressedThisFrame) step++;
        if (Pad != null && Pad.leftShoulder.wasPressedThisFrame) step--;

        SetView(zoom, pan);
        if (step != 0) Select(step);
        if (Key != null && Key.rKey.wasPressedThisFrame || Pad != null && Pad.buttonNorth.wasPressedThisFrame) CentreOnQori();
        if (Key != null && Key.enterKey.wasPressedThisFrame || Pad != null && Pad.buttonSouth.wasPressedThisFrame) TryTravel();
        RefreshMarkers();
        RefreshText();
    }

    // ---------------------------------------------------------------- opening and closing

    public void Show()
    {
        if (IsOpen) return;
        IsOpen = true;
        previousTimeScale = Time.timeScale; Time.timeScale = 0f;
        ModalUi.Open();
        canvas.enabled = true;
        // Opens on Qori's own region (or the region he's in a chamber of), at its previous zoom.
        string here = MraSession.Current != null ? MraSession.Current.regionId : null;
        Choices();
        viewing = here != null && choices.Contains(here) ? here : choices.Count > 0 ? choices[0] : null;
        Rebuild();
        if (viewing == here) CentreOnQori();
    }

    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false; closedFrame = Time.frameCount;
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        canvas.enabled = false;
        ModalUi.Close();
    }

    // The regions the Chart can show: visited ones (the descent only after the reveal), then the
    // final assembly after the reveal.
    void Choices()
    {
        choices.Clear();
        var world = World.Load(); if (world == null) return;
        foreach (var r in world.regions)
            if (GameSave.HasFlag(MraState.VisitedFlag(r.id)) && (!r.PostReveal || MraState.Revealed)) choices.Add(r.id);
        string here = MraSession.Current != null ? MraSession.Current.regionId : null;
        if (here != null && !choices.Contains(here) && !(MraSession.Current.Blocked)) choices.Insert(0, here);
        if (MraState.Revealed && MraChartArt.Load()?.finalAssembly != null) choices.Add(WholeClimb);
    }

    void Select(int step)
    {
        if (choices.Count == 0) return;
        int i = Mathf.Max(0, choices.IndexOf(viewing));
        viewing = choices[(i + step + choices.Count) % choices.Count];
        Rebuild();
    }

    public void SelectRegion(string id) { Choices(); if (choices.Contains(id)) { viewing = id; Rebuild(); } }

    // ---------------------------------------------------------------- the view

    Vector2 ContentSize
    {
        get
        {
            var sprite = mapImage.sprite;
            Vector2 native = sprite != null ? sprite.rect.size : new Vector2(1672f, 941f);
            float fit = Mathf.Min(MapW / native.x, MapH / native.y);
            return native * fit;
        }
    }

    // Zooms about the view's centre, and keeps the drawing covering the frame (no whole-sheet pull-out).
    public void SetView(float zoom, Vector2 pan)
    {
        if (viewing == null) return;
        zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        Vector2 size = ContentSize * zoom;
        float limitX = Mathf.Max(0f, (size.x - MapW) * .5f), limitY = Mathf.Max(0f, (size.y - MapH) * .5f);
        pan = new Vector2(Mathf.Clamp(pan.x, -limitX, limitX), Mathf.Clamp(pan.y, -limitY, limitY));
        zoomOf[viewing] = zoom; panOf[viewing] = pan;
        content.localScale = new Vector3(zoom, zoom, 1f);
        content.anchoredPosition = pan;
        ScaleOverlay(zoom);
    }

    public void CentreOnQori()
    {
        if (viewing == null || viewing == WholeClimb || QoriMarker == null || !QoriMarker.gameObject.activeSelf) return;
        float zoom = Mathf.Max(Zoom, 1.6f);   // a useful scale: the local path, not the whole sheet
        SetView(zoom, -QoriMarker.anchoredPosition * zoom);
    }

    Vector2 Local(Vector2 uv) { Vector2 s = ContentSize; return new Vector2((uv.x - .5f) * s.x, (uv.y - .5f) * s.y); }

    // ---------------------------------------------------------------- building the sheet

    void Build()
    {
        canvas = GameHud.CreateCanvas("MRA Chart Canvas", 90);
        canvas.transform.SetParent(transform, false);
        var dim = GameHud.AddImage(canvas.transform, "Dim", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000f, 4000f));
        dim.color = Dim; dim.preserveAspect = false;

        var art = MraChartArt.Load();
        var frame = GameHud.AddImage(canvas.transform, "Frame", art != null ? art.frame : null, new Vector2(.5f, .5f), new Vector2(MapX, MapY), new Vector2(MapW + 120f, MapH + 120f));
        frame.preserveAspect = false;
        if (frame.sprite != null && frame.sprite.border != Vector4.zero) frame.type = Image.Type.Sliced;
        if (frame.sprite == null) frame.color = Ink;

        var vpImage = GameHud.AddImage(canvas.transform, "Viewport", null, new Vector2(.5f, .5f), new Vector2(MapX, MapY), new Vector2(MapW, MapH));
        vpImage.color = Parchment; vpImage.preserveAspect = false;
        viewport = vpImage.rectTransform;
        viewport.gameObject.AddComponent<RectMask2D>();
        content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        mapImage = GameHud.AddImage(content, "Relief", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(MapW, MapH));
        mapImage.preserveAspect = false;
        overlay = new GameObject("Navigation", typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.SetParent(content, false);

        title = TownUi.Label(canvas.transform, "Title", new Vector2(.5f, 1f), new Vector2(MapX, -48f), new Vector2(MapW, 50f), 30, TextAnchor.MiddleLeft, TownUi.Ink);
        list = TownUi.Label(canvas.transform, "Regions", new Vector2(.5f, .5f), new Vector2(ListX, 160f), new Vector2(300f, 520f), 24, TextAnchor.UpperLeft, TownUi.Ink);
        legend = TownUi.Label(canvas.transform, "Legend", new Vector2(.5f, .5f), new Vector2(ListX, -300f), new Vector2(300f, 300f), 24, TextAnchor.UpperLeft, TownUi.Dim);
        hints = TownUi.Label(canvas.transform, "Actions", new Vector2(.5f, 0f), new Vector2(0f, 34f), new Vector2(1840f, 44f), 24, TextAnchor.MiddleCenter, TownUi.Ink);
    }

    readonly List<RectTransform> lines = new List<RectTransform>(), markers = new List<RectTransform>();

    void Rebuild()
    {
        foreach (Transform child in overlay) Destroy(child.gameObject);
        lines.Clear(); markers.Clear(); QoriMarker = null; RouteSegmentsShown = 0; CavesShown = 0;
        var art = MraChartArt.Load();
        var world = World.Load();
        if (viewing == WholeClimb) mapImage.sprite = art != null ? art.finalAssembly : null;
        else
        {
            var region = world?.RegionById(viewing);
            mapImage.sprite = region != null && art != null && region.ordinal - 1 < art.maps.Length ? art.maps[region.ordinal - 1] : null;
            if (region != null) BuildOverlay(region, world, art);
        }
        mapImage.rectTransform.sizeDelta = ContentSize;
        overlay.sizeDelta = ContentSize;
        SetView(Zoom, Pan);
        RefreshMarkers(); RefreshText();
    }

    void BuildOverlay(Region region, World world, MraChartArt art)
    {
        bool page = TownState.Has(MraState.PageFlag(region.id));
        foreach (var e in region.edges)
        {
            if (!page && !GameSave.HasFlag(MraState.EdgeFlag(e.id))) continue;
            for (int i = 0; i < e.chartLine.Length - 1; i++)
            {
                Line(Local(e.chartLine[i]), Local(e.chartLine[i + 1]), Parchment, Stroke + Halo * 2f);
                Line(Local(e.chartLine[i]), Local(e.chartLine[i + 1]), Route, Stroke);
                RouteSegmentsShown++;
            }
        }
        // The lit Waymark (its level charted), and shrines whose relic Qori holds.
        foreach (var n in region.nodes)
        {
            if (n.waymark && GameSave.IsCharted("mra:" + region.id)) Marker(art?.waymark, n.chartUv, Ink, "Waymark");
            if (!string.IsNullOrEmpty(n.ability) && GameSave.HasRelic(n.ability)) Marker(art?.shrine, n.chartUv, Ink, "Shrine");
        }
        // Side chambers: only once discovered; pinned to their entrance.
        foreach (var c in world.ChambersOf(region.id))
            if (GameSave.HasFlag("mra:" + c.id + ":discovered")) { Marker(art?.cave, c.chartUv, GameSave.HasFlag(c.rewardId) ? Ink * .7f : Ink, c.name); CavesShown++; }
        if (MraSession.Current != null && MraSession.Current.regionId == region.id)
        {
            QoriMarker = Marker(art?.qori, Vector2.one * .5f, Amber, "Qori");
            QoriMarker.SetAsLastSibling();
        }
    }

    void Line(Vector2 a, Vector2 b, Color color, float width)
    {
        var img = GameHud.AddImage(overlay, "Route", null, new Vector2(.5f, .5f), (a + b) * .5f, new Vector2(Vector2.Distance(a, b) + width * .5f, width));
        img.color = color; img.preserveAspect = false; img.raycastTarget = false;
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        lines.Add(img.rectTransform);
    }

    RectTransform Marker(Sprite sprite, Vector2 uv, Color color, string label)
    {
        var img = GameHud.AddImage(overlay, "Marker " + label, sprite, new Vector2(.5f, .5f), Local(uv), new Vector2(MarkerSize, MarkerSize));
        img.raycastTarget = false;
        if (sprite == null) { img.color = color; img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f); }   // a shape-coded diamond
        markers.Add(img.rectTransform);
        return img.rectTransform;
    }

    // Lines and markers keep their screen size at any zoom.
    void ScaleOverlay(float zoom)
    {
        foreach (var m in markers) if (m != null) m.localScale = Vector3.one / zoom;
        foreach (var l in lines) if (l != null) l.localScale = new Vector3(1f, 1f / zoom, 1f);
    }

    void RefreshMarkers()
    {
        if (QoriMarker == null) return;
        var session = MraSession.Current;
        bool known = false;
        if (session != null && session.InChamber) { QoriUv = session.Chamber.chartUv; known = true; }
        else if (MraRouteTracker.Current != null && MraRouteTracker.Current.HasFix) { QoriUv = MraRouteTracker.Current.Fix.uv; known = true; }
        QoriMarker.gameObject.SetActive(known);
        if (known) QoriMarker.anchoredPosition = Local(QoriUv);
    }

    // ---------------------------------------------------------------- Waymark travel

    // Standing at a lit Waymark, Qori can travel to another region's lit Waymark.
    public bool CanTravel(out Region to, out Node waymark)
    {
        to = null; waymark = null;
        var world = World.Load(); var session = MraSession.Current;
        if (world == null || session == null || session.InChamber || viewing == null || viewing == WholeClimb || viewing == session.regionId) return false;
        if (!AtLitWaymark(session.Region)) return false;
        to = world.RegionById(viewing);
        if (to == null || !GameSave.IsCharted("mra:" + to.id)) return false;
        waymark = to.NodeById(to.primaryWaymark);
        return waymark != null && !string.IsNullOrEmpty(waymark.checkpointId);
    }

    static bool AtLitWaymark(Region region)
    {
        var node = region?.NodeById(region.primaryWaymark);
        var qori = FindAnyObjectByType<PlayerMovement>();
        return node != null && qori != null && GameSave.IsCharted("mra:" + region.id) && Vector2.Distance(qori.transform.position, node.spawn) < 3f;
    }

    public bool TryTravel()
    {
        if (!CanTravel(out var to, out var waymark)) return false;
        var qori = FindAnyObjectByType<PlayerMovement>();
        Hide();
        AreaTransition.Travel(to.scene, waymark.checkpointId, qori, "");
        return true;
    }

    // ---------------------------------------------------------------- text

    void RefreshText()
    {
        var world = World.Load();
        string name = viewing == WholeClimb ? "The whole climb" : world?.RegionById(viewing)?.name ?? "";
        title.text = "The Chart  -  " + name;
        var sb = new System.Text.StringBuilder();
        foreach (string id in choices)
        {
            string n = id == WholeClimb ? "The whole climb" : world.RegionById(id).name;
            sb.Append(id == viewing ? "<color=#D79835>> " + n + "</color>\n" : "   " + n + "\n");
        }
        list.text = sb.ToString();
        legend.text = "<color=#D79835>Amber mark</color>: Qori\nRing: lit Waymark\nLeaf cup: shrine\nArch: side passage\nLine: known path";
        string travel = CanTravel(out var to, out _) ? (padMode ? "    (A) Travel to " : "    Enter: travel to ") + to.name : "";
        hints.text = padMode
            ? "(L) Pan    LT / RT Zoom    LB / RB Region    (Y) Qori    (B) Close" + travel
            : "WASD / drag: pan    Z / X / wheel: zoom    Q / E: region    R: Qori    M / Esc: close" + travel;
    }
}

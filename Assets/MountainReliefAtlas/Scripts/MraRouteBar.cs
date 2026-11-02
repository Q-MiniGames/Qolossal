using System.Collections.Generic;
using Mra;
using UnityEngine;
using UnityEngine.UI;

// The route bar at the top middle of the screen: how far Qori is through the current region, from
// its entry (left end) to its exit (right end), the distance still to walk, and markers for what
// lies along the way. Distances are measured along the region's route (the gameplay surface of
// each edge, in order), not straight across, so a switchback counts its full length; one unit is
// shown as one metre. Qori's place on it comes from the same projection the Chart uses (MraRoute).
// Markers sit at their node's distance along the route: side passages (a found one with its icon,
// dimmed once its find is taken; one not yet found as the unexplored mark; a secret one only after
// it's found), the shrine, the Waymark, and small pips for the resting places. The bar is the
// accepted HUD_Resource_Bar frame (sliced at run time so its leaf caps keep their shape) with its
// mint fill for the distance walked; the icons are the accepted Chart icons.
// Made by MraSession in every region scene (not in side chambers or homes); it fades out while a
// menu, the Chart, a knot's stir, the reveal, the ending or the listening seat has the screen.
[DisallowMultipleComponent]
public sealed class MraRouteBar : MonoBehaviour
{
    // 1920x1080 reference px.
    const float MaxWidth = 820f, SideClear = 560f, Top = 80f, TrackH = 15f, IconSize = 44f, QoriSize = 56f, PipSize = 12f, TierRise = 40f;
    // HUD_Resource_Bar (512x128 px): the channel inside the frame is x 75..437, y 52..74 from the bottom.
    const float FramePx = 512f, FrameHPx = 128f, ChannelLeftPx = 75f, ChannelHPx = 22f, CapReachPx = 54f;
    static readonly Color Plate = new Color(.1f, .08f, .06f, .62f), Rim = new Color(.93f, .88f, .76f, .85f),
        Walked = new Color(1f, .74f, .3f, .95f), Backing = new Color(.1f, .08f, .06f, .42f), PipColor = new Color(.93f, .88f, .76f, 1f);

    public static MraRouteBar Current { get; private set; }
    /// <summary>Qori's progress from entry (0) to exit (1).</summary>
    public float Fraction { get; private set; }
    public float Remaining => Mathf.Max(0f, Total - walked);
    public float Total { get; private set; }
    /// <summary>The markers drawn now (caves, shrine, Waymark, resting places).</summary>
    public int MarkersShown { get; private set; }
    public bool Showing => group != null && group.alpha > .5f;
    // Pixel bounds in IMGUI's top-left coordinates, shared with the armory hint.
    readonly Vector3[] screenCorners = new Vector3[4];
    public Rect ScreenBounds
    {
        get
        {
            if (backing == null) return Rect.zero;
            backing.rectTransform.GetWorldCorners(screenCorners);
            var lo = RectTransformUtility.WorldToScreenPoint(null, screenCorners[0]);
            var hi = RectTransformUtility.WorldToScreenPoint(null, screenCorners[2]);
            return new Rect(lo.x, Screen.height - hi.y, hi.x - lo.x, hi.y - lo.y);
        }
    }

    Region region;
    Transform qori;
    float[] edgeStart, edgeLength;
    float walked, shownFraction = -1f, width;
    int hint = -1;
    bool markersStale;
    float laidOutFor;
    Image rim, backing, fillImage;
    RectTransform track;
    float capReach;
    Canvas canvas; CanvasGroup group; RectTransform bar, fill, marker; Text remaining;
    readonly List<GameObject> markers = new List<GameObject>();
    MraChartArt art;

    public static void Ensure(Region region)
    {
        if (region == null) return;   // one per region scene, made by its MraSession
        new GameObject("MRA Route Bar").AddComponent<MraRouteBar>().region = region;
    }

    void Awake() { Current = this; GameSave.FlagsChanged += MarkStale; GameSave.WorldChanged += MarkStale; }
    void OnDestroy() { GameSave.FlagsChanged -= MarkStale; GameSave.WorldChanged -= MarkStale; if (Current == this) Current = null; }
    void MarkStale() => markersStale = true;

    void Start()
    {
        if (region == null || region.edges == null || region.edges.Length == 0) { enabled = false; return; }
        var player = FindAnyObjectByType<PlayerMovement>();
        if (player != null) qori = player.transform;
        // Cumulative length along the route: edge i starts where edges 0..i-1 end.
        edgeStart = new float[region.edges.Length]; edgeLength = new float[region.edges.Length];
        float sum = 0f;
        for (int i = 0; i < region.edges.Length; i++) { edgeStart[i] = sum; edgeLength[i] = MraRoute.Length(region.edges[i].points); sum += edgeLength[i]; }
        Total = sum;
        art = MraChartArt.Load();
        Build();
    }

    // Distance along the route to a node: the start of the edge it begins (the exit: the whole route).
    float AlongNode(string nodeId)
    {
        for (int i = 0; i < region.edges.Length; i++) if (region.edges[i].source == nodeId) return edgeStart[i];
        for (int i = 0; i < region.edges.Length; i++) if (region.edges[i].target == nodeId) return edgeStart[i] + edgeLength[i];
        return -1f;
    }

    float X(float along) => (Total > 0f ? Mathf.Clamp01(along / Total) - .5f : 0f) * width;

    void Build()
    {
        canvas = GameHud.CreateCanvas("Route Bar Canvas", 11);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false; group.blocksRaycasts = false;
        // A soft dark plate under the whole bar, so it reads on bright sky and pale rock.
        backing = GameHud.AddImage(canvas.transform, "Backing", null, new Vector2(.5f, 1f), Vector2.zero, Vector2.zero);
        backing.color = Backing; backing.preserveAspect = false;
        // The bar's channel: the dark track, the walked fill, the frame over both, then the markers and Qori.
        var holder = GameHud.AddImage(canvas.transform, "Bar", null, new Vector2(.5f, 1f), new Vector2(0f, -Top), Vector2.zero);
        holder.enabled = false;
        bar = holder.rectTransform;
        var plate = GameHud.AddImage(bar, "Track", null, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        plate.color = Plate; plate.preserveAspect = false;
        track = plate.rectTransform;
        var walkedImage = GameHud.AddImage(bar, "Walked", Sliced(art?.routeFill, new Rect(76f, 52f, 361f, 22f), new Vector4(8f, 0f, 8f, 0f)), new Vector2(0f, .5f), Vector2.zero, Vector2.zero);
        walkedImage.preserveAspect = false;
        if (walkedImage.sprite != null) { walkedImage.type = Image.Type.Sliced; walkedImage.pixelsPerUnitMultiplier = ChannelHPx / TrackH; }
        else walkedImage.color = Walked;
        fillImage = walkedImage;
        fill = walkedImage.rectTransform; fill.pivot = new Vector2(0f, .5f);
        rim = GameHud.AddImage(bar, "Frame", Sliced(art?.routeFrame, new Rect(0f, 0f, FramePx, FrameHPx), new Vector4(76f, 0f, 76f, 0f)), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
        rim.preserveAspect = false;
        if (rim.sprite != null) { rim.type = Image.Type.Sliced; rim.pixelsPerUnitMultiplier = ChannelHPx / TrackH; }
        else { rim.color = Rim; rim.transform.SetSiblingIndex(0); }   // no art: a plain rim behind the track

        remaining = TownUi.Label(canvas.transform, "Remaining", new Vector2(.5f, 1f), Vector2.zero, new Vector2(150f, 40f), 28, TextAnchor.MiddleLeft, TownUi.Ink);

        var qoriIcon = GameHud.AddImage(bar, "Qori", art != null ? art.qori : null, new Vector2(.5f, .5f), Vector2.zero, Vector2.one * QoriSize);
        if (qoriIcon.sprite == null) qoriIcon.color = TownUi.Mint;
        marker = qoriIcon.rectTransform;
        Layout();
    }

    // The accepted art has no slicing borders: a sliced copy is made here, so the caps keep their
    // painted shape at any bar length (the pixels are untouched).
    static Sprite Sliced(Sprite source, Rect rect, Vector4 border)
    {
        if (source == null) return null;
        rect.position += source.rect.position;
        return Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    // Narrower on screens less wide than 16:9 (the canvas keeps about 1080 tall), clear of the
    // hearts on the left and the Amber on the right; redone when the canvas width changes.
    void Layout()
    {
        laidOutFor = ((RectTransform)canvas.transform).rect.width;
        width = Mathf.Clamp((laidOutFor > 0f ? laidOutFor : 1920f) - SideClear * 2f, 360f, MaxWidth);
        float k = TrackH / ChannelHPx;
        bar.sizeDelta = track.sizeDelta = new Vector2(width, TrackH);
        rim.rectTransform.sizeDelta = rim.sprite != null ? new Vector2(width + 2f * ChannelLeftPx * k, FrameHPx * k) : new Vector2(width + 6f, TrackH + 6f);
        rim.rectTransform.anchoredPosition = rim.sprite != null ? new Vector2(0f, -k) : Vector2.zero;   // the channel sits 1 px below the frame's middle
        capReach = rim.sprite != null ? CapReachPx * k : 3f;
        remaining.rectTransform.anchoredPosition = new Vector2(width * .5f + capReach + 14f + 75f, -Top);
        RebuildMarkers();
    }

    // Saved flags decide which markers show, so they're rebuilt when a cave is found or a find taken.
    void RebuildMarkers()
    {
        foreach (var m in markers) Destroy(m);
        markers.Clear();
        var world = World.Load();
        var icons = new List<(float x, Sprite sprite, Color tint, string name)>();
        foreach (var n in region.nodes)
        {
            float along = AlongNode(n.id);
            if (along < 0f) continue;
            if (n.waymark) icons.Add((X(along), art?.waymark, Color.white, "Waymark " + n.name));
            else if (!string.IsNullOrEmpty(n.ability)) icons.Add((X(along), art?.shrine, Color.white, "Shrine " + n.name));
            else if (n.checkpoint) Pip(X(along), "Rest " + n.name);
        }
        if (world != null)
            foreach (var c in world.ChambersOf(region.id))
            {
                float along = AlongNode(c.entranceNode);
                if (along < 0f) continue;
                bool found = GameSave.HasFlag(c.DiscoveredFlag);
                if (!found && c.type == "secret") continue;   // secrets stay secret until found
                if (found) icons.Add((X(along), art?.cave, GameSave.HasFlag(c.rewardId) ? new Color(1f, 1f, 1f, .5f) : Color.white, "Cave " + c.name));
                else icons.Add((X(along), art?.unexplored, new Color(1f, 1f, 1f, .7f), "Cave unexplored"));
            }
        // Left to right; an icon that would overlap the one before steps up a tier.
        icons.Sort((a, b) => a.x.CompareTo(b.x));
        var tierEnd = new List<float>();
        foreach (var icon in icons)
        {
            int tier = 0;
            while (tier < tierEnd.Count && icon.x - tierEnd[tier] < IconSize * .8f) tier++;
            if (tier == tierEnd.Count) tierEnd.Add(float.NegativeInfinity);
            tierEnd[tier] = icon.x;
            var image = GameHud.AddImage(bar, icon.name, icon.sprite, new Vector2(.5f, .5f),
                new Vector2(icon.x, TrackH * .5f + IconSize * .5f + 7f + tier * TierRise), Vector2.one * IconSize);
            image.color = icon.sprite != null ? icon.tint : new Color(PipColor.r, PipColor.g, PipColor.b, icon.tint.a);
            markers.Add(image.gameObject);
        }
        MarkersShown = markers.Count;
        // The plate spans the track, the icon tiers above it and the distance on the right.
        float rise = TrackH * .5f + 9f + IconSize + Mathf.Max(0, tierEnd.Count - 1) * TierRise, below = QoriSize * .5f + 2f;
        float left = width * .5f + capReach + 12f, right = width * .5f + capReach + 14f + 150f;
        backing.rectTransform.sizeDelta = new Vector2(left + right, rise + below + 8f);
        // A second marker tier can exceed the old fixed 80px top margin.
        float safeTop = (Screen.height - Screen.safeArea.yMax) / canvas.scaleFactor;
        float top = Mathf.Max(Top, rise + safeTop + 16f);
        bar.anchoredPosition = new Vector2(0f, -top);
        remaining.rectTransform.anchoredPosition = new Vector2(width * .5f + capReach + 89f, -top);
        backing.rectTransform.anchoredPosition = new Vector2((right - left) * .5f, -top + (rise - below) * .5f);
        marker.SetAsLastSibling();   // Qori on top
        markersStale = false;
    }

    void Pip(float x, string name)
    {
        var pip = GameHud.AddImage(bar, name, null, new Vector2(.5f, .5f), new Vector2(x, 0f), Vector2.one * PipSize);
        pip.color = PipColor; pip.preserveAspect = false;
        pip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        markers.Add(pip.gameObject);
    }

    void Update()
    {
        if (bar == null) return;
        bool hidden = MraState.Busy || ListeningSpot.IsSitting || MraKnot.IsPlaying || MraReveal.IsPlaying || MraEnding.IsPlaying
            || MraSession.Current == null || MraSession.Current.Blocked;
        group.alpha = Mathf.MoveTowards(group.alpha, hidden ? 0f : 1f, Time.unscaledDeltaTime * 5f);
        if (!Mathf.Approximately(((RectTransform)canvas.transform).rect.width, laidOutFor)) Layout();
        else if (markersStale) RebuildMarkers();
        if (qori == null) { marker.gameObject.SetActive(false); return; }

        var tracker = MraRouteTracker.Current;
        MraRoute.Fix fix = tracker != null && tracker.HasFix ? tracker.Fix : MraRoute.Locate(region, qori.position, hint);
        if (fix.edge < 0) return;
        hint = fix.edge;
        walked = edgeStart[fix.edge] + fix.progress * edgeLength[fix.edge];
        Fraction = Total > 0f ? Mathf.Clamp01(walked / Total) : 0f;
        // Glide to the new place rather than jump when the nearest edge changes.
        shownFraction = shownFraction < 0f ? Fraction : Mathf.Lerp(shownFraction, Fraction, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        marker.anchoredPosition = new Vector2((shownFraction - .5f) * width, 2f);
        // A sliced fill can't be shorter than its two rounded ends.
        float filled = shownFraction * width;
        fill.gameObject.SetActive(fillImage.type != Image.Type.Sliced || filled >= 2f * 8f * TrackH / ChannelHPx);
        fill.sizeDelta = new Vector2(filled, TrackH);
        remaining.text = Mathf.CeilToInt(Remaining) + " m";
    }
}

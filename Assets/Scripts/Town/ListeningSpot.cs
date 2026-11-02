using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The listening spot (world redesign proposal v2, section 5.3): a bench under a tree. Press Up
// beside it to sit: Qori's controls and the HUD go quiet, the camera eases out to frame the spot,
// and a small player appears in the bottom-right corner: one line with the chosen track. Tracks are
// unlocked by song shells found around the world (a flag "shell:<id>"); a track with no shell is
// known from the start. Left/Right choose a track, Confirm plays or stops, H (north button) hides or
// shows the player, Cancel gets up. At the Qvale bench, sitting fades to the painted listening
// retreat, and Up/Down choose its place (seven, fading between them; elsewhere Up/Down choose tracks
// too). While a track plays the player fades away after a few idle seconds so the view is left
// alone; any key brings it back. A track without a clip (no Suno music yet) uses a placeholder.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class ListeningSpot : MonoBehaviour
{
    [Serializable]
    public sealed class Track
    {
        public string title = "Track";
        [Tooltip("The song shell that unlocks it (flag shell:<id>); empty: known from the start.")] public string shell = "";
        public AudioClip clip;
        [Tooltip("For the placeholder when there's no clip.")] public int seed = 1; public float bpm = 72f;
    }

    public List<Track> tracks = new List<Track>();
    public Vector2 frameCentre; public float frameSize = 6.5f;
    [Tooltip("Seat Qori on the bench (`seat`: the middle of the seat's top); off: he sits where he stands.")] public bool hasSeat;
    public Vector2 seat;
    Vector2 standAt; QoriAnimator pose;
    public static bool IsSitting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsSitting = false;

    bool near; Rigidbody2D qori; int selected, playing = -1, openedFrame;
    AudioSource source; float targetVolume;
    Canvas canvas; CanvasGroup group; Text title, count, hint, place; Image bar;
    Camera view; CameraFollow follow; Vector3 cameraFrom; float sizeFrom, ease; bool leaving;
    Canvas hud;
    bool retreat;   // the painted listening retreat shows (Batch 25a-11 and 26)
    Sprite barSprite;
    const float IdleFade = 4f;   // seconds without input, while a track plays, before the player fades
    float idle, shown = 1f; bool hidden;

    public bool Sitting => IsSitting && enabled && canvas != null && canvas.enabled;
    public int Playing => playing;
    public AudioSource Source => source;
    public bool IsUnlocked(int i) => string.IsNullOrEmpty(tracks[i].shell) || TownState.Has("shell:" + tracks[i].shell);

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true; source.playOnAwake = false; source.spatialBlend = 0f; source.volume = 0f;
        source.outputAudioMixerGroup = Sfx.MusicGroup;
    }

    static Rigidbody2D QoriOf(Collider2D other) =>
        other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null ? other.attachedRigidbody : null;
    void OnTriggerEnter2D(Collider2D other) { var q = QoriOf(other); if (q != null) { near = true; qori = q; } }
    void OnTriggerExit2D(Collider2D other) { if (QoriOf(other) != null) near = false; }

    // Sits down (the Up key calls this; so can tests).
    public void Sit()
    {
        if (IsSitting) return;
        if (hasSeat) retreat = ListeningRetreat.Open();   // fades to the retreat; the world view eases out behind it
        if (retreat) barSprite = ListeningRetreat.PlayerBar;
        if (canvas == null) Build();
        Sfx.Play("Qori_Sit");
        IsSitting = true; leaving = false; openedFrame = Time.frameCount; selected = Mathf.Max(0, playing);
        VistaZone.Suspended = true;   // a zoom zone around the bench lets go while the bench frames the view
        // Onto the seat: held still there in the Sit pose until he gets up.
        if (qori != null)
        {
            standAt = qori.position;
            pose = qori.GetComponentInChildren<QoriAnimator>();
            if (pose != null) pose.Hold("Sit");
            if (hasSeat)
            {
                // His hip joint (the thighs' pivot) just above the seat's top, so the thighs rest on it.
                var hip = pose != null ? Find(pose.transform, "ThighNear") : null;
                Vector2 offset = hip != null ? (Vector2)(hip.position - qori.transform.position) : new Vector2(0f, -.3f);
                qori.linearVelocity = Vector2.zero; qori.simulated = false;
                qori.transform.position = new Vector3(seat.x - offset.x, seat.y + .07f - offset.y, qori.transform.position.z);
            }
        }
        ModalUi.Open(); canvas.enabled = true;
        view = Camera.main; follow = view != null ? view.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.enabled = false;
        cameraFrom = view.transform.position; sizeFrom = view.orthographicSize; ease = 0f;
        var gameHud = FindAnyObjectByType<GameHud>(); hud = gameHud != null ? gameHud.GetComponentInChildren<Canvas>() : null;
        if (hud != null) hud.enabled = false;
        idle = 0f; hidden = false; shown = 1f;
        Refresh();
    }

    // Gets up: the window closes and the view returns to Qori; the music fades out.
    public void Leave()
    {
        if (!IsSitting || leaving) return;
        leaving = true; canvas.enabled = false; ease = 0f;
        Sfx.Play("UI_Back");
        if (retreat) { ListeningRetreat.Close(); retreat = false; }
        // Up off the seat, back where he stood; the view then eases back to him.
        if (pose != null) pose.Release();
        if (qori != null && hasSeat) { qori.transform.position = standAt; qori.simulated = true; qori.linearVelocity = Vector2.zero; }
        cameraFrom = view.transform.position; sizeFrom = view.orthographicSize;
        Play(-1);
    }

    public void Play(int i)
    {
        if (i >= 0 && !IsUnlocked(i)) { TownHud.Toast("Find its song shell to hear it."); return; }
        if (i == playing || i < 0) { playing = -1; targetVolume = 0f; return; }
        var t = tracks[i];
        if (t.clip == null) t.clip = PlaceholderMusic.Make(t.seed, t.bpm);
        source.clip = t.clip; source.time = 0f; source.Play();
        playing = i; targetVolume = .8f;
    }

    static Transform Find(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t) { var f = Find(c, name); if (f != null) return f; }
        return null;
    }

    void Build()
    {
        canvas = GameHud.CreateCanvas("Listening Canvas", 85);
        canvas.transform.SetParent(transform, false);
        group = canvas.gameObject.AddComponent<CanvasGroup>(); group.interactable = false; group.blocksRaycasts = false;
        // One line in the bottom-right corner, where the retreat painting is kept calm: the leaf bar
        // (UI_ListeningSpot_TrackRow_Playing, carried by the retreat) with the track, and a hint above.
        var anchor = new Vector2(1f, 0f);
        const float x = -260f, y = 104f, w = 440f, h = 72f;
        var sprite = barSprite;
        if (sprite != null)
        {
            bar = GameHud.AddImage(canvas.transform, "Bar", sprite, anchor, new Vector2(x, y), new Vector2(w, h));
            bar.preserveAspect = false; bar.type = Image.Type.Sliced;
        }
        else bar = TownUi.Panel(canvas.transform, "Bar", anchor, new Vector2(x, y), new Vector2(w, h + 40f));
        title = TownUi.Label(canvas.transform, "Track", anchor, new Vector2(x - 18f, y), new Vector2(w - 150f, h - 16f), 25, TextAnchor.MiddleCenter, TownUi.PanelInk, false);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;
        count = TownUi.Label(canvas.transform, "Count", anchor, new Vector2(x + w * .5f - 68f, y), new Vector2(80f, h - 16f), 18, TextAnchor.MiddleCenter, TownUi.PanelDim, false);
        // The place, just above the bar (the retreat only), and the keys above that. White with a
        // shadow: the scenes run from pale paving to night.
        place = TownUi.Label(canvas.transform, "Place", anchor, new Vector2(x, y + 52f), new Vector2(w + 60f, 30f), 20, TextAnchor.MiddleCenter, TownUi.Ink);
        place.enabled = barSprite != null && ListeningRetreat.Use;
        hint = TownUi.Label(canvas.transform, "Hint", anchor, new Vector2(x - 60f, y + (place.enabled ? 84f : 56f)), new Vector2(w + 150f, 30f), 17, TextAnchor.MiddleCenter, TownUi.Ink);
        hint.text = place.enabled ? "◀ ▶ song   ▲ ▼ place   Confirm: play/stop   H: hide   Cancel: get up"
                                  : "◀ ▶ choose    Confirm: play / stop    H: hide    Cancel: get up";
        canvas.enabled = false;
    }

    void Refresh()
    {
        bool known = IsUnlocked(selected);
        string name = known ? tracks[selected].title : "<color=#8C8577>???  a song shell</color>";
        title.text = "◀  " + (selected == playing ? "♪ " : "") + name + "  ▶";
        count.text = (selected + 1) + " / " + tracks.Count;
        if (place.enabled) place.text = "▲  " + ListeningRetreat.Titles[ListeningRetreat.Current] + "  ▼";
    }

    void Update()
    {
        source.volume = Mathf.MoveTowards(source.volume, targetVolume * SfxSettings.Music, Time.deltaTime / 1.5f);
        if (source.volume <= 0f && targetVolume <= 0f && source.isPlaying) source.Stop();

        if (!IsSitting)
        {
            if (near && !ModalUi.IsOpen && !GamePauseMenu.IsPaused && TownInput.Up()) Sit();
            return;
        }
        // Ease the camera out to the spot, or back to Qori when leaving.
        ease = Mathf.MoveTowards(ease, 1f, Time.deltaTime / 1.4f);
        float k = Mathf.SmoothStep(0f, 1f, ease);
        Vector3 to = leaving ? new Vector3(qori.position.x, qori.position.y + 1f, cameraFrom.z) : new Vector3(frameCentre.x, frameCentre.y, cameraFrom.z);
        view.transform.position = Vector3.Lerp(cameraFrom, to, k);
        view.orthographicSize = Mathf.Lerp(sizeFrom, leaving ? VistaZone.NormalSize : frameSize, k);
        if (leaving)
        {
            if (ease < 1f) return;
            IsSitting = false; leaving = false;
            VistaZone.Suspended = false;
            if (follow != null) follow.enabled = true;
            if (hud != null) hud.enabled = true;
            ModalUi.Close();
            return;
        }
        if (Time.frameCount == openedFrame) return;
        if (TownInput.Cancel()) { Leave(); return; }
        bool up = TownInput.Up(), down = TownInput.Down();
        bool prev = TownInput.Left() || !retreat && up, next = TownInput.Right() || !retreat && down;
        int placeBy = retreat ? (up ? -1 : 0) + (down ? 1 : 0) : 0;
        bool confirm = TownInput.Confirm(), toggle = TownInput.Toggle();
        if (toggle) { hidden = !hidden && shown > .5f; idle = 0f; }
        else if (prev || next || confirm || placeBy != 0)
        {
            // While hidden or faded, the first key only brings the player back.
            bool wasAway = hidden || shown < .5f;
            hidden = false; idle = 0f;
            if (!wasAway)
            {
                if (prev) { selected = (selected + tracks.Count - 1) % tracks.Count; Sfx.Play("UI_Move"); }
                if (next) { selected = (selected + 1) % tracks.Count; Sfx.Play("UI_Move"); }
                if (confirm) { Sfx.Play("UI_Confirm"); Play(selected); }
                if (placeBy != 0) { ListeningRetreat.Step(placeBy); Sfx.Play("UI_Move"); }   // fades to the next place; the music plays on
            }
        }
        else idle += Time.unscaledDeltaTime;
        // Away when hidden, or after a few idle seconds while music plays; back on any key.
        bool away = hidden || playing >= 0 && idle > IdleFade;
        shown = Mathf.MoveTowards(shown, away ? 0f : 1f, Time.unscaledDeltaTime / (away ? .8f : .2f));
        group.alpha = Mathf.SmoothStep(0f, 1f, shown);
        Refresh();
    }

    void OnGUI()
    {
        if (!near || IsSitting || ModalUi.IsOpen || GamePauseMenu.IsPaused) return;
        var cam = Camera.main; if (cam == null) return;
        Vector3 p = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(Screen.height * .022f), fontStyle = FontStyle.Bold };
        style.normal.textColor = new Color(.75f, 1f, .9f);
        GUI.Label(new Rect(p.x - 150f, Screen.height - p.y - 30f, 300f, 40f), "▲ Sit and listen", style);
    }

    void OnDestroy() { if (IsSitting) { IsSitting = false; ModalUi.Close(); } if (retreat) ListeningRetreat.Close(); }
}

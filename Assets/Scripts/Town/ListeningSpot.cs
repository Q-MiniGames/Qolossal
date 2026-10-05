using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The listening spot (world redesign proposal v2, section 5.3): a bench under a tree. Press Up
// beside it to sit: Qori's controls and the HUD go quiet, the camera eases out to frame the spot,
// and a list of tracks appears. Tracks are unlocked by song shells found around the world (a flag
// "shell:<id>"); a track with no shell is known from the start. Up and Down choose, Confirm plays or
// stops, Cancel gets up. A track without a clip (no Suno music yet) uses a placeholder.
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
    Canvas canvas; Text heading; readonly List<Text> rows = new List<Text>(); Text hint;
    Camera view; CameraFollow follow; Vector3 cameraFrom; float sizeFrom, ease; bool leaving;
    Canvas hud;

    public bool Sitting => IsSitting && enabled && canvas != null && canvas.enabled;
    public int Playing => playing;
    public AudioSource Source => source;
    public bool IsUnlocked(int i) => string.IsNullOrEmpty(tracks[i].shell) || TownState.Has("shell:" + tracks[i].shell);

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true; source.playOnAwake = false; source.spatialBlend = 0f; source.volume = 0f;
    }

    static Rigidbody2D QoriOf(Collider2D other) =>
        other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<PlayerMovement>() != null ? other.attachedRigidbody : null;
    void OnTriggerEnter2D(Collider2D other) { var q = QoriOf(other); if (q != null) { near = true; qori = q; } }
    void OnTriggerExit2D(Collider2D other) { if (QoriOf(other) != null) near = false; }

    // Sits down (the Up key calls this; so can tests).
    public void Sit()
    {
        if (IsSitting) return;
        if (canvas == null) Build();
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
        Refresh();
    }

    // Gets up: the window closes and the view returns to Qori; the music fades out.
    public void Leave()
    {
        if (!IsSitting || leaving) return;
        leaving = true; canvas.enabled = false; ease = 0f;
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
        float h = 250f + tracks.Count * 64f;
        TownUi.Panel(canvas.transform, "Panel", new Vector2(1f, .5f), new Vector2(-400f, 0f), new Vector2(700f, h));
        heading = TownUi.Label(canvas.transform, "Title", new Vector2(1f, .5f), new Vector2(-400f, h * .5f - 80f), new Vector2(560f, 56f), 38, TextAnchor.MiddleCenter, TownUi.PanelMint, false);
        heading.text = "The Listening Tree";
        for (int i = 0; i < tracks.Count; i++)
            rows.Add(TownUi.Label(canvas.transform, "Track " + i, new Vector2(1f, .5f), new Vector2(-400f, h * .5f - 150f - i * 64f), new Vector2(560f, 56f), 32, TextAnchor.MiddleLeft, TownUi.PanelInk, false));
        hint = TownUi.Label(canvas.transform, "Hint", new Vector2(1f, .5f), new Vector2(-400f, -h * .5f + 92f), new Vector2(560f, 40f), 24, TextAnchor.MiddleCenter, TownUi.PanelDim, false);
        hint.text = "Confirm: play / stop     Cancel: get up";
        canvas.enabled = false;
    }

    void Refresh()
    {
        for (int i = 0; i < tracks.Count; i++)
        {
            string mark = i == playing ? "♪ " : "   ";
            rows[i].text = (i == selected ? "▶ " : "   ") + mark + (IsUnlocked(i) ? tracks[i].title : "<color=#8C8577>???  (a song shell, somewhere)</color>");
            rows[i].color = i == selected ? TownUi.PanelInk : TownUi.PanelDim;
        }
    }

    void Update()
    {
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.deltaTime / 1.5f);
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
        if (TownInput.Up()) selected = (selected + tracks.Count - 1) % tracks.Count;
        if (TownInput.Down()) selected = (selected + 1) % tracks.Count;
        if (TownInput.Confirm()) Play(selected);
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

    void OnDestroy() { if (IsSitting) { IsSitting = false; ModalUi.Close(); } }
}

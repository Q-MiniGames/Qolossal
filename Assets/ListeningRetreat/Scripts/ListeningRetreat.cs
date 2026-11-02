using UnityEngine;

// One listening-retreat scene (Batch 25a-11 and Batch 26, Claude reviews 64-67): a static-camera
// painted place with Qori seen from behind on the bench, looking out over the view. Each scene is a
// prefab under Resources (ListeningRetreat_<id>, built by ListeningRetreatBuilder from its
// registration), drawn by its own camera far from the world. ListeningRetreatDirector shows one at a
// time, fading through black when the player sits, gets up or picks another place.
//
// Motion stays inside each registration's limits: clouds and mist drift and wrap, the far and middle
// distance drift a few pixels, the weather tile scrolls inside its mask, props and foreground pieces
// sway at about two thirds of their registered range, lamps flicker within their opacity range, and
// Qori's head sways up to 1.5 degrees with his ears up to 6. His body is fixed: the backrest covers it.
public sealed class ListeningRetreat : MonoBehaviour
{
    public const float Ppu = 100f, CanvasW = 25.6f, CanvasH = 14.4f;
    [Tooltip("Off: the bench keeps the old in-world view.")] public static bool Use = true;

    // The places, in the order the player steps through them (ids are the art prefixes).
    public static readonly string[] Scenes = { "OverlookTerrace", "OverlookNight", "CradleMeadow", "TerracesRain", "CausewayDusk", "RibwoodGlade", "HeightsClouds" };
    public static readonly string[] Titles = { "The Overlook", "The Overlook by Night", "The Cradle Meadow", "Rain on the Terraces", "The Causeway at Dusk", "The Ribwood Glade", "Above the Clouds" };
    public static string ResourcePath(int i) => "ListeningRetreat/ListeningRetreat_" + Scenes[i];

    public Camera view;
    public Transform clouds, mist, weather, head, earFar, earNear;
    public float weatherPeriod = 4.8f, weatherSpeed = .22f, cloudSpeed = .05f, mistSpeed = .1f;
    [Tooltip("Far/Mid groups and the pixels each drifts either way.")] public Transform[] drifters; public float[] driftPx;
    [Tooltip("Props and foreground pieces, and the degrees each sways either way.")] public Transform[] swayers; public float[] swayDeg;
    [Tooltip("Lamp glows and their opacity range (low, high).")] public SpriteRenderer[] glows; public Vector2[] glowRange;
    [Tooltip("Qori's closed eyelid (Batch 26h), shown briefly for a blink; none before 26h.")] public SpriteRenderer blink;
    [Tooltip("The listening spot's player bar (UI_ListeningSpot_TrackRow_Playing), carried here so it loads with the retreat.")] public Sprite playerBar;
    [Tooltip("The preview scene shows it from the start.")] public bool previewOnStart;
    [Tooltip("Ludo's animated seated Qori (back view, 18 Oct), in place of the painted parts: three short clips (a breeze lifting leaves off his cloak, a slow look round, a wider look round), one after another with a still rest between.")]
    public SpriteRenderer seatedView; public Sprite[] seated = new Sprite[0]; public float seatedFps = 12f;
    [Tooltip("The first frame of each clip in `seated`, and the frames in each.")] public int[] seatedClipStarts = new int[0]; public int seatedClipLength = 36;
    [Tooltip("Laid over the new clip's first frame and faded out, so one clip's resting pose melts into the next.")]
    public SpriteRenderer seatedBlend;
    [Tooltip("Seconds Qori sits still (on the last clip's final frame) before the next clip plays.")]
    public float seatedRest = 7f;

    void Awake() { if (view != null) view.enabled = previewOnStart; }

    void Update()
    {
        if (view != null && view.enabled) { Fit(view); Tick(Time.unscaledTime); }
    }

    // Covers the screen with the 16:9 painting. Narrower screens keep its left side (Qori and the
    // bench); wider ones crop top and bottom equally.
    public static void Fit(Camera cam)
    {
        float aspect = cam.aspect;
        if (aspect >= CanvasW / CanvasH) { cam.orthographicSize = CanvasW / aspect * .5f; cam.transform.localPosition = new Vector3(0f, 0f, -10f); }
        else { cam.orthographicSize = CanvasH * .5f; float half = CanvasH * .5f * aspect; cam.transform.localPosition = new Vector3(-CanvasW * .5f + half, 0f, -10f); }
    }

    static void Turn(Transform t, float degrees) { if (t != null) t.localRotation = Quaternion.Euler(0f, 0f, degrees); }

    // Poses everything at time t (seconds); also used by the capture tool.
    public void Tick(float t)
    {
        if (clouds != null) clouds.localPosition = new Vector3(Mathf.Repeat(t * cloudSpeed, CanvasW), 0f, 0f);
        if (mist != null) mist.localPosition = new Vector3(Mathf.Repeat(t * mistSpeed, CanvasW), 0f, 0f);
        if (weather != null) weather.localPosition = new Vector3(0f, -Mathf.Repeat(t * weatherSpeed, weatherPeriod), 0f);
        if (drifters != null)
            for (int i = 0; i < drifters.Length; i++)
                if (drifters[i] != null) drifters[i].localPosition = new Vector3(Mathf.Sin(t * .09f + i * .5f) * driftPx[i] / Ppu, 0f, 0f);
        // A slow breeze shared by everything that sways, each piece with its own rhythm.
        float breeze = Mathf.Sin(t * .7f) * .6f + Mathf.Sin(t * 1.9f + 1f) * .4f;
        if (swayers != null)
            for (int i = 0; i < swayers.Length; i++)
                Turn(swayers[i], swayDeg[i] * (.55f * Mathf.Sin(t * (1.1f + .23f * i) + i * 1.7f) + .45f * breeze));
        if (glows != null)
            for (int i = 0; i < glows.Length; i++)
            {
                if (glows[i] == null) continue;
                float f = .5f + .3f * Mathf.Sin(t * 7.3f + i) * Mathf.Sin(t * 2.9f + 1f) + .2f * Mathf.Sin(t * 13.1f + i * 2f);
                var c = glows[i].color; c.a = Mathf.Lerp(glowRange[i].x, glowRange[i].y, .3f + .55f * f); glows[i].color = c;
            }
        // A blink every few seconds, now and then a double one.
        if (blink != null) { float b = Mathf.Repeat(t, 4.7f); blink.enabled = b < .12f || Mathf.Repeat(t, 14.1f) > 13.75f && b > .3f && b < .42f; }
        // A slow breath in the head; the ears follow it a little late, with a rare flick.
        float breath = Mathf.Sin(t * Mathf.PI * 2f / 4.6f);
        Turn(head, breath * 1.2f + Mathf.Sin(t * .37f) * .3f);
        float flick = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * .53f)), 24f) * 3f;
        Turn(earFar, Mathf.Sin(t * Mathf.PI * 2f / 4.6f - .6f) * 2.2f + Mathf.Sin(t * 1.7f) * .6f + breeze * .4f);
        Turn(earNear, Mathf.Sin(t * Mathf.PI * 2f / 4.6f - .9f) * 2.6f + Mathf.Sin(t * 1.3f + 2f) * .6f + flick);
        if (seatedView != null && seated != null && seated.Length > 1)
        {
            // Mostly still. Every seatedRest seconds the next clip plays once; between, he rests on that clip's last frame.
            // As a clip starts, the previous one's resting frame fades out over it.
            int clips = seatedClipStarts != null && seatedClipStarts.Length > 0 ? seatedClipStarts.Length : 1;
            int len = clips > 1 ? seatedClipLength : seated.Length;
            // Every clip starts from (nearly) the same pose, lined up on export, so Qori always rests on a clip's first
            // frame. A cycle: the resting pose cross-fades into the next clip's first frame (still), the clip plays, and
            // comes back to its first frame (the look-round clips play back the way they came; the breeze, whose leaves
            // fly off, cross-fades back while still), then he rests.
            const float swap = 1f, settle = .8f;
            float play = len / Mathf.Max(.1f, seatedFps);
            int n = 0; float m = t;
            float Cycle(int i) => swap + (PingPong(i) ? 2f * play : play + settle) + seatedRest;
            for (int guard = 0; guard < 100000 && m >= Cycle(n); guard++) { m -= Cycle(n); n++; }
            int Start(int i) => clips > 1 ? seatedClipStarts[((i % clips) + clips) % clips] : 0;
            bool PingPong(int i) => clips > 1 && ((i % clips) + clips) % clips != 0;
            int start = Start(n), lastF = start + len - 1;
            int Fr(float sec) => Mathf.Clamp(Mathf.FloorToInt(sec * seatedFps), 0, len - 1);
            Sprite main = seated[start]; Sprite over = null; float overA = 0f;
            float p = m - swap;
            if (m < swap) { if (n > 0) { over = seated[Start(n - 1)]; overA = 1f - Mathf.SmoothStep(0f, 1f, m / swap); } }
            else if (PingPong(n)) { if (p < 2f * play) main = seated[start + (p < play ? Fr(p) : Fr(2f * play - p))]; }
            else if (p < play) main = seated[start + Fr(p)];
            else if (p < play + settle) { over = seated[lastF]; overA = 1f - Mathf.SmoothStep(0f, 1f, (p - play) / settle); }
            seatedView.sprite = main;
            if (seatedBlend != null)
            {
                seatedBlend.enabled = over != null;
                if (over != null) { seatedBlend.sprite = over; var c = seatedBlend.color; c.a = overA; seatedBlend.color = c; }
            }
        }
    }

    // The director's interface, kept here so callers need only this class.
    public static int Current { get => ListeningRetreatDirector.Current; set => ListeningRetreatDirector.Current = value; }
    public static bool IsShowing => ListeningRetreatDirector.IsShowing;
    /// <summary>Fades to the current place. False when the retreat is off or its prefab is missing.</summary>
    public static bool Open() => Use && ListeningRetreatDirector.Show(Current);
    public static void Close() => ListeningRetreatDirector.Hide();
    /// <summary>Fades to another place (wrapping round the list) while the retreat shows.</summary>
    public static void Step(int by) => ListeningRetreatDirector.Show((Current + by + Scenes.Length) % Scenes.Length);
    public static Sprite PlayerBar => ListeningRetreatDirector.PlayerBar;
}

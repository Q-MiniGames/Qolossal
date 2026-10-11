using UnityEngine;
using UnityEngine.UI;

// The listening retreat (Batch 25a-6, CLAUDE_REVIEW_59): a static-camera painted scene of the Qvale
// overlook terrace, Qori seen from behind on the bench at the parapet, looking out over the valley.
// Sitting at the Qvale listening bench fades to it; getting up fades back. It is its own small scene
// (a prefab under Resources, built by ListeningRetreatBuilder from the delivery's registration), drawn
// by its own camera far from the world.
//
// Motion stays inside the registration's limits: clouds and mist drift and wrap, the far and middle
// valley drift a few pixels, seed motes float down inside the weather mask, the olive tree barely
// sways, the lantern swings and flickers, grass and leaves sway, and Qori's head sways up to 1.5
// degrees with his ears up to 6. His body is fixed: the backrest covers it.
public sealed class ListeningRetreat : MonoBehaviour
{
    public const string ResourcePath = "ListeningRetreat/ListeningRetreat_Overlook";
    public const float Ppu = 100f, CanvasW = 25.6f, CanvasH = 14.4f;
    [Tooltip("Off: the bench keeps the old in-world view.")] public static bool Use = true;

    public Camera view;
    public Transform clouds, far, mid, mist, weather, tree, lantern, head, earFar, earNear;
    public Transform[] foreground;
    public SpriteRenderer glow;
    [Tooltip("The listening spot's player bar (UI_ListeningSpot_TrackRow_Playing), carried here so it loads with the retreat.")] public Sprite playerBar;
    [Tooltip("Weather repeat tile height (u).")] public float weatherPeriod = 4.8f;
    public float weatherSpeed = .22f, cloudSpeed = .05f, mistSpeed = .1f;
    [Tooltip("The preview scene shows it from the start.")] public bool previewOnStart;

    static ListeningRetreat instance;
    Canvas fadeCanvas; Image fade;
    float fadeTo, fadeAt = 1f; bool showing, swapPending;

    public static bool IsShowing => instance != null && instance.showing;

    // Opens the retreat (fades through black). Returns null when the prefab is missing.
    public static ListeningRetreat Open()
    {
        if (!Use) return null;
        if (instance == null)
        {
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null) return null;
            instance = Instantiate(prefab).GetComponent<ListeningRetreat>();
            instance.name = prefab.name;
        }
        instance.Show(true);
        return instance;
    }

    public void Close() => Show(false);

    void Show(bool on)
    {
        if (fadeCanvas == null) BuildFade();
        showing = on; swapPending = true; fadeTo = 1f;
    }

    void BuildFade()
    {
        fadeCanvas = GameHud.CreateCanvas("Retreat Fade", 84);   // above the HUD, below the track list (85)
        fadeCanvas.transform.SetParent(transform, false);
        var go = new GameObject("Fade", typeof(RectTransform));
        go.transform.SetParent(fadeCanvas.transform, false);
        fade = go.AddComponent<Image>(); fade.color = new Color(0f, 0f, 0f, 0f); fade.raycastTarget = false;
        var r = (RectTransform)go.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        fadeAt = 0f;
    }

    void Awake()
    {
        if (view != null) view.enabled = previewOnStart;
        showing = previewOnStart;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (fade != null)
        {
            // Out to black, swap the view, then back in.
            fadeAt = Mathf.MoveTowards(fadeAt, fadeTo, dt / (fadeTo > fadeAt ? .25f : .35f));
            fade.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, fadeAt));
            if (swapPending && fadeAt >= 1f)
            {
                swapPending = false; fadeTo = 0f;
                if (view != null) view.enabled = showing;
                Debug.Log("[Retreat] view " + (showing ? "on" : "off"));
            }
        }
        if (view != null && view.enabled) { Fit(view); Tick(Time.unscaledTime); }
    }

    // Covers the screen with the 16:9 painting. Narrower screens keep its left side (Qori, the bench
    // and the lantern); wider ones crop top and bottom equally.
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
        if (far != null) far.localPosition = new Vector3(Mathf.Sin(t * .09f) * .08f, 0f, 0f);          // ±8 px of the ±32 reviewed
        if (mid != null) mid.localPosition = new Vector3(Mathf.Sin(t * .09f + .5f) * .14f, 0f, 0f);    // ±14 of ±32
        // A slow breeze: the tree (±0.2 of ±0.3), the lantern on it (±2.5 of ±4), grass and leaves (±2 of ±3).
        float breeze = Mathf.Sin(t * .7f) * .6f + Mathf.Sin(t * 1.9f + 1f) * .4f;
        Turn(tree, breeze * .2f);
        Turn(lantern, Mathf.Sin(t * 1.3f) * 2f + breeze * .5f);
        if (foreground != null)
            for (int i = 0; i < foreground.Length; i++) Turn(foreground[i], Mathf.Sin(t * (1.1f + .23f * i) + i * 1.7f) * 1.4f + breeze * .6f);
        if (glow != null)
        {
            float f = .86f + .07f * Mathf.Sin(t * 7.3f) * Mathf.Sin(t * 2.9f + 1f) + .04f * Mathf.Sin(t * 13.1f);   // within 0.65-1.0
            var c = glow.color; c.a = f; glow.color = c;
        }
        // A slow breath in the head; the ears follow it a little late, with a rare flick.
        float breath = Mathf.Sin(t * Mathf.PI * 2f / 4.6f);
        Turn(head, breath * 1.2f + Mathf.Sin(t * .37f) * .3f);
        float flick = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(t * .53f)), 24f) * 3f;
        Turn(earFar, Mathf.Sin(t * Mathf.PI * 2f / 4.6f - .6f) * 2.2f + Mathf.Sin(t * 1.7f) * .6f + breeze * .4f);
        Turn(earNear, Mathf.Sin(t * Mathf.PI * 2f / 4.6f - .9f) * 2.6f + Mathf.Sin(t * 1.3f + 2f) * .6f + flick);
    }

    void OnDestroy() { if (instance == this) instance = null; }
}

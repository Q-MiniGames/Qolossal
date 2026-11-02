using UnityEngine;
using UnityEngine.UI;

// Shows one listening-retreat place at a time (see ListeningRetreat): fades to black, swaps the
// place, fades back in. Only the place on screen is loaded; the one it replaces is destroyed and its
// textures unloaded, so memory holds one scene's layers at most.
public sealed class ListeningRetreatDirector : MonoBehaviour
{
    static ListeningRetreatDirector instance;
    public static int Current;
    static int target = -1;   // -1: nothing should show
    public static bool IsShowing => instance != null && target >= 0;
    public static Sprite PlayerBar { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { Current = 0; target = -1; PlayerBar = null; instance = null; }

    Canvas fadeCanvas; Image fade; float fadeAt;
    int shown = -1; ListeningRetreat place;

    public static bool Show(int index)
    {
        if (index < 0 || index >= ListeningRetreat.Scenes.Length) return false;
        var prefab = Resources.Load<GameObject>(ListeningRetreat.ResourcePath(index));
        if (prefab == null) return false;
        var r = prefab.GetComponent<ListeningRetreat>();
        if (r != null && r.playerBar != null) PlayerBar = r.playerBar;
        if (instance == null) instance = new GameObject("Listening Retreat").AddComponent<ListeningRetreatDirector>();
        Current = index; target = index;
        return true;
    }

    public static void Hide() { target = -1; }

    void Awake()
    {
        fadeCanvas = GameHud.CreateCanvas("Retreat Fade", 84);   // above the HUD, below the player (85)
        fadeCanvas.transform.SetParent(transform, false);
        var go = new GameObject("Fade", typeof(RectTransform));
        go.transform.SetParent(fadeCanvas.transform, false);
        fade = go.AddComponent<Image>(); fade.color = new Color(0f, 0f, 0f, 0f); fade.raycastTarget = false;
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool swap = target != shown;
        // Out to black while a swap is due, then back in.
        fadeAt = Mathf.MoveTowards(fadeAt, swap ? 1f : 0f, dt / (swap ? .25f : .35f));
        fade.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, fadeAt));
        if (swap && fadeAt >= 1f)
        {
            if (place != null) { Destroy(place.gameObject); place = null; Resources.UnloadUnusedAssets(); }
            if (target >= 0)
            {
                var prefab = Resources.Load<GameObject>(ListeningRetreat.ResourcePath(target));
                if (prefab != null)
                {
                    place = Instantiate(prefab).GetComponent<ListeningRetreat>();
                    place.name = prefab.name;
                    place.view.enabled = true;
                }
            }
            shown = target;
            Debug.Log("[Retreat] view " + (shown >= 0 ? "on: " + ListeningRetreat.Scenes[shown] : "off"));
        }
    }

    void OnDestroy() { if (instance == this) { instance = null; target = -1; } }
}

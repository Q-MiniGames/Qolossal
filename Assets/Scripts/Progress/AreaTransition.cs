using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Moves Qori between areas through portals: fades to black, loads the destination scene, puts
// him beside the matching portal with the hearts and weapon he left with, and fades back in.
public sealed class AreaTransition : MonoBehaviour
{
    const float FadeSeconds = .35f, NameSeconds = 2.5f;

    public static bool IsTransitioning { get; private set; }
    static AreaTransition instance;

    Image curtain; Text areaName;
    float nameShownAt = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsTransitioning = false; instance = null; }

    public static bool CanTravelTo(string scene) => !string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene);

    // Starts the trip; ignored while one is already under way.
    public static void Travel(string scene, string portalId, PlayerMovement player)
    {
        if (IsTransitioning || !CanTravelTo(scene)) return;
        if (instance == null)
        {
            instance = new GameObject("Area Transition").AddComponent<AreaTransition>();
            DontDestroyOnLoad(instance.gameObject);
            instance.Build();
        }
        var health = player.GetComponent<PlayerHealth>();
        var combat = player.GetComponent<PlayerCombat>();
        instance.StartCoroutine(instance.Run(scene, portalId, health != null ? health.Health : -1, combat != null ? combat.EquippedWeapon : null));
    }

    void Build()
    {
        var canvas = GameHud.CreateCanvas("Area Transition Canvas", 100);
        canvas.transform.SetParent(transform, false);
        curtain = GameHud.AddImage(canvas.transform, "Curtain", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000f, 4000f));
        curtain.color = new Color(.03f, .05f, .05f, 0f); curtain.preserveAspect = false;
        var obj = new GameObject("Area Name", typeof(RectTransform));
        obj.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.anchoredPosition = new Vector2(0f, -170f); rt.sizeDelta = new Vector2(1200f, 90f);
        areaName = obj.AddComponent<Text>();
        areaName.font = UiSkin.Font; areaName.fontSize = 52; areaName.alignment = TextAnchor.MiddleCenter;
        areaName.raycastTarget = false; areaName.color = new Color(1f, 1f, 1f, 0f);
        obj.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, .6f);
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
        {
            SetCurtain(Mathf.Lerp(from, to, t / FadeSeconds));
            yield return null;
        }
        SetCurtain(to);
    }

    void SetCurtain(float a) { Color c = curtain.color; c.a = a; curtain.color = c; }

    IEnumerator Run(string scene, string portalId, int hearts, WeaponDefinition weapon)
    {
        IsTransitioning = true;
        yield return Fade(0f, 1f);
        yield return SceneManager.LoadSceneAsync(scene);
        yield return null;   // let the new scene's Start methods run (checkpoint restore) first

        var player = FindAnyObjectByType<PlayerMovement>();
        Portal arrival = null;
        foreach (var portal in FindObjectsByType<Portal>(FindObjectsSortMode.None))
            if (portal.portalId == portalId) arrival = portal;
        if (player != null)
        {
            if (arrival != null) { player.PlaceAt(arrival.ArrivalPoint, arrival.exitSide, !player.HasCheckpoint); }
            else Debug.LogWarning($"[AreaTransition] no portal '{portalId}' in {scene}; Qori starts at the scene's start");
            var health = player.GetComponent<PlayerHealth>();
            if (health != null && hearts > 0) health.SetHealth(hearts);
            var combat = player.GetComponent<PlayerCombat>();
            if (combat != null && weapon != null) combat.EquipWeapon(weapon);
        }
        var area = GameArea.InScene;
        areaName.text = area != null ? area.displayName : "";
        nameShownAt = Time.unscaledTime + FadeSeconds * .5f;
        yield return Fade(1f, 0f);
        IsTransitioning = false;
    }

    void Update()
    {
        float t = Time.unscaledTime - nameShownAt;
        float a = t < 0f ? 0f : Mathf.Clamp01(Mathf.Min(t / .4f, (NameSeconds - t) / .6f));
        areaName.color = new Color(1f, 1f, 1f, a);
    }
}

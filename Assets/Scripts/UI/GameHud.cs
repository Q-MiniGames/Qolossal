using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Top-left HUD: the root-and-leaf frame with one leaf per heart (fresh, half, or withered) and
// the equipped weapon in the ring. Created automatically in any scene that has Qori.
[DisallowMultipleComponent]
public sealed class GameHud : MonoBehaviour
{
    const float FrameWidth = 460f, Margin = 36f;   // reference px at 1920x1080

    PlayerHealth health;
    PlayerCombat combat;
    UiSkin skin;
    Image[] leaves;
    Image weapon;
    WeaponDefinition shownWeapon;
    int shownHealth = -1, shownMaximum = -1;
    float[] leafPulse;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindFirstObjectByType<GameHud>() != null || UiSkin.Load() == null) return;
        var player = FindFirstObjectByType<PlayerHealth>();
        if (player == null) return;
        // Every playable scene needs the pause menu, not only scenes that were set up with one.
        if (FindFirstObjectByType<GamePauseMenu>() == null) new GameObject("Pause Menu").AddComponent<GamePauseMenu>();
        new GameObject("Game HUD").AddComponent<GameHud>().Build(player);
    }

    // A screen-space canvas scaled from a 1920x1080 reference, plus an event system for menus.
    public static Canvas CreateCanvas(string name, int order)
    {
        var obj = new GameObject(name);
        var canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = obj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = .5f;
        obj.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }
        return canvas;
    }

    public static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
        var image = obj.AddComponent<Image>();
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        return image;
    }

    void Build(PlayerHealth player)
    {
        health = player; combat = player.GetComponent<PlayerCombat>(); skin = UiSkin.Load();
        var canvas = CreateCanvas("HUD Canvas", 10);
        canvas.transform.SetParent(transform, false);

        float scale = FrameWidth / skin.hudFrame.rect.width;
        Vector2 frameSize = skin.hudFrame.rect.size * scale;
        // Leaves and weapon sit under the frame, showing through its openings.
        var frame = AddImage(canvas.transform, "HUD", null, new Vector2(0f, 1f), new Vector2(Margin + frameSize.x * .5f, -Margin - frameSize.y * .5f), frameSize);
        frame.enabled = false;
        // Frame px (from top-left) -> position inside the frame's rect (centre origin).
        Vector2 Local(Vector2 px) => new Vector2(px.x * scale - frameSize.x * .5f, frameSize.y * .5f - px.y * scale);

        weapon = AddImage(frame.transform, "Weapon", null, new Vector2(.5f, .5f), Local(skin.weaponRingCentre),
            Vector2.one * skin.weaponRingDiameter * scale * .8f);
        weapon.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        weapon.enabled = false;

        leaves = new Image[skin.leafSockets.Length];
        leafPulse = new float[leaves.Length];
        for (int i = 0; i < leaves.Length; i++)
            leaves[i] = AddImage(frame.transform, "Leaf " + (i + 1), skin.leafFull, new Vector2(.5f, .5f), Local(skin.leafSockets[i]), Vector2.one * skin.leafSize * scale);
        AddImage(frame.transform, "Frame", skin.hudFrame, new Vector2(.5f, .5f), Vector2.zero, frameSize);
    }

    void Update()
    {
        if (health == null) return;
        int hp = health.Health, max = health.MaximumHealth;
        if (hp != shownHealth || max != shownMaximum)
        {
            for (int i = 0; i < leaves.Length; i++)
            {
                bool had = i < shownHealth;
                leaves[i].enabled = i < max;
                leaves[i].sprite = i < hp ? skin.leafFull : skin.leafEmpty;
                if (had != (i < hp) && shownHealth >= 0) leafPulse[i] = 1f;   // lost or regained: a small pop
            }
            shownHealth = hp; shownMaximum = max;
        }
        for (int i = 0; i < leaves.Length; i++)
        {
            leafPulse[i] = Mathf.MoveTowards(leafPulse[i], 0f, Time.unscaledDeltaTime * 3f);
            float s = 1f + .35f * leafPulse[i] * leafPulse[i];
            leaves[i].rectTransform.localScale = new Vector3(s, s, 1f);
        }

        WeaponDefinition w = combat != null ? combat.EquippedWeapon : null;
        if (w != shownWeapon)
        {
            shownWeapon = w;
            weapon.sprite = w != null ? w.weaponArtwork : null;
            weapon.enabled = weapon.sprite != null;
        }
    }
}

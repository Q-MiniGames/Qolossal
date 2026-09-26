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
    const float RingSize = 150f, LeafSize = 56f, LeafSpacing = 46f, LeafGap = 30f, VineHeight = 26f, Margin = 30f;   // reference px at 1920x1080

    PlayerHealth health;
    PlayerCombat combat;
    UiSkin skin;
    Image[] leaves;
    Image weapon, vine;
    Vector2 vineStub;
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

        // Round medallion (HUD_Ring) holding the weapon icon on a dark backing, and a vine from
        // its stub threading one leaf per heart.
        float scale = RingSize / skin.ring.rect.width;
        Vector2 ringSize = skin.ring.rect.size * scale;
        var root = AddImage(canvas.transform, "HUD", null, new Vector2(0f, 1f), new Vector2(Margin + ringSize.x * .5f, -Margin - ringSize.y * .5f), ringSize);
        root.enabled = false;
        // Ring px (from its top-left) -> position inside the ring's rect (centre origin).
        Vector2 Local(Vector2 px) => new Vector2(px.x * scale - ringSize.x * .5f, ringSize.y * .5f - px.y * scale);

        int count = skin.leafSockets.Length;
        Vector2 stub = Local(skin.ringStubTip);
        vineStub = stub;
        vine = AddImage(root.transform, "Vine", skin.vine, new Vector2(.5f, .5f), stub, new Vector2(1f, VineHeight));
        vine.type = Image.Type.Tiled; vine.preserveAspect = false;
        vine.pixelsPerUnitMultiplier = skin.vine.rect.height / VineHeight;
        SizeVine(count);

        AddImage(root.transform, "Backing", skin.ringBacking, new Vector2(.5f, .5f), Vector2.zero, ringSize);
        weapon = AddImage(root.transform, "Weapon", null, new Vector2(.5f, .5f), Local(skin.ringOpeningCentre),
            Vector2.one * skin.ringOpeningDiameter * scale * .82f);
        weapon.enabled = false;
        AddImage(root.transform, "Ring", skin.ring, new Vector2(.5f, .5f), Vector2.zero, ringSize);

        leaves = new Image[count];
        leafPulse = new float[count];
        for (int i = 0; i < count; i++)
            leaves[i] = AddImage(root.transform, "Leaf " + (i + 1), skin.leafFull, new Vector2(.5f, .5f),
                stub + new Vector2(LeafGap + i * LeafSpacing, 0f), Vector2.one * LeafSize);
    }

    // The vine runs from the ring's stub to just past the last heart.
    void SizeVine(int hearts)
    {
        float length = LeafGap + Mathf.Max(0, hearts - 1) * LeafSpacing + LeafSize * .45f;
        vine.rectTransform.sizeDelta = new Vector2(length, VineHeight);
        vine.rectTransform.anchoredPosition = vineStub + new Vector2(length * .5f - 4f, 0f);
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
            if (max != shownMaximum) SizeVine(Mathf.Min(max, leaves.Length));
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
            weapon.sprite = w == null ? null : skin.IconFor(w.weaponId) ?? w.weaponArtwork;
            weapon.enabled = weapon.sprite != null;
        }
    }
}

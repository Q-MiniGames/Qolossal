using UnityEngine;
using UnityEngine.UI;

// Shared building blocks for the town's windows (dialogue, shop, listening spot, HUD): panels in
// the game's UiSkin panel art when it's there, and text in the game's font. Positions are in the
// 1920x1080 reference space of GameHud.CreateCanvas.
public static class TownUi
{
    // On the screen (HUD, captions): light. On the parchment panels: dark ink.
    public static readonly Color Ink = new Color(.96f, .93f, .84f), Dim = new Color(.7f, .67f, .6f), Mint = new Color(.72f, 1f, .88f), AmberColor = new Color(1f, .74f, .3f);
    public static readonly Color PanelInk = new Color(.24f, .17f, .11f), PanelDim = new Color(.47f, .4f, .32f), PanelAmber = new Color(.7f, .4f, .07f), PanelMint = new Color(.1f, .42f, .34f);

    public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var skin = UiSkin.Load();
        var image = GameHud.AddImage(parent, name, skin != null ? skin.panel : null, anchor, position, size);
        image.preserveAspect = false;
        if (image.sprite != null && image.sprite.border != Vector4.zero) image.type = Image.Type.Sliced;
        if (image.sprite == null) image.color = new Color(.93f, .88f, .76f, .96f);   // parchment, like the skin's panel
        return image;
    }

    public static Text Label(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, int fontSize, TextAnchor align, Color color, bool shadow = true)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = position; rt.sizeDelta = size;
        var text = obj.AddComponent<Text>();
        text.font = UiSkin.Font; text.fontSize = fontSize; text.alignment = align; text.color = color;
        text.raycastTarget = false; text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
        if (shadow) { var drop = obj.AddComponent<Shadow>(); drop.effectColor = new Color(0f, 0f, 0f, .6f); drop.effectDistance = new Vector2(2f, -2f); }
        return text;
    }

    // A world point's position in the reference canvas space, for labels over characters.
    public static bool ScreenPoint(Vector3 world, out Vector2 canvas)
    {
        canvas = default;
        var view = Camera.main; if (view == null) return false;
        Vector3 p = view.WorldToViewportPoint(world);
        if (p.z < 0f || p.x < -.1f || p.x > 1.1f || p.y < -.1f || p.y > 1.1f) return false;
        canvas = new Vector2(p.x, p.y);
        return true;
    }
}

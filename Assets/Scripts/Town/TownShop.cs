using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// A shop window, opened after its TownNpc's conversation: a list of goods with Amber prices. Up and
// Down choose, Confirm buys, Cancel leaves. Goods are either upgrades for Qori (a heart seed) or
// the town's requests: paying for one sets a flag that changes the town (lamps lit, a hint shown).
[DisallowMultipleComponent]
public sealed class TownShop : MonoBehaviour
{
    public enum Effect { HeartSeed, SetFlag }

    [Serializable]
    public sealed class Item
    {
        public string name = "Item";
        [TextArea] public string description = "";
        public int price = 10;
        public Effect effect;
        [Tooltip("For SetFlag: the flag set when bought.")] public string flag = "";
        [Tooltip("Sold once, then marked as sold.")] public bool once = true;
        [Tooltip("TownState.Check condition for it to be listed at all; empty: always.")] public string available = "";
    }

    public string title = "Shop";
    public List<Item> items = new List<Item>();

    public static bool IsOpenAny { get; private set; }
    public bool IsOpen { get; private set; }
    readonly List<int> listed = new List<int>();   // the items on sale now (their `available` holds)
    int selected; Canvas canvas; Text heading, amber, detail; readonly List<Text> rows = new List<Text>();
    float deniedAt = -10f; int openedFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsOpenAny = false;

    string SoldFlag(int i) => $"sold:{name}:{i}";
    public bool IsSold(int i) => items[i].once && TownState.Has(SoldFlag(i));

    public void Open()
    {
        if (IsOpen) return;
        if (canvas == null) Build();
        listed.Clear();
        for (int i = 0; i < items.Count; i++) if (TownState.Check(items[i].available)) listed.Add(i);
        IsOpen = IsOpenAny = true; openedFrame = Time.frameCount; selected = 0;
        ModalUi.Open(); canvas.enabled = true; Refresh();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = IsOpenAny = false; canvas.enabled = false; ModalUi.Close();
    }

    void Build()
    {
        canvas = GameHud.CreateCanvas("Shop Canvas", 85);
        canvas.transform.SetParent(transform, false);
        float h = 300f + items.Count * 72f;
        TownUi.Panel(canvas.transform, "Panel", new Vector2(.5f, .5f), new Vector2(0f, 40f), new Vector2(1000f, h));
        heading = TownUi.Label(canvas.transform, "Title", new Vector2(.5f, .5f), new Vector2(-150f, 40f + h * .5f - 85f), new Vector2(520f, 60f), 42, TextAnchor.MiddleLeft, TownUi.PanelAmber, false);
        amber = TownUi.Label(canvas.transform, "Amber", new Vector2(.5f, .5f), new Vector2(270f, 40f + h * .5f - 85f), new Vector2(280f, 60f), 34, TextAnchor.MiddleRight, TownUi.PanelInk, false);
        for (int i = 0; i < items.Count; i++)
            rows.Add(TownUi.Label(canvas.transform, "Row " + i, new Vector2(.5f, .5f), new Vector2(0f, 40f + h * .5f - 165f - i * 72f), new Vector2(800f, 64f), 34, TextAnchor.MiddleLeft, TownUi.PanelInk, false));
        detail = TownUi.Label(canvas.transform, "Detail", new Vector2(.5f, .5f), new Vector2(0f, 40f - h * .5f + 112f), new Vector2(780f, 80f), 27, TextAnchor.MiddleCenter, TownUi.PanelDim, false);
        canvas.enabled = false;
    }

    void Refresh()
    {
        heading.text = title;
        amber.text = $"Amber  <color=#B3650F>{TownState.Amber}</color>";
        for (int row = 0; row < rows.Count; row++)
        {
            if (row >= listed.Count) { rows[row].text = ""; continue; }
            int i = listed[row];
            var it = items[i];
            string price = IsSold(i) ? "<color=#8C8577>sold</color>" : TownState.Amber >= it.price ? $"<color=#B3650F>{it.price}</color>" : $"<color=#A0503A>{it.price}</color>";
            rows[row].text = (row == selected ? "▶  " : "    ") + it.name + "  —  " + price;
            rows[row].color = row == selected ? TownUi.PanelInk : TownUi.PanelDim;
        }
        bool denied = Time.time - deniedAt < 1.2f;
        detail.text = denied ? "<color=#A0503A>Not enough Amber.</color>" : listed.Count > 0 ? items[listed[selected]].description : "Nothing for sale yet.";
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Time.frameCount != openedFrame)
        {
            if (TownInput.Cancel()) { Close(); return; }
            int n = Mathf.Max(1, listed.Count);
            if (TownInput.Up()) selected = (selected + n - 1) % n;
            if (TownInput.Down()) selected = (selected + 1) % n;
            if (TownInput.Confirm() && listed.Count > 0) Buy(listed[selected]);
        }
        Refresh();
    }

    // Buys item `i` if it's for sale and affordable (Confirm calls this; so can tests).
    public bool Buy(int i)
    {
        if (IsSold(i)) return false;
        var it = items[i];
        if (!TownState.Spend(it.price)) { deniedAt = Time.time; return false; }
        if (it.once) TownState.Set(SoldFlag(i));
        switch (it.effect)
        {
            case Effect.HeartSeed:
                var health = FindAnyObjectByType<PlayerHealth>();
                if (health != null) health.AddMaximum(1);
                TownHud.Toast("A new heart seed takes root. (+1 heart)");
                break;
            case Effect.SetFlag:
                TownState.Set(it.flag);
                TownHud.Toast(it.name + ": done.");
                break;
        }
        return true;
    }

    void OnDestroy() { if (IsOpen) { IsOpenAny = false; ModalUi.Close(); } }
}

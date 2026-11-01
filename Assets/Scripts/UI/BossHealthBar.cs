using UnityEngine;
using UnityEngine.UI;

// The boss's bar across the bottom of the screen, while it fights: its name, its health, a pale chip that trails each hit
// down to the new level, a pop on every hit, and notches where its phases change. CreatureHitFeel keeps it showing.
public sealed class BossHealthBar : MonoBehaviour
{
    const float FullWidth = 900f, Height = 22f, Y = 74f;
    static BossHealthBar instance;

    public static Canvas CanvasForTest => instance != null ? instance.canvas : null;
    public static float ChipForTest => instance != null ? instance.chip : -1f;

    Canvas canvas; Image back, chipImage, fill; Image[] notches; Text title;
    EnemyBase boss; float seenAt = -9f, chip = 1f, have = 1f, chipWaits, popAt = -9f;

    static float Now => Time.realtimeSinceStartup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => instance = null;

    public static void Show(EnemyBase enemy)
    {
        if (instance == null) instance = new GameObject("Boss health bar").AddComponent<BossHealthBar>();
        instance.Watch(enemy);
    }

    void Build()
    {
        canvas = GameHud.CreateCanvas("Boss bar canvas", 12);
        canvas.transform.SetParent(transform, false);
        var bottom = new Vector2(.5f, 0f);
        back = GameHud.AddImage(canvas.transform, "Back", null, bottom, new Vector2(0f, Y), new Vector2(FullWidth + 8f, Height + 8f));
        back.color = new Color(.06f, .07f, .06f, .85f);
        chipImage = GameHud.AddImage(canvas.transform, "Chip", null, bottom, new Vector2(0f, Y), new Vector2(FullWidth, Height));
        chipImage.color = new Color(1f, .95f, .75f);
        fill = GameHud.AddImage(canvas.transform, "Fill", null, bottom, new Vector2(0f, Y), new Vector2(FullWidth, Height));
        fill.color = new Color(.78f, .22f, .2f);
        notches = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            float x = FullWidth * ((i + 1) / 3f - .5f);
            notches[i] = GameHud.AddImage(canvas.transform, "Phase notch", null, bottom, new Vector2(x, Y), new Vector2(4f, Height + 8f));
            notches[i].color = new Color(.06f, .07f, .06f);
        }
        title = TownUi.Label(canvas.transform, "Boss name", bottom, new Vector2(0f, Y + 34f), new Vector2(FullWidth, 40f), 28, TextAnchor.MiddleCenter, Color.white);
    }

    void Watch(EnemyBase enemy)
    {
        if (canvas == null) Build();
        if (boss != enemy) { boss = enemy; chip = have = enemy.HealthFraction; title.text = enemy.BossBarName; }
        seenAt = Now;
    }

    void LateUpdate()
    {
        if (canvas == null) return;
        bool on = boss != null && Now - seenAt < .9f;
        canvas.enabled = on;
        if (!on) { boss = null; return; }
        var t = FeedbackTuning.Current;
        float now = boss.HealthFraction;
        if (now < have) { popAt = Now; chipWaits = Now + t.healthBarChipDelay; }
        if (now > have) chip = now;   // the fight started over
        have = now;
        if (Now >= chipWaits) chip = Mathf.MoveTowards(chip, have, Time.unscaledDeltaTime * t.healthBarChipSpeed * .5f);
        chip = Mathf.Max(chip, have);
        float pop = Mathf.Clamp01(1f - (Now - popAt) / .14f);
        SetBar(chipImage, chip, pop); SetBar(fill, have, pop);
        back.rectTransform.sizeDelta = new Vector2(FullWidth + 8f, Height + 8f + 8f * pop);
        fill.color = Color.Lerp(new Color(.78f, .22f, .2f), Color.white, pop * .8f);
        back.rectTransform.anchoredPosition = new Vector2(Random.Range(-1f, 1f) * 5f * pop, Y);
    }

    static void SetBar(Image bar, float fraction, float pop)
    {
        float w = FullWidth * Mathf.Clamp01(fraction);
        bar.rectTransform.sizeDelta = new Vector2(w, Height + 8f * pop);
        bar.rectTransform.anchoredPosition = new Vector2(-(FullWidth - w) * .5f, Y);
    }
}

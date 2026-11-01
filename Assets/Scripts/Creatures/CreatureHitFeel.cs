using System.Collections.Generic;
using UnityEngine;

// What a landed blow does to any creature, on top of the freeze, shake and spark the attack brings: its art blinks white,
// squashes and springs back, leaves fly, and a small health bar shows what the hit took. EnemyBase adds this and reports
// hits to it; the numbers live in FeedbackTuning. Times run on the real clock so a freeze doesn't stretch them.
[DisallowMultipleComponent]
public sealed class CreatureHitFeel : MonoBehaviour
{
    static Material flash;
    static Material Flash
    {
        get
        {
            if (flash != null) return flash;
            var shader = Resources.Load<Shader>("FX/SpriteWhiteFlash");
            if (shader != null) flash = new Material(shader) { hideFlags = HideFlags.DontSave };
            return flash;
        }
    }

    EnemyBase enemy;
    Transform visual; Vector3 restScale;
    readonly List<SpriteRenderer> extras = new List<SpriteRenderer>();
    readonly List<(SpriteRenderer renderer, Material rest)> blinking = new List<(SpriteRenderer, Material)>();
    float blinkEnds = -1f, squashAt = -9f;

    // The bar over its head (regular creatures; a boss has BossHealthBar instead).
    SpriteRenderer barBack, barChip, barFill;
    float shownUntil = -1f, chip = 1f, chipWaitsUntil;
    float Width => Mathf.Clamp(barWidth, .9f, 2.6f);
    float barWidth = 1f, barHeight = 1f;

    static float Now => Time.realtimeSinceStartup;

    public static CreatureHitFeel On(EnemyBase enemy)
    {
        var feel = enemy.gameObject.AddComponent<CreatureHitFeel>();
        feel.enemy = enemy;
        return feel;
    }

    // The art that squashes, when it isn't simply the creature's own transform (frames drawn on a child).
    public void SetVisual(Transform art) { visual = art; if (art != null) restScale = art.localScale; }
    // Art kept outside the creature's hierarchy (a stand-in's frames).
    public void AddRenderer(SpriteRenderer r) { if (r != null && !extras.Contains(r)) extras.Add(r); }

    public void Landed(Vector2 point)
    {
        var t = FeedbackTuning.Current;
        Blink(t.hitFlashSeconds);
        squashAt = Now;
        shownUntil = Now + t.healthBarHoldSeconds;
        chipWaitsUntil = Now + t.healthBarChipDelay;
        if (t.hitLeaves > 0 && Fx.Library != null) Fx.Leaves(point, t.hitLeaves, .6f);
        var c = GetComponentInChildren<Collider2D>();
        if (c != null) { barWidth = c.bounds.size.x; barHeight = c.bounds.max.y - transform.position.y; }
    }

    void Blink(float seconds)
    {
        if (seconds <= 0f || Flash == null) return;
        if (blinkEnds < 0f)
        {
            foreach (var r in GetComponentsInChildren<SpriteRenderer>()) { if (r.enabled) { blinking.Add((r, r.sharedMaterial)); r.sharedMaterial = Flash; } }
            foreach (var r in extras) if (r != null) { blinking.Add((r, r.sharedMaterial)); r.sharedMaterial = Flash; }
        }
        blinkEnds = Now + seconds;
    }

    void EndBlink()
    {
        foreach (var (r, rest) in blinking) if (r != null) r.sharedMaterial = rest;
        blinking.Clear(); blinkEnds = -1f;
    }

    void LateUpdate()
    {
        if (blinkEnds > 0f && Now >= blinkEnds) EndBlink();
        var t = FeedbackTuning.Current;
        float u = (Now - squashAt) / Mathf.Max(.01f, t.hitSquashSeconds);
        if (visual != null && u >= 0f && u <= 1.05f)
        {
            float s = u < 1f ? t.hitSquash * (1f - u) * Mathf.Cos(u * 9f) : 0f;   // a damped spring
            visual.localScale = new Vector3(restScale.x * (1f + s), restScale.y * (1f - s), restScale.z);
        }
        UpdateBar(t);
    }

    void UpdateBar(FeedbackTuning t)
    {
        if (enemy == null) return;
        if (enemy.BossBarName != null) { if (enemy.BossBarVisible) BossHealthBar.Show(enemy); return; }
        bool show = enemy.IsAlive && Now < shownUntil && t.healthBarHoldSeconds > 0f;
        if (!show) { HideBar(); chip = enemy.HealthFraction; return; }
        if (barBack == null) BuildBar();
        barBack.enabled = barChip.enabled = barFill.enabled = true;
        float have = enemy.HealthFraction;
        if (Now >= chipWaitsUntil) chip = Mathf.MoveTowards(chip, have, Time.unscaledDeltaTime * t.healthBarChipSpeed);
        chip = Mathf.Max(chip, have);
        float w = Width, pop = 1f + .25f * Mathf.Clamp01(1f - (Now - squashAt) / .12f);
        Vector3 at = transform.position + Vector3.up * (barHeight + .35f);
        Place(barBack, at, w + .08f, .2f * pop);
        Place(barChip, at + Vector3.left * (w * (1f - chip) * .5f), w * chip, .12f * pop);
        Place(barFill, at + Vector3.left * (w * (1f - have) * .5f), w * have, .12f * pop);
    }

    static void Place(SpriteRenderer r, Vector3 at, float w, float h) { r.transform.position = at; r.transform.localScale = new Vector3(w, h, 1f); }

    void BuildBar()
    {
        // Free of the creature, so a squash or a turn never bends it.
        barBack = Bar("Health bar", new Color(.08f, .09f, .08f, .85f), 60);
        barChip = Bar("Health bar chip", new Color(1f, .95f, .75f), 61);
        barFill = Bar("Health bar fill", new Color(.82f, .25f, .22f), 62);
    }

    static SpriteRenderer Bar(string name, Color color, int order)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.sprite = UgArenaArt.Square; r.color = color; r.sortingOrder = order;
        return r;
    }

    // Any of these may already be gone when a scene unloads.
    void HideBar() { foreach (var r in new[] { barBack, barChip, barFill }) if (r != null) r.enabled = false; }

    void OnDisable() { EndBlink(); HideBar(); }

    void OnDestroy()
    {
        foreach (var r in new[] { barBack, barChip, barFill }) if (r != null) Destroy(r.gameObject);
    }
}

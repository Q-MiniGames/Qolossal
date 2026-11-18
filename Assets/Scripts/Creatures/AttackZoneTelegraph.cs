using UnityEngine;

// The danger zone (polish WS2, test for the user's verdict, 3 Nov): a soft shape on a creature's real strike zone, drawn
// from the same numbers as the hit so it can't lie. It fills during the warning, flashes as the strike lands and goes.
// Ember and ivory, never mint: mint means the titan is alive. Guardians and mini-bosses show it by default.
public sealed class AttackZoneTelegraph : MonoBehaviour
{
    public enum Level { Off, BigAttacks, All }
    const string PrefKey = "Qolossal.AttackZones";
    public static Level Mode
    {
        get => (Level)PlayerPrefs.GetInt(PrefKey, (int)Level.BigAttacks);
        set => PlayerPrefs.SetInt(PrefKey, (int)value);
    }

    static readonly Color Ember = new Color(1f, .42f, .16f), Ivory = new Color(1f, .95f, .82f);
    static Sprite square;
    SpriteRenderer area, fill;
    bool big;

    public static AttackZoneTelegraph For(Component owner, bool bigAttack)
    {
        var t = new GameObject("Danger zone").AddComponent<AttackZoneTelegraph>();
        t.transform.SetParent(owner.transform, false);
        t.big = bigAttack;
        if (square == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { name = "Danger zone" };
            var px = new Color32[16]; for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply(false, true);
            square = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4f);
        }
        t.area = t.Make("Area"); t.fill = t.Make("Fill");
        return t;
    }

    SpriteRenderer Make(string label)
    {
        var r = new GameObject(label).AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = square; r.sortingOrder = 6; r.enabled = false;
        return r;
    }

    bool Allowed => Mode == Level.All || Mode == Level.BigAttacks && big;

    // Progress 0..1 across the warning: the zone's area glows, and a fill closes in from its edges to the centre line.
    public void Warn(Vector2 centre, Vector2 size, float progress)
    {
        if (!Allowed) { Hide(); return; }
        progress = Mathf.Clamp01(progress);
        Place(area, centre, size, new Color(Ember.r, Ember.g, Ember.b, Mathf.Lerp(.04f, .1f, progress)));
        Place(fill, centre, new Vector2(size.x * progress, size.y), new Color(Ember.r, Ember.g, Ember.b, Mathf.Lerp(.05f, .16f, progress)));
    }

    public void Strike(Vector2 centre, Vector2 size)
    {
        if (!Allowed) { Hide(); return; }
        Place(area, centre, size, new Color(Ivory.r, Ivory.g, Ivory.b, .28f));
        fill.enabled = false;
    }

    public void Hide()
    {
        if (area != null) area.enabled = false;
        if (fill != null) fill.enabled = false;
    }

    static void Place(SpriteRenderer r, Vector2 centre, Vector2 size, Color color)
    {
        r.enabled = true; r.color = color;
        r.transform.position = new Vector3(centre.x, centre.y, r.transform.position.z);
        r.transform.localScale = new Vector3(size.x, size.y, 1f);
    }
}

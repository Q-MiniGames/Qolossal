using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The moment a knot wakes (world design 3.3, beats 4 and 5). Qori's hands are off the controls
// while it plays. The ability is given and named, Echo (the titan's dream-voice) speaks its
// lines, and then the titan stirs: the ground shakes and the view pulls back. Under a mint flash
// the knot is saved as awake, so the stir variants in this scene change while the screen is
// white, and every earlier level loads in its new state from then on. A caption says what the
// titan did. Until Codex paints the stir vistas the "cinematic" is the shake, the pull-back and
// the flash; a vista sprite, when given, is shown full-screen through the flash.
[DefaultExecutionOrder(1001)]   // after CameraFollow and CombatCameraShake, like them restoring its offset before the next follow
public sealed class StirSequence : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }
    public static StirSequence Current { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { IsPlaying = false; Current = null; }

    const float TypeSeconds = .035f, LineHold = 2.2f;
    static readonly Color Mint = new Color(.72f, 1f, .88f), Flash = new Color(.9f, 1f, .95f);

    string knot; AbilityDefinition ability; string[] echo; Sprite vista;
    Image curtain, vistaImage; Text title, caption, voice, banner;
    Camera view; float baseSize, shake; Vector3 shaken;

    // Starts the sequence for `knot`; `ability` (may be null) is unlocked on Qori.
    public static StirSequence Play(string knot, AbilityDefinition ability, string[] echoLines, Sprite vista = null)
    {
        if (IsPlaying) return null;
        var seq = new GameObject("Stir Sequence").AddComponent<StirSequence>();
        seq.knot = knot; seq.ability = ability; seq.echo = echoLines ?? new string[0]; seq.vista = vista;
        seq.Build();
        seq.StartCoroutine(seq.Run());
        return Current = seq;
    }

    public bool Finished { get; private set; }

    void Build()
    {
        var canvas = GameHud.CreateCanvas("Stir Canvas", 90);
        canvas.transform.SetParent(transform, false);
        vistaImage = GameHud.AddImage(canvas.transform, "Vista", vista, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1920f, 1080f));
        vistaImage.color = new Color(1f, 1f, 1f, 0f); vistaImage.enabled = vista != null;
        curtain = GameHud.AddImage(canvas.transform, "Flash", null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000f, 4000f));
        curtain.preserveAspect = false; curtain.color = new Color(Flash.r, Flash.g, Flash.b, 0f);
        banner = Label(canvas.transform, "Ability", new Vector2(.5f, 1f), -140f, 44, new Vector2(1400f, 150f));
        voice = Label(canvas.transform, "Echo", new Vector2(.5f, 0f), 190f, 38, new Vector2(1500f, 140f));
        voice.fontStyle = FontStyle.Italic;
        title = Label(canvas.transform, "Title", new Vector2(.5f, .5f), 60f, 64, new Vector2(1400f, 100f));
        caption = Label(canvas.transform, "Caption", new Vector2(.5f, .5f), -30f, 36, new Vector2(1400f, 130f));
    }

    static Text Label(Transform parent, string name, Vector2 anchor, float y, int size, Vector2 box)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        var rt = (RectTransform)obj.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = new Vector2(0f, y); rt.sizeDelta = box;
        var text = obj.AddComponent<Text>();
        text.font = UiSkin.Font; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false; text.color = new Color(1f, 1f, 1f, 0f);
        var shadow = obj.AddComponent<Shadow>(); shadow.effectColor = new Color(0f, 0f, 0f, .7f); shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    static void Alpha(Graphic g, float a) { Color c = g.color; c.a = a; g.color = c; }

    IEnumerator FadeText(Text text, float to, float seconds)
    {
        float from = text.color.a;
        for (float t = 0f; t < seconds; t += Time.deltaTime) { Alpha(text, Mathf.Lerp(from, to, t / seconds)); yield return null; }
        Alpha(text, to);
    }

    IEnumerator Run()
    {
        IsPlaying = true;
        view = Camera.main; baseSize = view != null ? view.orthographicSize : 5f;
        var player = FindAnyObjectByType<PlayerAbilityController>();

        // The hand on the knot: the ability flows into Qori.
        yield return new WaitForSeconds(.9f);
        if (ability != null && player != null)
        {
            player.Unlock(ability);
            banner.text = string.IsNullOrEmpty(ability.hint) ? ability.displayName : ability.displayName + "\n<size=28>" + ability.hint + "</size>";
            banner.color = new Color(1f, 1f, 1f, 0f);
            StartCoroutine(FadeText(banner, 1f, .4f));
        }

        // Echo speaks, one line at a time, typed out.
        voice.color = new Color(Mint.r, Mint.g, Mint.b, 1f);
        foreach (string line in echo)
        {
            for (int i = 1; i <= line.Length; i++) { voice.text = "“" + line.Substring(0, i) + "”"; yield return new WaitForSeconds(TypeSeconds); }
            yield return new WaitForSeconds(LineHold);
        }
        StartCoroutine(FadeText(voice, 0f, .5f));
        StartCoroutine(FadeText(banner, 0f, .5f));

        // The stir: the ground shakes harder, the view pulls back, and under the flash the titan moves.
        const float Build = 1.8f;
        for (float t = 0f; t < Build; t += Time.deltaTime)
        {
            float k = t / Build;
            shake = Mathf.Lerp(.02f, .22f, k * k);
            if (view != null) view.orthographicSize = baseSize * (1f + .35f * Mathf.SmoothStep(0f, 1f, k));
            Alpha(curtain, Mathf.Clamp01((k - .7f) / .3f));
            yield return null;
        }
        Alpha(curtain, 1f);
        GameSave.WakeKnot(knot);   // the stir variants change now, unseen
        if (vista != null)
        {
            for (float t = 0f; t < .6f; t += Time.deltaTime) { Alpha(vistaImage, t / .6f); yield return null; }
            yield return new WaitForSeconds(2f);
        }
        else yield return new WaitForSeconds(.35f);
        shake = .08f;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float k = t / 1.2f;
            Alpha(curtain, 1f - k); Alpha(vistaImage, vistaImage.color.a > 0f ? 1f - k : 0f);
            shake = Mathf.Lerp(.08f, 0f, k);
            if (view != null) view.orthographicSize = baseSize * (1f + .35f * (1f - Mathf.SmoothStep(0f, 1f, k)));
            yield return null;
        }
        Alpha(curtain, 0f); Alpha(vistaImage, 0f); shake = 0f;
        if (view != null) view.orthographicSize = baseSize;

        // The caption, and Qori's hands back.
        title.text = "The Qolossal stirs.";
        caption.text = Knots.Stir(knot) + "\n<size=28>Places you have been have changed.</size>";
        caption.color = new Color(Mint.r, Mint.g, Mint.b, 0f);
        StartCoroutine(FadeText(title, 1f, .6f)); StartCoroutine(FadeText(caption, 1f, .9f));
        IsPlaying = false;
        yield return new WaitForSeconds(4.2f);
        StartCoroutine(FadeText(title, 0f, .8f)); yield return FadeText(caption, 0f, .8f);
        Finished = true;
        if (Current == this) Current = null;
        Destroy(gameObject);
    }

    // The shake: an offset added after the camera follows, taken off again before the next follow.
    void Update() { if (view != null) view.transform.position -= shaken; shaken = Vector3.zero; }
    void LateUpdate()
    {
        if (view == null || shake <= 0f) return;
        float t = Time.time;
        shaken = new Vector3(Mathf.PerlinNoise(t * 17f, 0f) - .5f, Mathf.PerlinNoise(0f, t * 19f) - .5f, 0f) * 2f * shake;
        view.transform.position += shaken;
    }

    void OnDestroy()
    {
        if (view != null) { view.transform.position -= shaken; if (IsPlaying) view.orthographicSize = baseSize; }
        if (Current == this) { Current = null; IsPlaying = false; }
    }
}

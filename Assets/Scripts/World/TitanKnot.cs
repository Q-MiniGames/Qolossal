using System.Collections.Generic;
using UnityEngine;

// A knot where the titan's life gathers, at the heart of a Knot Chamber. While its guardian
// lives the knot is choked in thorns and dim. When the guardian falls the thorns wither and the
// knot glows; Qori lays his hand on it by touching it, and the stir sequence plays: the ability,
// Echo's lines, and the titan stirring (StirSequence). A knot already woken glows steadily.
// Placeholder art until Codex's knot is delivered: a glow pod core in a soft mint halo, wrapped
// in the thorn barrier.
[DisallowMultipleComponent]
public sealed class TitanKnot : MonoBehaviour
{
    [Tooltip("A Knots id, e.g. grip.")] public string knot = Knots.Grip;
    public AbilityDefinition ability;
    [Tooltip("The chamber's guardian(s): the knot stays sealed while any of them is alive.")] public List<GameObject> guardians = new List<GameObject>();
    [TextArea] public string[] echoLines = new string[0];
    [Tooltip("Optional full-screen painting of the body part moving, shown through the stir's flash.")] public Sprite vista;
    public SpriteRenderer core, seal;
    public Sprite coreDim, coreLit;

    public enum State { Sealed, Ready, Waking, Awake }
    public State Current { get; private set; }

    SpriteRenderer halo; Texture2D haloTexture; Sprite haloSprite;
    float stateAt; Vector3 coreScale, sealScale;
    PlayerMovement near;
    static readonly Color Mint = new Color(.62f, 1f, .86f);

    void Awake()
    {
        coreScale = core.transform.localScale;
        if (seal != null) sealScale = seal.transform.localScale;
        const int size = 64;
        haloTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Knot halo", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float f = Mathf.Clamp01(1f - new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f).magnitude);
                pixels[y * size + x] = new Color(1f, 1f, 1f, f * f);
            }
        haloTexture.SetPixels(pixels); haloTexture.Apply(false, true);
        haloSprite = Sprite.Create(haloTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        halo = new GameObject("Halo").AddComponent<SpriteRenderer>();
        halo.transform.SetParent(core.transform.parent, false);
        halo.transform.position = core.bounds.center;   // the core's pivot is at its base
        halo.sprite = haloSprite; halo.sortingOrder = core.sortingOrder - 1;
    }

    void Start() => Enter(GameSave.IsKnotAwake(knot) ? State.Awake : GuardiansDefeated ? State.Ready : State.Sealed);

    public bool GuardiansDefeated
    {
        get
        {
            foreach (var g in guardians)
            {
                if (g == null || !g.activeInHierarchy) continue;
                var crawler = g.GetComponent<GroundCreature>(); if (crawler != null && !crawler.IsAlive) continue;
                var enemy = g.GetComponent<EnemyBase>(); if (enemy != null && !enemy.IsAlive) continue;
                return false;
            }
            return true;
        }
    }

    void Enter(State state)
    {
        Current = state; stateAt = Time.time;
        core.sprite = state == State.Sealed ? coreDim : coreLit;
        if (seal != null) seal.enabled = state == State.Sealed || state == State.Ready;   // Ready: withering
        if (state == State.Ready && Fx.Library != null) Fx.Leaves(core.transform.position, 8, 1.4f);
    }

    static PlayerMovement PlayerOf(Collider2D other) =>
        other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<PlayerMovement>() : null;

    void OnTriggerEnter2D(Collider2D other) { var p = PlayerOf(other); if (p != null) { near = p; TryWake(); } }
    void OnTriggerStay2D(Collider2D other) { if (near != null) TryWake(); }
    void OnTriggerExit2D(Collider2D other) { if (PlayerOf(other) == near) near = null; }

    // Qori's hand on the knot: wakes it if it is ready (touching it calls this; so can tests).
    public bool TryWake()
    {
        if (Current != State.Ready || Time.time - stateAt < .8f || StirSequence.IsPlaying || AreaTransition.IsTransitioning) return false;
        if (StirSequence.Play(knot, ability, echoLines, vista) == null) return false;
        Enter(State.Waking);
        var lib = Fx.Library;
        if (lib != null) Fx.Pop(lib.telegraphGlint, core.transform.position, 2.4f, .6f, 45);
        return true;
    }

    void Update()
    {
        float t = Time.time, since = t - stateAt;
        if (Current == State.Sealed && GuardiansDefeated) Enter(State.Ready);
        if (Current == State.Waking && GameSave.IsKnotAwake(knot)) Enter(State.Awake);

        float pulse, glow, grow = 1f;
        switch (Current)
        {
            case State.Sealed: pulse = .03f * Mathf.Sin(t * 1.1f); glow = .12f; break;
            case State.Ready: pulse = .07f * Mathf.Sin(t * 3.2f); glow = .45f + .15f * Mathf.Sin(t * 3.2f); break;
            case State.Waking: pulse = .1f * Mathf.Sin(t * 9f); glow = .8f; grow = 1f + .25f * Mathf.Clamp01(since / .9f); break;
            default: pulse = .04f * Mathf.Sin(t * 1.6f); glow = .55f + .08f * Mathf.Sin(t * 1.6f); break;
        }
        core.transform.localScale = coreScale * grow * (1f + pulse);
        halo.transform.localScale = Vector3.one * (core.bounds.size.x * 2.6f) / Mathf.Max(.0001f, halo.transform.parent.lossyScale.x);
        halo.color = new Color(Mint.r, Mint.g, Mint.b, glow);
        if (seal != null && Current == State.Ready)
        {
            // The thorns wither: they shrink back and fade in the first second.
            float k = Mathf.Clamp01(since / 1f);
            seal.transform.localScale = sealScale * Mathf.Lerp(1f, .7f, k);
            seal.color = new Color(.75f, .65f, .6f, 1f - k);
            if (k >= 1f) seal.enabled = false;
        }
    }

    void OnGUI()
    {
        if (near == null || GamePauseMenu.IsPaused || StirSequence.IsPlaying) return;
        string text = Current == State.Sealed ? "Thorns choke the knot. Its guardian still stands."
            : Current == State.Awake ? Knots.DisplayName(knot) + " is awake." : null;
        if (text == null) return;
        float width = Mathf.Min(520f, Screen.width - 24f);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height - 110f, width, 40f), text);
    }

    void OnDestroy()
    {
        if (haloSprite != null) Destroy(haloSprite);
        if (haloTexture != null) Destroy(haloTexture);
    }
}

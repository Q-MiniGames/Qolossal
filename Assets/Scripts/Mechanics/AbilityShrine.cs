using UnityEngine;

// A root-wrapped stone pedestal with a relic hovering over its leaf cup. When Qori touches it
// the relic's ability unlocks at once; the relic flies into him and bursts into leaves and
// light, and a banner names it and says how to use it. A shrine whose relic Qori already
// has stands empty.
[DisallowMultipleComponent]
public sealed class AbilityShrine : MonoBehaviour
{
    public AbilityDefinition ability;
    public SpriteRenderer relic;
    [Tooltip("Height of the relic's centre above the shrine's base.")] public float hoverHeight = 1.72f;

    enum State { Waiting, Flying, Taken }
    State state;
    PlayerAbilityController taker;
    float stateAt; Vector3 relicScale, flyFrom;
    SpriteRenderer glow; Texture2D glowTexture; Sprite glowSprite;

    const float FlySeconds = .55f, BannerSeconds = 5f;
    static readonly Color Mint = new Color(.62f, 1f, .86f);

    public bool IsTaken => state != State.Waiting;

    void Awake()
    {
        relicScale = relic.transform.localScale;
        // A soft mint halo behind the relic.
        const int size = 64;
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Shrine halo", wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float f = Mathf.Clamp01(1f - new Vector2((x + .5f) / size * 2f - 1f, (y + .5f) / size * 2f - 1f).magnitude);
                pixels[y * size + x] = new Color(1f, 1f, 1f, f * f);
            }
        glowTexture.SetPixels(pixels); glowTexture.Apply(false, true);
        glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
        glow = new GameObject("Halo").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, hoverHeight, 0f);
        glow.sprite = glowSprite; glow.sortingOrder = relic.sortingOrder - 1;
    }

    void Start()
    {
        var player = FindAnyObjectByType<PlayerAbilityController>();
        if (ability != null && player != null && player.HasAbility(ability.abilityId)) SetTaken();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (state != State.Waiting || ability == null || other.attachedRigidbody == null) return;
        var player = other.attachedRigidbody.GetComponent<PlayerAbilityController>();
        if (player == null) return;
        Collect(player);
    }

    // Unlocks the ability now; the flight into Qori is only for show.
    public void Collect(PlayerAbilityController player)
    {
        if (state != State.Waiting) return;
        taker = player; player.Unlock(ability);
        Sfx.Play("Relic_Get");
        state = State.Flying; stateAt = Time.time; flyFrom = relic.transform.position;
    }

    void SetTaken() { state = State.Taken; stateAt = float.NegativeInfinity; relic.enabled = false; glow.enabled = false; }

    void Update()
    {
        float t = Time.time;
        if (state == State.Waiting)
        {
            relic.transform.localPosition = new Vector3(0f, hoverHeight + .08f * Mathf.Sin(t * 2.6f), 0f);
            relic.transform.localRotation = Quaternion.Euler(0f, 0f, 4f * Mathf.Sin(t * 1.3f));
            glow.transform.localScale = Vector3.one * (1.5f + .08f * Mathf.Sin(t * 2.6f));
            glow.color = new Color(Mint.r, Mint.g, Mint.b, .38f + .1f * Mathf.Sin(t * 3.1f));
        }
        else if (state == State.Flying)
        {
            float k = Mathf.Clamp01((t - stateAt) / FlySeconds), ease = k * k;
            Vector3 to = taker != null ? taker.transform.position + Vector3.up * .3f : flyFrom;
            // Lifts a little first, then swoops into Qori, shrinking as it goes.
            relic.transform.position = Vector3.Lerp(flyFrom, to, ease) + Vector3.up * (.6f * Mathf.Sin(k * Mathf.PI));
            relic.transform.localScale = relicScale * Mathf.Lerp(1f, .25f, ease);
            relic.transform.Rotate(0f, 0f, 540f * Time.deltaTime);
            glow.color = new Color(Mint.r, Mint.g, Mint.b, .38f * (1f - k));
            if (k >= 1f) Burst(to);
        }
    }

    void Burst(Vector3 at)
    {
        relic.enabled = false; glow.enabled = false;
        state = State.Taken; stateAt = Time.time;
        var lib = Fx.Library; if (lib == null) return;
        Fx.Pop(lib.telegraphGlint, at, 1.6f, .45f, 45);
        if (lib.checkpointMote != null)
            for (int i = 0; i < 6; i++)
            {
                // The mote art's bright head is at the bottom: turn it to lead outward.
                var mote = Fx.Pop(lib.checkpointMote, at, .35f, .7f, 44, i * 60f);
                if (mote != null) mote.velocity = Quaternion.Euler(0f, 0f, i * 60f - 90f) * Vector2.right * 2.4f;
            }
        Fx.Leaves(at, 8, 1.2f);
    }

    void OnGUI()
    {
        if (state != State.Taken || ability == null || GamePauseMenu.IsPaused || Time.time - stateAt > BannerSeconds) return;
        float fade = Mathf.Clamp01((BannerSeconds - (Time.time - stateAt)) / .6f);
        GUI.color = new Color(1f, 1f, 1f, fade);
        float width = Mathf.Min(520f, Screen.width - 24f);
        string text = string.IsNullOrEmpty(ability.hint) ? ability.displayName : ability.displayName + "\n" + ability.hint;
        GUI.Box(new Rect((Screen.width - width) * .5f, 60f, width, string.IsNullOrEmpty(ability.hint) ? 40f : 58f), text);
        GUI.color = Color.white;
    }

    void OnDestroy()
    {
        if (glowSprite != null) Destroy(glowSprite);
        if (glowTexture != null) Destroy(glowTexture);
    }
}

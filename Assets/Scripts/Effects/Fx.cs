using UnityEngine;

// One-shot sprite effects from the accepted Codex art (Resources/FX/FxLibrary, built by
// Qolossal > FX > Build FX Library): flipbooks such as dust puffs and death puffs, single-sprite
// pops such as sparks and glints, and small bursts of falling leaves.
public static class Fx
{
    static FxLibrary library;
    public static FxLibrary Library => library != null ? library : library = Resources.Load<FxLibrary>("FX/FxLibrary");

    // Plays `frames` once at `fps`, `size` world units across the canvas, then removes itself.
    public static FxFlipbook Play(Sprite[] frames, Vector2 at, float fps, float size, int order = 30, bool flipX = false, float rotation = 0f, Color? tint = null)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null) return null;
        var obj = new GameObject("FX " + frames[0].name);
        obj.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, rotation));
        var r = obj.AddComponent<SpriteRenderer>();
        r.sprite = frames[0]; r.sortingOrder = order; r.flipX = flipX; r.color = tint ?? Color.white;
        float canvas = frames[0].bounds.size.x;
        obj.transform.localScale = Vector3.one * (size / Mathf.Max(.0001f, canvas));
        var fb = obj.AddComponent<FxFlipbook>();
        fb.frames = frames; fb.fps = fps; fb.image = r;
        return fb;
    }

    // A single sprite that pops (scales up) and fades out over `life` seconds.
    public static FxFlipbook Pop(Sprite sprite, Vector2 at, float size, float life = .22f, int order = 32, float rotation = 0f, bool flipX = false)
    {
        var fb = Play(new[] { sprite }, at, 1f / life, size, order, flipX, rotation);
        if (fb != null) { fb.popLife = life; }
        return fb;
    }

    // An enemy defeat: a puff of leaves, bark chips and a mint wisp, plus loose leaves.
    public static void DeathPuff(Vector2 at, float size = 1.3f)
    {
        var lib = Library; if (lib == null) return;
        Play(lib.deathPuff, at, 12f, size, 35);
        Leaves(at, 3, .6f);
    }

    // The bright four-point glint that warns of an enemy attack.
    public static void Glint(Vector2 at) { var lib = Library; if (lib != null) Pop(lib.telegraphGlint, at, .55f, .35f, 45); }

    // A few loose leaves thrown up from `at`; they tumble and fall.
    public static void Leaves(Vector2 at, int count, float strength)
    {
        var lib = Library; if (lib == null || lib.leaves.Length == 0) return;
        for (int i = 0; i < count; i++)
        {
            Sprite s = lib.leaves[Random.Range(0, lib.leaves.Length)];
            var fb = Play(new[] { s }, at + Random.insideUnitCircle * .15f, .5f, .22f + .08f * strength, 28, Random.value < .5f, Random.Range(0f, 360f));
            if (fb == null) continue;
            fb.velocity = new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(.8f, 1.8f)) * (.6f + strength);
            fb.gravity = 3.5f; fb.spin = Random.Range(-220f, 220f); fb.popLife = 0f;
            fb.fadeFrom = .55f;
        }
    }
}

// Runs one effect: steps through frames (or pops a single sprite), with optional drift.
public sealed class FxFlipbook : MonoBehaviour
{
    public Sprite[] frames; public float fps = 12f; public SpriteRenderer image;
    public bool loop;
    public float popLife = -1f;            // > 0: single sprite that pops and fades over this long
    public Vector2 velocity; public float gravity, spin, fadeFrom = -1f;
    float age; Vector3 baseScale;

    void Start() => baseScale = transform.localScale;

    void Update()
    {
        float dt = Time.deltaTime; age += dt;
        velocity.y -= gravity * dt; velocity *= 1f - dt * .6f;
        transform.position += (Vector3)(velocity * dt);
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);
        float life = frames.Length / Mathf.Max(.01f, fps);
        if (popLife > 0f)
        {
            float t = age / popLife;
            transform.localScale = baseScale * (.7f + .45f * Mathf.Sqrt(Mathf.Clamp01(t)));
            SetAlpha(1f - t * t);
            if (t >= 1f) Destroy(gameObject);
            return;
        }
        if (fadeFrom >= 0f)
        {
            float t = age / life;
            SetAlpha(t < fadeFrom ? 1f : 1f - (t - fadeFrom) / (1f - fadeFrom));
            if (t >= 1f) Destroy(gameObject);
            return;
        }
        int frame = Mathf.FloorToInt(age * fps);
        if (loop) frame %= frames.Length;
        else if (frame >= frames.Length) { Destroy(gameObject); return; }
        image.sprite = frames[frame];
    }

    void SetAlpha(float a) { Color c = image.color; c.a = Mathf.Clamp01(a); image.color = c; }
}

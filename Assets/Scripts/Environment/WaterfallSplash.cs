using UnityEngine;

// Brings the still Waterfall_Splash_Base painting to life: the splash swells and settles in an
// uneven rhythm with a slight sway and shimmer, a second, fainter copy pulses out of phase so the
// spray never looks frozen, and droplets are thrown up and fall back into the pool.
[DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class WaterfallSplash : MonoBehaviour
{
    [Tooltip("Sprite for the thrown droplets (a small water drip); none: droplets off.")] public Sprite droplet;
    [Min(0f)] public float dropletsPerSecond = 9f;
    [Tooltip("Half-width of the area droplets are thrown from, world units.")] public float spread = .9f;

    SpriteRenderer image, echo;
    Vector3 baseScale; Color baseColor;
    float nextDrop;

    void Awake()
    {
        image = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale; baseColor = image.color;
        echo = new GameObject("Splash echo").AddComponent<SpriteRenderer>();
        echo.transform.SetParent(transform, false);
        echo.sprite = image.sprite; echo.sortingOrder = image.sortingOrder + 1; echo.flipX = true;
        SfxEmitter.Attach(gameObject, "Waterfall");
    }

    void Update()
    {
        float t = Time.time;
        // Two incommensurate waves so the rhythm never visibly repeats.
        float pulse = .5f * Mathf.Sin(t * 7.3f) + .3f * Mathf.Sin(t * 11.9f + 1.3f) + .2f * Mathf.Sin(t * 3.1f);
        transform.localScale = new Vector3(baseScale.x * (1f + .03f * pulse), baseScale.y * (1f + .09f * pulse), baseScale.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, .8f * Mathf.Sin(t * 2.3f));
        image.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (.9f + .1f * Mathf.Sin(t * 13.7f)));

        float e = Mathf.Repeat(t * 1.6f, 1f);   // the echo: a burst that swells and fades, over and over
        echo.transform.localScale = new Vector3(.85f + .25f * e, .7f + .45f * e, 1f);
        echo.transform.localPosition = new Vector3(0f, -.1f + .15f * e, 0f);
        echo.color = new Color(1f, 1f, 1f, .45f * Mathf.Sin(e * Mathf.PI));

        if (droplet == null || dropletsPerSecond <= 0f || t < nextDrop) return;
        nextDrop = t + Random.Range(.5f, 1.5f) / dropletsPerSecond;
        Vector2 at = (Vector2)transform.position + new Vector2(Random.Range(-spread, spread), Random.Range(-.05f, .25f));
        var fb = Fx.Play(new[] { droplet }, at, .8f, Random.Range(.16f, .3f), image.sortingOrder + 2, Random.value < .5f);
        if (fb == null) return;
        float side = Mathf.Sign(at.x - transform.position.x);
        fb.velocity = new Vector2(side * Random.Range(.5f, 2.2f), Random.Range(2.5f, 4.5f));
        fb.gravity = 14f; fb.fadeFrom = .45f; fb.popLife = 0f;
    }
}

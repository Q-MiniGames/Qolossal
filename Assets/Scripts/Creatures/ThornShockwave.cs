using UnityEngine;

// The claw slam's shockwave: a row of thorns bursting up along the floor, rolling away from the slam (jump it).
public sealed class ThornShockwave : MonoBehaviour, IContactHazard
{
    float direction, speed, left;
    static Sprite thorns;
    public bool HurtsOnContact => true;

    public static void Launch(Vector2 from, float direction, float speed, float range)
    {
        var obj = new GameObject("Thorn shockwave");
        obj.transform.position = from;
        var r = obj.AddComponent<SpriteRenderer>(); r.sprite = Thorns(); r.sortingOrder = 31; r.flipX = direction < 0f;
        obj.transform.localScale = new Vector3(1.4f, 1f, 1f);
        var c = obj.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = new Vector2(.7f, .7f); c.offset = new Vector2(0, .35f);
        var rb = obj.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic;
        var w = obj.AddComponent<ThornShockwave>(); w.direction = direction; w.speed = speed; w.left = range;
    }

    void Update()
    {
        float step = speed * Time.deltaTime;
        Vector2 pos = transform.position;
        if (Physics2D.Raycast(pos + Vector2.up * .3f, new Vector2(direction, 0), step + .2f, GroundLayer.Mask) || (left -= step) <= 0f) { Destroy(gameObject); return; }
        transform.position = pos + new Vector2(direction * step, 0);
        float pulse = 1f + Mathf.Sin(Time.time * 30f) * .12f;
        transform.localScale = new Vector3(1.4f, pulse, 1f);
        if (Fx.Library != null && Random.value < Time.deltaTime * 12f) Fx.Play(Fx.Library.dustRun, pos, 14f, .8f, 30, direction > 0);
    }

    // Placeholder art made in code: three bramble thorns (dark bark with pale tips), until a drawn one exists.
    static Sprite Thorns()
    {
        if (thorns != null) return thorns;
        const int W = 64, H = 40;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[W * H];
        int[] tips = { 12, 32, 50 }; int[] tall = { 26, 38, 22 };
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                for (int k = 0; k < 3; k++)
                {
                    float half = 9f * (1f - y / (float)tall[k]);
                    if (y < tall[k] && Mathf.Abs(x - tips[k]) < half)
                        c = y > tall[k] - 8 ? new Color32(214, 200, 160, 255) : new Color32(92, 64, 44, 255);
                }
                px[y * W + x] = c;
            }
        tex.SetPixels32(px); tex.Apply();
        thorns = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, 0f), 64f);
        return thorns;
    }
}

using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ThreadAnchor))]
public sealed class AnchorSeedGlow : MonoBehaviour
{
    [SerializeField] private Vector2 seedOffset = Vector2.zero;
    [SerializeField, Min(0.01f)] private float glowSize = 0.8f;
    [SerializeField, Range(0f, 1f)] private float selectedOpacity = 0.65f;
    private ThreadAnchor anchor;
    private SpriteRenderer glow;
    private Texture2D texture;
    private Sprite glowSprite;
    private float blend;

    private void Awake()
    {
        anchor = GetComponent<ThreadAnchor>();
        // A soft light overlay keeps the cue within the seed artwork.
        const int size = 64;
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Anchor seed light";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float radius = new Vector2((x + 0.5f) / size * 2f - 1f,
                (y + 0.5f) / size * 2f - 1f).magnitude;
            float falloff = Mathf.Clamp01(1f - radius);
            pixels[y * size + x] = new Color(1f, 1f, 1f, falloff * falloff);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), size);
        GameObject lightObject = new GameObject("Seed Selection Glow");
        lightObject.transform.SetParent(transform, false);
        glow = lightObject.AddComponent<SpriteRenderer>();
        glow.sprite = glowSprite;
        Transform art = transform.Find("AnchorVisual");
        SpriteRenderer artwork = art != null ? art.GetComponent<SpriteRenderer>() : null;
        glow.sortingLayerID = artwork != null ? artwork.sortingLayerID : 0;
        glow.sortingOrder = artwork != null ? artwork.sortingOrder + 1 : 3;
        glow.enabled = false;
    }

    private void LateUpdate()
    {
        blend = Mathf.MoveTowards(blend, anchor.IsHighlighted ? 1f : 0f, Time.deltaTime * 7f);
        glow.enabled = blend > 0.001f;
        glow.transform.localPosition = seedOffset;
        glow.transform.localScale = new Vector3(glowSize, glowSize * 1.15f, 1f);
        float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 3f);
        glow.color = new Color(0.65f, 1f, 1f, blend * selectedOpacity * pulse);
    }

    private void OnDisable()
    {
        blend = 0f;
        if (glow != null) glow.enabled = false;
    }

    private void OnDestroy()
    {
        if (glow != null) Destroy(glow.gameObject);
        if (glowSprite != null) Destroy(glowSprite);
        if (texture != null) Destroy(texture);
    }
}

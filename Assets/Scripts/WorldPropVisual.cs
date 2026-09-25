using UnityEngine;

// Painted props live on their own visual child so artwork cannot resize triggers or collision.
[DisallowMultipleComponent]
public sealed class WorldPropVisual : MonoBehaviour
{
    public enum Kind { GrowthFlower, GrowthPlant, PlatformLeaf, Checkpoint, LevelFinish }
    private Kind kind;
    private SpriteRenderer source, artwork, glow;
    private Transform artRoot;
    private Texture2D glowTexture;
    private Sprite glowSprite;
    private Material material;
    private bool sourceWasEnabled;
    private float floorOffset, baseScale = 1f, energy, targetEnergy, pulse, unfold = 1f;
    private Vector2 glowOffset;
    private float glowDiameter;
    private Color lightColor;
    private readonly SpriteRenderer[] motes = new SpriteRenderer[5];
    public SpriteRenderer Artwork => artwork;
    public bool Ready => artwork != null;
    public bool GroundSeated { get; private set; }

    public static WorldPropVisual Create(SpriteRenderer placeholder, Kind type,
        float platformWidth = 2.2f, float platformTop = 0.175f)
    {
        string[] names = { "GrowthFlower_v1", "GrowthPlatformPlant_v1", "GrowthPlatformLeaf_v1", "Checkpoint_v1", "LevelFinish_v1" };
        Sprite sprite = Resources.Load<Sprite>("WorldProps/" + names[(int)type]);
        Shader shader = type == Kind.PlatformLeaf ? Resources.Load<Shader>("WorldProps/LeafChromaKey") : Shader.Find("Sprites/Default");
        if (sprite == null || shader == null) return null;
        WorldPropVisual visual = placeholder.gameObject.AddComponent<WorldPropVisual>();
        visual.kind = type;
        visual.source = placeholder;
        visual.sourceWasEnabled = placeholder.enabled;
        visual.floorOffset = type == Kind.PlatformLeaf ? platformTop : -placeholder.bounds.extents.y;
        visual.baseScale = type == Kind.PlatformLeaf ? platformWidth / 2.2f : 1f;
        visual.artRoot = new GameObject("Painted " + type).transform;
        visual.artRoot.SetParent(placeholder.transform, false);
        visual.artwork = visual.artRoot.gameObject.AddComponent<SpriteRenderer>();
        visual.material = new Material(shader);
        visual.artwork.sharedMaterial = visual.material;
        visual.artwork.sprite = sprite;
        visual.artwork.color = Color.white;
        visual.artwork.sortingLayerID = placeholder.sortingLayerID;
        visual.artwork.sortingOrder = Mathf.Max(placeholder.sortingOrder, 1);
        placeholder.enabled = false;
        if (type != Kind.PlatformLeaf) visual.InitializeLights();
        visual.ApplyTransform();
        return visual;
    }

    private void Start()
    {
        if (kind == Kind.PlatformLeaf) { GroundSeated = true; return; }
        // Seat decorative roots on nearby ground without moving the interaction marker.
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position + Vector3.up * .25f,
            Vector2.down, 3f, LayerMask.GetMask("Ground"));
        foreach (RaycastHit2D ground in hits)
        {
            if (ground.collider == null || ground.collider.isTrigger || ground.normal.y <= .5f ||
                ground.collider.GetComponentInParent<GrowthFlower>() != null) continue;
            // Bury the root tips slightly in the moss instead of balancing on its edge.
            floorOffset = ground.point.y - transform.position.y - .035f;
            break;
        }
        ApplyTransform();
        GroundSeated = true;
    }

    public void SetState(float light, float activationPulse = 0f)
    {
        targetEnergy = Mathf.Clamp01(light);
        pulse = Mathf.Clamp01(activationPulse);
    }

    public void SetUnfold(float amount) { unfold = Mathf.Clamp01(amount); }

    private void ApplyTransform()
    {
        if (artRoot == null) return;
        artRoot.position = transform.position + Vector3.up * floorOffset;
        artRoot.rotation = Quaternion.identity;
        float sx = baseScale, sy = baseScale;
        if (kind == Kind.GrowthFlower) { sx *= 1f - pulse * 0.06f; sy *= 1f + pulse * 0.16f; }
        if (kind == Kind.PlatformLeaf) { sx *= Mathf.Lerp(0.25f, 1f, unfold); sy *= Mathf.Lerp(0.65f, 1f, unfold); }
        Vector3 parentScale = transform.lossyScale;
        artRoot.localScale = new Vector3(sx / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
            sy / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
    }

    private void InitializeLights()
    {
        lightColor = kind == Kind.Checkpoint ? new Color(1f, 0.79f, 0.32f) : new Color(0.55f, 1f, 0.84f);
        glowOffset = kind == Kind.GrowthFlower ? new Vector2(0f, 0.42f) :
            kind == Kind.GrowthPlant ? new Vector2(0.20f, 0.46f) :
            kind == Kind.Checkpoint ? new Vector2(-0.02f, 0.79f) : new Vector2(0f, 2.78f);
        glowDiameter = kind == Kind.LevelFinish ? 0.55f : kind == Kind.GrowthPlant ? 0.22f : 0.4f;
        const int size = 48;
        glowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        glowTexture.wrapMode = TextureWrapMode.Clamp;
        glowTexture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size*size];
        for (int y=0; y<size; y++)
        for (int x=0; x<size; x++)
        {
            float radius = new Vector2((x+.5f)/size*2f-1f, (y+.5f)/size*2f-1f).magnitude;
            float a = Mathf.Clamp01(1f-radius);
            pixels[y*size+x] = new Color(1f,1f,1f,a*a);
        }
        glowTexture.SetPixels(pixels);
        glowTexture.Apply(false,true);
        glowSprite = Sprite.Create(glowTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),size);
        glow = CreateLight("Seed light",glowDiameter);
        glow.transform.localPosition = glowOffset;
        for (int i=0; i<motes.Length; i++) motes[i] = CreateLight("Rising seed mote " + i, 0.055f);
    }

    private SpriteRenderer CreateLight(string title, float diameter)
    {
        GameObject child = new GameObject(title);
        child.transform.SetParent(artRoot,false);
        child.transform.localScale = Vector3.one*diameter;
        SpriteRenderer sprite = child.AddComponent<SpriteRenderer>();
        sprite.sprite = glowSprite;
        sprite.sharedMaterial = material;
        sprite.sortingLayerID = artwork.sortingLayerID;
        sprite.sortingOrder = artwork.sortingOrder+1;
        sprite.color = Color.clear;
        return sprite;
    }

    private void LateUpdate()
    {
        if (!Ready) return;
        ApplyTransform();
        if (kind == Kind.PlatformLeaf) return;
        energy = Mathf.MoveTowards(energy,targetEnergy,Time.deltaTime*4f);
        float brightness = kind == Kind.Checkpoint ? Mathf.Lerp(0.62f,1f,energy) : 1f;
        artwork.color = new Color(brightness,brightness,brightness,1f);
        float breathe = 0.88f + 0.12f*Mathf.Sin(Time.time*2.5f);
        Color light = lightColor; light.a = energy*breathe*0.7f;
        glow.color = light;
        float strength = kind == Kind.GrowthFlower ? pulse : Mathf.Max(0f,energy-0.5f)*2f;
        for (int i=0; i<motes.Length; i++)
        {
            float t = Mathf.Repeat(Time.time*0.65f+i/(float)motes.Length,1f);
            float x = Mathf.Sin(i*2.4f+t*3f)*0.14f;
            motes[i].transform.localPosition = glowOffset+new Vector2(x,t*0.6f);
            Color color = lightColor; color.a = Mathf.Sin(t*Mathf.PI)*strength*0.8f;
            motes[i].color = color;
        }
    }

    private void OnEnable() { if (artRoot != null) artRoot.gameObject.SetActive(true); }
    private void OnDisable() { if (artRoot != null) artRoot.gameObject.SetActive(false); }
    private void OnDestroy()
    {
        if (source != null) source.enabled = sourceWasEnabled;
        if (artRoot != null) Destroy(artRoot.gameObject);
        if (glowSprite != null) Destroy(glowSprite);
        if (glowTexture != null) Destroy(glowTexture);
        if (material != null) Destroy(material);
    }
}

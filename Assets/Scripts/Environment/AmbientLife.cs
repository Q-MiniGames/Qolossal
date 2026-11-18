using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The world's ambient life (polish plan, WS5): wind in the plants, moving water, flickering lamps and shrine flames,
// creeping thorns, gust streaks, drips, dust, leaves, mist, light shafts, birds and butterflies. It starts by itself in
// every Mountain Relief Atlas scene and edits none: it finds the art by sprite name, swaps materials on the copies in
// memory and draws the rest with a few pooled particle systems, so the accepted art bytes are unchanged and repainted
// backgrounds need nothing here. It adds no collider and only reads physics, so nothing here can touch gameplay.
[DefaultExecutionOrder(10001), DisallowMultipleComponent]
public sealed class AmbientLife : MonoBehaviour
{
    // 0 full, 1 reduced, 2 minimal. Consoles haven't been measured: these are starting values.
    public static int Tier = DefaultTier();
    public static bool AutoStart = true;
    public static AmbientLife Instance { get; private set; }

    public int SwayCount { get; private set; }
    public int WaterCount { get; private set; }
    public int ThornCount { get; private set; }
    public int GustCount { get; private set; }
    public int ShrineCount { get; private set; }
    public int LampCount { get; private set; }
    public float CostMs { get; private set; }
    public string ProfileName => profile.name;
    public float Wind => wind;
    public int LiveParticles { get { int n = 0; foreach (var l in layers) n += l.system.particleCount; return n; } }
    public int MaxParticles { get { int n = 0; foreach (var l in layers) n += l.system.main.maxParticles; return n; } }

    static int DefaultTier()
    {
#if UNITY_SWITCH
        return 1;
#else
        return 0;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (AutoStart && Instance == null && FindAnyObjectByType<MraSession>() != null) Create();
    }

    public static AmbientLife Create() => new GameObject("Ambient life (generated)").AddComponent<AmbientLife>();

    // --- What each place feels like ---------------------------------------------------------------------------------

    sealed class Profile
    {
        public string name;
        public float wind = .3f;
        public float dust = 1.2f; public Color dustColor = new Color(1f, .93f, .75f, .45f); public float dustSize = .16f;
        public float leaves; public Color leafColor = Color.white;
        public float mist; public Color mistColor = new Color(.85f, .88f, .85f, 1f);
        public float shafts; public Color shaftColor = new Color(1f, .93f, .72f, 1f);
        public float fireflies, drips, skyWisps;
        public int butterflies, birds;
    }

    // Never mint here: mint means the titan is alive. Caves take a fraction of their region's wind (CaveOf).
    static readonly Dictionary<string, Profile> Regions = new Dictionary<string, Profile>
    {
        ["MR01"] = new Profile { name = "Cradle", wind = .35f, dust = 1.3f, dustColor = new Color(.95f, .95f, .68f, .45f), leaves = .12f, mist = .1f, shafts = .1f, fireflies = .7f },
        ["MR02"] = new Profile { name = "Causeway", wind = .55f, dust = 1.2f, dustColor = new Color(1f, .96f, .8f, .5f), leaves = .1f, skyWisps = .1f, butterflies = 3, birds = 5 },
        ["MR03"] = new Profile { name = "Terraces", wind = .65f, dust = 1.6f, dustColor = new Color(.98f, .86f, .58f, .5f), leaves = .22f, leafColor = new Color(1f, .85f, .55f), skyWisps = .1f, butterflies = 3, birds = 5 },
        ["MR04"] = new Profile { name = "Qvale", wind = .35f, dust = 1f, dustColor = new Color(1f, .84f, .58f, .45f), fireflies = .8f, skyWisps = .08f, birds = 3, mist = .06f },
        ["MR05"] = new Profile { name = "Ribwood", wind = .3f, dust = 1.8f, dustColor = new Color(.9f, .92f, .62f, .5f), leaves = .55f, mist = .14f, shafts = .16f, fireflies = .4f },
        ["MR06"] = new Profile { name = "Heights", wind = .95f, dust = 1.4f, dustColor = new Color(.9f, .94f, 1f, .4f), leaves = .1f, skyWisps = .1f, birds = 4 },
        ["MR07"] = new Profile { name = "Summit", wind = 1.15f, dust = 1.8f, dustColor = new Color(.92f, .96f, 1f, .45f), mist = .14f, skyWisps = .08f, birds = 2 },
    };

    // Inside a home: still air and a little warm dust, no weather or sky.
    static readonly Profile Indoors = new Profile { name = "Indoors", wind = .04f, dust = .7f, dustColor = new Color(1f, .86f, .62f, .4f) };

    static Profile CaveOf(string group)
    {
        Profile cave = new Profile { name = "Cave " + group, wind = .08f, dust = 1f, dustColor = new Color(.85f, .8f, .68f, .35f), mist = .07f, mistColor = new Color(.6f, .65f, .7f, 1f), drips = .5f };
        switch (group)
        {
            case "A": cave.drips = 1.3f; cave.mist = .1f; cave.dustColor = new Color(.72f, .82f, .9f, .3f); break;
            case "C": cave.drips = .3f; cave.dust = 1.8f; cave.dustColor = new Color(.92f, .85f, .5f, .45f); cave.mist = .1f; break;
            case "D": cave.wind = .4f; cave.drips = .1f; cave.dust = 1.5f; cave.dustColor = new Color(.88f, .9f, .95f, .35f); break;
            case "E": cave.wind = .3f; cave.drips = .8f; cave.mist = .12f; cave.dustColor = new Color(.88f, .92f, 1f, .35f); break;
            case "H": cave.wind = .05f; cave.drips = .2f; cave.dust = .8f; cave.dustColor = new Color(1f, .78f, .5f, .35f); break;
        }
        return cave;
    }

    // --- Pooled particle layers -------------------------------------------------------------------------------------

    sealed class Layer
    {
        public ParticleSystem system;
        ParticleSystem.EmitParams shot = new ParticleSystem.EmitParams { applyShapeToPosition = false };

        public void Emit(Vector2 at, Vector2 velocity, Vector2 size, float life, Color color, float rotation = 0f, float spin = 0f)
        {
            shot.position = at; shot.velocity = velocity;
            shot.startSize3D = new Vector3(size.x, size.y, 1f);
            shot.startLifetime = life; shot.startColor = color;
            shot.rotation = rotation; shot.angularVelocity = spin;
            system.Emit(shot, 1);
        }
    }

    readonly List<Layer> layers = new List<Layer>();
    Layer dust, embers, leaves, mist, skyMist, shafts, drops, rings, streaks, glows;

    Layer MakeLayer(string name, Texture picture, int max, int order, bool added, float gravity = 0f, float noise = 0f, bool stretch = false, float grow = 0f)
    {
        var go = new GameObject("Ambient " + name);
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(1, Tier == 0 ? max : Tier == 1 ? max * 6 / 10 : max * 3 / 10);
        main.gravityModifier = gravity; main.startSize3D = true; main.startRotation3D = false;
        var emission = system.emission; emission.enabled = false;
        var shape = system.shape; shape.enabled = false;
        var fade = system.colorOverLifetime; fade.enabled = true;
        var curve = new Gradient();
        curve.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, .2f), new GradientAlphaKey(1f, .7f), new GradientAlphaKey(0f, 1f) });
        fade.color = curve;
        if (noise > 0f)
        {
            var wander = system.noise; wander.enabled = true; wander.strength = noise; wander.frequency = .35f; wander.scrollSpeed = .2f; wander.damping = false; wander.quality = ParticleSystemNoiseQuality.Low;
        }
        if (grow > 0f)
        {
            var size = system.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f - grow, 1f, 1f));
        }
        var view = go.GetComponent<ParticleSystemRenderer>();
        view.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (stretch) { view.velocityScale = .02f; view.lengthScale = 1f; }
        view.sortingOrder = order;
        var material = new Material(Shader.Find("Qolossal/AmbientParticle")) { name = "Ambient " + name, hideFlags = HideFlags.DontSave, mainTexture = picture };
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)(added ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
        view.sharedMaterial = material;
        system.Play();
        var layer = new Layer { system = system };
        layers.Add(layer);
        return layer;
    }

    // Small pictures made here so they can take any tint. Procedural only where no accepted art fits.
    static Texture2D softDot, shaftPicture;
    static Sprite haloSprite;

    static Texture2D Paint(string name, int w, int h, System.Func<float, float, float> alpha)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = name, hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(alpha((x + .5f) / w, (y + .5f) / h))));
        t.SetPixels32(pixels); t.Apply(false, true);
        return t;
    }

    static void PaintPictures()
    {
        if (softDot != null) return;
        softDot = Paint("Ambient soft dot", 32, 32, (x, y) => { float d = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(.5f, .5f)) * 2f); return d * d; });
        shaftPicture = Paint("Ambient shaft", 64, 256, (x, y) => { float across = Mathf.Sin(x * Mathf.PI); return across * across * Mathf.SmoothStep(0f, .35f, y) * Mathf.SmoothStep(1f, .85f, y); });
        haloSprite = Sprite.Create(softDot, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32f);
    }

    // --- Setting up ---------------------------------------------------------------------------------------------------

    static readonly int WindId = Shader.PropertyToID("_AmbientWind"), ActorId = Shader.PropertyToID("_AmbientActor"), TimeId = Shader.PropertyToID("_AmbientTime"), ShiverId = Shader.PropertyToID("_Shiver");
    static readonly int RootId = Shader.PropertyToID("_Root"), AmpId = Shader.PropertyToID("_Amp"), RateId = Shader.PropertyToID("_Rate"), RippleId = Shader.PropertyToID("_Ripple"), PartId = Shader.PropertyToID("_Part"), DirId = Shader.PropertyToID("_SwayDir");
    static readonly int FlowId = Shader.PropertyToID("_Flow"), WobbleId = Shader.PropertyToID("_Wobble"), GlintId = Shader.PropertyToID("_Glint");

    Profile profile = new Profile { name = "none" };
    MraSession session;
    RegionalUnderground underground;
    Camera view;
    AmbientKit kit;
    PlayerMovement qori;
    int groundMask;
    float wind, windPhase, nextGroup;
    string lastGroup;
    float dustDebt, leafDebt, mistDebt, skyDebt, shaftDebt, fireflyDebt, dripDebt, shrineCheck;

    sealed class Flicker { public SpriteRenderer source, art, halo; public Color artColor, haloColor; public Vector3 haloScale; public float seed; public Vector2 flame; }
    sealed class Thorn { public SpriteRenderer art; public MaterialPropertyBlock block; public float shiver; }
    sealed class Lane { public MraUgGust gust; public Rect rect; public Vector2 dir; }
    sealed class Drip { public float at; public Vector2 floor; }
    sealed class Butterfly { public Transform body; public Vector2 home; public float seed, size; public SpriteRenderer art; }
    sealed class Bird { public Transform body; public SpriteRenderer art; public Vector2 offset; public float seed; }
    sealed class Splash { public SpriteRenderer art, source; public MraTimedHazard hazard; public Vector2 at; public float seed; }
    sealed class Perched { public SpriteRenderer art, twig; public Vector2 home; public Vector2 velocity; public float clock, back, twigSpeed; public int stage; }

    readonly List<Flicker> flames = new List<Flicker>();
    readonly List<Thorn> thorns = new List<Thorn>();
    readonly List<Lane> lanes = new List<Lane>();
    readonly List<Drip> falling = new List<Drip>();
    readonly List<Butterfly> butterflies = new List<Butterfly>();
    readonly List<Bird> birds = new List<Bird>();
    readonly List<Splash> splashes = new List<Splash>();
    readonly List<Perched> perched = new List<Perched>();
    readonly List<Material> made = new List<Material>();
    Vector2 forge; SpriteRenderer smith, smithy, forgeFire; bool hasForge;
#if UNITY_EDITOR
    bool previewForge;
#endif
    Sprite[] forgeFrames;
    static readonly Vector2 ForgeFirePixel = new Vector2(1441f, 680f);   // the frames' centre in the smithy painting, from its top left
    int skyOrder = -95, hazeOrder = -60;
    float flockAt, nextFlock; int flockDir = 1; Vector2 flockCamera;
    Transform flock;

    void Awake()
    {
        Instance = this;
        PaintPictures();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (var m in made) Destroy(m);
        foreach (var l in layers) if (l.system != null) Destroy(l.system.GetComponent<ParticleSystemRenderer>().sharedMaterial);
    }

    void Start()
    {
        session = MraSession.Current;
        underground = FindAnyObjectByType<RegionalUnderground>();
        qori = FindAnyObjectByType<PlayerMovement>();
        view = Camera.main;
        groundMask = LayerMask.GetMask("Ground");
        ReadBackgroundOrders();
        kit = Resources.Load<AmbientKit>("FX/AmbientKit");
        if (kit == null) { Debug.LogError("[AmbientLife] FX/AmbientKit is missing; run Qolossal > FX > Build Ambient Kit."); enabled = false; return; }

        dust = MakeLayer("dust", softDot, 90, 30, false, 0f, .3f);
        embers = MakeLayer("embers", softDot, 30, 30, true, 0f, .5f);
        leaves = MakeLayer("leaves", kit.leaf.texture, 16, 30, false, 0f, .25f);
        mist = MakeLayer("mist", kit.mist.texture, 10, hazeOrder, false);
        skyMist = MakeLayer("sky mist", kit.mist.texture, 6, skyOrder, false);
        shafts = MakeLayer("shafts", shaftPicture, 5, hazeOrder + 1, true);
        drops = MakeLayer("drops", softDot, 40, 30, false, 1f, 0f, true);
        rings = MakeLayer("rings", kit.ripple.texture, 8, 8, false, 0f, 0f, false, .6f);
        streaks = MakeLayer("streaks", kit.gust.texture, 30, 32, false);
        glows = MakeLayer("fireflies", kit.firefly.texture, 10, 30, true, 0f, .6f);

        var all = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var art in all)
        {
            if (art == null || art.sprite == null) continue;
            string name = art.sprite.name;
            if (ThornArt(art)) continue;
            if (WaterArt(art, name)) continue;
            if (FlameArt(art, name)) continue;
            SwayArt(art, name);
        }
        MakeForgeFire(kit);
        FindGustLanes();
        Choose(true);
        wind = profile.wind;
        Publish(0f);
        MakeButterflies(all);
        MakeFlock();
        MakePerchedBirds(all);
        Prewarm();
    }

    void ReadBackgroundOrders()
    {
        // Sky wisps go just over the farthest plane; haze just over the nearest plane behind the terrain.
        int far = int.MaxValue, near = int.MinValue;
        foreach (var plane in FindObjectsByType<ParallaxLayer>(FindObjectsSortMode.None))
        {
            if (plane.sortingOrder >= -40) continue;
            far = Mathf.Min(far, plane.sortingOrder); near = Mathf.Max(near, plane.sortingOrder);
        }
        if (far != int.MaxValue) { skyOrder = far + 1; hazeOrder = near + 1; }
        else if (underground != null) { hazeOrder = -60; }
    }

    // Plants, by the word in their art's name: rooted at the foot, or hanging from the top.
    static readonly string[] Rooted = { "grass", "fern", "reed", "hay_tuft", "flowers", "daisies", "seedheads", "mosstuft", "moss_tuft" };
    static readonly string[] Hanging = { "hanging", "ivy", "lash_root", "roottassel", "trailing" };

    bool SwayArt(SpriteRenderer art, string spriteName)
    {
        if (Tier >= 2 && SwayCount >= 40) return false;
        string lower = spriteName.ToLowerInvariant();
        bool hangs = Contains(lower, Hanging), rooted = !hangs && Contains(lower, Rooted);
        if ((!hangs && !rooted) || art.drawMode != SpriteDrawMode.Simple) return false;
        float height = art.bounds.size.y;
        float amp = Mathf.Clamp(height * (hangs ? .05f : .1f), .03f, .16f);
        ApplySway(art, hangs, amp, 1f, .55f, Mathf.Min(.28f, height * .25f));
        SwayCount++;
        return true;
    }

    static bool Contains(string name, string[] words)
    {
        foreach (var w in words) if (name.Contains(w)) return true;
        return false;
    }

    readonly Dictionary<(Sprite, bool, float, float, float, float, bool), Material> sways = new Dictionary<(Sprite, bool, float, float, float, float, bool), Material>();

    void ApplySway(SpriteRenderer art, bool rootAtTop, float amp, float rate, float ripple, float part)
    {
        var shader = Shader.Find("Qolossal/AmbientSway");
        if (shader == null) return;
        // Which end of the art is world up, so a mirrored or upended sprite keeps its true root.
        Vector3 up = art.transform.up;
        float scale = art.transform.lossyScale.y;
        bool uvTopIsUp = (up.y >= 0f) == (scale >= 0f);
        if (art.flipY) uvTopIsUp = !uvTopIsUp;
        bool sideways = Mathf.Abs(up.y) < .5f;
        bool rootAtUvTop = rootAtTop == uvTopIsUp;
        amp = Mathf.Round(amp * 50f) / 50f; part = Mathf.Round(part * 25f) / 25f;
        var key = (art.sprite, rootAtUvTop, amp, rate, ripple, part, sideways);
        if (!sways.TryGetValue(key, out var material))
        {
            material = new Material(shader) { name = "Sway " + art.sprite.name, hideFlags = HideFlags.DontSave };
            float min = 1f, max = 0f;
            foreach (var uv in art.sprite.uv) { min = Mathf.Min(min, uv.y); max = Mathf.Max(max, uv.y); }
            float span = Mathf.Max(.0001f, max - min);
            material.SetVector(RootId, rootAtUvTop ? new Vector4(max, -1f / span) : new Vector4(min, 1f / span));
            material.SetFloat(AmpId, amp); material.SetFloat(RateId, rate); material.SetFloat(RippleId, ripple); material.SetFloat(PartId, part);
            material.SetVector(DirId, sideways ? Vector2.up : Vector2.right);
            sways[key] = material; made.Add(material);
        }
        art.sharedMaterial = material;
    }

    // Slow creep, never a change in how far they reach: Thorn art moves by a few hundredths of a unit.
    bool ThornArt(SpriteRenderer art)
    {
        if (!art.sprite.name.StartsWith("Thorns_", System.StringComparison.Ordinal)) return false;
        var hazard = art.GetComponentInParent<ThornHazard>(true);
        if (hazard == null) return false;
        ApplySway(art, false, .05f, .55f, 1.4f, .08f);
        thorns.Add(new Thorn { art = art, block = new MaterialPropertyBlock() });
        hazard.gameObject.AddComponent<AmbientThornTouch>().Init(this);
        ThornCount++;
        return true;
    }

    readonly Dictionary<(Sprite, Vector2), Material> waters = new Dictionary<(Sprite, Vector2), Material>();

    bool WaterArt(SpriteRenderer art, string name)
    {
        bool sheet = name.StartsWith("Water_", System.StringComparison.Ordinal) && !name.Contains("Drip") && !name.Contains("Ripple");
        bool fall = name.StartsWith("Spill_", System.StringComparison.Ordinal) || name.StartsWith("Waterfall_Column", System.StringComparison.Ordinal);
        bool gust = name.StartsWith("Gust_", System.StringComparison.Ordinal);
        if (!sheet && !fall && !gust) return false;
        // A tile cut out of a bigger texture can't wrap: it stays still.
        var s = art.sprite;
        if (s.packed || s.rect.width != s.texture.width || s.rect.height != s.texture.height) return true;
        if (gust) return true;   // takes its flow from its lane in FindGustLanes
        SetWater(art, fall ? new Vector2(0f, .45f) : new Vector2(.035f, 0f), fall ? .006f : .014f, fall ? .15f : .5f);
        WaterCount++;
        if (fall) MakeSplash(art);
        if (sheet && Tier < 2 && art.GetComponentInParent<Hazard>() != null)
        {
            MakeCaustic(art);
            art.GetComponentInParent<Hazard>().gameObject.AddComponent<AmbientWaterTouch>().Init(this);
        }
        return true;
    }

    // Where a fall lands. Shows only while a timed spill is running, so it also says when the spill is on.
    void MakeSplash(SpriteRenderer fall)
    {
        if (Tier >= 2 || kit.splash == null || kit.splash.Length == 0) return;
        var box = fall.bounds;
        var ground = Physics2D.Raycast(new Vector2(box.center.x, box.min.y + 1f), Vector2.down, 3f, groundMask);
        float floor = ground.collider != null ? ground.point.y : box.min.y;
        float across = Mathf.Max(1.6f, box.size.x * 1.8f);
        var size = kit.splash[0].bounds.size;
        var art = new GameObject("Ambient splash").AddComponent<SpriteRenderer>();
        art.transform.SetParent(fall.transform.parent, true);
        float scale = across / size.x;
        art.transform.localScale = new Vector3(scale, scale, 1f);
        art.transform.position = new Vector3(box.center.x, floor + size.y * scale * .46f, 0f);
        art.sprite = kit.splash[0]; art.sortingOrder = fall.sortingOrder + 1;
        splashes.Add(new Splash { art = art, source = fall, hazard = fall.GetComponentInParent<MraTimedHazard>(), at = art.transform.position, seed = Random.value * 8f });
    }

    void SetWater(SpriteRenderer art, Vector2 flow, float wobble, float glint)
    {
        var shader = Shader.Find("Qolossal/AmbientWater");
        if (shader == null) return;
        if (!waters.TryGetValue((art.sprite, flow), out var material))
        {
            material = new Material(shader) { name = "Water " + art.sprite.name, hideFlags = HideFlags.DontSave };
            material.SetVector(FlowId, flow); material.SetFloat(WobbleId, wobble); material.SetFloat(GlintId, glint);
            waters[(art.sprite, flow)] = material; made.Add(material);
        }
        art.sharedMaterial = material;
    }

    static Mesh causticQuad;
    Material causticMaterial;

    void MakeCaustic(SpriteRenderer water)
    {
        if (causticMaterial == null)
        {
            causticMaterial = new Material(Shader.Find("Qolossal/AmbientCaustic")) { name = "Ambient caustics", hideFlags = HideFlags.DontSave };
            made.Add(causticMaterial);
        }
        if (causticQuad == null)
            causticQuad = new Mesh
            {
                name = "Ambient caustic quad", hideFlags = HideFlags.DontSave,
                vertices = new[] { new Vector3(0f, 0f), new Vector3(1f, 0f), new Vector3(0f, 1f), new Vector3(1f, 1f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 1, 2, 3 },
            };
        var bounds = water.bounds;
        var go = new GameObject("Ambient caustics");
        go.transform.SetParent(water.transform.parent, true);
        go.transform.position = new Vector3(bounds.min.x - .5f, bounds.max.y - .1f, 0f);
        go.transform.localScale = new Vector3(bounds.size.x + 1f, 3f, 1f);
        go.AddComponent<MeshFilter>().sharedMesh = causticQuad;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = causticMaterial; r.sortingOrder = 4;
    }

    // A looping Ludo fire over the hearth's mouth only, once the smith is home; the painting underneath is untouched.
    void MakeForgeFire(AmbientKit kit)
    {
        if (smithy == null || kit.forgeFire == null || kit.forgeFire.Length == 0) return;
        forgeFrames = kit.forgeFire;
        forgeFire = new GameObject("Ambient forge fire").AddComponent<SpriteRenderer>();
        var painting = smithy.sprite;
        forgeFire.transform.SetParent(smithy.transform, false);
        forgeFire.transform.localPosition = new Vector3((ForgeFirePixel.x - painting.pivot.x) / painting.pixelsPerUnit, (painting.rect.height - ForgeFirePixel.y - painting.pivot.y) / painting.pixelsPerUnit, 0f);
        forgeFire.sprite = forgeFrames[0];
        forgeFire.sortingOrder = smithy.sortingOrder + 1;
    }

    void UpdateForgeFire()
    {
        if (forgeFire == null) return;
        bool lit = smith != null && smith.gameObject.activeInHierarchy;
#if UNITY_EDITOR
        // Editor only: F9 previews the lit forge without touching the save, since it needs Brannick's rescue otherwise.
        var keys = UnityEngine.InputSystem.Keyboard.current;
        if (keys != null && keys.f9Key.wasPressedThisFrame) previewForge = !previewForge;
        lit |= previewForge;
#endif
        if (forgeFire.enabled != lit) forgeFire.enabled = lit;
        if (lit) forgeFire.sprite = forgeFrames[Mathf.FloorToInt(Time.time * 12f) % forgeFrames.Length];
    }

    bool FlameArt(SpriteRenderer art, string name)
    {
        bool lamp = name == "Town_Qvale_StreetLamp_Glow", shrine = name.StartsWith("Checkpoint_Shrine", System.StringComparison.Ordinal);
        if (name == "Town_Smith_Body") smith = art;
        if (name == "Town_Qvale_Smithy") { var b = art.bounds; hasForge = true; forge = new Vector2(b.min.x + b.size.x * .69f, b.min.y + b.size.y * .2f); smithy = art; }
        if (!lamp && !shrine) return false;
        var box = art.bounds;
        Vector2 flame = lamp ? new Vector2(box.min.x + box.size.x * .73f, box.min.y + box.size.y * .68f) : new Vector2(box.center.x, box.min.y + box.size.y * .86f);
        var flicker = new Flicker { source = art, art = lamp ? art : null, artColor = art.color, seed = Random.value * 50f, flame = flame };
        if (Tier < 2)
        {
            var halo = new GameObject("Ambient flame glow").AddComponent<SpriteRenderer>();
            halo.transform.SetParent(art.transform, true);
            halo.transform.position = new Vector3(flame.x, flame.y, 0f);
            float across = lamp ? 4f : 1.8f;
            Vector3 parent = art.transform.lossyScale;
            halo.transform.localScale = new Vector3(across / Mathf.Max(.0001f, parent.x), across / Mathf.Max(.0001f, parent.y), 1f);
            halo.sprite = haloSprite; halo.sharedMaterial = HaloMaterial();
            halo.sortingOrder = art.sortingOrder + 1;
            flicker.halo = halo; flicker.haloColor = lamp ? new Color(1f, .68f, .32f, .16f) : new Color(1f, .62f, .25f, .14f);
            flicker.haloScale = halo.transform.localScale;
        }
        flames.Add(flicker);
        if (lamp) LampCount++; else ShrineCount++;
        return true;
    }

    Material haloMaterial;
    Material HaloMaterial()
    {
        if (haloMaterial != null) return haloMaterial;
        haloMaterial = new Material(Shader.Find("Qolossal/AmbientHalo")) { name = "Ambient halo", hideFlags = HideFlags.DontSave };
        made.Add(haloMaterial);
        return haloMaterial;
    }

    // A lane blows toward the shelter it sends Qori back to, so its streaks tell the truth about the direction.
    void FindGustLanes()
    {
        var shelters = FindObjectsByType<MraUgShelter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var gust in FindObjectsByType<MraUgGust>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var box = gust.GetComponent<BoxCollider2D>();
            Vector2 centre = box.transform.TransformPoint(box.offset), size = Vector2.Scale(box.size, box.transform.lossyScale);
            var rect = new Rect(centre - size * .5f, size);
            Vector2 toward = Vector2.left; float best = float.MaxValue;
            foreach (var shelter in shelters)
            {
                float d = (shelter.feet - centre).sqrMagnitude;
                if (d < best) { best = d; toward = shelter.feet - centre; }
            }
            Vector2 dir = size.x >= size.y ? new Vector2(toward.x < 0f ? -1f : 1f, 0f) : new Vector2(0f, toward.y < 0f ? -1f : 1f);
            lanes.Add(new Lane { gust = gust, rect = rect, dir = dir });
            GustCount++;
            if (gust.art != null && gust.art.sprite != null && !gust.art.sprite.packed && size.x >= size.y)
                SetWater(gust.art, new Vector2(-dir.x * .35f, 0f), .004f, 0f);
        }
    }

    void MakeButterflies(SpriteRenderer[] all)
    {
        if (Tier >= 2 || profile.butterflies <= 0 || kit.butterfly == null) return;
        var homes = new List<Vector2>();
        foreach (var art in all)
        {
            if (art == null || art.sprite == null) continue;
            string lower = art.sprite.name.ToLowerInvariant();
            if (lower.Contains("flowers") || lower.Contains("daisies") || lower.Contains("bean_basket")) homes.Add(art.bounds.center + Vector3.up * .5f);
        }
        if (homes.Count == 0) return;
        Color[] tints = { Color.white, new Color(1f, .84f, .78f), new Color(.92f, .96f, 1f) };
        float size = .34f / kit.butterfly.bounds.size.x;
        for (int i = 0; i < profile.butterflies * homes.Count && i < 12; i++)
        {
            var art = new GameObject("Ambient butterfly").AddComponent<SpriteRenderer>();
            art.transform.SetParent(transform, false);
            art.transform.localScale = new Vector3(size, size, 1f);
            art.sprite = kit.butterfly; art.color = tints[i % tints.Length]; art.sortingOrder = 30;
            butterflies.Add(new Butterfly { body = art.transform, art = art, home = homes[i % homes.Count], seed = Random.value * 50f, size = size });
        }
    }

    void MakeFlock()
    {
        if (profile.birds <= 0 || Tier >= 2 || kit.birdFlap == null || kit.birdFlap.Length == 0) return;
        flock = new GameObject("Ambient birds").transform;
        flock.SetParent(transform, false);
        for (int i = 0; i < profile.birds; i++)
        {
            var art = new GameObject("Bird").AddComponent<SpriteRenderer>();
            art.transform.SetParent(flock, false);
            float far = .42f / kit.birdFlap[0].bounds.size.x;
            art.transform.localScale = new Vector3(far, far, 1f);
            art.sprite = kit.birdFlap[0]; art.color = new Color(.2f, .22f, .28f, .8f); art.sortingOrder = skyOrder;
            birds.Add(new Bird { body = art.transform, art = art, offset = new Vector2(-(i * .55f) + Random.Range(-.15f, .15f), (i % 2 == 0 ? 1 : -1) * i * .16f + Random.Range(-.1f, .1f)), seed = Random.value * 6f });
        }
        flock.gameObject.SetActive(false);
        nextFlock = Time.time + Random.Range(4f, 14f);
    }

    // --- Each frame ---------------------------------------------------------------------------------------------------

    void Choose(bool now)
    {
        if (underground != null)
        {
            string group = underground.CurrentRoom != null ? underground.CurrentRoom.group : "A";
            if (group == lastGroup && !now) return;
            lastGroup = group;
            profile = CaveOf(group);
        }
        else if (session != null && session.InChamber)
        {
            profile = Indoors;
        }
        else
        {
            string id = session != null && !string.IsNullOrEmpty(session.regionId) ? session.regionId : "MR01";
            profile = Regions.TryGetValue(id, out var found) ? found : Regions["MR01"];
        }
    }

    void Publish(float dt)
    {
        // One wind value per place, with slow swells and an occasional gust on top.
        float t = Time.time;
        float swell = .8f + .35f * Mathf.PerlinNoise(t * .11f, 3.7f) + .5f * Mathf.Pow(Mathf.PerlinNoise(t * .23f, 9.1f), 4f) * 2f;
        wind = Mathf.MoveTowards(wind, profile.wind * swell, dt * .4f);
        windPhase += dt * (1.1f + wind * 1.4f);
        Shader.SetGlobalVector(WindId, new Vector4(wind, windPhase, 0f, 0f));
        Shader.SetGlobalFloat(TimeId, t);
        Vector2 at = qori != null ? (Vector2)qori.transform.position : new Vector2(1e6f, 1e6f);
        Shader.SetGlobalVector(ActorId, new Vector4(at.x, at.y + .3f, .9f, 1.4f));
    }

    void LateUpdate()
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        Step();
        CostMs = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000f / System.Diagnostics.Stopwatch.Frequency;
    }

    void Step()
    {
        if (view == null) { view = Camera.main; if (view == null) return; }
        float dt = Time.deltaTime;
        if (Time.unscaledTime >= nextGroup) { nextGroup = Time.unscaledTime + .5f; Choose(false); }
        Publish(dt);
        if (dt <= 0f) return;   // paused: nothing new appears
        Rect sight = Sight(2f);
        float density = Tier == 0 ? 1f : Tier == 1 ? .6f : .3f;
        SpawnAir(dt * density, sight);
        UpdateFlames(dt, sight);
        UpdateForgeFire();
        UpdateThorns(dt, sight);
        UpdateGusts(dt, sight, density);
        UpdateDrips(dt * density, sight);
        UpdateButterflies(sight);
        UpdateFlock(dt, sight);
        UpdateSplashes(sight);
        UpdatePerched(dt, sight);
    }

    Rect Sight(float margin)
    {
        float h = view.orthographicSize + margin, w = view.orthographicSize * view.aspect + margin;
        Vector2 c = view.transform.position;
        return new Rect(c.x - w, c.y - h, w * 2f, h * 2f);
    }

    static bool Due(ref float debt, float amount)
    {
        // Added on every test of a while loop: a whole unit or more per test (one long frame) would never end.
        debt += Mathf.Min(amount, .5f);
        if (debt < 1f) return false;
        debt -= 1f;
        return true;
    }

    bool InRock(Vector2 p) => Physics2D.OverlapPoint(p, groundMask) != null;

    void SpawnAir(float dt, Rect sight)
    {
        while (Due(ref dustDebt, profile.dust * dt * 4f)) SpawnDust(sight, 1f);
        while (Due(ref leafDebt, profile.leaves * dt)) SpawnLeaf(sight);
        while (Due(ref fireflyDebt, profile.fireflies * dt)) SpawnFirefly(sight);
        if (Tier < 2)
        {
            while (Due(ref mistDebt, (profile.mist > 0f ? .2f : 0f) * dt)) SpawnMist(sight, 1f);
            while (Due(ref skyDebt, (profile.skyWisps > 0f ? .2f : 0f) * dt)) SpawnSky(sight, 1f);
            while (Due(ref shaftDebt, (profile.shafts > 0f ? .22f : 0f) * dt)) SpawnShaft(sight, 1f);
        }
    }

    void SpawnDust(Rect sight, float age)
    {
        Vector2 p = Inside(sight);
        if (InRock(p)) return;
        float s = profile.dustSize * Random.Range(.5f, 1.3f);
        dust.Emit(p, new Vector2(wind * Random.Range(.15f, .4f), Random.Range(-.05f, .08f)), new Vector2(s, s), Random.Range(5f, 9f) * age, profile.dustColor);
    }

    void SpawnLeaf(Rect sight)
    {
        Vector2 p = new Vector2(Random.Range(sight.xMin, sight.xMax), sight.yMax);
        if (InRock(p)) return;
        float fall = Random.Range(.7f, 1.05f);
        // It fades out where it lands, so no leaf falls through the ground.
        var hit = Physics2D.Raycast(p, Vector2.down, 24f, groundMask);
        float life = Mathf.Min(hit.collider != null ? hit.distance / fall : 14f, 14f);
        if (life < 1.5f) return;
        float s = Random.Range(.16f, .26f);
        Color c = profile.leafColor;
        leaves.Emit(p, new Vector2(wind * Random.Range(.4f, 1.2f), -fall), new Vector2(s, s), life, c, Random.Range(0f, 360f), Random.Range(-90f, 90f));
    }

    void SpawnFirefly(Rect sight)
    {
        Vector2 p = Inside(sight);
        if (InRock(p)) return;
        glows.Emit(p, Random.insideUnitCircle * .15f, new Vector2(.55f, .55f), Random.Range(3f, 6f), new Color(1f, .95f, .75f, .9f));
    }

    void SpawnMist(Rect sight, float age)
    {
        float w = Random.Range(5f, 9f);
        Vector2 p = new Vector2(Random.Range(sight.xMin, sight.xMax), Mathf.Lerp(sight.yMin, sight.yMax, Random.Range(.05f, .4f)));
        Color c = profile.mistColor; c.a = profile.mist * .7f;
        mist.Emit(p, new Vector2(wind * .25f + .03f, 0f), new Vector2(w, w * .5f), Random.Range(14f, 22f) * age, c);
    }

    void SpawnSky(Rect sight, float age)
    {
        float w = Random.Range(10f, 16f);
        Vector2 p = new Vector2(Random.Range(sight.xMin, sight.xMax), Mathf.Lerp(sight.yMin, sight.yMax, Random.Range(.6f, .95f)));
        skyMist.Emit(p, new Vector2(.12f + wind * .1f, 0f), new Vector2(w, w * .5f), Random.Range(24f, 36f) * age, new Color(1f, 1f, 1f, profile.skyWisps));
    }

    void SpawnShaft(Rect sight, float age)
    {
        Vector2 p = new Vector2(Random.Range(sight.xMin, sight.xMax), Mathf.Lerp(sight.yMin, sight.yMax, .55f));
        Color c = profile.shaftColor; c.a = profile.shafts;
        shafts.Emit(p, Vector2.zero, new Vector2(Random.Range(1.2f, 2.6f), Random.Range(10f, 14f)), Random.Range(9f, 15f) * age, c, -14f);
    }

    static Vector2 Inside(Rect r) => new Vector2(Random.Range(r.xMin, r.xMax), Random.Range(r.yMin, r.yMax));

    void Prewarm()
    {
        var sight = Sight(2f);
        float density = Tier == 0 ? 1f : Tier == 1 ? .6f : .3f;
        for (int i = 0, n = Mathf.RoundToInt(profile.dust * 28f * density); i < n; i++) SpawnDust(sight, Random.Range(.3f, 1f));
        if (Tier < 2)
        {
            if (profile.mist > 0f) for (int i = 0; i < 3; i++) SpawnMist(sight, Random.Range(.3f, 1f));
            if (profile.skyWisps > 0f) for (int i = 0; i < 3; i++) SpawnSky(sight, Random.Range(.3f, 1f));
            if (profile.shafts > 0f) for (int i = 0; i < 2; i++) SpawnShaft(sight, Random.Range(.4f, 1f));
        }
    }

    void UpdateFlames(float dt, Rect sight)
    {
        float t = Time.time;
        bool emit = Time.time >= shrineCheck;
        if (emit) shrineCheck = Time.time + .25f;
        foreach (var f in flames)
        {
            if (!sight.Contains(f.flame)) continue;
            bool lit = f.source != null && f.source.enabled && f.source.gameObject.activeInHierarchy;
            if (f.halo != null && f.halo.enabled != lit) f.halo.enabled = lit;
            if (!lit) continue;
            float flick = .78f + .22f * Mathf.PerlinNoise(t * 3.1f + f.seed, f.seed) + .1f * Mathf.Sin(t * 13f + f.seed) * Mathf.PerlinNoise(t * .7f, f.seed);
            if (f.art != null) { var c = f.artColor; c.a *= Mathf.Clamp01(.72f + .28f * flick); f.art.color = c; }
            if (f.halo != null)
            {
                var c = f.haloColor; c.a *= flick; f.halo.color = c;
                f.halo.transform.localScale = f.haloScale * (.94f + .1f * flick);
            }
            if (emit && f.art == null && Random.value < .22f) Ember(f.flame + new Vector2(Random.Range(-.08f, .08f), .1f));
        }
        if (emit && hasForge && smith != null && smith.gameObject.activeInHierarchy && sight.Contains(forge) && Random.value < .85f)
            Ember(forge + new Vector2(Random.Range(-.3f, .3f), .1f));
    }

    void Ember(Vector2 at)
    {
        float s = Random.Range(.05f, .09f);
        embers.Emit(at, new Vector2(wind * .2f + Random.Range(-.08f, .08f), Random.Range(.3f, .6f)), new Vector2(s, s), Random.Range(1.4f, 2.6f), new Color(1f, .62f, .22f, .95f));
    }

    void UpdateThorns(float dt, Rect sight)
    {
        if (qori == null) return;
        Vector2 q = qori.transform.position;
        foreach (var thorn in thorns)
        {
            if (thorn.art == null) continue;
            var box = thorn.art.bounds;
            if (!sight.Overlaps(new Rect(box.min, box.size))) continue;
            // They stir as Qori comes near: a warning that gets stronger, never a change in their reach.
            float gap = Vector2.Distance(q, box.ClosestPoint(q));
            float want = Mathf.Clamp01(1f - gap / 3.5f);
            if (Mathf.Approximately(want, thorn.shiver) && want == 0f) continue;
            thorn.shiver = Mathf.MoveTowards(thorn.shiver, want, dt * 3f);
            thorn.art.GetPropertyBlock(thorn.block);
            thorn.block.SetFloat(ShiverId, thorn.shiver);
            thorn.art.SetPropertyBlock(thorn.block);
        }
    }

    public void ThornsTouched(Vector2 at)
    {
        for (int i = 0; i < 8; i++)
            drops.Emit(at + Random.insideUnitCircle * .2f, new Vector2(Random.Range(-3f, 3f), Random.Range(1.5f, 4.5f)), new Vector2(.09f, .09f), Random.Range(.25f, .5f), new Color(1f, .78f, .4f, 1f));
    }

    public void WaterEntered(Vector2 at, float surface)
    {
        Vector2 foot = new Vector2(at.x, surface);
        for (int i = 0; i < 10; i++)
            drops.Emit(foot, new Vector2(Random.Range(-2.6f, 2.6f), Random.Range(2.2f, 5f)), new Vector2(.08f, .08f), Random.Range(.4f, .75f), new Color(.8f, .92f, 1f, .9f));
        rings.Emit(foot, Vector2.zero, new Vector2(2.2f, 2.2f), .7f, new Color(1f, 1f, 1f, .8f));
    }

    void UpdateGusts(float dt, Rect sight, float density)
    {
        foreach (var lane in lanes)
        {
            if (lane.gust == null || !lane.gust.isActiveAndEnabled || !sight.Overlaps(lane.rect)) continue;
            float rate = lane.gust.Now == MraUgGust.Phase.Pulse ? 26f : lane.gust.Now == MraUgGust.Phase.Tell ? 5f : .5f;
            float speed = lane.gust.Now == MraUgGust.Phase.Pulse ? 15f : lane.gust.Now == MraUgGust.Phase.Tell ? 6f : 2f;
            float alpha = lane.gust.Now == MraUgGust.Phase.Pulse ? .42f : lane.gust.Now == MraUgGust.Phase.Tell ? .24f : .08f;
            if (Random.value >= rate * dt * density) continue;
            Vector2 p = Inside(lane.rect);
            float angle = Mathf.Atan2(lane.dir.y, lane.dir.x) * Mathf.Rad2Deg;
            // The FX_Gust art points right. In the system a positive rotation is clockwise.
            streaks.Emit(p, lane.dir * speed * Random.Range(.8f, 1.2f), new Vector2(1.1f, .55f), Mathf.Min(.9f, Mathf.Max(lane.rect.width, lane.rect.height) / speed), new Color(1f, 1f, 1f, alpha), -angle);
        }
    }

    void UpdateDrips(float dt, Rect sight)
    {
        for (int i = falling.Count - 1; i >= 0; i--)
        {
            if (Time.time < falling[i].at) continue;
            rings.Emit(falling[i].floor, Vector2.zero, new Vector2(.9f, .9f), .55f, new Color(1f, 1f, 1f, .55f));
            falling.RemoveAt(i);
        }
        while (Due(ref dripDebt, profile.drips * dt))
        {
            Vector2 p = new Vector2(Random.Range(sight.xMin, sight.xMax), Random.Range(sight.yMin, sight.yMax));
            if (InRock(p)) continue;
            var ceiling = Physics2D.Raycast(p, Vector2.up, 12f, groundMask);
            if (ceiling.collider == null) continue;
            Vector2 start = ceiling.point + Vector2.down * .05f;
            var floor = Physics2D.Raycast(start, Vector2.down, 16f, groundMask);
            float fall = floor.collider != null ? floor.distance : 8f;
            float life = Mathf.Sqrt(2f * fall / Mathf.Abs(Physics.gravity.y));
            drops.Emit(start, Vector2.zero, new Vector2(.05f, .05f), life, new Color(.82f, .92f, 1f, .85f));
            if (floor.collider != null && falling.Count < 8) falling.Add(new Drip { at = Time.time + life, floor = floor.point });
        }
    }

    void UpdateButterflies(Rect sight)
    {
        float t = Time.time;
        foreach (var b in butterflies)
        {
            bool near = sight.Contains(b.home);
            if (b.art.enabled != near) b.art.enabled = near;
            if (!near) continue;
            float k = b.seed;
            Vector2 p = b.home + new Vector2(Mathf.Sin(t * .37f + k) * 1.1f + Mathf.Sin(t * 1.1f + k * 2f) * .3f, Mathf.Sin(t * .53f + k * 3f) * .5f + Mathf.Sin(t * 2.3f + k) * .12f + .3f);
            b.art.flipX = p.x < b.body.position.x;
            b.body.position = new Vector3(p.x, p.y, 0f);
            float flap = Mathf.Abs(Mathf.Sin(t * 13f + k));
            b.body.localScale = new Vector3(b.size * (.88f + .12f * flap), b.size * (.5f + .5f * flap), 1f);
        }
    }

    void UpdateSplashes(Rect sight)
    {
        foreach (var splash in splashes)
        {
            if (splash.art == null || !sight.Contains(splash.at)) continue;
            bool on = splash.hazard == null || splash.hazard.IsUp;
            if (splash.art.enabled != on) splash.art.enabled = on;
            if (on) splash.art.sprite = kit.splash[Mathf.FloorToInt(Time.time * 12f + splash.seed) % kit.splash.Length];
        }
    }

    // Sparrows sitting along the route, by the plants; they fly off as Qori comes near and come back once he has gone.
    void MakePerchedBirds(SpriteRenderer[] all)
    {
        if (Tier >= 1 || profile.birds <= 0 || kit.birdPerch == null || kit.birdPerch.Length == 0 || kit.birdRise == null || kit.birdFlap == null || kit.birdTwig == null) return;
        const float Spacing = 28f, FootShare = .869f;
        float scale = .55f / kit.birdPerch[0].bounds.size.x, height = kit.birdPerch[0].bounds.size.y * scale;
        var homes = new List<Vector2>();
        foreach (var art in all)
        {
            if (homes.Count >= 7) break;
            if (art == null || art.sprite == null || !art.gameObject.activeInHierarchy || !Contains(art.sprite.name.ToLowerInvariant(), Rooted)) continue;
            Vector2 c = art.bounds.center + Vector3.right * .8f;
            if (homes.Exists(h => Vector2.Distance(h, c) < Spacing)) continue;
            var ground = Physics2D.Raycast(c + Vector2.up * 1.5f, Vector2.down, 4f, groundMask);
            if (ground.collider == null || ground.distance <= 0f) continue;
            homes.Add(ground.point);
        }
        foreach (var foot in homes)
        {
            var bird = new GameObject("Ambient sparrow").AddComponent<SpriteRenderer>();
            bird.transform.SetParent(transform, false);
            bird.transform.localScale = new Vector3(scale, scale, 1f);
            var at = new Vector2(foot.x, foot.y + (FootShare - .5f) * height);
            bird.transform.position = at;
            bird.sprite = kit.birdPerch[0]; bird.sortingOrder = 28; bird.flipX = Random.value < .5f;
            var twig = new GameObject("Ambient twig").AddComponent<SpriteRenderer>();
            twig.transform.SetParent(transform, false);
            twig.transform.localScale = bird.transform.localScale;
            twig.sprite = kit.birdTwig; twig.sortingOrder = 27; twig.enabled = false;
            perched.Add(new Perched { art = bird, twig = twig, home = at });
        }
    }

    void UpdatePerched(float dt, Rect sight)
    {
        if (qori == null) return;
        Vector2 q = qori.transform.position;
        foreach (var bird in perched)
        {
            bird.clock += dt;
            Vector2 at = bird.art.transform.position;
            switch (bird.stage)
            {
                case 0:   // sitting: a small bob and a look about
                    if (!sight.Contains(bird.home)) break;
                    bird.art.sprite = kit.birdPerch[Mathf.PingPong(bird.clock * .6f, 1f) > .85f ? Mathf.Min(1, kit.birdPerch.Length - 1) : 0];
                    if (Vector2.Distance(q, bird.home) < 3.6f)
                    {
                        float away = at.x >= q.x ? 1f : -1f;
                        bird.art.flipX = away < 0f;
                        bird.twig.flipX = away < 0f;
                        bird.velocity = new Vector2(away * 3.4f, 2.6f);
                        bird.stage = 1; bird.clock = 0f;
                    }
                    break;
                case 1:   // the crouch and the spring
                {
                    int all = kit.birdPerch.Length + kit.birdRise.Length, frame = Mathf.FloorToInt(bird.clock * 30f);
                    if (frame >= all) { bird.stage = 2; bird.clock = 0f; break; }
                    bird.art.sprite = frame < kit.birdPerch.Length ? kit.birdPerch[frame] : kit.birdRise[frame - kit.birdPerch.Length];
                    if (frame >= kit.birdPerch.Length + 2 && !bird.twig.enabled)
                    {
                        bird.twig.transform.position = bird.home; bird.twig.color = Color.white; bird.twig.enabled = true; bird.twigSpeed = 0f;
                    }
                    if (frame >= kit.birdPerch.Length) bird.art.transform.position = at + bird.velocity * dt * .5f;
                    break;
                }
                case 2:   // away
                    bird.art.sprite = kit.birdFlap[Mathf.FloorToInt(bird.clock * 22f) % kit.birdFlap.Length];
                    bird.art.transform.position = at + bird.velocity * dt;
                    if (!sight.Contains(at)) { bird.art.enabled = false; bird.stage = 3; bird.back = Random.Range(25f, 50f); bird.clock = 0f; }
                    break;
                case 3:   // gone until Qori has left and nobody can see it land
                    if (bird.clock > bird.back && !sight.Contains(bird.home))
                    {
                        bird.art.transform.position = bird.home; bird.art.sprite = kit.birdPerch[0]; bird.art.enabled = true;
                        bird.stage = 0; bird.clock = 0f;
                    }
                    break;
            }
            if (bird.twig.enabled)
            {
                bird.twigSpeed += 7f * dt;
                bird.twig.transform.position += Vector3.down * bird.twigSpeed * dt;
                var c = bird.twig.color; c.a -= dt * 1.6f; bird.twig.color = c;
                if (c.a <= 0f) bird.twig.enabled = false;
            }
        }
    }

    void UpdateFlock(float dt, Rect sight)
    {
        if (flock == null) return;
        float t = Time.time;
        if (!flock.gameObject.activeSelf)
        {
            if (t < nextFlock) return;
            flock.gameObject.SetActive(true);
            flockCamera = view.transform.position;
            flockDir = Random.value < .5f ? 1 : -1;
            flockAt = flockDir > 0 ? sight.xMin - 3f : sight.xMax + 3f;
            flock.position = new Vector3(flockAt, Mathf.Lerp(sight.yMin, sight.yMax, Random.Range(.68f, .9f)), 0f);
        }
        flockAt += flockDir * dt * 2.2f;
        // Far away: it moves most of the way with the camera.
        Vector2 camera = view.transform.position;
        flockAt += (camera.x - flockCamera.x) * .92f;
        Vector3 p = flock.position;
        p.x = flockAt;
        p.y += (camera.y - flockCamera.y) * .92f + Mathf.Sin(t * .4f) * dt * .12f;
        flock.position = p;
        flockCamera = camera;
        foreach (var bird in birds)
        {
            bird.body.localPosition = new Vector3(bird.offset.x * flockDir, bird.offset.y, 0f);
            bird.art.sprite = kit.birdFlap[Mathf.FloorToInt(t * 16f + bird.seed * 4f) % kit.birdFlap.Length];
            bird.art.flipX = flockDir < 0;
        }
        if (flockDir > 0 ? flockAt > sight.xMax + 5f : flockAt < sight.xMin - 5f)
        {
            flock.gameObject.SetActive(false);
            nextFlock = t + Random.Range(25f, 55f);
        }
    }
}

// Sparks when Qori touches thorns. Only watches: ThornHazard still does the damage.
public sealed class AmbientThornTouch : MonoBehaviour
{
    AmbientLife life; float next;
    public void Init(AmbientLife owner) => life = owner;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (life == null || Time.time < next || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerHealth>() == null) return;
        next = Time.time + .4f;
        life.ThornsTouched(other.bounds.ClosestPoint(transform.position));
    }
}

// A splash when Qori falls in. Only watches: Hazard still sends him back.
public sealed class AmbientWaterTouch : MonoBehaviour
{
    AmbientLife life; float next; BoxCollider2D box;
    public void Init(AmbientLife owner) { life = owner; box = GetComponent<BoxCollider2D>(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (life == null || Time.time < next || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        next = Time.time + .4f;
        life.WaterEntered(other.bounds.center, box != null ? box.bounds.max.y : other.bounds.min.y);
    }
}

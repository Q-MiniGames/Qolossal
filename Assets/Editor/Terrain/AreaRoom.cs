using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The shared pieces an area room builder puts together (A1, A2, ...): terrain, the three-layer
// background, back walls, water, thorns, rubble, swing rings, falling rocks, a secret alcove with
// a heart seed, enemies in the area's palette, and decor from the area's sheet. Each builds its
// objects under a named root and seats them into the terrain (GroundedProp). Inside a Stir scope
// everything goes under a StirVariant instead, so it exists only before or after a knot wakes.
// Then the world pieces: Waymarks, Vein Gates (fixed and wild), dormant gates, Lore Stones, knots.
public sealed class AreaRoom
{
    public readonly string area;      // "A0", "A1", "A2", ...
    public readonly TerrainKit kit;
    readonly Transform terrainRoot, mechanicsRoot, enemiesRoot, decorRoot;
    Transform scope;
    public Transform terrain => scope != null ? scope : terrainRoot;
    public Transform mechanics => scope != null ? scope : mechanicsRoot;
    public Transform enemies => scope != null ? scope : enemiesRoot;
    public Transform decor => scope != null ? scope : decorRoot;
    const int PropOrder = A0TestRoomBuilder.PropOrder;

    public AreaRoom(string area)
    {
        this.area = area;
        kit = area == "A0" ? A0TestRoomBuilder.BuildKit() : TerrainKitBuilder.Build(area);
        terrainRoot = new GameObject("Terrain").transform;
        mechanicsRoot = new GameObject("Mechanics").transform;
        enemiesRoot = new GameObject("Enemies").transform;
        decorRoot = new GameObject("Decor").transform;
    }

    // Builds under one existing root (A0, whose builder predates this helper).
    public AreaRoom(string area, TerrainKit kit, Transform root)
    {
        this.area = area; this.kit = kit;
        terrainRoot = mechanicsRoot = enemiesRoot = decorRoot = root;
    }

    // ---------------------------------------------------------------- stir variants

    sealed class Scope : System.IDisposable
    {
        readonly System.Action end; public Scope(System.Action end) => this.end = end;
        public void Dispose() => end();
    }

    // Everything built inside `using (room.Stir(...))` exists only after (or before) `knot` wakes.
    // The scene is saved in a new game's state: before-variants shown, after-variants hidden.
    public System.IDisposable Stir(string name, string knot, bool afterKnot)
    {
        var variant = Variant(name, knot, afterKnot);
        scope = variant.transform;
        return new Scope(() => { scope = null; FreshState(variant); });
    }

    public static StirVariant Variant(string name, string knot, bool afterKnot)
    {
        var root = GameObject.Find("Stir Variants")?.transform ?? new GameObject("Stir Variants").transform;
        var variant = new GameObject($"{name} ({(afterKnot ? "after" : "before")} {knot})").AddComponent<StirVariant>();
        variant.transform.SetParent(root, false);
        variant.knot = knot; variant.when = afterKnot ? StirVariant.When.AfterKnot : StirVariant.When.BeforeKnot;
        return variant;
    }

    public static void FreshState(StirVariant variant)
    {
        foreach (Transform child in variant.transform) child.gameObject.SetActive(variant.when == StirVariant.When.BeforeKnot);
    }

    public static Sprite Art(string category, string name) => A0TestRoomBuilder.Art(category, name);
    public static SpriteRenderer Image(Transform parent, string name, Sprite sprite, Vector2 at, int order = PropOrder) =>
        A0TestRoomBuilder.Image(parent, name, sprite, at, order);

    // ---------------------------------------------------------------- terrain

    public TerrainBlock Block(string name, float x, float top, float width, float height, int offset,
        bool left = false, bool right = false, bool bottom = false, TerrainBlock.Surface surface = TerrainBlock.Surface.Rock) =>
        A0TestRoomBuilder.AddBlock(terrain, kit, name, x, top, width, height, offset, left, right, bottom, surface);

    public TerrainPiece Piece(string name, TerrainPiece.Kind kind, float x, float y)
    {
        var obj = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(terrain, false); obj.transform.position = new Vector3(x, y, 0f);
        var piece = obj.AddComponent<TerrainPiece>(); piece.kit = kit; piece.kind = kind; piece.Rebuild();
        return piece;
    }

    // A block whose left end sits under a slope's crest: fades into it and lets the slope carry
    // the walk surface until it reaches full height.
    public TerrainBlock AfterSlope(TerrainPiece slope, string name, float width, float depth, int offset, bool right = false)
    {
        float top = slope.transform.position.y + slope.SlopeRise;
        float x = slope.transform.position.x + slope.SurfaceWidth - 1.6f;
        var block = Block(name, x, top, width, depth, offset, right: right);
        block.blendLeft = 1.2f;
        block.colliderInsetLeft = Mathf.Max(0f, slope.transform.position.x + slope.SlopeTopStart + .05f - x);
        block.Rebuild();
        return block;
    }

    // ---------------------------------------------------------------- background

    // A far painting from Resources/WorldBackground (imported as a sprite here), then the area's
    // Mid and Near layers. Band colours are each layer's average bottom row.
    public void Background(Camera camera, string farPainting, Vector2 start, Color midBand, Color nearBand)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(farPainting);
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f; importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        var root = new GameObject("Background").transform;
        void Layer(string name, Sprite sprite, float height, bool repeat, Vector2 follow, float bottomAtStart, int order, Color below)
        {
            var layer = new GameObject(name).AddComponent<ParallaxLayer>();
            layer.transform.SetParent(root, false);
            layer.sprite = sprite ?? throw new FileNotFoundException(name);
            layer.height = height; layer.repeat = repeat; layer.follow = follow;
            layer.baseY = bottomAtStart - (start.y + 1f) * follow.y;
            layer.baseX = repeat ? 0f : start.x * (1f - follow.x);
            layer.extendBelow = repeat ? 14f : 0f;
            layer.belowColor = below; layer.sortingOrder = order; layer.targetCamera = camera;
        }
        Layer("Far - " + Path.GetFileNameWithoutExtension(farPainting), AssetDatabase.LoadAssetAtPath<Sprite>(farPainting), 16f, false, new Vector2(.985f, .96f), -4.8f, -100, Color.white);
        Layer($"Mid - BG_{area}_Mid", Art("Backgrounds", $"BG_{area}_Mid"), 0f, true, new Vector2(.82f, .85f), -.3f, -90, midBand);
        Layer($"Near - BG_{area}_Near", Art("Backgrounds", $"BG_{area}_Near"), 0f, true, new Vector2(.62f, .72f), -2.2f, -80, nearBand);
    }

    // A dark back wall of fill rock behind an enclosed space, so it reads as deep, not open sky.
    public ScrollingStrip BackWall(string name, float x, float top, float width, float height, Color shade, float fadeSides = 0f)
    {
        var root = GameObject.Find("Back Walls")?.transform ?? new GameObject("Back Walls").transform;
        return Strip(root, name, kit.groundFill, x, top, width, height, Vector2.zero, shade, -60, false, false, 2.5f, 0f, fadeSides);
    }

    public static ScrollingStrip Strip(Transform parent, string name, Sprite sprite, float x, float top, float width, float height, Vector2 scroll, Color tint, int order,
        bool fitHeight, bool fitWidth, float fadeTop = 0f, float fadeBottom = 0f, float fadeSides = 0f)
    {
        var strip = new GameObject(name).AddComponent<ScrollingStrip>();
        strip.enabled = false;   // configure before it builds its mesh
        strip.transform.SetParent(parent, false); strip.transform.position = new Vector3(x, top, 0f);
        strip.sprite = sprite; strip.width = width; strip.height = height; strip.scroll = scroll; strip.tint = tint;
        strip.sortingOrder = order; strip.fitHeight = fitHeight; strip.fitWidth = fitWidth;
        strip.fadeTop = fadeTop; strip.fadeBottom = fadeBottom; strip.fadeSides = fadeSides;
        strip.enabled = true;
        return strip;
    }

    // ---------------------------------------------------------------- water

    // A shallow stream at the walk line, fading out at both banks and into the ground below.
    public WaterPool Stream(Transform root, string name, float x0, float x1, float floor)
    {
        Sprite surface = Art("Hazards", "Water_Surface"), body = Art("Hazards", "Water_Body");
        float top = floor + .07f;
        var pool = new GameObject(name).AddComponent<WaterPool>();
        pool.transform.SetParent(root, false); pool.transform.position = new Vector3(x0, top, 0f);
        pool.surfaceY = top; pool.ripple = Art("Effects", "FX_Water_Ripple");
        var box = pool.gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true; box.size = new Vector2(x1 - x0, 1f); box.offset = new Vector2((x1 - x0) * .5f, -.3f);
        Strip(pool.transform, "Body", body, x0, top, x1 - x0, .5f, new Vector2(.03f, .01f), new Color(1f, 1f, 1f, .5f), 18, false, false, 0f, .3f, 1.1f);
        float h = surface.rect.height / surface.pixelsPerUnit;   // the painted line is at the canvas's middle
        Strip(pool.transform, "Surface", surface, x0, top + h * .5f, x1 - x0, h, new Vector2(.08f, 0f), new Color(1f, 1f, 1f, .85f), 19, true, false, 0f, 0f, 1.1f);
        return pool;
    }

    // ---------------------------------------------------------------- mechanics

    public Portal Portal(Vector2 at, string id, string destinationScene, string destinationPortal, string destinationName, float exitSide) =>
        A0TestRoomBuilder.AddPortal(mechanics, at, id, destinationScene, destinationPortal, destinationName, exitSide, area, 48f, kit);

    // A0 has no shrine art: its checkpoints are the lantern.
    public Checkpoint Checkpoint(Vector2 at, string id) => area == "A0"
        ? A0TestRoomBuilder.AddCheckpoint(mechanics, at, id)
        : A0TestRoomBuilder.AddCheckpoint(mechanics, at, id, Art("Props", "Checkpoint_Shrine_" + area), 27f, kit);

    // ---------------------------------------------------------------- the titan's world

    // A Waymark: a checkpoint that charts the level, with a map leaf floating over it.
    public Waymark Waymark(Vector2 at, string id) => AddWaymark(Checkpoint(at, id), area == "A0" ? 1.6f : 1.7f);

    public static Waymark AddWaymark(Checkpoint checkpoint, float iconHeight)
    {
        var waymark = checkpoint.gameObject.AddComponent<Waymark>();
        waymark.unchartedIcon = Art("UI", "Map_Icon_Unexplored"); waymark.chartedIcon = Art("UI", "Map_Icon_Checkpoint");
        waymark.iconHeight = iconHeight;
        waymark.icon = Image(checkpoint.transform, "Map Leaf", waymark.unchartedIcon, (Vector2)checkpoint.transform.position + Vector2.up * iconHeight, PropOrder + 4);
        waymark.icon.transform.localScale = new Vector3(.3f, .3f, 1f);   // the 2.56 u icon canvas to about .75 u
        return waymark;
    }

    // A Wild Vein: the hidden-gate arch with a flickering membrane. When `settlesWith` wakes it
    // settles into a fixed shortcut to `settledArrival` (a portal or Waymark) in `settledScene`.
    public Portal WildVein(Vector2 at, string id, string settlesWith, string settledScene, string settledArrival, string settledName, float exitSide) =>
        AddWildVein(mechanics, kit, at, id, settlesWith, settledScene, settledArrival, settledName, exitSide);

    public static Portal AddWildVein(Transform parent, TerrainKit kit, Vector2 at, string id, string settlesWith, string settledScene, string settledArrival, string settledName, float exitSide)
    {
        var portal = HiddenArchPortal(parent, kit, at, id, "", "", "", exitSide);
        portal.name = "Wild Vein " + id;
        portal.vein = global::Portal.Vein.Wild; portal.settlesWith = settlesWith;
        portal.settledScene = settledScene; portal.settledArrival = settledArrival; portal.settledName = settledName;
        return portal;
    }

    // A Vein Gate in the hidden-gate arch: a vein grown by a stir, rather than an area's own gate.
    public Portal GrownVein(Vector2 at, string id, string destinationScene, string destinationPortal, string destinationName, float exitSide) =>
        HiddenArchPortal(mechanics, kit, at, id, destinationScene, destinationPortal, destinationName, exitSide);

    internal static Portal HiddenArchPortal(Transform parent, TerrainKit kit, Vector2 at, string id, string destinationScene, string destinationPortal, string destinationName, float exitSide)
    {
        // The hidden arch shares the A1 arch's canvas (398 x 480 at 120 px/u, 48 px below its base), so the A1 membrane fits it.
        var portal = A0TestRoomBuilder.AddPortal(parent, at, id, destinationScene, destinationPortal, destinationName, exitSide, "A1", 48f, kit);
        portal.transform.Find("Arch").GetComponent<SpriteRenderer>().sprite = Art("Props", "Portal_Gate_Hidden_Awake");
        return portal;
    }

    // Where a vein will grow after a stir: the dormant hidden arch, mossed over, with no membrane.
    public SpriteRenderer DormantGate(Vector2 at)
    {
        Sprite arch = Art("Props", "Portal_Gate_Hidden");
        var r = Image(mechanics, "Dormant Vein Gate", arch, at + new Vector2(0f, A0TestRoomBuilder.Grounded(arch, 48f)), PropOrder + 2);
        r.color = new Color(.8f, .82f, .8f);
        A0TestRoomBuilder.Ground(r.gameObject, kit, at.y, 2.5f, PropOrder + 2);
        return r;
    }

    // A Lore Stone standing on the ground at `at`.
    public LoreStone Lore(Vector2 at, string id, string text)
    {
        Sprite art = Art("Props", "Collectible_LoreStone");
        const float Scale = 1.6f;
        var stone = new GameObject("Lore Stone " + id).AddComponent<LoreStone>();
        stone.transform.SetParent(mechanics, false); stone.transform.position = at;
        stone.stoneId = id; stone.text = text;
        float lift = Scale * (art.pivot.y - 3f) / art.pixelsPerUnit - .06f;
        stone.image = Image(stone.transform, "Art", art, at + new Vector2(0f, lift), PropOrder + 1);
        stone.image.transform.localScale = new Vector3(Scale, Scale, 1f);
        var box = stone.gameObject.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(1.2f, 1.8f); box.offset = new Vector2(0f, .9f);
        A0TestRoomBuilder.Ground(stone.image.gameObject, kit, at.y, 1f, PropOrder + 1);
        return stone;
    }

    // A knot standing on the ground at `at` (placeholder art: a glow pod wrapped in the thorn
    // barrier), sealed until every guardian is defeated.
    public TitanKnot Knot(Vector2 at, string knotId, AbilityDefinition ability, string[] echoLines, params GameObject[] guardians)
    {
        var knot = new GameObject("Knot " + knotId).AddComponent<TitanKnot>();
        knot.transform.SetParent(mechanics, false); knot.transform.position = at;
        knot.knot = knotId; knot.ability = ability; knot.echoLines = echoLines; knot.guardians.AddRange(guardians);
        knot.coreDim = Art("Hazards", "GlowPod_Light_Off"); knot.coreLit = Art("Hazards", "GlowPod_Light_On");
        knot.core = Image(knot.transform, "Core", knot.coreDim, at + new Vector2(0f, -.1f), PropOrder + 2);
        knot.core.transform.localScale = new Vector3(2.3f, 2.3f, 1f);
        // The thorns bind the knot's lower half, so the dim knot still shows above them.
        knot.seal = Image(knot.transform, "Thorn Seal", Art("Props", "Barrier_Thorns_Intact"), at + new Vector2(0f, -.15f), PropOrder + 3);
        knot.seal.transform.localScale = new Vector3(.62f, .5f, 1f);
        var box = knot.gameObject.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(1.8f, 2.8f); box.offset = new Vector2(0f, 1.4f);
        var seat = new GameObject("Grounding"); seat.transform.SetParent(knot.transform, false); seat.transform.position = at;
        A0TestRoomBuilder.Ground(seat, kit, at.y, 2.2f, PropOrder + 3);
        return knot;
    }

    // Swing rings: grapple anchors in the area's ring art.
    public void SwingRings(float y, params float[] xs)
    {
        var anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ThreadAnchor01.prefab");
        Sprite ring = Art("Props", "Anchor_Ring_" + area);
        foreach (float x in xs)
        {
            var anchor = (GameObject)PrefabUtility.InstantiatePrefab(anchorPrefab, mechanics);
            anchor.name = "Swing Ring " + x;
            anchor.transform.position = new Vector3(x, y, 0f); anchor.transform.localScale = Vector3.one;
            // The prefab's visible art is its AnchorVisual child (the root renderer only carries the highlight tint).
            var art = anchor.transform.Find("AnchorVisual");
            art.localPosition = Vector3.zero; art.localScale = Vector3.one;
            var r = art.GetComponent<SpriteRenderer>(); r.sprite = ring; r.sortingOrder = PropOrder + 3;
        }
    }

    // A strip of floor thorns: its flat base sunk to its middle row (112 px) at the walk line, its
    // ends faded, and the ground's moss lip in front of it.
    public void Thorns(string name, float x0, float x1, float ground)
    {
        Sprite art = Art("Hazards", "Hazard_Thorns_Floor_" + area);
        float h = art.rect.height / art.pixelsPerUnit, w = x1 - x0;
        var strip = Strip(mechanics, name, art, x0, ground + 112f / art.pixelsPerUnit, w, h, Vector2.zero, Color.white, PropOrder + 1, true, false, 0f, 0f, .45f);
        var seat = new GameObject("Grounding");   // the moss lip centres on its object
        seat.transform.SetParent(strip.transform, false); seat.transform.position = new Vector3(x0 + w * .5f, ground, 0f);
        var grounded = A0TestRoomBuilder.Ground(seat, kit, ground, w - .6f, PropOrder + 1);
        grounded.enabled = false; grounded.shadow = false; grounded.enabled = true;
        var hazard = new GameObject("Hazard");
        hazard.transform.SetParent(strip.transform, false); hazard.transform.position = new Vector3(x0 + w * .5f, ground + .18f, 0f);
        var box = hazard.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(w - .2f, .3f);
        hazard.AddComponent<ThornHazard>();
    }

    // A rubble wall that only the mace breaks.
    public Breakable Rubble(float x, float ground)
    {
        Sprite art = Art("Props", "Barrier_Rubble_Intact_" + area);
        var rubble = new GameObject("Rubble Barrier (mace)") { layer = LayerMask.NameToLayer("Ground") };
        rubble.transform.SetParent(mechanics, false); rubble.transform.position = new Vector3(x, ground + 1.7f, 0f);
        rubble.AddComponent<BoxCollider2D>().size = new Vector2(1.3f, 3.4f);
        var breakable = rubble.AddComponent<Breakable>();
        breakable.breaksWith = Breakable.Rule.Mace; breakable.solid = rubble.GetComponent<Collider2D>();
        breakable.intact = Image(rubble.transform, "Art", art, new Vector2(x, ground + A0TestRoomBuilder.Grounded(art, 45f)));
        breakable.pieces = A0TestRoomBuilder.Pieces("Props", "Barrier_Rubble_Pieces_" + area);
        A0TestRoomBuilder.Ground(breakable.intact.gameObject, kit, ground, 1.7f, PropOrder);
        return breakable;
    }

    // A loose rock in a ceiling whose underside is at `ceilingY`.
    public FallingRock Rock(float x, float ceilingY)
    {
        var rock = new GameObject("Falling Rock " + x);
        rock.transform.SetParent(mechanics, false); rock.transform.position = new Vector3(x, ceilingY - .5f, 0f);
        var fr = rock.AddComponent<FallingRock>();
        fr.image = Image(rock.transform, "Art", Art("Hazards", "Hazard_FallingRock"), rock.transform.position, PropOrder + 4);
        fr.image.transform.localScale = new Vector3(.52f, .52f, 1f);
        fr.dust = Art("Hazards", "FX_Dust_Warning");
        var box = rock.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(1.2f, .8f);
        return fr;
    }

    // A false wall covering an alcove (centre `centre`, size `size`, floor at `floor`), with a
    // heart seed inside at `seedAt`.
    public void SecretAlcove(Vector2 centre, Vector2 size, float floor, Vector2 seedAt, string seedId)
    {
        var secret = new GameObject("Secret Wall");
        secret.transform.SetParent(mechanics, false); secret.transform.position = centre;
        var sw = secret.AddComponent<SecretWall>();
        secret.GetComponent<BoxCollider2D>().size = size;
        sw.overlay = Image(secret.transform, "Overlay", Art("Props", "Wall_Secret_Overlay_" + area), centre, 40);
        sw.overlay.transform.localScale = new Vector3((size.x + .4f) / 8.53f, (size.y + .4f) / 8.53f, 1f);   // 1024 px canvas at 120 px/u
        var wallBase = A0TestRoomBuilder.Ground(sw.overlay.gameObject, kit, floor, size.x - .4f, 40);   // moss over its foot; no shadow (it's drawn in front of Qori)
        wallBase.enabled = false; wallBase.shadow = false; wallBase.enabled = true;
        var seed = new GameObject("Heart Seed");
        seed.transform.SetParent(mechanics, false); seed.transform.position = seedAt;
        var hs = seed.AddComponent<HeartSeed>(); hs.seedId = seedId;
        hs.image = Image(seed.transform, "Art", Art("Props", "Pickup_HeartSeed"), seedAt, PropOrder + 3);
        var seedBox = seed.AddComponent<CircleCollider2D>(); seedBox.isTrigger = true; seedBox.radius = .45f;
    }

    // ---------------------------------------------------------------- enemies and decor

    // Places an enemy prefab; enemies from A0 are recoloured to the area's palette, the area's own
    // creatures (recolour: false) keep their colours.
    public GameObject Enemy(string prefab, Vector2 at, bool recolour = true)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + prefab + ".prefab"), enemies);
        obj.transform.position = at;
        if (recolour && AreaPalette.Apply(obj, area) == 0) Debug.LogWarning($"[AreaRoom] {prefab} has no {area} palette parts");
        return obj;
    }

    System.Collections.Generic.Dictionary<string, Sprite> decorSprites;

    // A piece from Decor_<area>_Sheet standing on (or hanging from) a surface at height y. Solid
    // pieces get a contact shadow and the ground's moss in front.
    public SpriteRenderer Decor(string name, float x, float y, int order = -20, bool hanging = false, bool flip = false, bool solid = false)
    {
        decorSprites ??= AssetDatabase.LoadAllAssetsAtPath($"Assets/Art/Codex/Decor/Decor_{area}_Sheet.png").OfType<Sprite>()
            .ToDictionary(sprite => sprite.name.Replace($"Decor_{area}_", ""));
        const float Sink = .12f;   // slice rects keep a little transparent margin below the contact point
        var r = new GameObject("Decor " + name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(decor, false);
        r.transform.position = new Vector3(x, y + (hanging ? Sink : -Sink), 0f);
        r.sprite = decorSprites[name]; r.sortingOrder = order; r.flipX = flip;
        if (solid && !hanging) A0TestRoomBuilder.Ground(r.gameObject, kit, y, r.sprite.bounds.size.x * .75f, order);
        return r;
    }
}

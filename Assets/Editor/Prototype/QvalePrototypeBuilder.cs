using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Proto_Qvale.unity, the Qvale prototype from the world redesign proposal v2
// (section 5): the town in the warm basin, in placeholder shapes, to try the hub's systems before
// any art: talking (DialogueBox, TownNpc), houses you walk into (CutawayHouse), the listening spot
// (ListeningSpot, with placeholder music), Amber and the smithy (TownPickup, TownShop), and a quake
// that changes what the town says and shows (TownQuake, TownVariant). Nothing is saved.
//
// West to east: Qori comes down a long slope into the basin (Amber on the way); the square with a
// warm spring that pulses like a heartbeat; the Brambles' house (Pip the child inside, Marrow the
// farmer at the door; crates up to the roof, where a song shell sits); Brannick's smithy (the shop);
// Wick the lantern keeper by the lamps; Grandfather Tallow's house (the old man; a song shell on
// his loft); the listening tree (a bench, Fennel the musician); then a rise out of town to a knot
// of roots, which makes the quake.
// Menu: Qolossal > Prototype > Build Qvale Prototype
public static class QvalePrototypeBuilder
{
    public const string ScenePath = "Assets/Scenes/Proto_Qvale.unity";
    public static readonly Vector2 Start = new Vector2(-9f, 16.6f);

    static readonly Vector2[] Ground =
    {
        new Vector2(-16f, 16.4f), new Vector2(-10f, 15.6f), new Vector2(-4f, 14f), new Vector2(2f, 12f), new Vector2(8f, 9.5f), new Vector2(14f, 6.6f),
        new Vector2(20f, 3.6f), new Vector2(25f, 1.4f), new Vector2(29f, .3f), new Vector2(33f, 0f), new Vector2(166f, 0f), new Vector2(170f, .5f),
        new Vector2(175f, 2.4f), new Vector2(180f, 5f), new Vector2(186f, 7.6f), new Vector2(192f, 8.6f), new Vector2(198f, 8.8f),
    };
    public const float SpringX = 44f, HouseX = 62f, HouseW = 15f, SmithyX = 86f, WickX = 107f, OldX = 116f, OldW = 17f, TreeX = 150f, KnotX = 191f;

    static readonly Color Earth = new Color(.66f, .55f, .42f), EarthDeep = new Color(.42f, .34f, .27f), Moss = new Color(.5f, .62f, .34f);
    static readonly Color Wood = new Color(.5f, .36f, .25f), WoodDark = new Color(.32f, .23f, .17f), Plaster = new Color(.86f, .78f, .64f), Window = new Color(1f, .82f, .48f);
    static readonly Color Interior = new Color(.36f, .27f, .2f), InteriorDark = new Color(.24f, .18f, .14f);
    const int GroundOrder = -25, BackOrder = -30, PropOrder = -12, FigureOrder = 4, FacadeOrder = 60;

    [MenuItem("Qolossal/Prototype/Build Qvale Prototype")]
    public static void Build()
    {
        var scene = A0TestRoomBuilder.NewRoom("Qvale Prototype", Start, out Camera camera);
        camera.backgroundColor = new Color(.9f, .79f, .66f);   // late afternoon
        camera.gameObject.AddComponent<AudioListener>();   // NewRoom's camera has none, and the listening spot plays music
        var room = new AreaRoom("A0");
        new GameObject("Town HUD").AddComponent<TownHud>();

        // The basin's rim far off: rolling warm hills (in truth, the titan's thighs and belly).
        var far = Group("Far - basin rim", new Vector2(.8f, .7f), new Vector2(80f, 4f), camera);
        Shape(far, "Far hills", At(new Vector2(80f, 4f), Hills(-60f, 230f, 9f, 6f, 3).ToArray()), null, new Color(.8f, .7f, .62f), new Color(.86f, .77f, .68f), Color.clear, -95, false);
        var mid = Group("Mid - basin slopes", new Vector2(.5f, .4f), new Vector2(80f, 4f), camera);
        Shape(mid, "Mid hills", At(new Vector2(80f, 4f), Hills(-60f, 230f, 4f, 4.5f, 7).ToArray()), null, new Color(.73f, .62f, .5f), new Color(.8f, .7f, .58f), Color.clear, -90, false);

        // The ground: a long slope down from the west, the flat basin floor, a rise east to the knot.
        var town = new GameObject("Qvale").transform;
        var outline = new List<Vector2>(Ground) { new Vector2(198f, -9f), new Vector2(-16f, -9f) };
        Shape(town, "Basin floor", outline.ToArray(), Ground, Earth, EarthDeep, Moss, GroundOrder, true);
        Wall("West Wall (invisible)", new Vector2(-15f, 30f), new Vector2(2f, 40f));
        Wall("East Wall (invisible)", new Vector2(197f, 30f), new Vector2(2f, 40f));

        Square(town);
        BramblesHouse(town);
        Smithy(town);
        Lamps(town);
        OldMansHouse(town);
        ListeningTree(town);
        Knot(town);

        // Amber along the way (125 in all; the smithy's goods cost 105).
        foreach (var (x, n) in new[] { (-6f, 5), (0f, 5), (6f, 5), (12f, 5), (18f, 5), (30f, 10), (38f, 10), (52f, 10), (164f, 15), (178f, 10), (184f, 10) })
            Amber(town, new Vector2(x, SurfaceY(x) + 1f), n);
        Amber(town, new Vector2(HouseX - 3.2f, 6.2f), 15);           // over the crates
        Amber(town, new Vector2(HouseX + 1.5f, 7.6f), 10);           // on the roof's slope
        Amber(town, new Vector2(OldX + 4.5f, 1f), 10);               // in the old man's house

        Note("Note - start", new Vector2(-14f, 12f), new Vector2(0f, 20f),
            "Qvale prototype. ▲ (W / up) talks, sits and shops. Space / E confirms, Esc leaves.\nCollect Amber, find two song shells, then touch the knot east of town, and talk to everyone again.");

        room.Decor("fern", 24f, SurfaceY(24f) - .15f, -20); room.Decor("grass", 9f, SurfaceY(9f) - .15f, -20); room.Decor("white_flowers", 36f, -.15f, -20);
        room.Decor("mushrooms", 81f, -.15f, -20); room.Decor("grass", 103f, -.15f, -20); room.Decor("white_flowers", 137f, -.15f, -20);
        room.Decor("grass", 170f, SurfaceY(170f) - .15f, -20); room.Decor("fern", 182f, SurfaceY(182f) - .15f, -20);

        // Saved as a new game sees it: no quake yet, nothing paid for.
        foreach (var variant in Object.FindObjectsByType<TownVariant>(FindObjectsSortMode.None))
            foreach (Transform child in variant.transform) child.gameObject.SetActive(false);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[QvalePrototypeBuilder] Built " + ScenePath);
    }

    // ------------------------------------------------------------------ the town

    // The square: the warm spring (it beats like a heart, faster after the quake), and the crack
    // that opens across the square in the quake.
    static void Square(Transform town)
    {
        var spring = new GameObject("Warm Spring").transform; spring.SetParent(town, false);
        Shape(spring, "Pool", new[] { new Vector2(SpringX - 3f, .05f), new Vector2(SpringX + 3f, .05f), new Vector2(SpringX + 2.2f, -.5f), new Vector2(SpringX - 2.2f, -.5f) },
            null, new Color(.5f, .72f, .7f), new Color(.36f, .55f, .55f), Color.clear, GroundOrder + 2, false);
        Shape(spring, "Rim stones", Ellipse(new Vector2(SpringX, .05f), 3.4f, .35f, 16), null, new Color(.6f, .55f, .5f), new Color(.5f, .45f, .4f), Color.clear, GroundOrder + 3, false);
        var glowRoot = new GameObject("Glow").transform; glowRoot.SetParent(spring, false); glowRoot.position = new Vector3(SpringX, .3f, 0f);
        var glow = Shape(glowRoot, "Glow", Ellipse(Vector2.zero, 2.4f, .9f, 20), null, new Color(1f, .9f, .62f, .55f), new Color(1f, .9f, .62f, .15f), Color.clear, GroundOrder + 4, false);
        var ws = spring.gameObject.AddComponent<WarmSpring>(); ws.glow = glowRoot; ws.glowShape = glow;

        var crack = new GameObject("After the quake: a crack across the square").AddComponent<TownVariant>();
        crack.transform.SetParent(town, false); crack.condition = "quake";
        Line(crack.transform, "Crack", new Color(.25f, .18f, .14f), .3f, GroundOrder + 2,
            new Vector2(48f, .02f), new Vector2(50f, -.6f), new Vector2(51.5f, -.2f), new Vector2(53.5f, -1f), new Vector2(55f, -.3f), new Vector2(57f, -.8f));
        Shape(crack.transform, "Heaved slab", new[] { new Vector2(50.5f, 0f), new Vector2(53.2f, 0f), new Vector2(52.8f, .45f), new Vector2(50.8f, .3f) }, null, Shade(Earth, 1.05f), EarthDeep, Moss, GroundOrder + 1, true);
    }

    // The Brambles' house: walk in through the door on the left; Pip inside, Marrow at the door;
    // crates up to the roof, where the first song shell sits.
    static void BramblesHouse(Transform town)
    {
        var house = House(town, "The Brambles' house", HouseX, HouseW, 5.5f, 4f, new Color(.78f, .63f, .5f), new Color(.6f, .42f, .32f));
        Prop(house, "Table", new Vector2(HouseX + 5.5f, 0f), new Vector2(2.6f, 1f), Wood);
        Prop(house, "Bed", new Vector2(HouseX + 11.5f, 0f), new Vector2(3.2f, .8f), new Color(.66f, .5f, .5f));
        Prop(house, "Shelf", new Vector2(HouseX + 8.5f, 2.8f), new Vector2(2.4f, .3f), WoodDark);
        Shape(house, "Drawing on the wall (a sleeping face, half rubbed out)", Ellipse(new Vector2(HouseX + 3f, 3.2f), .9f, .7f, 14), null,
            new Color(.9f, .86f, .78f, .5f), new Color(.9f, .86f, .78f, .3f), Color.clear, BackOrder + 1, false);
        Line(house, "Drawing: closed eye", new Color(.3f, .25f, .22f, .55f), .07f, BackOrder + 2, new Vector2(HouseX + 2.5f, 3.3f), new Vector2(HouseX + 2.9f, 3.15f), new Vector2(HouseX + 3.3f, 3.3f));

        Npc(town, "pip", "Pip", new Vector2(HouseX + 7.5f, 0f), new Color(.85f, .55f, .4f), .75f, false, false, new[]
        {
            T("calm", true, "Grandfather Tallow says the mountain dreams.", "I drew it! There, on the wall. It's sleeping. See, that's its face.",
                "He made me rub the face out. He said it's rude to draw someone who's asleep."),
            T("calm", false, "Want to see where I hide my Amber? ...No. It's a secret."),
            T("quake", true, "Did you feel the floor jump? Mum dropped the soup!", "I think the mountain rolled over in its sleep."),
            T("quake", false, "I'm drawing the face again. Don't tell Grandfather."),
        });
        Npc(town, "marrow", "Marrow", new Vector2(HouseX - 10.5f, 0f), new Color(.55f, .62f, .38f), 1.05f, false, false, new[]
        {
            T("calm", true, "The terraces have never failed us. Warm soil all year round, even in a hard frost.", "Folk say it's the springs. Folk say a lot of things."),
            T("calm", false, "Warm soil, good roots. Can't complain."),
            T("quake", true, "My rows have shifted a hand's width overnight. A hand's width!", "Soil doesn't walk."),
            T("quake", false, "Soil doesn't walk. It doesn't."),
        });

        // Crates up to the eave, and the song shell at the roof's peak (a mint sparkle marks it once Brannick has told you).
        // Three crates, each step well within a jump, the last clear of the roof's overhang so Qori can jump up past it.
        Crate(town, new Vector2(HouseX - 8.2f, 0f), new Vector2(2f, 1.6f));
        Crate(town, new Vector2(HouseX - 6.2f, 0f), new Vector2(2f, 3.2f));
        Crate(town, new Vector2(HouseX - 4.2f, 0f), new Vector2(2f, 4.6f));
        Shell(town, "roof", "Roofsong", new Vector2(HouseX + HouseW * .5f, 5.5f + 4f + .9f));
        var hint = new GameObject("After the rumour: a sparkle over the roof").AddComponent<TownVariant>();
        hint.transform.SetParent(town, false); hint.condition = "flag:shell-hint";
        Shape(hint.transform, "Sparkle", Star(new Vector2(HouseX + HouseW * .5f, 12.2f), .6f, .2f), null, TownUi.Mint, TownUi.Mint, Color.clear, PropOrder + 5, false);
    }

    // Brannick's smithy: open-fronted under an awning, with the anvil and the forge. Brannick runs the shop.
    static void Smithy(Transform town)
    {
        var s = new GameObject("Brannick's smithy").transform; s.SetParent(town, false);
        float x = SmithyX, w = 14f;
        Shape(s, "Back wall", Rect(x, 0f, w, 5.2f), null, new Color(.5f, .4f, .33f), new Color(.38f, .3f, .25f), Color.clear, BackOrder, false);
        Shape(s, "Awning", new[] { new Vector2(x - 1.5f, 5f), new Vector2(x + w + 1.5f, 5f), new Vector2(x + w, 7f), new Vector2(x, 7f) }, new[] { new Vector2(x, 7f), new Vector2(x + w, 7f) },
            new Color(.62f, .32f, .24f), new Color(.5f, .25f, .2f), Moss, GroundOrder + 2, true);
        foreach (float px in new[] { x - .8f, x + w + .3f }) Shape(s, "Post", Rect(px, 0f, .5f, 5f), null, Wood, WoodDark, Color.clear, GroundOrder + 1, false);
        Prop(s, "Anvil", new Vector2(x + 5f, 0f), new Vector2(1.6f, 1f), new Color(.35f, .35f, .38f));
        Shape(s, "Forge", Rect(x + 9.5f, 0f, 3f, 2.2f), null, new Color(.45f, .38f, .34f), new Color(.3f, .25f, .22f), Color.clear, PropOrder, false);
        Shape(s, "Forge fire", Ellipse(new Vector2(x + 11f, 1.6f), .9f, .5f, 12), null, new Color(1f, .62f, .25f), new Color(1f, .4f, .15f), Color.clear, PropOrder + 1, false);

        var smith = Npc(town, "brannick", "Brannick", new Vector2(x + 7f, 0f), new Color(.45f, .38f, .35f), 1.2f, false, false, new[]
        {
            T("quake", true, "That shook a whole rack of tools off the wall, that did.", "Well. What'll it be?"),
            T("", true, "Amber for goods, little sprout. That's how Qvale works.", "Bring me what you find out there and I'll see what I can do."),
            T("", false, "What'll it be?"),
        });
        var shop = smith.gameObject.AddComponent<TownShop>();
        shop.title = "Brannick's smithy";
        shop.items = new List<TownShop.Item>
        {
            new TownShop.Item { name = "Heart seed", price = 60, effect = TownShop.Effect.HeartSeed, description = "A seed from the old orchard. Plant it, and your heart grows. (+1 heart)" },
            new TownShop.Item { name = "Oil for Wick's lamps", price = 30, effect = TownShop.Effect.SetFlag, flag = "lamps", description = "Wick hasn't had oil for the street lamps since the frost. Pay for a season." },
            new TownShop.Item { name = "A rumour", price = 15, effect = TownShop.Effect.SetFlag, flag = "shell-hint", description = "Brannick heard something singing on the Brambles' roof." },
        };
    }

    // The street lamps, dark until their oil is paid for, and Wick the lantern keeper.
    static void Lamps(Transform town)
    {
        var lit = new GameObject("After the oil: lamps lit").AddComponent<TownVariant>();
        lit.transform.SetParent(town, false); lit.condition = "flag:lamps";
        foreach (float x in new[] { 36f, 48.5f, 104f, 112f, 140f })
        {
            Shape(town, "Lamp post", Rect(x - .12f, 0f, .24f, 3.6f), null, WoodDark, WoodDark, Color.clear, PropOrder, false);
            Shape(town, "Lamp", Ellipse(new Vector2(x, 3.8f), .35f, .3f, 10), null, new Color(.55f, .52f, .45f), new Color(.45f, .42f, .38f), Color.clear, PropOrder + 1, false);
            Shape(lit.transform, "Lamp light", Ellipse(new Vector2(x, 3.8f), 1.6f, 1.4f, 18), null, new Color(1f, .85f, .5f, .45f), new Color(1f, .85f, .5f, .05f), Color.clear, PropOrder + 2, false);
            Shape(lit.transform, "Flame", Ellipse(new Vector2(x, 3.8f), .28f, .24f, 10), null, Window, Window, Color.clear, PropOrder + 3, false);
        }
        Npc(town, "wick", "Wick", new Vector2(WickX, 0f), new Color(.4f, .45f, .6f), 1f, false, false, new[]
        {
            T("quake", true, "Every lamp went out at once when the ground shook.", "Then the smoke leaned the other way. Toward the high ground, then back. In, and out."),
            T("flag:lamps", true, "Oil! You paid for the oil? Bless you, sprout.", "Look at the street now. First time in a month."),
            T("flag:lamps", false, "The flames always lean toward the high ground, then back. In, and out. Like the air's being breathed."),
            T("calm", true, "Evening. I light the lamps at dusk... when I can afford the oil.", "If you've Amber to spare, Brannick at the smithy sells it."),
            T("", false, "No oil, no light. Brannick sells it, if you're feeling generous."),
        });
    }

    // Grandfather Tallow's house: taller, with a loft (a song shell on it), and the old man inside.
    static void OldMansHouse(Transform town)
    {
        var house = House(town, "Grandfather Tallow's house", OldX, OldW, 7f, 4.5f, new Color(.72f, .7f, .62f), new Color(.45f, .4f, .38f));
        Shape(house, "Loft", Rect(OldX + 9f, 3.5f, OldW - 9.6f, .4f), new[] { new Vector2(OldX + 9f, 3.9f), new Vector2(OldX + OldW - .6f, 3.9f) }, Wood, WoodDark, Shade(Wood, 1.15f), GroundOrder + 2, true);
        Crate(house, new Vector2(OldX + 6.6f, 0f), new Vector2(1.8f, 1.8f));
        Prop(house, "Chair", new Vector2(OldX + 3f, 0f), new Vector2(1.2f, 1.1f), Wood);
        Prop(house, "Stove", new Vector2(OldX + 13.2f, 0f), new Vector2(1.6f, 1.6f), new Color(.3f, .28f, .26f));
        Shell(house, "lullaby", "The Old Man's Lullaby", new Vector2(OldX + 14.5f, 4.8f));
        Npc(town, "tallow", "Grandfather Tallow", new Vector2(OldX + 4.6f, 0f), new Color(.62f, .6f, .56f), .95f, true, true, new[]
        {
            T("calm", true, "Hm. Another climber.", "You came up from the hollows below, didn't you. Warm down there. Warm up here too.",
                "Folk say the springs heat the ground. I've lived here eighty winters, and the springs have never once been cold.",
                "Listen at night, when the wind drops. The hills hum. Some things hum because they're sleeping, child.", "Mind your feet, the higher you go."),
            T("calm", false, "The higher you go, the louder it hums. Mind your feet."),
            T("quake", true, "You felt that. Everyone did.",
                "Look at the square. That crack wasn't there this morning. And the spring is beating faster. Beating, I said. Not bubbling.",
                "Whatever you touched out there...", "Don't do it again."),
            T("quake", false, "My grandfather said the last time the ground moved, the river ran backwards for a week. Then it lay down again.",
                "Lay down, he said. That's the word he used."),
        });
    }

    // The listening tree: a great tree with a bench beneath it, and Fennel the musician.
    static void ListeningTree(Transform town)
    {
        var t = new GameObject("The listening tree").transform; t.SetParent(town, false);
        Shape(t, "Trunk", new[] { new Vector2(TreeX - 3.2f, 0f), new Vector2(TreeX - 1.4f, 0f), new Vector2(TreeX - 1f, 6f), new Vector2(TreeX - 3f, 9f), new Vector2(TreeX - 4.2f, 8.6f), new Vector2(TreeX - 2.6f, 5.5f) },
            null, new Color(.42f, .32f, .24f), new Color(.3f, .22f, .17f), Color.clear, BackOrder + 2, false);
        Shape(t, "Canopy", Blob(new Vector2(TreeX - 1f, 11f), 9f, 4.2f, 9), null, new Color(.48f, .6f, .36f), new Color(.36f, .48f, .28f), Color.clear, BackOrder + 1, false);
        Shape(t, "Canopy (front)", Blob(new Vector2(TreeX + 3f, 9.6f), 5f, 2.6f, 4), null, new Color(.52f, .64f, .38f), new Color(.42f, .54f, .3f), Color.clear, BackOrder + 3, false);
        Prop(t, "Bench", new Vector2(TreeX + 1f, 0f), new Vector2(3.2f, .9f), Wood);
        var spot = new GameObject("Listening Spot").AddComponent<ListeningSpot>();
        spot.transform.SetParent(t, false); spot.transform.position = new Vector3(TreeX + 1f, 0f, 0f);
        var box = spot.GetComponent<BoxCollider2D>(); box.size = new Vector2(4f, 3f); box.offset = new Vector2(0f, 1.5f);
        spot.frameCentre = new Vector2(TreeX + 4.5f, 4.6f); spot.frameSize = 6.8f;
        spot.tracks = new List<ListeningSpot.Track>
        {
            new ListeningSpot.Track { title = "Qvale at Dusk", seed = 3, bpm = 66f },
            new ListeningSpot.Track { title = "Roofsong", shell = "roof", seed = 11, bpm = 84f },
            new ListeningSpot.Track { title = "The Old Man's Lullaby", shell = "lullaby", seed = 23, bpm = 56f },
        };
        Npc(town, "fennel", "Fennel", new Vector2(TreeX + 6f, 0f), new Color(.6f, .45f, .6f), .95f, false, false, new[]
        {
            T("quake", true, "Did you hear it? Under the shaking, a low note.", "Deeper than any string I own."),
            T("calm", true, "Sit a while. Music sounds better here; the ground hums along with it.", "I only know one tune of my own. Find me song shells and I'll learn theirs."),
            T("", false, "Sit on the bench. I'll play."),
        });
    }

    // A knot of roots on the rise east of town: touching it makes the quake.
    static void Knot(Transform town)
    {
        float y = SurfaceY(KnotX);
        var quake = new GameObject("Knot (the quake)").AddComponent<TownQuake>();
        quake.transform.SetParent(town, false); quake.transform.position = new Vector3(KnotX, y, 0f);
        Vector2 centre = new Vector2(KnotX, y + 2.05f - .35f);
        quake.core = AreaRoom.Image(quake.transform, "Knot", AreaRoom.Art("WorldSystems", "Wakeknot_Dormant"), centre, A0TestRoomBuilder.PropOrder + 2);
        quake.coreLit = AreaRoom.Art("WorldSystems", "Wakeknot_Awake");
        quake.glow = AreaRoom.Image(quake.transform, "Glow", AreaRoom.Art("WorldSystems", "Wakeknot_Glow"), centre, A0TestRoomBuilder.PropOrder + 3);
        quake.glow.color = new Color(1f, 1f, 1f, 0f);
        var box = quake.GetComponent<BoxCollider2D>(); box.size = new Vector2(2.4f, 3.6f); box.offset = new Vector2(0f, 1.8f);
        Note("Note - knot", new Vector2(KnotX - 6f, y), new Vector2(KnotX - 1.5f, y + 4f), "A knot of roots, pulsing like the spring in town. Touch it.");
    }

    // ------------------------------------------------------------------ pieces

    // A house you walk into from the left: back wall, walls (a doorway on the left), a gabled roof
    // you can stand on, and a front that fades away while Qori is inside (CutawayHouse).
    static Transform House(Transform town, string name, float x, float w, float h, float peak, Color front, Color roof)
    {
        var house = new GameObject(name).transform; house.SetParent(town, false);
        Shape(house, "Back wall", Rect(x, 0f, w, h), null, Interior, InteriorDark, Color.clear, BackOrder, false);
        Shape(house, "Wall (left, over the door)", Rect(x, 3.2f, .6f, h - 3.2f), null, Wood, WoodDark, Color.clear, GroundOrder + 1, true);
        Shape(house, "Wall (right)", Rect(x + w - .6f, 0f, .6f, h), null, Wood, WoodDark, Color.clear, GroundOrder + 1, true);
        var roofLine = new[] { new Vector2(x - 1.2f, h), new Vector2(x + w * .5f, h + peak), new Vector2(x + w + 1.2f, h) };
        Shape(house, "Roof", new[] { new Vector2(x - 1.2f, h - .5f), new Vector2(x + w + 1.2f, h - .5f), roofLine[2], roofLine[1], roofLine[0] }, roofLine, roof, Shade(roof, .75f), Moss, GroundOrder + 2, true);

        var cutaway = new GameObject("Cutaway").AddComponent<CutawayHouse>();
        cutaway.transform.SetParent(house, false);
        var box = cutaway.GetComponent<BoxCollider2D>(); box.offset = new Vector2(x + w * .5f, h * .5f); box.size = new Vector2(w - 1.4f, h);
        cutaway.facade.Add(Shape(house, "Front wall", Rect(x, 0f, w, h), null, front, Shade(front, .85f), Color.clear, FacadeOrder, false));
        cutaway.facade.Add(Shape(house, "Door", Rect(x + .9f, 0f, 1.8f, 3f), null, WoodDark, WoodDark, Color.clear, FacadeOrder + 1, false));
        for (float wx = x + 4.5f; wx < x + w - 2f; wx += 4.5f)
            cutaway.facade.Add(Shape(house, "Window", Rect(wx, 2f, 1.6f, 1.4f), null, Window, Shade(Window, .9f), Color.clear, FacadeOrder + 1, false));
        return house;
    }

    // A townsperson: a simple figure (robe, head, eyes; a beard and a stick for the old man) that
    // turns to face Qori, and the TownNpc that holds their lines.
    static TownNpc Npc(Transform town, string id, string displayName, Vector2 feet, Color robe, float height, bool beard, bool stick, TownNpc.Talk[] talks)
    {
        var root = new GameObject("NPC " + displayName); root.transform.SetParent(town, false); root.transform.position = feet;
        var figure = new GameObject("Figure").transform; figure.SetParent(root.transform, false);
        float s = height;
        Shape(figure, "Robe", new[] { new Vector2(-.5f * s, 0f), new Vector2(.5f * s, 0f), new Vector2(.32f * s, 1.15f * s), new Vector2(-.32f * s, 1.15f * s) }, null, robe, Shade(robe, .75f), Color.clear, FigureOrder, false);
        Shape(figure, "Head", Ellipse(new Vector2(0f, 1.5f * s), .38f * s, .38f * s, 16), null, new Color(.93f, .86f, .74f), new Color(.85f, .76f, .64f), Color.clear, FigureOrder + 1, false);
        Shape(figure, "Eye", Ellipse(new Vector2(.15f * s, 1.55f * s), .05f * s, .06f * s, 8), null, Color.black, Color.black, Color.clear, FigureOrder + 2, false);
        if (beard) Shape(figure, "Beard", new[] { new Vector2(-.05f * s, 1.38f * s), new Vector2(.36f * s, 1.42f * s), new Vector2(.12f * s, .85f * s) }, null, new Color(.95f, .95f, .92f), new Color(.85f, .85f, .82f), Color.clear, FigureOrder + 3, false);
        if (stick) Line(figure, "Stick", WoodDark, .09f, FigureOrder + 3, new Vector2(.6f * s, 0f), new Vector2(.68f * s, 1.5f * s));
        var npc = root.AddComponent<TownNpc>();
        npc.id = id; npc.displayName = displayName; npc.talks = talks; npc.figure = figure; npc.labelHeight = 2.2f * s;
        var box = root.GetComponent<BoxCollider2D>(); box.size = new Vector2(3.4f, 3f); box.offset = new Vector2(0f, 1.5f);
        return npc;
    }

    static TownNpc.Talk T(string when, bool once, params string[] lines) => new TownNpc.Talk { when = when, once = once, lines = lines };

    static void Amber(Transform parent, Vector2 at, int amount)
    {
        var obj = new GameObject($"Amber ({amount})"); obj.transform.SetParent(parent, false); obj.transform.position = at;
        float r = amount >= 15 ? .42f : amount >= 10 ? .34f : .26f;
        Shape(obj.transform, "Gem", new[] { new Vector2(0f, r * 1.3f), new Vector2(r, 0f), new Vector2(0f, -r * 1.1f), new Vector2(-r, 0f) }, null,
            new Color(1f, .8f, .38f), new Color(.9f, .5f, .15f), Color.clear, PropOrder + 4, false);
        obj.AddComponent<CircleCollider2D>().radius = .6f;
        var p2 = obj.AddComponent<TownPickup>(); p2.kind = TownPickup.Kind.Amber; p2.amount = amount;
    }

    static void Shell(Transform parent, string id, string title, Vector2 at)
    {
        var obj = new GameObject("Song shell: " + title); obj.transform.SetParent(parent, false); obj.transform.position = at;
        Shape(obj.transform, "Shell", new[] { new Vector2(-.55f, -.3f), new Vector2(.55f, -.3f), new Vector2(.45f, .1f), new Vector2(0f, .5f), new Vector2(-.45f, .1f) },
            null, new Color(.98f, .93f, .86f), new Color(.86f, .76f, .7f), Color.clear, PropOrder + 4, false);
        Line(obj.transform, "Spiral", new Color(.62f, .5f, .45f), .06f, PropOrder + 5,
            Enumerable.Range(0, 18).Select(i => new Vector2(Mathf.Cos(i * .55f) * (.04f + .016f * i), .05f + Mathf.Sin(i * .55f) * (.04f + .016f * i))).ToArray());
        obj.AddComponent<CircleCollider2D>().radius = .7f;
        var p = obj.AddComponent<TownPickup>(); p.kind = TownPickup.Kind.SongShell; p.shellId = id; p.trackTitle = title;
    }

    static void Crate(Transform parent, Vector2 foot, Vector2 size)
    {
        var top = new[] { new Vector2(foot.x, foot.y + size.y), new Vector2(foot.x + size.x, foot.y + size.y) };
        Shape(parent, "Crate", Rect(foot.x, foot.y, size.x, size.y), top, new Color(.6f, .45f, .3f), new Color(.45f, .33f, .22f), new Color(.7f, .55f, .38f), GroundOrder + 3, true);
    }

    static void Prop(Transform parent, string name, Vector2 foot, Vector2 size, Color color) =>
        Shape(parent, name, Rect(foot.x - size.x * .5f, foot.y, size.x, size.y), null, color, Shade(color, .75f), Color.clear, PropOrder - 2, false);

    public static float SurfaceY(float x)
    {
        for (int i = 1; i < Ground.Length; i++)
            if (x <= Ground[i].x) return Mathf.Lerp(Ground[i - 1].y, Ground[i].y, Mathf.InverseLerp(Ground[i - 1].x, Ground[i].x, x));
        return Ground[Ground.Length - 1].y;
    }

    static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

    static Vector2[] Rect(float x, float y, float w, float h) => new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h) };

    static Vector2[] Ellipse(Vector2 c, float rx, float ry, int n) =>
        Enumerable.Range(0, n).Select(i => c + new Vector2(Mathf.Cos(i * 2f * Mathf.PI / n) * rx, Mathf.Sin(i * 2f * Mathf.PI / n) * ry)).ToArray();

    static Vector2[] Star(Vector2 c, float outer, float inner) =>
        Enumerable.Range(0, 8).Select(i => c + (Vector2)(Quaternion.Euler(0f, 0f, i * 45f) * Vector3.up) * (i % 2 == 0 ? outer : inner)).ToArray();

    // A lumpy canopy: an ellipse whose radius wobbles.
    static Vector2[] Blob(Vector2 c, float rx, float ry, int seed)
    {
        var rnd = new System.Random(seed);
        return Enumerable.Range(0, 28).Select(i =>
        {
            float a = i * 2f * Mathf.PI / 28f, k = .85f + (float)rnd.NextDouble() * .25f;
            return c + new Vector2(Mathf.Cos(a) * rx * k, Mathf.Sin(a) * ry * k);
        }).ToArray();
    }

    // Rolling hills along [x0, x1]: a closed outline from a smooth wavy top down to y = -30.
    static IEnumerable<Vector2> Hills(float x0, float x1, float baseY, float amp, int seed)
    {
        var rnd = new System.Random(seed);
        float p1 = (float)rnd.NextDouble() * 6f, p2 = (float)rnd.NextDouble() * 6f;
        for (float x = x0; x <= x1; x += 4f) yield return new Vector2(x, baseY + amp * (.6f * Mathf.Sin(x * .035f + p1) + .4f * Mathf.Sin(x * .09f + p2)));
        yield return new Vector2(x1, -30f); yield return new Vector2(x0, -30f);
    }

    static Vector2[] At(Vector2 groupAt, params Vector2[] points) => points.Select(p => p - groupAt).ToArray();

    static BodyShape Shape(Transform parent, string name, Vector2[] outline, Vector2[] rim, Color top, Color bottom, Color rimColor, int order, bool solid)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var shape = obj.AddComponent<BodyShape>();
        shape.outline = outline; shape.rim = rim ?? new Vector2[0];
        shape.top = top; shape.bottom = bottom; shape.rimColor = rimColor;
        shape.sortingOrder = order; shape.solid = solid;
        shape.Rebuild();
        return shape;
    }

    static void Line(Transform parent, string name, Color color, float width, int order, params Vector2[] points)
    {
        var line = Shape(parent, name, new Vector2[0], points, color, color, color, order, false);
        line.rimWidth = width; line.Rebuild();
    }

    static void Wall(string name, Vector2 centre, Vector2 size)
    {
        var wall = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        wall.transform.position = centre;
        wall.AddComponent<BoxCollider2D>().size = size;
        wall.AddComponent<WallSurface>().allowsWallCling = false;
    }

    static Transform Group(string name, Vector2 follow, Vector2 alignedAt, Camera camera)
    {
        var group = new GameObject(name).AddComponent<ParallaxShape>();
        group.follow = follow; group.alignedAt = alignedAt; group.targetCamera = camera;
        group.home = alignedAt; group.transform.position = alignedAt;
        return group.transform;
    }

    static void Note(string name, Vector2 min, Vector2 max, string text)
    {
        var obj = new GameObject(name);
        obj.transform.position = (min + max) * .5f;
        obj.AddComponent<BoxCollider2D>().size = max - min;
        obj.AddComponent<PrototypeNote>().text = text;
    }

    // Batch-mode review: builds the scene and renders it from several camera positions.
    // Usage: -executeMethod QvalePrototypeBuilder.Capture -captureDir <folder>
    public static void Capture()
    {
        Build();
        string[] args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, "-captureDir");
        string folder = index >= 0 && index + 1 < args.Length ? args[index + 1] : "Temp/Captures";
        Directory.CreateDirectory(folder);
        foreach (var old in Directory.GetFiles(folder, "qvale_*.png")) File.Delete(old);
        EditorSceneManager.OpenScene(ScenePath);
        foreach (var b in Object.FindObjectsByType<TerrainBlock>(FindObjectsSortMode.None)) b.Rebuild();
        foreach (var shape in Object.FindObjectsByType<BodyShape>(FindObjectsSortMode.None)) shape.Rebuild();
        Camera camera = Camera.main; camera.aspect = 16f / 9f;
        var texture = new RenderTexture(1920, 1080, 24);
        var read = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        void Shot(string name, Vector2 at, float size)
        {
            camera.transform.position = new Vector3(at.x, at.y, -10f); camera.orthographicSize = size;
            foreach (var layer in Object.FindObjectsByType<ParallaxShape>(FindObjectsSortMode.None)) layer.Refresh(camera);
            camera.targetTexture = texture; camera.Render();
            RenderTexture.active = texture; read.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); read.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), read.EncodeToPNG());
        }
        Shot("qvale_01_overview", new Vector2(90f, 14f), 34f);
        Shot("qvale_02_arrival", new Vector2(8f, 11f), 7f);
        Shot("qvale_03_square_and_house", new Vector2(60f, 4f), 9f);
        Shot("qvale_04_smithy", new Vector2(95f, 4f), 7f);
        Shot("qvale_05_old_mans_house", new Vector2(125f, 5f), 7f);
        Shot("qvale_06_tree", new Vector2(TreeX + 2f, 6f), 8f);
        camera.targetTexture = null; RenderTexture.active = null;
        Debug.Log("[QvalePrototypeBuilder] Captures written to " + Path.GetFullPath(folder));
    }
}

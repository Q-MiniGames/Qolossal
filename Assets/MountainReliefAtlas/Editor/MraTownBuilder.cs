using System.Collections.Generic;
using System.Linq;
using Mra;
using UnityEngine;

// Qvale (the Lantern Vaults). Each home and the musician's loft is an enterable place: on the street,
// its accepted closed exterior stands on the ground by its door threshold, with a door (Up goes in,
// MraChamberDoor) and a safe landing outside; inside is its own scene (BuildHouse), the home's
// accepted cutaway painting as the room, with the same residents and shops, left by walking back out
// of the door. The smithy stays open-fronted on the street. The listening tree is a peaceful
// overlook on a broad level shelf (design_route.py RESTING), its loft and climb moved indoors.
// Services follow the rescues, all saved: the smith's forge after the Underarch Workshop (MR02_C01),
// the farmer at home after the Farmer Shelter (MR03_C03), the lamps lit after the Lost Lantern
// (MR05_C01); the traveller and the last climber move in when found. Scribble sells pages only for
// places Qori has visited. Shop prices: the heart seed keeps the existing 60; the rest are proposed
// (see the handoff), and weapon upgrades are recorded without a stat change (no tier system yet).
public static partial class MraWorldBuilder
{
    const float Street = 8f;
    const int Behind = -30, Front = 60;
    // Exteriors on the street stand behind the road's edge (its fill and moss lip draw in front of their
    // painted bases), so they sit behind the street rather than their floors running across it.
    const int Streetside = MraTerrain.FillOrder - 6;

    static void Town(Region r, Family f, Transform root, List<Piece> pieces)
    {
        TownHomes(r, f, root, pieces);
        TownStreet(r, root, pieces);
        // Along the street the camera eases out so each home shows whole (the tallest, the old
        // man's, stands about 12.4 u): a 15.2 u tall view, lifted so the street sits low in the frame.
        float x0 = world.ChambersOf(r.id).Min(c => c.entrance.x) - 12f, x1 = Pos(r.NodeById("MR04_N09")).x + 16f;
        var zoom = new GameObject("Street view (zoomed out for the houses)").AddComponent<VistaZone>();
        zoom.transform.SetParent(root, false); zoom.transform.position = new Vector3((x0 + x1) * .5f, Street + 3f, 0f);
        zoom.GetComponent<BoxCollider2D>().size = new Vector2(x1 - x0, 8f);
        zoom.size = 7.6f; zoom.lift = 3.4f; zoom.easeSeconds = 1.2f;
    }

    // While an interior scene is built, a home's local coordinates are the scene's own.
    static bool buildingInterior;
    static Vector2 Origin(Chamber c) => buildingInterior ? Vector2.zero : new Vector2(c.entrance.x - c.playerSpawn.x, Street - 1f);
    static Vector2 Local(Chamber c, Vector2 p) => Origin(c) + p;
    static Chamber C(string id) => world.ChamberById(id);

    static void TownHomes(Region r, Family f, Transform root, List<Piece> pieces)
    {
        // The smithy: open-fronted. Brannick works it once he's home; until then the forge is cold.
        var smithy = C("MR04_C01");
        var s = Group(root, "Interior MR04_C01 Smithy");
        var forge = Image(s, "Smithy", Art("Town/Town_Qvale_Smithy"), Local(smithy, new Vector2(10f, 1f)), Streetside);
        GroundProp(forge.transform, pieces, new[] { forge }, .1f);
        Discovery(s, smithy);
        var cold = Variant(s, "Before Brannick is home", new[] { "flag:mra:MR02_C01:reward" }, invert: true);
        Npc(cold, "cold-forge", "The cold forge", Local(smithy, new Vector2(12f, 1f)), null, new[]
        {
            T("", false, "The forge is cold and the tools hang still. Whoever works here hasn't come home."),
        });
        var home = Variant(s, "After the Underarch Workshop rescue", new[] { "flag:mra:MR02_C01:reward" });
        var smith = Npc(home, "brannick", "Brannick", Local(smithy, smithy.resident[0] - new Vector2(0f, .8f)), "Smith", new[]
        {
            T("", true, "You got me out of that workshop, little one. The forge is lit again.", "Bring Amber and I'll put a green sap edge on anything you carry."),
            T("quake", false, "Another tremor. Rattled every hook on the wall. What'll it be?"),
            T("", false, "What'll it be?"),
        });
        Shop(smith, "Brannick's forge",
            Item("Heart seed", 60, TownShop.Effect.HeartSeed, "", "A seed from the old orchard. Plant it, and your heart grows. (+1 heart)"),
            Item("Leaf Staff: sap edge", 80, TownShop.Effect.SetFlag, "upgrade:staff-t2", "A green sap edge for the staff. (Prototype: recorded; no stat change yet.)"),
            Item("Leaf Sword: sap edge", 80, TownShop.Effect.SetFlag, "upgrade:sword-t2", "A green sap edge for the sword. (Prototype: recorded; no stat change yet.)"),
            Item("Seedpod Mace: sap edge", 80, TownShop.Effect.SetFlag, "upgrade:mace-t2", "(Prototype: recorded; no stat change yet.)"),
            Item("Thorn Spear: sap edge", 80, TownShop.Effect.SetFlag, "upgrade:spear-t2", "(Prototype: recorded; no stat change yet.)"));

        // The homes and the loft: closed exteriors with doors on the street; their rooms are scenes.
        foreach (var c in world.ChambersOf(r.id).Where(x => x.IsHouse)) HouseFront(f, root, c, pieces);
    }

    // What's inside each home (its own interior scene, in that scene's coordinates).
    static void HouseContents(Family f, Transform root, Chamber c)
    {
        switch (c.id)
        {
            case "MR04_C02": Herbalist(f, root); break;
            case "MR04_C03": Mapmaker(f, root); break;
            case "MR04_C04": Residents(f, root); break;
            case "MR04_C05": OldMan(f, root); break;
            case "MR04_C06": Loft(f, root); break;
        }
    }

    static void Herbalist(Family f, Transform root)
    {
        // The herbalist's home (home A).
        var herb = C("MR04_C02");
        var h = Home(f, root, herb, "Interior MR04_C02 Herbalist Home", "HomeA");
        var herbalist = Npc(h, "herbalist", "Sorrel", Local(herb, herb.resident[0] - new Vector2(0f, .8f)), "Herbalist", new[]
        {
            T("", true, "Mind the drying racks. You look like you've climbed a long way.", "Heart seeds take root in brave little chests. I'll sell you one, if you've the Amber."),
            T("quake", false, "The jars all hopped on the shelf. Nothing broken. This time."),
            T("", false, "Something for the road?"),
        });
        Shop(herbalist, "Sorrel's remedies",
            Item("Heart seed", 60, TownShop.Effect.HeartSeed, "", "Plant it, and your heart grows. (+1 heart)"),
            Item("Sap vial", 25, TownShop.Effect.SetFlag, "upgrade:sap-vial", "A vial of green sap. (Prototype: recorded; healing items aren't in yet.)"));

    }

    static void Mapmaker(Family f, Transform root)
    {
        // The mapmaker's room (home B): Scribble sells pages for visited places.
        var map = C("MR04_C03");
        var m = Home(f, root, map, "Interior MR04_C03 Mapmaker Room", "HomeB");
        var scribble = Npc(m, "scribble", "Scribble", Local(map, map.resident[0] - new Vector2(0f, .8f)), null, new[]
        {
            T("", true, "Pages! Every place you've walked, I can draw it neater than you saw it.", "Places you haven't been? Go there first. I don't draw guesses."),
            T("", false, "Which page?"),
        });
        ScribbleFigure(scribble.transform, Local(map, map.resident[0] - new Vector2(0f, .8f)));
        var pages = world.regions.Select(reg => Item($"Page: {reg.name}", 15, TownShop.Effect.SetFlag, MraState.PageFlag(reg.id),
            $"The paths of {reg.name}, neatly drawn. Hidden places stay yours to find.", "flag:visited:" + reg.id)).ToArray();
        Shop(scribble, "Scribble's pages", pages);

    }

    static void Residents(Family f, Transform root)
    {
        // Residents' home (home C): Pip, and Marrow once he's back from the Farmer Shelter.
        var res = C("MR04_C04");
        var hc = Home(f, root, res, "Interior MR04_C04 Residents' Home A", "HomeC");
        Npc(hc, "pip", "Pip", Local(res, new Vector2(9f, 1f)), "Child", new[]
        {
            T("calm", true, "Did you come up from the hollows? Grandfather says nobody climbs up from there.", "I want to see the lake at the top one day."),
            T("quake", true, "The floor jumped! My cup rolled right under the bed.", "Grandfather says it's nothing. He said it twice."),
            T("", false, "Want to see my drawing? It's a mountain. A sleepy one."),
        });
        var farmer = Variant(hc, "After the Farmer Shelter rescue", new[] { "flag:mra:MR03_C03:reward" });
        Npc(farmer, "marrow", "Marrow", Local(res, res.resident[0] - new Vector2(0f, .8f)), "Farmer", new[]
        {
            T("", true, "Home! You got me out of that shelter, and I won't forget it.", "The terraces will need me back. Come by in the growing season."),
            T("", false, "Warm soil, good roots. Can't complain."),
        });

    }

    static void OldMan(Family f, Transform root)
    {
        // The old man's home: a warning, said kindly.
        var old = C("MR04_C05");
        var ho = Home(f, root, old, "Interior MR04_C05 Old Man's Home", "OldMan");
        Npc(ho, "tallow", "Grandfather Tallow", Local(old, old.resident[0] - new Vector2(0f, .8f)), "Elder", new[]
        {
            T("calm", true, "Hm. Another climber.", "The springs are warm, the soil is warm, and the hills hum at night.", "Some things hum because they're sleeping, child. Mind your feet up there."),
            T("quake", true, "You felt that. Everyone did.", "Every knot of roots you wake up there, the town shakes down here.", "I won't tell you to stop. I'll tell you to be careful."),
            T("", false, "Careful, child. That's all I ask."),
        });

    }

    static void Loft(Family f, Transform root)
    {
        // The musician's loft (Batch 13 cutaway, 100 PPU, registered on the door threshold): its
        // painted stair has 1.2 u risers 5.4, 10.4 and 14.4 u right of the door, the loft floor 3 u
        // deep (W2 REGISTRATION.json). The collision follows those walk lines; the painting is the look.
        var loft = C("MR04_C06");
        var l = Group(root, "Interior MR04_C06 Musician Loft");
        var room = ArtOrNull("Town/Town_Qvale_Loft_Cutaway");
        Vector2 shellAt = loft.rewardAt[0];
        var stair = new List<Vector2>();
        if (room != null)
        {
            Image(l, "Interior (cutaway)", room, Local(loft, new Vector2(1.6f, 1f)), Behind);
            float[] riserX = { 9f, 14f, 18f }; float[] treadY = { 2.2f, 3.4f, 4.6f };
            for (int i = 0; i < 3; i++) { stair.Add(Local(loft, new Vector2(riserX[i], treadY[i]))); stair.Add(Local(loft, new Vector2(i < 2 ? riserX[i + 1] : 21f, treadY[i]))); }
            shellAt = new Vector2(19.9f, 5.4f);
            var steps = Terrain(l, "Loft stair (collision on the painted walk lines)", stair, Local(loft, new Vector2(0f, 1f)).y, f, true, false);
            steps.enabled = false; steps.fill = null; steps.top = null; steps.face = null; steps.enabled = true;
        }
        else
        {
            var back = Terrain(l, "Loft back wall", new List<Vector2> { new Vector2(-2f, loft.size.y + 2f), new Vector2(loft.size.x + 2f, loft.size.y + 2f) }, -4f, f, true, true, false, new Color(.62f, .5f, .4f), -20);
            back.enabled = false; back.top = null; back.enabled = true;
            Image(l, "Musician corner", Art("Town/Town_Qvale_MusicianCorner"), Local(loft, new Vector2(4.5f, 1f)), Behind + 2);
            for (int i = 1; i < loft.platforms.Length; i++)
            {
                var p = loft.platforms[i];
                float right = i + 1 < loft.platforms.Length ? loft.platforms[i + 1].x : p.x + p.z;
                stair.Add(Local(loft, new Vector2(p.x, p.y))); stair.Add(Local(loft, new Vector2(right, p.y)));
            }
            Terrain(l, "Loft stair", stair, Local(loft, new Vector2(0f, 1f)).y, f, true, false, true, new Color(.78f, .64f, .5f));
        }
        var shell = new GameObject("Reward song-shell (Musician Loft)");
        shell.transform.SetParent(l, false); shell.transform.position = Local(loft, shellAt);
        shell.AddComponent<CircleCollider2D>().radius = .7f;
        var rw = shell.AddComponent<MraReward>(); rw.kind = MraReward.Kind.SongShell; rw.rewardId = loft.rewardId; rw.title = loft.name;
        rw.image = Image(shell.transform, "Art", Art("Props/Pickup_SongShell"), shell.transform.position, -8);
    }

    // A home's room, in its interior scene: the accepted cutaway painting, its door threshold on the floor.
    static Transform Home(Family f, Transform root, Chamber c, string name, string art)
    {
        var g = Group(root, name);
        Image(g, "Interior (cutaway)", Art($"Town/Town_Qvale_{art}_Cutaway"), Local(c, new Vector2(1.6f, 1f)), Behind);
        return g;
    }

    static readonly Dictionary<string, string> HouseArt = new Dictionary<string, string>
    {
        { "MR04_C02", "HomeA" }, { "MR04_C03", "HomeB" }, { "MR04_C04", "HomeC" }, { "MR04_C05", "OldMan" },
    };

    // The painting a home's interior shows (its accepted cutaway; the loft's Batch 13 cutaway).
    static Sprite InteriorArt(Chamber c) =>
        HouseArt.TryGetValue(c.id, out string art) ? Art($"Town/Town_Qvale_{art}_Cutaway") : c.id == "MR04_C06" ? ArtOrNull("Town/Town_Qvale_Loft_Cutaway") : null;

    // A home on the street: its accepted closed exterior standing on the ground by its door
    // threshold (the art's pivot), a door at the painted doorway with an Up prompt, and the walk-up
    // discovery. The loft has no exterior painting yet: a rock doorway with the musician's corner.
    static void HouseFront(Family f, Transform root, Chamber c, List<Piece> pieces)
    {
        var g = Group(root, $"House {c.id} {c.name}");
        Discovery(g, c);
        Vector2 threshold = Local(c, new Vector2(1.6f, 1f));
        threshold.y = Ground(pieces, threshold.x);
        float doorX;
        if (HouseArt.TryGetValue(c.id, out string art))
        {
            Image(g, "Exterior (closed)", Art($"Town/Town_Qvale_{art}_Closed"), threshold - new Vector2(0f, .04f), Streetside);
            doorX = threshold.x + DoorOffset(Art($"Town/Town_Qvale_{art}_Closed"));
        }
        else
        {
            var mouth = Image(g, "Loft doorway (stand-in)", Art(f.entrance), threshold + new Vector2(1.2f, 0f), Streetside, EntranceScale);
            GroundProp(mouth.transform, pieces, new[] { mouth }, .12f);
            var corner = Image(g, "Musician corner", Art("Town/Town_Qvale_MusicianCorner"), threshold + new Vector2(-1.4f, 0f), Behind + 1, .8f);
            GroundProp(corner.transform, pieces, new[] { corner }, .06f);
            doorX = threshold.x + 1.2f;
        }
        var obj = new GameObject($"Door {c.id} {c.name}");
        obj.transform.SetParent(g, false); obj.transform.position = new Vector2(doorX, threshold.y);
        Trigger(obj, new Vector2(1.8f, 2.6f), new Vector2(0f, 1.3f));
        var door = obj.AddComponent<MraChamberDoor>();
        door.chamberId = c.id; door.chamberName = c.name; door.chamberScene = c.scene; door.prerequisite = c.prerequisite;
        door.promptHeight = 2.6f;
        // Coming back out: on the street in front of this door, facing out into the town.
        Arrival(g, c.returnId, new Vector2(doorX + .6f, threshold.y + .6f), 1f);
    }

    // The painted doorway's middle, right of a home's pivot, in world units: measured on each
    // accepted closed exterior (HomeC's door is up its stone steps).
    static readonly Dictionary<string, float> DoorOffsets = new Dictionary<string, float>
    {
        { "HomeA", 2.43f }, { "HomeB", 2.02f }, { "HomeC", 4.95f }, { "OldMan", 3.64f },
    };
    static float DoorOffset(Sprite s) => DoorOffsets.TryGetValue(s.name.Replace("Town_Qvale_", "").Replace("_Closed", ""), out float d) ? d : 1f;

    // A home's interior scene: the room, its floor at the door threshold, the far wall, the way back
    // out through the door (like a side chamber's return), and who lives there.
    public static void BuildHouse(Chamber c)
    {
        var f = FamilyOf(c.region);
        var region = world.RegionById(c.region);
        groundingScene = c.scene;
        var scene = Begin(ScenePath(c.scene), out Transform root);
        var camera = Scaffold(scene, c.playerSpawn, c.name, "mra:" + c.id, c.region, c.id, new Color(.11f, .085f, .06f));
        Music(root, RegionMusic[c.region]);   // the town's track carries on indoors
        // The camera shows the whole painted home: sized to its height, the room bounds to its width.
        var art = InteriorArt(c);
        float left = 0f, right = c.size.x, top = c.size.y;
        if (art != null)
        {
            left = 1.6f - art.pivot.x / art.pixelsPerUnit; right = left + art.rect.width / art.pixelsPerUnit;
            top = 1f + (art.rect.height - art.pivot.y) / art.pixelsPerUnit;
        }
        float floorView = -.4f, camSize = Mathf.Max(5.5f, (top + .5f - floorView) * .5f);
        camera.orthographicSize = camSize;
        float halfW = Mathf.Max((right - left) * .5f + .6f, camSize * 16f / 9f);
        var bounds = new GameObject("Camera Bounds").AddComponent<CameraBounds>();
        bounds.transform.SetParent(root, false);
        bounds.room = new Rect((left + right) * .5f - halfW, floorView, halfW * 2f, camSize * 2f + .2f); bounds.top = true;
        Arrival(root, "entry", c.playerSpawn, 1f);
        var room = Group(root, "Room");
        float back = Mathf.Max(c.size.x - 1.6f, right - .4f);
        Terrain(room, "Floor", new List<Vector2> { new Vector2(-3f, 1f), new Vector2(back + 3f, 1f) }, -4f, f, true, false, true, new Color(.55f, .45f, .36f));
        Wall(room, "Back of the room", new Vector2(back, 6f), new Vector2(1f, 14f));
        buildingInterior = true;
        try { HouseContents(f, room, c); }
        finally { buildingInterior = false; }
        var exit = new GameObject("Out to " + region.name);
        exit.transform.SetParent(root, false); exit.transform.position = new Vector3(.3f, c.size.y * .5f, 0f);
        Trigger(exit, new Vector2(1.2f, c.size.y + 4f), Vector2.zero);
        var ret = exit.AddComponent<MraChamberReturn>(); ret.parentScene = region.scene; ret.returnId = c.returnId; ret.label = region.name;
        Save(scene, ScenePath(c.scene));
    }

    // Walking up to a home or shop discovers it for the Chart, like a side chamber's mouth.
    static void Discovery(Transform parent, Chamber c)
    {
        var obj = new GameObject("Door " + c.id);
        obj.transform.SetParent(parent, false); obj.transform.position = Local(c, new Vector2(c.playerSpawn.x, 1f));
        Trigger(obj, new Vector2(4f, 4f), new Vector2(0f, 2f));
        var d = obj.AddComponent<MraDiscovery>(); d.flag = c.DiscoveredFlag; d.label = c.name; d.rewardId = c.rewardId;
        d.rewardOnVisit = c.type != "sequence";   // the loft's song shell is a find of its own
    }

    static void TownStreet(Region r, Transform root, List<Piece> pieces)
    {
        var street = Group(root, "Street");
        float G(float x) => Ground(pieces, x);
        // The lamps: dark until Wick is home from the Lost Lantern.
        var lit = Variant(street, "After the Lost Lantern rescue: lamps lit", new[] { "flag:mra:MR05_C01:reward" });
        var dark = Variant(street, "Before: lamps dark", new[] { "flag:mra:MR05_C01:reward" }, invert: true);
        foreach (float x in new[] { 30f, 52f, 74f, 96f, 146f, 210f })
        {
            var unlit = Image(dark, "Lamp (unlit)", Art("Town/Town_Qvale_StreetLamp_Unlit"), new Vector2(x, G(x)), Behind + 3, .7f);
            var on = Image(lit, "Lamp (lit)", Art("Town/Town_Qvale_StreetLamp_Lit"), new Vector2(x, G(x)), Behind + 3, .7f);
            var glow = Image(lit, "Lamp glow", Art("Town/Town_Qvale_StreetLamp_Glow"), new Vector2(x, G(x)), Behind + 4, .7f);
            glow.color = new Color(1f, 1f, 1f, .7f);
            // Both lamp states share a canvas: ground the post by the unlit one, move the rest with it.
            float moved = GroundProp(unlit.transform, pieces, new[] { unlit }, .06f);
            on.transform.position += new Vector3(0f, moved, 0f); glow.transform.position += new Vector3(0f, moved, 0f);
        }
        Npc(lit, "wick", "Wick", new Vector2(64f, G(64f)), "Keeper", new[]
        {
            T("", true, "Every lamp in Qvale, lit again. First time since I got lost.", "You found me in the dark. I'll keep a light on for you."),
            T("", false, "The flames lean toward the high ground, then back. In, and out."),
        });
        // Newcomers on the residents' lane.
        var traveller = Variant(street, "After the Weather Cave rescue", new[] { "flag:mra:MR06_C06:reward" });
        Npc(traveller, "traveller", "A traveller", new Vector2(182f, G(182f)), "Musician", new[] { T("", false, "Qvale's warmer than the heights. I might stay a season.") });
        var last = Variant(street, "After the Watcher's Hollow rescue", new[] { "flag:mra:MR07_C05:reward" });
        Npc(last, "last", "The last climber", new Vector2(187f, G(187f)), "Child", new[] { T("", false, "I watched the lake for so long. It's nice to hear people again.") });

        // The listening tree: a peaceful overlook. The bench and the tree stand mid-shelf on level
        // ground (design_route.py RESTING: 4 u before the beat to 15 u after), the loft's door at the
        // shelf's far end, Fennel beyond the bench, nothing climbable or hostile near; the seated
        // view is framed toward the open valley. Tracks unlock with song shells.
        var tree = Group(street, "The listening tree");
        float tx = r.NodeById("MR04_N09").position.x, bx = tx + 7f;
        var trunk = Image(tree, "Tree (back)", Art("Town/Town_Qvale_ListeningTree_Back"), new Vector2(bx + 2.4f, G(bx + 2.4f)), Behind - 2, .8f);
        GroundProp(trunk.transform, pieces, new[] { trunk }, .25f);
        var bench = Image(tree, "Bench", Art("Town/Town_Qvale_ListeningBench"), new Vector2(bx, G(bx)), Behind + 4);
        GroundProp(bench.transform, pieces, new[] { bench }, .05f);
        var spot = new GameObject("Listening Spot").AddComponent<ListeningSpot>();
        spot.transform.SetParent(tree, false); spot.transform.position = new Vector3(bx, G(bx), 0f);
        var box = spot.GetComponent<BoxCollider2D>(); box.size = new Vector2(4f, 3f); box.offset = new Vector2(0f, 1.5f);
        spot.frameCentre = new Vector2(bx - 1.5f, G(bx) + 3.3f); spot.frameSize = 6.2f;
        // The seat's top: the bench art's seat plank is 0.75 u above its registered foot (rows 114 / 204).
        spot.hasSeat = true; spot.seat = new Vector2(bench.transform.position.x, bench.transform.position.y + .75f);
        // The real soundtrack (in-game titles stay the song shells' places; the songs' own titles are
        // never shown). Nightsong opens; the shells cycle through the rest.
        spot.tracks = new List<ListeningSpot.Track> { new ListeningSpot.Track { title = "Qvale at Dusk", seed = 3, bpm = 66f, clip = MusicClip("Nightsong") } };
        var pool = new[] { "MoonlitForestPath", "GuardiansOfTheMoss", "Forest", "TheWetlandsSecret", "AwakeningInTheTitansPalm", "TerracesOfTheTitan",
                           "ASmallLightInTheDark", "ThePathToThePeak", "SanctuaryOfLivingRoots", "TheTitansDream", "TheTitansSleep" };
        int seed = 11, k = 0;
        foreach (var c in world.chambers.Where(x => x.reward == "song-shell"))
            spot.tracks.Add(new ListeningSpot.Track { title = c.name, shell = c.rewardId, seed = seed += 7, bpm = 56f + seed % 30, clip = MusicClip(pool[k++ % pool.Length]) });
        Npc(tree, "fennel", "Fennel", new Vector2(bx + 5.5f, G(bx + 5.5f)), "Musician", new[]
        {
            T("", true, "Sit a while. Every song shell you bring back, I'll learn."),
            T("", false, "Sit on the bench. I'll play."),
        });

        // After a quake: a crack across the square.
        var crack = new GameObject("After a quake: the square cracks").AddComponent<TownVariant>();
        crack.transform.SetParent(street, false); crack.condition = "quake";
        float cx = r.NodeById("MR04_N03").position.x + 4f;
        Image(crack.transform, "Crack", Art("Town/Town_Qvale_QuakeCrack"), new Vector2(cx, G(cx) + .05f), -19);
        foreach (Transform child in crack.transform) child.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- town helpers

    static Transform Variant(Transform parent, string name, string[] requires, bool invert = false)
    {
        var v = new GameObject(name).AddComponent<MraVariant>();
        v.transform.SetParent(parent, false); v.requires = requires; v.invert = invert;
        return v.transform;
    }

    static TownNpc Npc(Transform parent, string id, string displayName, Vector2 feet, string rig, TownNpc.Talk[] talks)
    {
        var root = new GameObject("NPC " + displayName);
        root.transform.SetParent(parent, false); root.transform.position = feet;
        var npc = root.AddComponent<TownNpc>();
        npc.id = id; npc.displayName = displayName; npc.talks = talks; npc.labelHeight = 2.4f;
        if (rig != null) npc.figure = Townsfolk(root.transform, rig, feet);
        var box = root.GetComponent<BoxCollider2D>(); box.size = new Vector2(3.4f, 3f); box.offset = new Vector2(0f, 1.5f);
        return npc;
    }

    static void ScribbleFigure(Transform parent, Vector2 feet)
    {
        var fig = Group(parent, "Figure Scribble");
        Image(fig, "Leg", Art("Characters/Scribble/Scribble_Leg"), feet + new Vector2(0f, .23f), 4);
        Image(fig, "Body", Art("Characters/Scribble/Scribble_Body"), feet + new Vector2(0f, .62f), 5);
        Image(fig, "Head", Art("Characters/Scribble/Scribble_Head"), feet + new Vector2(.32f, 1.05f), 6);
        Image(fig, "Map roll", Art("Characters/Scribble/Scribble_MapRoll"), feet + new Vector2(-.35f, .7f), 7);
        parent.GetComponent<TownNpc>().figure = fig;
    }

    static TownNpc.Talk T(string when, bool once, params string[] lines) => new TownNpc.Talk { when = when, once = once, lines = lines };

    static TownShop.Item Item(string name, int price, TownShop.Effect effect, string flag, string description, string available = "") =>
        new TownShop.Item { name = name, price = price, effect = effect, flag = flag, description = description, available = available };

    static void Shop(TownNpc npc, string title, params TownShop.Item[] items)
    {
        var shop = npc.gameObject.AddComponent<TownShop>();
        shop.title = title; shop.items = items.ToList();
    }
}

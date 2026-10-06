using System.Collections.Generic;
using System.Linq;
using Mra;
using UnityEditor;
using UnityEngine;

// A side chamber's own small scene, from its local geometry (CHAMBERS.md): platforms
// [left, top, width, depth], the catch floor (same convention), walls / gates / water / hazards
// [left, bottom, width, height], points for controls, anchors, pods, residents and the reward.
// Every chamber is enclosed, with a full-height return threshold at its entrance end that is always
// open (a failed trial never needs a climb back to a high door), and a persistent reward.
public static partial class MraWorldBuilder
{
    public static void BuildChamber(Chamber c)
    {
        var region = world.RegionById(c.region);
        var f = FamilyOf(c.region);
        groundingScene = c.scene;
        var scene = Begin(ScenePath(c.scene), out Transform root);
        var camera = Scaffold(scene, c.playerSpawn, c.name, "mra:" + c.id, c.region, c.id, f.skyColour * .45f + new Color(0f, 0f, 0f, 1f));
        var bounds = new GameObject("Camera Bounds").AddComponent<CameraBounds>();
        bounds.transform.SetParent(root, false);
        bounds.room = new Rect(-1f, -2f, c.size.x + 2f, c.size.y + 3f); bounds.top = true;
        Arrival(root, "entry", c.playerSpawn, 1f);
        Music(root, CaveMusic);

        var geo = Group(root, "Chamber");
        float w = c.size.x, h = c.size.y;
        // The chamber's back wall, and its enclosing rock.
        var back = Terrain(geo, "Back wall", new List<Vector2> { new Vector2(-2f, h + 2f), new Vector2(w + 2f, h + 2f) }, -4f, f, true, true, false, new Color(.45f, .42f, .4f), -20);
        back.enabled = false; back.top = null; back.enabled = true;
        Box(geo, "West rock", Rect.MinMaxRect(-3f, -4f, 0f, h + 3f), f, true, false);
        Box(geo, "East rock", Rect.MinMaxRect(w, -4f, w + 3f, h + 3f), f, true, false);
        var ceiling = Box(geo, "Ceiling", Rect.MinMaxRect(-3f, h, w + 3f, h + 3f), f, true, false);
        // Floating slabs (platforms, the ceiling) get the kit's stone underside band.
        var under = ArtOrNull("Terrain/" + (f.id == "Qvale" ? "A5" : f.id) + "_Ceiling_Under");
        // Tinted toward the cave rock, so a kit's warm or dark band reads as the same stone.
        Color caveRock = Average(Art(f.caveFill), 0f, 1f), bandRock = under != null ? Average(under, .7f, 1f) : Color.white;
        Color underTint = new Color(Mathf.Clamp01(caveRock.r / Mathf.Max(.05f, bandRock.r)), Mathf.Clamp01(caveRock.g / Mathf.Max(.05f, bandRock.g)), Mathf.Clamp01(caveRock.b / Mathf.Max(.05f, bandRock.b)));
        underTint = Color.Lerp(Color.white, underTint, .75f);
        void Underside(MraTerrain t) { if (under == null) return; t.enabled = false; t.under = under; t.underTint = underTint; t.enabled = true; }
        Underside(ceiling);
        foreach (var p in c.catchFloor) Terrain(geo, "Catch floor", Platform(p), p.y - p.w, f, true);
        if (c.catchFloor.Length == 0) Terrain(geo, "Catch floor", new List<Vector2> { new Vector2(0f, -1f), new Vector2(w, -1f) }, -4f, f, true);
        for (int i = 0; i < c.platforms.Length; i++)
        {
            var slab = Terrain(geo, "Platform " + i, Platform(c.platforms[i]), c.platforms[i].y - c.platforms[i].w, f, true);
            if (c.platforms[i].y - c.platforms[i].w > .2f) Underside(slab);   // only slabs standing clear of the floor
        }
        for (int i = 0; i < c.walls.Length; i++)
        {
            var r = c.walls[i];   // [left, bottom, width, height]: moss walls Qori climbs with Climbing Moss
            Box(geo, "Climbing wall " + i, new Rect(r.x, r.y, r.z, r.w), f, true, true);
        }

        // The way out: the chamber's entrance end, full height.
        var exit = new GameObject("Return to " + region.name);
        exit.transform.SetParent(root, false); exit.transform.position = new Vector3(.2f, h * .5f, 0f);
        Trigger(exit, new Vector2(1.2f, h + 4f), Vector2.zero);
        var ret = exit.AddComponent<MraChamberReturn>(); ret.parentScene = region.scene; ret.returnId = c.returnId; ret.label = region.name;

        var mech = Group(root, "Trial");
        var gates = c.gates.Select((g, i) => Gate(mech, f, g, c.rewardId, i)).ToList();
        switch (c.type)
        {
            case "pressure-plate":
                Plate(mech, c.plate[0], gates);
                Crate(mech, f, c.crate[0]);
                break;
            case "sequence":
                if (c.switches.Length > 0) Sequence(mech, c.switches, gates);
                break;
            case "rescue":
                foreach (var s in c.switches) Lever(mech, s - new Vector2(0f, 1f), "Turn the winch", gates.ToArray());
                break;
            case "water-level":
            {
                var water = Water(mech, c.water[0]);
                Lever(mech, c.lever[0] - new Vector2(0f, .8f), "Open the sluice", water);
                break;
            }
            case "thread-trial":
                foreach (var a in c.anchors) Anchor(mech, a, f, "Thread anchor");
                break;
            case "dash-trial":
                foreach (var hz in c.hazards) TimedThorns(mech, f, hz);
                break;
            case "pogo-trial":
                foreach (var p in c.pods) Pod(mech, p);
                break;
        }
        Reward(mech, c, f);
        if (!string.IsNullOrEmpty(c.objective)) Note(root, c.objective, new Vector2(c.playerSpawn.x + 3f, c.playerSpawn.y));
        Save(scene, ScenePath(c.scene));
    }

    static List<Vector2> Platform(Vector4 p) => new List<Vector2> { new Vector2(p.x, p.y), new Vector2(p.x + p.z, p.y) };

    // A gate (a root gate, solid, unclimbable) that its switch holds open and that stays open once
    // the chamber's reward is taken, so the way out is never shut behind Qori.
    static MraGate Gate(Transform parent, Family f, Vector4 g, string rewardId, int i)
    {
        var obj = new GameObject("Root gate " + i) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(parent, false); obj.transform.position = new Vector3(g.x + g.z * .5f, g.y + g.w * .5f, 0f);
        var solid = obj.AddComponent<BoxCollider2D>(); solid.size = new Vector2(g.z, g.w);
        obj.AddComponent<WallSurface>().allowsWallCling = false;
        var rg = RootGateArt(obj.transform, g.x + g.z * .5f, g.y, g.w);
        rg.solid = solid;
        var gate = obj.AddComponent<MraGate>(); gate.solid = solid; gate.visual = rg; gate.openAfterFlag = rewardId;
        return gate;
    }

    static void Plate(Transform parent, Vector2 at, List<MraGate> gates)
    {
        var obj = new GameObject("Pressure plate");
        obj.transform.SetParent(parent, false); obj.transform.position = new Vector3(at.x, at.y - .15f, 0f);
        var plate = obj.AddComponent<PressurePlate>();
        plate.up = Art("Props/Switch_Plate_Up"); plate.down = Art("Props/Switch_Plate_Down");
        plate.image = Image(obj.transform, "Art", plate.up, obj.transform.position, -14);
        var sensor = new GameObject("Sensor"); sensor.transform.SetParent(obj.transform, false);
        plate.sensor = Trigger(sensor, new Vector2(1.2f, .4f), new Vector2(0f, .3f));
        plate.targets.AddRange(gates);
    }

    // A crate (placeholder art: no accepted crate exists) with a dynamic body the plate can feel.
    static void Crate(Transform parent, Family f, Vector2 at)
    {
        var obj = new GameObject("Crate (placeholder art)") { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.SetParent(parent, false); obj.transform.position = at;
        var body = obj.AddComponent<Rigidbody2D>(); body.mass = 2f; body.freezeRotation = true; body.gravityScale = 3f;
        var box = obj.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.2f, 1.2f);
        var shape = obj.AddComponent<BodyShape>();
        shape.outline = new[] { new Vector2(-.6f, -.6f), new Vector2(.6f, -.6f), new Vector2(.6f, .6f), new Vector2(-.6f, .6f) };
        shape.rim = new[] { new Vector2(-.6f, .6f), new Vector2(.6f, .6f) };
        shape.top = new Color(.62f, .47f, .31f); shape.bottom = new Color(.45f, .33f, .22f); shape.rimColor = new Color(.72f, .57f, .4f);
        shape.sortingOrder = -10; shape.solid = false; shape.Rebuild();
    }

    static void Sequence(Transform parent, Vector2[] at, List<MraGate> gates)
    {
        var seq = new GameObject("Light sequence (1-3-2)").AddComponent<MraSequence>();
        seq.transform.SetParent(parent, false);
        seq.order = new[] { 0, 2, 1 };
        seq.targets.AddRange(gates);
        for (int i = 0; i < at.Length; i++)
        {
            var obj = new GameObject("Light " + (i + 1));
            obj.transform.SetParent(seq.transform, false); obj.transform.position = at[i] - new Vector2(0f, 1f);
            var light = obj.AddComponent<MraLever>();
            light.label = "Touch the light"; light.latch = false; light.sequence = seq; light.index = i;
            light.image = Image(obj.transform, "Art", Art("Hazards/GlowPod_Light_Off"), obj.transform.position, -12, .6f + .12f * i);   // size codes the order's place
            Trigger(obj, new Vector2(1.8f, 2.4f), new Vector2(0f, 1.2f));
            seq.lights.Add(light);
        }
    }

    static MraWater Water(Transform parent, Vector4 r)
    {
        var obj = new GameObject("Deep water");
        obj.transform.SetParent(parent, false); obj.transform.position = new Vector3(r.x + r.z * .5f, r.y + r.w * .5f, 0f);
        Trigger(obj, new Vector2(r.z, r.w), Vector2.zero);
        var water = obj.AddComponent<MraWater>();
        var art = Image(obj.transform, "Water", Art("Hazards/Water_Body"), obj.transform.position, 18);
        art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(r.z, r.w); art.color = new Color(1f, 1f, 1f, .8f);
        water.art = new[] { art };
        return water;
    }

    static void TimedThorns(Transform parent, Family f, Vector4 r)
    {
        var obj = new GameObject("Rising thorns (1.2 s safe window)");
        obj.transform.SetParent(parent, false); obj.transform.position = new Vector3(r.x + r.z * .5f, r.y + r.w * .5f, 0f);
        Trigger(obj, new Vector2(r.z, r.w), Vector2.zero);
        var hz = obj.AddComponent<MraTimedHazard>();
        var art = Image(obj.transform, "Thorns", Art(f.thorns), new Vector2(r.x + r.z * .5f, r.y + .4f), -9);
        art.drawMode = SpriteDrawMode.Tiled; art.size = new Vector2(r.z, art.sprite.bounds.size.y);
        hz.art = art.transform; hz.safeSeconds = 1.2f;
    }

    static void Pod(Transform parent, Vector2 at)
    {
        var obj = new GameObject("Bounce pod");
        obj.transform.SetParent(parent, false); obj.transform.position = at;
        obj.AddComponent<CircleCollider2D>().radius = .55f;
        var pod = obj.AddComponent<MraPod>();
        pod.art = Image(obj.transform, "Pod", Art("Enemies/Carrier_SeedPod_A2"), at, -11, .45f).transform;
    }

    // The chamber's reward, by kind; a rescue is a resident to talk to.
    static void Reward(Transform parent, Chamber c, Family f)
    {
        Vector2 at = c.rewardAt.Length > 0 ? c.rewardAt[0] : new Vector2(c.size.x - 3f, 2f);
        if (c.reward.EndsWith("rescue") || c.reward == "last-resident")
        {
            var (role, who, name) = Resident(c.reward);
            Vector2 feet = c.resident.Length > 0 ? c.resident[0] - new Vector2(0f, .8f) : at - new Vector2(0f, .8f);
            var obj = new GameObject("Resident " + name);
            obj.transform.SetParent(parent, false); obj.transform.position = feet;
            Trigger(obj, new Vector2(3f, 3f), new Vector2(0f, 1.5f));
            var res = obj.AddComponent<MraResident>();
            res.rewardId = c.rewardId; res.role = role; res.displayName = name;
            res.rescueLines = RescueLines(role);
            res.figure = Townsfolk(obj.transform, who, feet);
            return;
        }
        var reward = new GameObject("Reward " + c.reward);
        reward.transform.SetParent(parent, false); reward.transform.position = at;
        reward.AddComponent<CircleCollider2D>().radius = .7f;
        var rw = reward.AddComponent<MraReward>();
        rw.rewardId = c.rewardId; rw.title = c.name;
        (rw.kind, rw.image) = c.reward switch
        {
            "song-shell" => (MraReward.Kind.SongShell, Image(reward.transform, "Art", Art("Props/Pickup_SongShell"), at, -8)),
            "heart-seed" => (MraReward.Kind.HeartSeed, Image(reward.transform, "Art", Art("Props/Pickup_HeartSeed"), at, -8, 1.4f)),
            "amber" => (MraReward.Kind.Amber, Image(reward.transform, "Art", Art("Props/Pickup_Amber_Medium"), at, -8)),
            "lore-stone" => (MraReward.Kind.Lore, Image(reward.transform, "Art", Art("Props/Collectible_LoreStone"), at, -8, 1.4f)),
            "ending-reflection" => (MraReward.Kind.Reflection, Image(reward.transform, "Art", Art("Props/Collectible_LoreStone"), at, -8, 1.4f)),
            _ => (MraReward.Kind.Find, Image(reward.transform, "Art", Art("Props/Pickup_SapVial"), at, -8)),
        };
        rw.text = c.reward switch
        {
            "lore-stone" => "Old marks on a flat stone: a path climbing, and a warning to tread lightly.",
            "rare-find" => "Something old and well made, kept dry for a long time.",
            "ending-reflection" => "It's quiet here now. Everything is where it should be.",
            _ => "",
        };
    }

    static (string role, string who, string name) Resident(string reward) => reward switch
    {
        "smith-rescue" => ("smith", "Smith", "Brannick"),
        "farmer-rescue" => ("farmer", "Farmer", "Marrow"),
        "lantern-keeper-rescue" => ("keeper", "Keeper", "Wick"),
        "traveller-rescue" => ("traveller", "Musician", "A traveller"),
        _ => ("last", "Child", "The last climber"),
    };

    static string[] RescueLines(string role) => role switch
    {
        "smith" => new[] { "The roots shut behind me while I was looking at the old ironwork. Thank you, little one.", "I'll be at my forge in Qvale. Come and see me." },
        "farmer" => new[] { "The ground heaved and the door jammed. I thought I'd sleep here till the harvest.", "I'm going home to Qvale. Visit the family." },
        "keeper" => new[] { "My lantern went out and I lost the way. You found me in the dark!", "Qvale's lamps have been out without me. I'll light them again." },
        "traveller" => new[] { "Weather caught me on the heights. You've a good eye for a sheltered cave.", "I'll rest in Qvale a while." },
        _ => new[] { "I came up here to watch the lake, and stayed too long.", "I'll head down to Qvale. It's time." },
    };

    static void Note(Transform parent, string text, Vector2 at)
    {
        var obj = new GameObject("Note - objective");
        obj.transform.SetParent(parent, false); obj.transform.position = at;
        obj.AddComponent<BoxCollider2D>().size = new Vector2(6f, 4f);
        obj.AddComponent<PrototypeNote>().text = text;
    }
}

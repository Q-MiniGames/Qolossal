using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Qolossal.Atlas
{
    // What the save has found underground, shared by the minimap and the Chart so that the two always agree.
    // A room is found by entering it (RegionalUnderground saves the flag). Route E stays off every map until
    // the Summit reveal, the rule the Chart already keeps for the descent.
    public static class CaveDiscovery
    {
        public static string Flag(string roomId) => "regional:room:" + roomId;
        public static bool Hinted(AtlasNode room) => room.group != "E" || MraState.Revealed;
        public static bool Known(AtlasNode room) => Hinted(room) && GameSave.HasFlag(Flag(room.id));

        public static Vector2 World(AtlasMapping mapping, Vector2 map) => new Vector2((map.x - mapping.ox) / mapping.sx, (mapping.oy - map.y) / mapping.sy);
        public static Rect WorldRect(AtlasNode node) => new Rect(node.worldBounds[0], node.worldBounds[1], node.worldBounds[2], node.worldBounds[3]);

        public static float Distance(Rect rect, Vector2 p) =>
            new Vector2(Mathf.Max(rect.xMin - p.x, 0f, p.x - rect.xMax), Mathf.Max(rect.yMin - p.y, 0f, p.y - rect.yMax)).magnitude;

        // The room that owns each piece of each underground outline (null for the surface). The continuous walls
        // carry no room. A wall on a room's edge is that room's; a passage wall goes to the nearer of the two rooms
        // its line runs into, never to a room it merely passes close to, which drew stray walls floating beside it.
        public static string[][] ContourOwners(AtlasData data)
        {
            var mappings = new Dictionary<string, AtlasMapping>();
            foreach (var m in data.mappings) mappings[m.id] = m;
            var rooms = new List<(string id, Rect rect)>();
            foreach (var n in data.nodes) if (n.type == "underground") rooms.Add((n.id, WorldRect(n)));
            var owners = new string[data.contours.Length][];
            for (int c = 0; c < owners.Length; c++)
            {
                var contour = data.contours[c];
                if (!contour.underground || contour.path == null) continue;
                var owner = owners[c] = new string[contour.path.Length];
                if (!string.IsNullOrEmpty(contour.room)) { for (int i = 0; i < owner.Length; i++) owner[i] = contour.room; continue; }
                if (!mappings.TryGetValue(contour.mappingId ?? "", out var mapping)) continue;
                var points = new Vector2[owner.Length];
                var touching = new int[owner.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = World(mapping, contour.path[i]);
                    touching[i] = rooms.FindIndex(room => Distance(room.rect, points[i]) < OnEdge);
                }
                bool closed = points.Length > 2 && Vector2.Distance(points[0], points[points.Length - 1]) < .01f;
                int[] behind = Along(touching, closed, 1), ahead = Along(touching, closed, -1);
                for (int i = 1; i < owner.Length; i++)
                {
                    Vector2 mid = (points[i - 1] + points[i]) * .5f;
                    int best = Nearest(rooms, mid, -1, -1);
                    if (Distance(rooms[best].rect, mid) >= OnEdge && (behind[i - 1] >= 0 || ahead[i] >= 0)) best = Nearest(rooms, mid, behind[i - 1], ahead[i]);
                    owner[i] = rooms[best].id;
                }
            }
            return owners;
        }

        const float OnEdge = .05f;

        // For each point of a line, the last room touched walking along it (step 1: forwards; -1: backwards), or -1.
        static int[] Along(int[] touching, bool closed, int step)
        {
            var found = new int[touching.Length];
            int last = -1;
            for (int pass = closed ? 2 : 1; pass > 0; pass--)
                for (int n = 0, i = step > 0 ? 0 : touching.Length - 1; n < touching.Length; n++, i += step)
                {
                    if (touching[i] >= 0) last = touching[i];
                    found[i] = last;
                }
            return found;
        }

        // The nearest room to a point: of all of them with no choices given, otherwise of the one or two given.
        static int Nearest(List<(string id, Rect rect)> rooms, Vector2 p, int first, int second)
        {
            if (first < 0 && second < 0)
            {
                int best = 0; float distance = float.MaxValue;
                for (int r = 0; r < rooms.Count; r++) { float d = Distance(rooms[r].rect, p); if (d < distance) { distance = d; best = r; } }
                return best;
            }
            if (first < 0) return second;
            if (second < 0) return first;
            return Distance(rooms[first].rect, p) <= Distance(rooms[second].rect, p) ? first : second;
        }

        // Per contour, per segment: drawn? Null for a surface contour, which is always drawn.
        public static bool[][] ContourMask(string[][] owners, HashSet<string> known)
        {
            var mask = new bool[owners.Length][];
            for (int c = 0; c < owners.Length; c++)
            {
                if (owners[c] == null) continue;
                mask[c] = new bool[owners[c].Length];
                for (int i = 0; i < mask[c].Length; i++) mask[c][i] = owners[c][i] != null && known.Contains(owners[c][i]);
            }
            return mask;
        }
    }

    // The batched vector layer: everything but Qori's marker. CaveMinimap fills it only when something changes
    // and moves it as Qori moves, so following him never rebuilds the mesh.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CaveMinimapGraphic : MaskableGraphic
    {
        public CaveMinimap owner;
        protected override void OnPopulateMesh(VertexHelper v) { v.Clear(); if (owner != null) owner.Fill(v); }
    }

    // The cave map at the top right of the screen, in Qolossal_Underground only. World units are drawn straight,
    // so what's on it is the true shape of the caves; the Chart stays the place for the whole climb. Tab or the
    // right-stick click steps through three sizes and off, remembered on this machine. No names: rooms are never
    // labelled, like the Chart.
    [DisallowMultipleComponent]
    public sealed class CaveMinimap : MonoBehaviour
    {
        public enum Size { Small, Medium, Large, Off }
        public const string SizeKey = "Qolossal.CaveMinimap.Size";
        const float Scale = 3.2f, TopClear = 96f, SideClear = 40f, Border = 2f;   // reference px at 1920x1080; TopClear keeps the Amber count free
        const float StubLength = 8f, StubAcross = 1.5f, StubFloor = 3f, ExitStub = 6f;   // world units: how much of a passage every exit shows
        static readonly Vector2[] Panels = { new Vector2(300f, 190f), new Vector2(430f, 270f), new Vector2(640f, 400f) };
        static readonly Color Brass = new Color(.67f, .56f, .35f, .85f), Ground = new Color(.025f, .055f, .06f, .8f), Barred = new Color(.62f, .50f, .36f, 1f),
            Shut = new Color32(0xE2, 0x5B, 0x3C, 0xFF), Unexplored = new Color32(0xE9, 0xB9, 0x5A, 0xFF), Rest = new Color32(0x8F, 0xD8, 0xC2, 0xFF),
            Reward = new Color32(0xF2, 0xD4, 0x7A, 0xFF), Exit = new Color32(0xEF, 0xE0, 0xBE, 0xFF);

        // One way out of a room, with the first stretch of its passage, whatever else the room owns of it.
        sealed class Door
        {
            public AtlasDoor door; public Rect floor;
            public readonly List<(Vector2 a, Vector2 b)> stub = new List<(Vector2, Vector2)>(), extra = new List<(Vector2, Vector2)>();
        }

        sealed class Room
        {
            public AtlasNode node; public Rect rect, extent; public Color tint;
            public readonly List<(Vector2 a, Vector2 b)> walls = new List<(Vector2, Vector2)>(), pieces = new List<(Vector2, Vector2)>(), bars = new List<(Vector2, Vector2)>();
            public readonly List<Door> doors = new List<Door>();
            public readonly List<Rect> exitFloors = new List<Rect>();
            public Checkpoint rest; public readonly List<MraReward> rewards = new List<MraReward>();
            public readonly List<MraUgPortal> exits = new List<MraUgPortal>();
            public readonly List<(string flag, Vector2 at)> bosses = new List<(string, Vector2)>();
            public readonly List<(MraGate gate, UgArenaSeal seal, Rect rect)> gates = new List<(MraGate, UgArenaSeal, Rect)>();
        }

        RegionalUnderground host;
        readonly List<Room> rooms = new List<Room>();
        readonly Dictionary<string, Room> byId = new Dictionary<string, Room>();
        Canvas canvas; RectTransform frame, inset, mapLayer; CaveMinimapGraphic graphic;
        string scenePath, activeRest = "", shownRoom; Vector2 origin; int gateBits; float nextPoll; bool stale = true;
        Rect lastSafe; Size laidOut = (Size)(-1);
        Vector2 Span => Panels[(int)Mathf.Min((int)CurrentSize, 2)] / Scale;

        public static CaveMinimap Instance { get; private set; }
        public Size CurrentSize { get; private set; }
        public bool IsShown => canvas != null && canvas.enabled;
        public string CurrentRoomId => host != null ? host.CurrentRoom?.id : null;
        public int RoomCount => rooms.Count;
        // What the last rebuild drew, for the checks.
        public readonly List<string> DrawnRooms = new List<string>();
        public int ShutMarks { get; private set; }
        public int RewardMarks { get; private set; }
        public int BossMarks { get; private set; }
        public int DoorMarks { get; private set; }
        public int RestMarks { get; private set; }
        public int ExitMarks { get; private set; }
        public int VertexCount { get; private set; }
        // How well the exits read, for the checks: the shortest passage beginning drawn, in world units, and the cave exits opened.
        public float ShortestStub { get; private set; } = float.MaxValue;
        public int OpenedExits { get; private set; }
        public int KnownRoomCount { get { int n = 0; foreach (var r in rooms) if (CaveDiscovery.Known(r.node)) n++; return n; } }

        public static CaveMinimap Create(RegionalUnderground host)
        {
            var map = new GameObject("Cave Minimap").AddComponent<CaveMinimap>();
            map.host = host;
            return map;
        }

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }
        void OnEnable() { GameSave.FlagsChanged += MarkStale; GameSave.WorldChanged += MarkStale; }
        void OnDisable() { GameSave.FlagsChanged -= MarkStale; GameSave.WorldChanged -= MarkStale; }
        void MarkStale() => stale = true;

        void Start()
        {
            scenePath = SceneManager.GetActiveScene().path;
            CurrentSize = (Size)Mathf.Clamp(PlayerPrefs.GetInt(SizeKey, 0), 0, 3);
            var text = Resources.Load<TextAsset>("QolossalAtlas/AtlasData");
            if (text == null) { enabled = false; return; }
            ReadMap(JsonUtility.FromJson<AtlasData>(text.text));
            ReadScene();
            Build();
        }

        void ReadMap(AtlasData data)
        {
            var colors = new Dictionary<string, Color>();
            foreach (var r in data.regions) if (ColorUtility.TryParseHtmlString(r.color, out var c)) colors[r.id] = c;
            foreach (var n in data.nodes)
            {
                if (n.type != "underground") continue;
                var room = new Room { node = n, rect = CaveDiscovery.WorldRect(n), tint = colors.TryGetValue(n.region, out var c) ? c : new Color(.72f, .85f, .8f) };
                room.extent = room.rect;
                rooms.Add(room); byId[n.id] = room;
            }
            var mappings = new Dictionary<string, AtlasMapping>();
            foreach (var m in data.mappings) mappings[m.id] = m;
            var owners = CaveDiscovery.ContourOwners(data);
            var allWalls = new List<(Vector2 a, Vector2 b, string owner)>();
            for (int c = 0; c < data.contours.Length; c++)
            {
                var contour = data.contours[c];
                // A gate's outline is drawn live from the gate itself.
                if (owners[c] == null || contour.kind == "gate" || !mappings.TryGetValue(contour.mappingId ?? "", out var mapping)) continue;
                for (int i = 1; i < contour.path.Length; i++)
                {
                    if (owners[c][i] == null || !byId.TryGetValue(owners[c][i], out var room)) continue;
                    Vector2 a = CaveDiscovery.World(mapping, contour.path[i - 1]), b = CaveDiscovery.World(mapping, contour.path[i]);
                    (contour.kind == "grille" ? room.bars : string.IsNullOrEmpty(contour.kind) ? room.walls : room.pieces).Add((a, b));
                    if (string.IsNullOrEmpty(contour.kind)) allWalls.Add((a, b, owners[c][i]));
                    room.extent = Grow(Grow(room.extent, a), b);
                }
            }
            if (data.caveDoors != null) foreach (var d in data.caveDoors) if (byId.TryGetValue(d.room, out var room)) AddDoor(room, d, allWalls);
        }

        // A long straight passage wall belongs whole to one of its two rooms, which left the other with a bare gap.
        // So every door takes the real walls just outside it, up to where they reach any room.
        void AddDoor(Room room, AtlasDoor d, List<(Vector2 a, Vector2 b, string owner)> allWalls)
        {
            var door = new Door { door = d };
            Rect box;
            if (d.w == 0f)
            {
                bool left = Mathf.Abs(d.x - room.rect.xMin) < .01f;
                box = new Rect(left ? d.x - StubLength : d.x, d.y - StubAcross, StubLength, d.h + StubAcross * 2f);
                door.floor = new Rect(left ? d.x - StubFloor : d.x, d.y, StubFloor, d.h);
            }
            else
            {
                bool down = Mathf.Abs(d.y - room.rect.yMin) < .01f;
                box = new Rect(d.x - StubAcross, down ? d.y - StubLength : d.y, d.w + StubAcross * 2f, StubLength);
                door.floor = new Rect(d.x, down ? d.y - StubFloor : d.y, d.w, StubFloor);
            }
            float length = 0f;
            foreach (var wall in allWalls)
            {
                if (!Clip(wall.a, wall.b, box, out var a, out var b)) continue;
                Vector2 mid = (a + b) * .5f;
                if (rooms.Exists(r => CaveDiscovery.Distance(r.rect, mid) < .01f)) continue;
                door.stub.Add((a, b));
                if (wall.owner != room.node.id) door.extra.Add((a, b));
                length += Vector2.Distance(a, b);
                room.extent = Grow(Grow(room.extent, a), b);
            }
            ShortestStub = Mathf.Min(ShortestStub, length);
            room.doors.Add(door);
        }

        static bool Clip(Vector2 a, Vector2 b, Rect box, out Vector2 from, out Vector2 to)
        {
            float t0 = 0f, t1 = 1f; Vector2 d = b - a;
            bool Side(float p, float q)
            {
                if (p == 0f) return q >= 0f;
                float t = q / p;
                if (p < 0f) t0 = Mathf.Max(t0, t); else t1 = Mathf.Min(t1, t);
                return true;
            }
            bool inside = Side(-d.x, a.x - box.xMin) & Side(d.x, box.xMax - a.x) & Side(-d.y, a.y - box.yMin) & Side(d.y, box.yMax - a.y);
            from = a + d * t0; to = a + d * t1;
            return inside && t0 < t1;
        }

        // A way back to the surface ends in a shallow pocket with the portal inside. Open the pocket's far end and
        // run its floor and roof on, so it reads as the start of a passage like any other exit.
        void OpenExit(Room room, Vector2 at)
        {
            float side = at.x < room.rect.xMin ? -1f : at.x > room.rect.xMax ? 1f : 0f;
            if (side == 0f) return;
            float edge = side < 0f ? room.rect.xMin : room.rect.xMax, far = 0f, low = float.MaxValue, high = float.MinValue;
            foreach (var (a, b) in room.walls)
            {
                if ((a.x - edge) * side <= .02f || (b.x - edge) * side <= .02f || Vector2.Distance((a + b) * .5f, at) > 5f) continue;
                far = Mathf.Max(far, (a.x - edge) * side, (b.x - edge) * side);
                low = Mathf.Min(low, a.y, b.y); high = Mathf.Max(high, a.y, b.y);
            }
            if (far <= 0f || high <= low) return;
            room.walls.RemoveAll(w => (w.a.x - edge) * side > .02f && (w.b.x - edge) * side > .02f && Vector2.Distance((w.a + w.b) * .5f, at) <= 5f
                && (w.a.y + w.b.y) * .5f > low + .1f && (w.a.y + w.b.y) * .5f < high - .1f);
            float start = edge + side * far * .5f, end = edge + side * (far + ExitStub);
            room.walls.Add((new Vector2(start, low), new Vector2(end, low)));
            room.walls.Add((new Vector2(start, high), new Vector2(end, high)));
            room.exitFloors.Add(Rect.MinMaxRect(Mathf.Min(edge, end), low, Mathf.Max(edge, end), high));
            room.extent = Grow(Grow(room.extent, new Vector2(end, low)), new Vector2(end, high));
            OpenedExits++;
        }

        static Rect Grow(Rect r, Vector2 p) => Rect.MinMaxRect(Mathf.Min(r.xMin, p.x), Mathf.Min(r.yMin, p.y), Mathf.Max(r.xMax, p.x), Mathf.Max(r.yMax, p.y));

        Room RoomAt(Vector2 p)
        {
            Room best = null; float distance = float.MaxValue;
            foreach (var room in rooms) { float d = CaveDiscovery.Distance(room.rect, p); if (d < distance) { distance = d; best = room; } }
            return best;
        }

        static Rect BoxRect(Collider2D solid, Transform fallback)
        {
            if (solid is BoxCollider2D box)
            {
                Vector2 centre = box.transform.TransformPoint(box.offset), size = Vector2.Scale(box.size, box.transform.lossyScale);
                size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
                return new Rect(centre - size * .5f, size);
            }
            return new Rect((Vector2)fallback.position - Vector2.one * .5f, Vector2.one);
        }

        void ReadScene()
        {
            foreach (var rest in FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { var room = RoomAt(rest.transform.position); if (room != null) room.rest = rest; }
            foreach (var reward in FindObjectsByType<MraReward>(FindObjectsInactive.Include, FindObjectsSortMode.None)) RoomAt(reward.transform.position)?.rewards.Add(reward);
            foreach (var exit in FindObjectsByType<MraUgPortal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var room = RoomAt(exit.transform.position);
                if (room == null) continue;
                room.exits.Add(exit);
                OpenExit(room, exit.transform.position);
            }
            foreach (var keeper in FindObjectsByType<MraArenaGuardian>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (keeper.guardian != null) RoomAt(keeper.guardian.transform.position)?.bosses.Add((keeper.flag, keeper.guardian.transform.position));
            foreach (var gate in FindObjectsByType<MraGate>(FindObjectsInactive.Include, FindObjectsSortMode.None)) RoomAt(gate.transform.position)?.gates.Add((gate, null, BoxRect(gate.solid, gate.transform)));
            foreach (var seal in FindObjectsByType<UgArenaSeal>(FindObjectsInactive.Include, FindObjectsSortMode.None)) RoomAt(seal.transform.position)?.gates.Add((null, seal, BoxRect(seal.solid, seal.transform)));
        }

        void Build()
        {
            canvas = GameHud.CreateCanvas("Cave Minimap Canvas", 12);
            canvas.transform.SetParent(transform, false);
            var ground = GameHud.AddImage(canvas.transform, "Ground", null, Vector2.one, Vector2.zero, Vector2.one);
            ground.color = Ground; ground.preserveAspect = false;
            frame = ground.rectTransform; frame.pivot = Vector2.one;
            Edge(new Vector2(0f, 1f), Vector2.one, new Vector2(0f, Border)); Edge(Vector2.zero, Vector2.right, new Vector2(0f, Border));
            Edge(Vector2.zero, Vector2.up, new Vector2(Border, 0f)); Edge(Vector2.right, Vector2.one, new Vector2(Border, 0f));
            inset = new GameObject("View", typeof(RectTransform)).GetComponent<RectTransform>();
            inset.SetParent(frame, false);
            inset.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(6, 6);
            mapLayer = new GameObject("Map", typeof(RectTransform)).GetComponent<RectTransform>();
            mapLayer.SetParent(inset, false);
            graphic = mapLayer.gameObject.AddComponent<CaveMinimapGraphic>();
            graphic.owner = this; graphic.raycastTarget = false;
            // Qori: a dark-edged pale diamond, the Chart's own mark for him.
            var halo = GameHud.AddImage(inset, "Qori edge", null, new Vector2(.5f, .5f), Vector2.zero, Vector2.one * 15f);
            halo.color = new Color(.02f, .04f, .05f, 1f); halo.preserveAspect = false;
            halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var mark = GameHud.AddImage(halo.rectTransform, "Qori", null, new Vector2(.5f, .5f), Vector2.zero, Vector2.one * 10f);
            mark.color = Color.white; mark.preserveAspect = false;
        }

        // One brass side of the frame, along the ground's edge.
        void Edge(Vector2 from, Vector2 to, Vector2 thickness)
        {
            var side = GameHud.AddImage(frame, "Frame", null, from, Vector2.zero, thickness);
            side.color = Brass; side.preserveAspect = false;
            side.rectTransform.anchorMin = from; side.rectTransform.anchorMax = to;
        }

        void Layout()
        {
            laidOut = CurrentSize; lastSafe = Screen.safeArea;
            Vector2 panel = Panels[(int)Mathf.Min((int)CurrentSize, 2)];
            float factor = canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            float safeRight = (Screen.width - lastSafe.xMax) / factor, safeTop = (Screen.height - lastSafe.yMax) / factor;
            frame.anchoredPosition = new Vector2(-SideClear - safeRight, -TopClear - safeTop);
            frame.sizeDelta = panel + Vector2.one * Border * 2f;
            inset.sizeDelta = panel;
            stale = true;
        }

        // Stepping through the sizes works whenever Qori could act.
        void StepSize()
        {
            CurrentSize = (Size)(((int)CurrentSize + 1) % 4);
            PlayerPrefs.SetInt(SizeKey, (int)CurrentSize);
            PlayerPrefs.Save();
        }

        static bool Hidden => MraState.Busy || MraKnot.IsPlaying || MraReveal.IsPlaying || MraEnding.IsPlaying || StirSequence.IsPlaying;

        void Update()
        {
            if (canvas == null) return;
            if (!Hidden && !GamePauseMenu.BlocksGameplayInput)
            {
                var key = Keyboard.current; var pad = Gamepad.current;
                if (key != null && key.tabKey.wasPressedThisFrame || pad != null && pad.rightStickButton.wasPressedThisFrame) StepSize();
            }
            bool show = CurrentSize != Size.Off && !Hidden && host != null && host.player != null;
            if (canvas.enabled != show) canvas.enabled = show;
        }

        void LateUpdate()
        {
            if (canvas == null || !canvas.enabled) return;
            if (laidOut != CurrentSize || lastSafe != Screen.safeArea) Layout();
            Vector2 qori = host.player.transform.position;
            if (Time.unscaledTime >= nextPoll) { nextPoll = Time.unscaledTime + .2f; Poll(); }
            if (stale || Mathf.Abs(qori.x - origin.x) > Span.x * .75f || Mathf.Abs(qori.y - origin.y) > Span.y * .75f) Rebuild(qori);
            mapLayer.anchoredPosition = (origin - qori) * Scale;
        }

        // What can change on the map without a saved flag: the room, a gate on a switch, a boss door, the rest touched.
        void Poll()
        {
            int bits = 0, bit = 0;
            foreach (var room in rooms)
                foreach (var g in room.gates)
                {
                    if (GateShut(g)) bits ^= 1 << (bit % 31);
                    bit++;
                }
            string rest = GameSave.CheckpointIn(scenePath), here = CurrentRoomId;
            if (bits != gateBits || rest != activeRest || here != shownRoom) stale = true;
            gateBits = bits; activeRest = rest; shownRoom = here;
        }

        static bool GateShut((MraGate gate, UgArenaSeal seal, Rect rect) g) => g.gate != null ? !g.gate.IsOpen : g.seal != null && g.seal.IsShut;

        void Rebuild(Vector2 qori)
        {
            stale = false; origin = qori;
            DrawnRooms.Clear();
            Vector2 reach = Span * 1.5f;
            var near = new Rect(qori - reach, reach * 2f);
            foreach (var room in rooms) if (room.extent.Overlaps(near) && CaveDiscovery.Known(room.node)) DrawnRooms.Add(room.node.id);
            // The layer's rectangle has to hold its mesh, or the mask culls it whole.
            Vector2 half = reach;
            foreach (string id in DrawnRooms)
            {
                var e = byId[id].extent;
                half = Vector2.Max(half, new Vector2(Mathf.Max(Mathf.Abs(e.xMin - qori.x), Mathf.Abs(e.xMax - qori.x)), Mathf.Max(Mathf.Abs(e.yMin - qori.y), Mathf.Abs(e.yMax - qori.y))));
            }
            mapLayer.sizeDelta = half * Scale * 2f + Vector2.one * 40f;
            graphic.SetVerticesDirty();
        }

        Vector2 P(Vector2 world) => (world - origin) * Scale;

        public void Fill(VertexHelper v)
        {
            ShutMarks = RewardMarks = BossMarks = DoorMarks = RestMarks = ExitMarks = 0;
            string here = CurrentRoomId;
            foreach (string id in DrawnRooms) Floor(v, byId[id], id == here);
            foreach (string id in DrawnRooms)
            {
                var room = byId[id]; bool current = id == here;
                var piece = Color.Lerp(room.tint, Color.white, .15f); piece.a = current ? .6f : .4f;
                var wall = Color.Lerp(room.tint, Color.white, current ? .35f : 0f); wall.a = current ? 1f : .75f;
                Lines(v, room.pieces, 1.4f, piece);
                Lines(v, room.walls, 2f, wall);
                foreach (var door in room.doors) Lines(v, door.extra, 2f, wall);
                Lines(v, room.bars, 2f, Barred);
            }
            foreach (string id in DrawnRooms) Marks(v, byId[id]);
            VertexCount = v.currentVertCount;
        }

        void Floor(VertexHelper v, Room room, bool current)
        {
            Vector2 a = P(room.rect.min), z = P(room.rect.max);
            float cut = Mathf.Min(6f, (z.x - a.x) * .2f, (z.y - a.y) * .2f);
            var fill = Color.Lerp(room.tint, Color.black, .55f); fill.a = current ? .55f : .28f;
            int start = v.currentVertCount;
            foreach (var q in new[] { new Vector2(a.x + cut, a.y), new Vector2(z.x - cut, a.y), new Vector2(z.x, a.y + cut), new Vector2(z.x, z.y - cut), new Vector2(z.x - cut, z.y), new Vector2(a.x + cut, z.y), new Vector2(a.x, z.y - cut), new Vector2(a.x, a.y + cut) })
                v.AddVert(q, fill, Vector2.zero);
            for (int i = 1; i < 7; i++) v.AddTriangle(start, start + i, start + i + 1);
            foreach (var door in room.doors) FloorPatch(v, door.floor, fill);
            foreach (var exit in room.exitFloors) FloorPatch(v, exit, fill);
        }

        void FloorPatch(VertexHelper v, Rect rect, Color color)
        {
            Vector2 a = P(rect.min), z = P(rect.max);
            Quad(v, a, new Vector2(z.x, a.y), z, new Vector2(a.x, z.y), color);
        }

        void Marks(VertexHelper v, Room room)
        {
            foreach (var door in room.doors)
            {
                // Its passage's beginning turns bright while what's behind it is unexplored. Doors into Route E stay quiet until the reveal.
                if (!byId.TryGetValue(door.door.to, out var to) || CaveDiscovery.Known(to.node) || !CaveDiscovery.Hinted(to.node)) continue;
                Lines(v, door.stub, 2.4f, Unexplored);
                DoorMarks++;
            }
            foreach (var g in room.gates)
            {
                if (!GateShut(g)) continue;
                Bar(v, g.rect, Shut);
                ShutMarks++;
            }
            if (room.rest != null)
            {
                bool active = room.rest.CheckpointId == activeRest;
                Diamond(v, P(room.rest.transform.position), active ? 5f : 3f, active ? Rest : new Color(Rest.r, Rest.g, Rest.b, .55f));
                RestMarks++;
            }
            foreach (var exit in room.exits)
            {
                Arch(v, P(exit.transform.position), exit.Unlocked && exit.Open ? Exit : new Color(Exit.r, Exit.g, Exit.b, .45f));
                ExitMarks++;
            }
            foreach (var reward in room.rewards)
            {
                if (reward.Taken) continue;
                var gate = reward.GetComponent<MraUgRewardGate>();
                Ring(v, P(reward.transform.position), 3.2f, gate != null && !gate.Open ? new Color(Reward.r, Reward.g, Reward.b, .4f) : Reward);
                RewardMarks++;
            }
            foreach (var boss in room.bosses)
            {
                if (GameSave.HasFlag(boss.flag)) continue;
                Vector2 p = P(boss.at);
                Diamond(v, p, 8f, Ground);
                Diamond(v, p, 6f, Shut);
                BossMarks++;
            }
        }

        static void Quad(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int n = v.currentVertCount;
            v.AddVert(a, color, Vector2.zero); v.AddVert(b, color, Vector2.zero); v.AddVert(c, color, Vector2.zero); v.AddVert(d, color, Vector2.zero);
            v.AddTriangle(n, n + 1, n + 2); v.AddTriangle(n, n + 2, n + 3);
        }

        static void Stroke(VertexHelper v, Vector2 a, Vector2 b, float width, Color color)
        {
            var along = (b - a).normalized; var n = new Vector2(-along.y, along.x) * width * .5f;
            Quad(v, a + n, b + n, b - n, a - n, color);
        }

        void Lines(VertexHelper v, List<(Vector2 a, Vector2 b)> lines, float width, Color color)
        {
            foreach (var (a, b) in lines)
            {
                Vector2 pa = P(a), pb = P(b);
                if ((pb - pa).sqrMagnitude > .01f) Stroke(v, pa, pb, width, color);
            }
        }

        void Bar(VertexHelper v, Rect rect, Color color)
        {
            Vector2 a = P(rect.min), z = P(rect.max);
            Vector2 centre = (a + z) * .5f, half = Vector2.Max((z - a) * .5f, Vector2.one * 1.5f);
            Quad(v, centre - half, centre + new Vector2(half.x, -half.y), centre + half, centre + new Vector2(-half.x, half.y), color);
        }

        static void Diamond(VertexHelper v, Vector2 p, float r, Color color) =>
            Quad(v, p + Vector2.up * r, p + Vector2.right * r, p - Vector2.up * r, p - Vector2.right * r, color);

        static void Ring(VertexHelper v, Vector2 p, float r, Color color)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f, b = (i + 1) * Mathf.PI / 4f;
                Stroke(v, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, p + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * r, 1.6f, color);
            }
        }

        static void Arch(VertexHelper v, Vector2 p, Color color)
        {
            Stroke(v, p + new Vector2(-4f, -4f), p + new Vector2(-4f, 2f), 1.6f, color);
            Stroke(v, p + new Vector2(4f, -4f), p + new Vector2(4f, 2f), 1.6f, color);
            Stroke(v, p + new Vector2(-4f, 2f), p + new Vector2(0f, 6f), 1.6f, color);
            Stroke(v, p + new Vector2(0f, 6f), p + new Vector2(4f, 2f), 1.6f, color);
        }
    }
}

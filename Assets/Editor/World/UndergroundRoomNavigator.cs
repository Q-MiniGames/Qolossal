using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class UndergroundRoomNavigator : EditorWindow
{
    const string LabelsKey = "Qolossal.UndergroundRoomLabels";
    const string ScenePath = RegionalSceneCatalog.Folder + RegionalSceneCatalog.Underground + ".unity";
    ToolbarSearchField search;
    ScrollView list;
    Label count;
    UndergroundRoom[] rooms = Array.Empty<UndergroundRoom>();

    public static bool ShowLabels
    {
        get => EditorPrefs.GetBool(LabelsKey, true);
        set { EditorPrefs.SetBool(LabelsKey, value); SceneView.RepaintAll(); }
    }

    [MenuItem("Qolossal/Underground/Room Navigator")]
    public static void Open() => GetWindow<UndergroundRoomNavigator>("Underground Rooms");

    void OnEnable() => EditorApplication.hierarchyChanged += RefreshRooms;
    void OnDisable() => EditorApplication.hierarchyChanged -= RefreshRooms;

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new HelpBox("Find a gallery ID or room name. Click a room to select it and frame it in the Scene view.", HelpBoxMessageType.Info));
        var toolbar = new Toolbar();
        search = new ToolbarSearchField { name = "room-search", tooltip = "Examples: A01, MR01-C01, PC01, Warm Crack" };
        search.style.flexGrow = 1;
        search.RegisterValueChangedCallback(_ => RefreshList());
        toolbar.Add(search);
        toolbar.Add(new ToolbarButton(RefreshRooms) { text = "Refresh" });
        rootVisualElement.Add(toolbar);
        var labels = new Toggle("Room IDs in Scene view (Gizmos)") { value = ShowLabels };
        labels.RegisterValueChangedCallback(e => ShowLabels = e.newValue);
        rootVisualElement.Add(labels);
        rootVisualElement.Add(new Button(OpenScene) { text = "Open Underground Scene" });
        rootVisualElement.Add(new Button(WorldAreaNavigator.Open) { text = "Upper Areas and Cave Connections" });
        count = new Label { name = "room-count" };
        rootVisualElement.Add(count);
        list = new ScrollView { name = "room-list" };
        list.style.flexGrow = 1;
        rootVisualElement.Add(list);
        rootVisualElement.Add(new HelpBox("Room children contain platforms and gameplay objects. Continuous walls, backing and passages remain under Shared Terrain. Room entries are editing groups; moving a group does not rebuild the shared terrain or source layout.", HelpBoxMessageType.Info));
        minSize = new Vector2(330, 300);
        RefreshRooms();
    }

    void OpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var existing = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
        if (!existing.IsValid() || !existing.isLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        RefreshRooms();
    }

    void RefreshRooms()
    {
        rooms = FindObjectsByType<UndergroundRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.gameObject.scene.path == ScenePath)
            .OrderBy(r => r.roomId, StringComparer.Ordinal).ToArray();
        RefreshList();
    }

    public static bool Matches(UndergroundRoom room, string query)
    {
        string Normalize(string value) => (value ?? "").Replace("-", "").Replace("_", "").Replace(" ", "").ToUpperInvariant();
        return Normalize(room.roomId + " " + room.roomName + " " + room.region).Contains(Normalize(query));
    }

    void RefreshList()
    {
        if (list == null) return;
        list.Clear();
        var filtered = rooms.Where(r => r != null && Matches(r, search.value)).ToArray();
        count.text = $"{filtered.Length} / {rooms.Length} rooms";
        foreach (var room in filtered)
        {
            var button = new Button(() => FocusRoom(room)) { text = $"[{room.roomId}] {room.roomName}", name = "room-" + room.roomId };
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.tooltip = room.region + " | Select and frame";
            list.Add(button);
        }
    }

    public static void FocusRoom(UndergroundRoom room)
    {
        if (room == null) return;
        Selection.activeGameObject = room.gameObject;
        EditorGUIUtility.PingObject(room.gameObject);
        var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
        view.in2DMode = true;
        view.drawGizmos = true;
        view.Frame(room.WorldBounds, true);
        view.Focus();   // bring the Scene tab forward, or nothing seems to happen while the Game tab is showing
        view.Repaint();
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawRoom(UndergroundRoom room, GizmoType flags)
    {
        bool selected = Selection.activeTransform != null &&
            (Selection.activeTransform == room.transform || Selection.activeTransform.IsChildOf(room.transform));
        if (!ShowLabels && !selected) return;
        var bounds = room.WorldBounds;
        if (selected)
        {
            Gizmos.color = new Color(.35f, .95f, 1f, .8f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        var view = SceneView.currentDrawingSceneView;
        if (view == null || view.camera == null) return;
        var labelPoint = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
        var viewport = view.camera.WorldToViewportPoint(labelPoint);
        if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return;
        // At map scale show compact IDs; at room scale include the gallery name.
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, fontStyle = FontStyle.Bold };
        style.normal.textColor = selected ? Color.cyan : Color.white;
        Handles.Label(labelPoint, view.size < 100 ? $"[{room.roomId}] {room.roomName}" : room.roomId, style);
    }
}

[CustomEditor(typeof(UndergroundRoom))]
public sealed class UndergroundRoomInspector : Editor
{
    // Unity's frame-selection message hooks include the room's full authored bounds.
    bool HasFrameBounds() => true;
    Bounds OnGetFrameBounds() => ((UndergroundRoom)target).WorldBounds;

    public override VisualElement CreateInspectorGUI()
    {
        var room = (UndergroundRoom)target;
        var root = new VisualElement();
        root.Add(new Label($"[{room.roomId}] {room.roomName}"));
        root.Add(new Label("Region: " + room.region));
        root.Add(new Label("Authored room size: " + room.localBounds.size));
        root.Add(new Button(() => UndergroundRoomNavigator.FocusRoom(room)) { text = "Frame Room" });
        root.Add(new Button(UndergroundRoomNavigator.Open) { text = "Open Room Navigator" });
        root.Add(new Button(() => WorldAreaNavigator.ShowConnections(room.roomId)) { text = "Show Surface Access for This Room" });
        root.Add(new HelpBox("Expand this room in the Hierarchy to edit its platforms and gameplay objects. Shared terrain spans rooms; select it below to edit continuous walls and backing. Changing group transforms does not update the source layout.", HelpBoxMessageType.Info));
        var shared = new ObjectField("Shared Terrain") { objectType = typeof(Transform), value = room.sharedTerrain };
        shared.SetEnabled(false);
        root.Add(shared);
        root.Add(new Button(() => Selection.activeGameObject = room.sharedTerrain != null ? room.sharedTerrain.gameObject : room.gameObject) { text = "Select Shared Terrain" });
        return root;
    }
}


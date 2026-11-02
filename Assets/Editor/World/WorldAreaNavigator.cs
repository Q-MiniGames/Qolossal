using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class WorldAreaNavigator : EditorWindow
{
    public const string IndexPath = "Assets/Editor/World/WorldNavigationIndex.json";
    const string LabelsKey = "Qolossal.SurfaceAreaLabels";
    [Serializable] public sealed class AreaRecord { public string id, name, region, scene; }
    [Serializable] public sealed class LinkRecord { public string id, upperScene, upperRegion, upperAreaId, upperAreaName, lowerRoomId, lowerRoomName, requirements, activation; }
    [Serializable] public sealed class Index { public AreaRecord[] areas; public LinkRecord[] connections; }
    Index index;
    ToolbarSearchField search;
    ScrollView list;
    Label count;
    bool connections;
    public static bool ShowLabels { get => EditorPrefs.GetBool(LabelsKey, true); set { EditorPrefs.SetBool(LabelsKey, value); SceneView.RepaintAll(); } }
    public static Index ReadIndex() => JsonUtility.FromJson<Index>(AssetDatabase.LoadAssetAtPath<TextAsset>(IndexPath).text);

    [MenuItem("Qolossal/World/Areas and Cave Connections")]
    public static void Open() => GetWindow<WorldAreaNavigator>("World Areas & Caves");

    public static void ShowConnections(string query)
    {
        var window = GetWindow<WorldAreaNavigator>("World Areas & Caves");
        if (window.search == null) window.CreateGUI();
        window.connections = true;
        window.search.value = query;
        window.RefreshList();
        window.Show();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        minSize = new Vector2(440, 380);
        rootVisualElement.Add(new HelpBox("Upper landmarks and their paired underground access. Open either end to select and frame its actual scene object.", HelpBoxMessageType.Info));
        var toolbar = new Toolbar();
        toolbar.Add(new ToolbarButton(() => { connections = false; RefreshList(); }) { text = "Upper areas" });
        toolbar.Add(new ToolbarButton(() => { connections = true; RefreshList(); }) { text = "Cave connections" });
        toolbar.Add(new ToolbarButton(() => { index = ReadIndex(); RefreshList(); }) { text = "Refresh" });
        rootVisualElement.Add(toolbar);
        search = new ToolbarSearchField { name = "world-search", tooltip = "Region, landmark, room ID, room name, or unlock requirement" };
        search.RegisterValueChangedCallback(_ => RefreshList());
        rootVisualElement.Add(search);
        var labels = new Toggle("Area and cave labels in Scene view (Gizmos)") { value = ShowLabels };
        labels.RegisterValueChangedCallback(e => ShowLabels = e.newValue);
        rootVisualElement.Add(labels);
        count = new Label { name = "world-count" };
        rootVisualElement.Add(count);
        list = new ScrollView { name = "world-list" };
        list.style.flexGrow = 1;
        rootVisualElement.Add(list);
        rootVisualElement.Add(new HelpBox("52 paired surface accesses lead directly to 52 underground rooms. The other 37 rooms are reached through underground passages. Unlock requirements are preserved. Shared terrain and encounters retain their existing groups.", HelpBoxMessageType.Info));
        foreach (var child in rootVisualElement.Children()) child.style.flexShrink = 0;
        list.style.flexShrink = 1;
        list.style.minHeight = 80;
        index = ReadIndex();
        RefreshList();
    }

    public static bool Matches(string value, string query)
    {
        string Normalize(string s) => (s ?? "").Replace("-", "").Replace("_", "").Replace(" ", "").ToUpperInvariant();
        return Normalize(value).Contains(Normalize(query));
    }

    void RefreshList()
    {
        if (list == null || index == null) return;
        list.Clear();
        int shown = 0;
        if (connections)
        {
            foreach (var link in index.connections)
            {
                string title = $"{link.upperRegion} | [{link.upperAreaId}] {link.upperAreaName}\n↔ [{link.lowerRoomId}] {link.lowerRoomName}";
                if (!Matches(title + " " + link.requirements, search.value)) continue;
                var label = new Label(title); label.style.whiteSpace = WhiteSpace.Normal;
                list.Add(label);
                var actions = new VisualElement(); actions.style.flexDirection = FlexDirection.Row;
                actions.Add(new Button(() => OpenConnection(link.id, false)) { text = "Upper entrance" });
                actions.Add(new Button(() => OpenConnection(link.id, true)) { text = "Lower return" });
                actions.Add(new Button(() => OpenRoom(link.lowerRoomId)) { text = "Whole room" });
                list.Add(actions);
                var condition = new Label("Upper access: " + link.activation + " | " + link.requirements);
                condition.style.whiteSpace = WhiteSpace.Normal;
                condition.style.marginBottom = 12;
                list.Add(condition);
                shown++;
            }
        }
        else
        {
            foreach (var area in index.areas)
            {
                string title = $"{area.region} | [{area.id}] {area.name}";
                if (!Matches(title, search.value)) continue;
                var button = new Button(() => OpenArea(area.id)) { text = title };
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.whiteSpace = WhiteSpace.Normal;
                list.Add(button);
                var links = index.connections.Where(c => c.upperAreaId == area.id).ToArray();
                if (links.Length > 0)
                    list.Add(new Button(() => ShowConnections(area.id)) { text = "Cave access ↔ " + string.Join(", ", links.Select(c => c.lowerRoomId)) });
                shown++;
            }
        }
        count.text = connections ? $"Cave connections: {shown} / {index.connections.Length}" : $"Upper areas: {shown} / {index.areas.Length}";
        if (shown == 0) list.Add(new Label(connections ? "No direct surface access matches. Interior rooms are reached through underground passages." : "No matching upper area."));
    }

    static Scene OpenScene(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return default;
        string path = RegionalSceneCatalog.Folder + name + ".unity";
        var scene = SceneManager.GetSceneByPath(path);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return default;
            scene = EditorSceneManager.OpenScene(path);
        }
        SceneManager.SetActiveScene(scene);
        return scene;
    }

    static T[] InScene<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
    public static SurfaceArea OpenArea(string id)
    {
        var record = ReadIndex().areas.Single(a => a.id == id);
        var scene = OpenScene(record.scene); if (!scene.IsValid()) return null;
        var area = InScene<SurfaceArea>(scene).Single(a => a.areaId == id);
        Focus(area.gameObject, area.WorldBounds);
        return area;
    }
    public static CaveConnectionPoint OpenConnection(string id, bool lower)
    {
        var record = ReadIndex().connections.Single(c => c.id == id);
        var scene = OpenScene(lower ? RegionalSceneCatalog.Underground : record.upperScene); if (!scene.IsValid()) return null;
        var point = InScene<CaveConnectionPoint>(scene).Single(c => c.connectionId == id && c.undergroundSide == lower);
        Focus(point.gameObject, point.FocusBounds);
        return point;
    }
    public static UndergroundRoom OpenRoom(string id)
    {
        var scene = OpenScene(RegionalSceneCatalog.Underground); if (!scene.IsValid()) return null;
        var room = InScene<UndergroundRoom>(scene).Single(r => r.roomId == id);
        UndergroundRoomNavigator.FocusRoom(room);
        return room;
    }
    public static void Focus(GameObject target, Bounds bounds)
    {
        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        var view = SceneView.lastActiveSceneView ?? GetWindow<SceneView>();
        view.in2DMode = true; view.drawGizmos = true; view.Frame(bounds, true); view.Focus(); view.Repaint();
    }

    static void LabelAt(Vector3 position, string text, bool selected)
    {
        var view = SceneView.currentDrawingSceneView;
        if (view == null || view.camera == null) return;
        var p = view.camera.WorldToViewportPoint(position);
        if (p.z <= 0 || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) return;
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, fontStyle = FontStyle.Bold };
        style.normal.textColor = selected ? Color.cyan : Color.white;
        Handles.Label(position, text, style);
    }
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawArea(SurfaceArea area, GizmoType flags)
    {
        bool selected = Selection.activeTransform != null && Selection.activeTransform.IsChildOf(area.transform);
        if (!ShowLabels && !selected) return;
        var b = area.WorldBounds;
        if (selected) { Gizmos.color = Color.cyan; Gizmos.DrawWireCube(b.center, b.size); }
        LabelAt(new Vector3(b.center.x, b.max.y, 0), $"[{area.areaId}] {area.areaName}", selected);
    }
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawConnection(CaveConnectionPoint point, GizmoType flags)
    {
        bool selected = Selection.activeTransform != null && Selection.activeTransform.IsChildOf(point.transform);
        if (!ShowLabels && !selected) return;
        var p = point.EntrancePosition;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p + Vector3.up, .6f);
        string title = point.undergroundSide ? $"[{point.lowerRoomId}] ↔ {point.upperRegion} [{point.upperAreaId}]" : $"[{point.upperAreaId}] ↔ UG [{point.lowerRoomId}] {point.lowerRoomName}";
        LabelAt(p + Vector3.up * 5, title, selected);
        if (selected && point.arrival != null) Handles.DrawDottedLine(p, point.ArrivalPosition, 5);
    }
}

[CustomEditor(typeof(SurfaceArea))]
public sealed class SurfaceAreaInspector : Editor
{
    bool HasFrameBounds() => true;
    Bounds OnGetFrameBounds() => ((SurfaceArea)target).WorldBounds;
    public override VisualElement CreateInspectorGUI()
    {
        var area = (SurfaceArea)target;
        var root = new VisualElement();
        root.Add(new Label($"{area.regionName} | [{area.areaId}] {area.areaName}"));
        root.Add(new Button(() => WorldAreaNavigator.Focus(area.gameObject, area.WorldBounds)) { text = "Frame Area" });
        root.Add(new Button(() => WorldAreaNavigator.ShowConnections(area.areaId)) { text = "Show Cave Connections" });
        root.Add(new HelpBox("Expand the area for landmarks, cave entrances, decor and services. Terrain spans several areas and remains shared. Encounters retain their original group and are linked below.", HelpBoxMessageType.Info));
        root.Add(new Button(() => Selection.activeGameObject = area.sharedTerrain.gameObject) { text = "Select Shared Terrain" });
        foreach (var encounter in area.relatedEncounters ?? Array.Empty<GameObject>())
        {
            var item = encounter;
            if (item != null) root.Add(new Button(() => { Selection.activeGameObject = item; SceneView.lastActiveSceneView.FrameSelected(); }) { text = "Encounter: " + item.name });
        }
        return root;
    }
}

[CustomEditor(typeof(CaveConnectionPoint))]
public sealed class CaveConnectionPointInspector : Editor
{
    bool HasFrameBounds() => true;
    Bounds OnGetFrameBounds() => ((CaveConnectionPoint)target).FocusBounds;
    public override VisualElement CreateInspectorGUI()
    {
        var p = (CaveConnectionPoint)target;
        var root = new VisualElement();
        var title = new Label($"{p.upperRegion} [{p.upperAreaId}] {p.upperAreaName}\n↔ Underground [{p.lowerRoomId}] {p.lowerRoomName}");
        title.style.whiteSpace = WhiteSpace.Normal; root.Add(title);
        root.Add(new HelpBox("Upper access: " + p.activation + " | " + p.requirements, HelpBoxMessageType.Info));
        root.Add(new Button(() => WorldAreaNavigator.OpenConnection(p.connectionId, false)) { text = "Open Upper Entrance" });
        root.Add(new Button(() => WorldAreaNavigator.OpenConnection(p.connectionId, true)) { text = "Open Underground Return" });
        root.Add(new Button(() => WorldAreaNavigator.OpenRoom(p.lowerRoomId)) { text = "Frame Whole Underground Room" });
        root.Add(new Button(() => Selection.activeObject = p.entrance) { text = "Edit Entrance Trigger" });
        root.Add(new Button(() => Selection.activeObject = p.arrival) { text = "Edit Arrival / Spawn" });
        return root;
    }
}

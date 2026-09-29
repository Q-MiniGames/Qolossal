using UnityEditor;
using UnityEngine;

// Qolossal > World > World State: the saved game's world state, for play-testing. Wake or put
// back to sleep any knot (in Play mode the open level's stir variants change at once), give or
// take a relic, and see what has been charted, travelled and found.
public sealed class WorldStateWindow : EditorWindow
{
    Vector2 scroll;

    [MenuItem("Qolossal/World/World State")]
    static void Open() => GetWindow<WorldStateWindow>("World State");

    void OnFocus() { if (!Application.isPlaying) GameSave.Reload(); }
    void OnInspectorUpdate() => Repaint();

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Knots", EditorStyles.boldLabel);
        foreach (string knot in Knots.All)
        {
            bool awake = GameSave.IsKnotAwake(knot);
            bool set = EditorGUILayout.ToggleLeft(Knots.DisplayName(knot) + (awake ? "  (awake)" : ""), awake);
            if (set != awake) GameSave.SetKnotAwake(knot, set);
        }
        List("Levels charted", GameSave.Charted);
        List("Veins travelled", GameSave.Veins);
        List("Lore Stones", GameSave.LoreStones);
        EditorGUILayout.LabelField("Sproutlings", GameSave.Sproutlings.ToString());
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Relics", EditorStyles.boldLabel);
        var abilities = Application.isPlaying ? Object.FindFirstObjectByType<PlayerAbilityController>() : null;
        foreach (AbilityDefinition relic in Relics.All)
        {
            bool has = GameSave.HasRelic(relic.abilityId);
            bool set = EditorGUILayout.ToggleLeft(relic.displayName, has);
            if (set == has) continue;
            // In Play mode Qori gets (or loses) it at once; a scene outside the game areas gives him every relic anyway.
            if (set) { GameSave.AddRelic(relic.abilityId); if (abilities != null) abilities.Unlock(relic); }
            else { GameSave.RemoveRelic(relic.abilityId); if (abilities != null) abilities.Lock(relic.abilityId); }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(GameSave.FilePath, EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reload")) GameSave.Reload();
            if (GUILayout.Button("Show file")) EditorUtility.RevealInFinder(GameSave.FilePath);
            if (GUILayout.Button("New game (clear save)") && EditorUtility.DisplayDialog("Clear the save?", "Forget every relic, checkpoint, knot, vein and collectible?", "Clear", "Cancel"))
                GameSave.Clear();
        }
        EditorGUILayout.EndScrollView();
    }

    static void List(string title, System.Collections.Generic.IReadOnlyList<string> items)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"{title} ({items.Count})", EditorStyles.boldLabel);
        foreach (string item in items) EditorGUILayout.LabelField("   " + item);
    }
}

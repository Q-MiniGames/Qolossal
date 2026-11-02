using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MraSummitEntrance
{
    const string Root = "Assets/Art/Codex/Entrances/Summit4K/";
    public static void Apply(Scene scene, bool newSummitScene = false)
    {
        if (!newSummitScene && !scene.name.Equals("MRAtlas_MR07_Summit")) throw new InvalidOperationException("Summit entrance only");
        var objects = scene.GetRootGameObjects();
        var arch = objects.SelectMany(o => o.GetComponentsInChildren<SpriteRenderer>(true)).Single(r => r.name == "Threshold arch MR07_MR08");
        var exit = objects.SelectMany(o => o.GetComponentsInChildren<MraExit>(true)).Single(e => e.targetScene == "MRAtlas_MR08_Descent");
        var open = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "ENT_E_Open_Production_R2.png");
        var closed = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "ENT_E_Sealed_Production_R2.png");
        if (open == null || closed == null) throw new InvalidOperationException("Accepted Summit production art must be imported first");
        Undo.RecordObjects(new UnityEngine.Object[] { arch, arch.transform, exit, exit.transform, exit.GetComponent<BoxCollider2D>() }, "Integrate reviewed Summit entrance");
        arch.transform.localScale = Vector3.one;
        arch.transform.position = new Vector3(362.4f, 147.48712f, arch.transform.position.z);
        arch.sprite = closed;
        var state = arch.GetComponent<MraEntranceState>();
        if (state == null) state = Undo.AddComponent<MraEntranceState>(arch.gameObject);
        Undo.RecordObject(state, "Set entrance states");
        state.exit = exit; state.artwork = arch; state.open = open; state.sealedState = closed;
        exit.transform.position = new Vector3(362.4f, 148.28712f, exit.transform.position.z);
        var box = exit.GetComponent<BoxCollider2D>(); box.size = new Vector2(1.2f, 1.6f); box.offset = Vector2.zero; box.isTrigger = true;
        exit.groundOffset = .8f; exit.retryWhileInside = true;
        // Destination, arrival, reveal predicate and all terrain stay as authored.
        foreach (var o in new UnityEngine.Object[] { arch, arch.transform, state, exit, exit.transform, box }) EditorUtility.SetDirty(o);
        EditorSceneManager.MarkSceneDirty(scene);
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Starts the Mountain Relief Atlas play test (MraTestDriver) in Play mode. Run it in batch mode with
// its own save, so the player's save is never touched:
//   Unity -batchmode -projectPath <copy> -executeMethod MraPlayTest.Run -saveFile mra_playtest.json -captureDir <folder>
// (no -quit: the driver exits with 0 when everything passed, 1 otherwise).
[InitializeOnLoad]
public static class MraPlayTest
{
    const string Key = "Qolossal.MraPlayTest";

    static MraPlayTest()
    {
        if (!SessionState.GetBool(Key, false)) return;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            string[] args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-captureDir");
            var driver = new GameObject("MRA Play Test").AddComponent<MraTestDriver>();
            driver.captureDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/Captures";
        };
    }

    public static void Run()
    {
        if (Path.GetFileName(GameSave.FilePath) == "qolossal_save.json")
        {
            Debug.LogError("[MraPlayTest] refusing to run on the player's own save: pass -saveFile mra_playtest.json");
            EditorApplication.Exit(2);
            return;
        }
        EditorSceneManager.OpenScene(MraWorldBuilder.ScenePath("MRAtlas_MR01_Cradle"));
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
}

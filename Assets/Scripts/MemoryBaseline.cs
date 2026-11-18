#if DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Development builds, with "-memoryBaseline <file>": visits every scene in the build and writes what each one holds in
// memory, then quits (polish WS8, 3 Nov). Windows numbers only: says nothing about any console.
public sealed class MemoryBaseline : MonoBehaviour
{
    string file;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-memoryBaseline");
        if (at < 0 || at + 1 >= args.Length) return;
        var go = new GameObject("Memory baseline"); DontDestroyOnLoad(go);
        go.AddComponent<MemoryBaseline>().file = args[at + 1];
    }

    IEnumerator Start()
    {
        var lines = new List<string> { $"{Screen.width}x{Screen.height}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType}" };
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            yield return SceneManager.LoadSceneAsync(i);
            float settle = Time.realtimeSinceStartup + 6f;
            int frames = 0; float worst = 0f;
            while (Time.realtimeSinceStartup < settle) { frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime); yield return null; }
            yield return Resources.UnloadUnusedAssets();
            long textures = 0; int count = 0;
            var formats = new Dictionary<TextureFormat, long>();
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                long b = Profiler.GetRuntimeMemorySizeLong(t); textures += b; count++;
                formats.TryGetValue(t.format, out long had); formats[t.format] = had + b;
            }
            var by = new List<string>();
            foreach (var kv in formats) if (kv.Value > 16 * 1048576) by.Add($"{kv.Key} {kv.Value / 1048576}");
            lines.Add($"{Path.GetFileNameWithoutExtension(path)}: textures {textures / 1048576} MiB ({count}) [{string.Join(", ", by)}], " +
                      $"unity total {Profiler.GetTotalAllocatedMemoryLong() / 1048576} MiB, graphics {Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576} MiB, " +
                      $"{frames / 6f:0} fps idle, worst frame {worst * 1000f:0} ms");
            File.WriteAllLines(file, lines);
        }
        Application.Quit();
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mra;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Windows player measurement (only with "-mraProfile <report.json>" on the command line): loads each
// region in turn, puts Qori at every beat for 3 s, times each frame (unscaled) and samples memory,
// times every scene transition, opens the Chart for 3 s in Qvale, then writes the report and quits.
// It drives no input; it measures rendering, terrain, transparency, the Chart and transitions.
public sealed class MraFrameProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Application.isEditor || ReportPath == null || FindAnyObjectByType<MraFrameProbe>() != null) return;
        var probe = new GameObject("MRA Frame Probe").AddComponent<MraFrameProbe>();
        DontDestroyOnLoad(probe.gameObject);
    }

    static string ReportPath
    {
        get
        {
            var args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-mraProfile");
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }

    readonly StringBuilder rows = new StringBuilder();

    IEnumerator Start()
    {
        Application.targetFrameRate = -1; QualitySettings.vSyncCount = 0;   // measure the cost, not the cap
        GameSave.SetFlag(MraState.RevealFlag);   // the profile save may load the descent
        var world = World.Load();
        rows.Append("{\n \"device\": \"" + SystemInfo.deviceModel + " / " + SystemInfo.processorType + " / " + SystemInfo.graphicsDeviceName + "\",\n");
        rows.Append(" \"resolution\": \"" + Screen.width + "x" + Screen.height + "\", \"unity\": \"" + Application.unityVersion + "\",\n \"regions\": [\n");
        var regionRows = new List<string>();
        foreach (var r in world.regions)
        {
            Debug.Log("[MraFrameProbe] region " + r.id);
            float t0 = Time.realtimeSinceStartup;
            var load = SceneManager.LoadSceneAsync(r.scene);
            while (!load.isDone) yield return null;
            yield return null;
            float loadMs = (Time.realtimeSinceStartup - t0) * 1000f;
            var times = new List<float>();
            var qori = FindAnyObjectByType<PlayerMovement>();
            foreach (var n in r.nodes)
            {
                if (qori != null) qori.PlaceAt(n.spawn, 1f, false);
                Debug.Log($"[MraFrameProbe] {n.id} frame {Time.frameCount} t {Time.realtimeSinceStartup:F1}");
                float until = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < until) { times.Add(Time.unscaledDeltaTime); yield return null; }
            }
            if (r.id == "MR04")
            {
                MraChart.Open();
                var chartTimes = new List<float>();
                float chartUntil = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < chartUntil) { chartTimes.Add(Time.unscaledDeltaTime); yield return null; }
                if (MraChart.Instance != null) MraChart.Instance.Hide();
                regionRows.Add(Row("MR04_Chart", chartTimes, 0f));
            }
            regionRows.Add(Row(r.id, times, loadMs));
        }
        // The baseline: the existing A0 test room, Qori standing at its start, same duration.
        // Written before the baseline too, so a problem there can't lose the region results.
        void Write() => File.WriteAllText(ReportPath, rows.ToString() + string.Join(",\n", regionRows) + "\n ]\n}\n");
        Write();
        Debug.Log("[MraFrameProbe] baseline");
        var baseline = SceneManager.LoadSceneAsync("A0_TestRoom");
        if (baseline != null)
        {
            float limit = Time.realtimeSinceStartup + 30f;
            while (!baseline.isDone && Time.realtimeSinceStartup < limit) yield return null;
            var baseTimes = new List<float>();
            float end = Time.realtimeSinceStartup + 6f;
            while (Time.realtimeSinceStartup < end) { baseTimes.Add(Time.unscaledDeltaTime); yield return null; }
            regionRows.Add(Row("BASELINE_A0_TestRoom", baseTimes, 0f));
        }
        Write();
        Debug.Log("[MraFrameProbe] done");
        Application.Quit();
    }

    static float GfxMb => Profiler.GetAllocatedMemoryForGraphicsDriver() / 1048576f;

    static string Row(string id, List<float> times, float loadMs)
    {
        var skip = times.Skip(10).ToList();   // the first frames after a load warm up
        var sorted = skip.OrderBy(x => x).ToList();
        float P(float p) => sorted.Count == 0 ? 0f : sorted[Mathf.Clamp(Mathf.RoundToInt(p * (sorted.Count - 1)), 0, sorted.Count - 1)] * 1000f;
        return $"  {{\"id\": \"{id}\", \"frames\": {skip.Count}, \"load_ms\": {loadMs:F0}, \"mean_ms\": {(skip.Count > 0 ? skip.Average() * 1000f : 0f):F2}, " +
               $"\"p50_ms\": {P(.5f):F2}, \"p95_ms\": {P(.95f):F2}, \"p99_ms\": {P(.99f):F2}, \"max_ms\": {(skip.Count > 0 ? skip.Max() * 1000f : 0f):F2}, " +
               $"\"frames_over_16_7ms\": {skip.Count(x => x > 1f / 60f)}, \"allocated_mb\": {Profiler.GetTotalAllocatedMemoryLong() / 1048576f:F0}, " +
               $"\"reserved_mb\": {Profiler.GetTotalReservedMemoryLong() / 1048576f:F0}, \"gfx_driver_mb\": {GfxMb:F0}, \"gc_collections\": {System.GC.CollectionCount(0)}, " +
               $"\"first_frames_ms\": [{string.Join(", ", times.Take(120).Select(x => (x * 1000f).ToString("F1")))}]}}";
    }
}

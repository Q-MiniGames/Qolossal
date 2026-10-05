using System.Linq;
using Mra;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// A Windows (64-bit, development) player of the prototype's scenes only, for measurement
// (MraFrameProbe). Usage: -executeMethod MraWindowsBuild.Build -buildPath <folder>/Qolossal_MRA.exe
public static class MraWindowsBuild
{
    public static void Build()
    {
        var world = MraWorldBuilder.Prepare();
        string[] args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-buildPath");
        string path = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Builds/MRA/Qolossal_MRA.exe";
        var scenes = world.regions.Select(r => MraWorldBuilder.ScenePath(r.scene))
            .Concat(world.chambers.Where(c => !c.InPlace).Select(c => MraWorldBuilder.ScenePath(c.scene)))
            .Append("Assets/Scenes/A0_TestRoom.unity")   // a baseline: an existing, approved scene measured the same way
            .ToArray();
        // A measurement build keeps running without window focus (the profile runs unattended); the
        // project's own setting is restored afterwards.
        bool background = PlayerSettings.runInBackground;
        PlayerSettings.runInBackground = true;
        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = path, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development,
            });
        }
        finally { PlayerSettings.runInBackground = background; }
        Debug.Log($"[MraWindowsBuild] {report.summary.result}: {report.summary.totalErrors} errors, {report.summary.totalSize / 1048576} MB, {report.summary.totalTime.TotalSeconds:F0} s -> {path}");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}

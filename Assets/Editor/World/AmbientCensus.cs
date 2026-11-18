using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The file half of the ambient census (polish plan, WS5): per scene, each sprite and how it's drawn, and every
// component type that animates something. Read only; nothing is saved. AmbientLifeCheck measures what moves in play.
// Run: -batchmode -quit -executeMethod AmbientCensus.Run -captureDir <dir>
public static class AmbientCensus
{
    public static void Run()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        string dir = at >= 0 ? args[at + 1] : "Temp/AmbientCensus";
        Directory.CreateDirectory(dir);
        foreach (var path in Directory.GetFiles("Assets/Scenes/Game", "*.unity"))
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var report = new StringBuilder();
            var all = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(r => r.sprite != null).ToArray();
            report.AppendLine($"{path}: {all.Length} sprite renderers");
            foreach (var g in all.GroupBy(r => r.sprite.name).OrderBy(g => g.Min(r => r.sortingOrder)))
            {
                var r = g.First();
                report.AppendLine($"{g.Count(),4} order {g.Min(x => x.sortingOrder)}..{g.Max(x => x.sortingOrder)} {r.sprite.name} [{r.drawMode}] {r.sharedMaterial?.name} size {r.bounds.size.x:0.0}x{r.bounds.size.y:0.0} tile {r.sprite.bounds.size.x:0.0}x{r.sprite.bounds.size.y:0.0} first '{r.name}' flip {g.Count(x => x.flipY)} rot {g.Count(x => Mathf.Abs(x.transform.eulerAngles.z) > 1f)}");
            }
            report.AppendLine("-- behaviours");
            foreach (var g in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b != null).GroupBy(b => b.GetType().Name).OrderBy(g => g.Key))
                report.AppendLine($"{g.Count(),4} {g.Key}");
            report.AppendLine($"Animators {Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}, particle systems {Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}");
            File.WriteAllText(Path.Combine(dir, Path.GetFileNameWithoutExtension(path) + ".txt"), report.ToString());
        }
    }
}

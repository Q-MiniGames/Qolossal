using System.Linq;
using UnityEditor;
using UnityEngine;

// Measures the ground speed each of Qori's gait clips was drawn for: while a foot is planted (the
// lowest part of its arc) it should stay still on the ground, so the speed it slides backwards in the
// rig's world space is the speed Qori must move for no foot sliding. QoriAnimator's walkClipSpeed and
// runClipSpeed should match. Menu: Qolossal > Qori Rig > Measure Gait Speeds; batch: QoriGaitMeasure.Run.
public static class QoriGaitMeasure
{
    const string PrefabPath = "Assets/Art/Characters/QoriRig/QoriRig.prefab";

    [MenuItem("Qolossal/Qori Rig/Measure Gait Speeds", priority = 30)]
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            rig.transform.localScale = Vector3.one;   // in game QoriAnimator cancels the Player's scale, so the rig is at 1
            var animator = rig.GetComponentInChildren<Animator>();
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (string name in new[] { "Qori_Walk", "Qori_Run" })
            {
                var clip = clips.FirstOrDefault(c => c.name == name);
                if (clip == null) { Debug.LogWarning("[QoriGait] missing " + name); continue; }
                foreach (string foot in new[] { "FootNear", "FootFar" })
                {
                    var t = rig.GetComponentsInChildren<Transform>(true).First(x => x.name == foot);
                    const int n = 240;
                    var xs = new float[n]; var ys = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        clip.SampleAnimation(animator.gameObject, clip.length * i / n);
                        xs[i] = t.position.x; ys[i] = t.position.y;
                    }
                    float min = ys.Min(), max = ys.Max(), band = min + (max - min) * .08f;
                    // Planted frames: the foot within 8% of its lowest point; speed from consecutive pairs.
                    float dist = 0f, time = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        int j = (i + 1) % n;
                        if (ys[i] > band || ys[j] > band) continue;
                        dist += xs[i] - xs[j]; time += clip.length / n;
                    }
                    float speed = time > 0f ? dist / time : 0f;
                    Debug.Log($"[QoriGait] {name} {foot}: planted {time / clip.length:P0} of the cycle, slides back at {speed:F2} u/s (clip {clip.length:F2} s, lift {max - min:F2} u)");
                }
            }
        }
        finally { Object.DestroyImmediate(rig); }
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}

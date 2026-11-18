using UnityEngine;
using UnityEngine.SceneManagement;

// Gives the jointed resident figures their Ludo idle: on a scene's load, each "Figure <Rig>" with frames in
// Resources hides its parts and plays the frames from its feet. The figure's transform is untouched, so TownNpc and
// MraResident still flip it to face Qori. A rig with no frames keeps its parts. Purely visual.
public static class ResidentFigures
{
    const string Prefix = "Figure ";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneLoaded += Loaded;
        Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.gameObject.scene != scene || !t.name.StartsWith(Prefix, System.StringComparison.Ordinal) || t.Find(ResidentFramesPlayer.ChildName) != null) continue;
            var set = Resources.Load<ResidentIdle>("LudoFrames/Residents/" + t.name.Substring(Prefix.Length));
            if (set == null || set.idle == null || set.idle.Length == 0) continue;
            int order = 0;
            foreach (var part in t.GetComponentsInChildren<SpriteRenderer>(true))
            {
                order = Mathf.Max(order, part.sortingOrder);
                part.enabled = false;
            }
            var view = new GameObject(ResidentFramesPlayer.ChildName).AddComponent<SpriteRenderer>();
            view.transform.SetParent(t, false);
            view.sprite = set.idle[0]; view.sortingOrder = order;
            view.gameObject.AddComponent<ResidentFramesPlayer>().Init(set.idle, view);
        }
    }
}

public sealed class ResidentFramesPlayer : MonoBehaviour
{
    public const string ChildName = "Idle frames";
    const float FramesPerSecond = 8f;
    Sprite[] frames; SpriteRenderer view; float start;

    public void Init(Sprite[] idle, SpriteRenderer renderer)
    {
        frames = idle; view = renderer;
        start = Random.value * 10f;   // so neighbours don't breathe together
    }

    void Update()
    {
        if (frames != null) view.sprite = frames[Mathf.FloorToInt((Time.time + start) * FramesPerSecond) % frames.Length];
    }
}

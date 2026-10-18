using UnityEngine;
using UnityEngine.SceneManagement;

// Puts the Ludo.ai frame-by-frame animation on Qori and the enemies in every scene (19 Oct, the user: the new Ludo
// animations into the game). The frames and their settings are the ones tried in the Qori Walk Test room, saved by
// QoriWalkTestBuilder (Qolossal > Qori Walk Test > Build Scene) as Resources/LudoFrames/LudoQoriFrames (Qori) and
// Resources/LudoFrames/LudoEnemyFrames (each enemy kind). Without them nothing changes: the rigs show as before.
// Enemies placed later (spawned) are picked up by a light check twice a second.
public sealed class LudoFramesInstaller : MonoBehaviour
{
    [Tooltip("Off: the old cutout rigs everywhere, as before.")] public static bool Use = true;
    static LudoFramesInstaller instance;
    LudoEnemyLibrary library; GameObject qoriPrefab; float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Use || instance != null) return;
        var go = new GameObject("Ludo frames installer"); DontDestroyOnLoad(go);
        instance = go.AddComponent<LudoFramesInstaller>();
    }

    void Awake()
    {
        qoriPrefab = Resources.Load<GameObject>("LudoFrames/LudoQoriFrames");
        var lib = Resources.Load<GameObject>("LudoFrames/LudoEnemyFrames");
        library = lib != null ? lib.GetComponent<LudoEnemyLibrary>() : null;
        SceneManager.sceneLoaded += (s, m) => nextScan = 0f;
    }

    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + .5f;
        // Qori: one frame player per scene that has the player (the test room places its own).
        if (qoriPrefab != null && FindAnyObjectByType<PlayerMovement>() != null && FindAnyObjectByType<QoriWalkFramesOnPlayer>() == null)
            Instantiate(qoriPrefab).name = "Qori Ludo frames";
        if (library == null) return;
        foreach (var c in FindObjectsByType<GroundCreature>(FindObjectsSortMode.None))
            if (c.GetComponent<CrawlerFramesOnCreature>() == null) library.DressCrawler(c.gameObject);
        foreach (var e in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            if (e.GetComponent<EnemyFramesOnCreature>() == null) library.Dress(e);
    }
}

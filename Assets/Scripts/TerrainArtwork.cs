using UnityEngine;
using UnityEngine.SceneManagement;

// Decorate authored terrain and give ground cliffs solid sides below the landing surface.
[DisallowMultipleComponent]
public sealed class TerrainArtwork : MonoBehaviour
{
    private SpriteRenderer source;
    private Transform painting;
    private Material material;
    private bool wasEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= DecorateScene;
        SceneManager.sceneLoaded += DecorateScene;
    }

    private static void DecorateScene(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "OpeningLevel") return;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
        {
            string name = item.name;
            bool cliff = name == "SecondGround" || name == "ThirdGround";
            bool hanging = name == "DestinationPlatform" || name == "HighLandingPlatform" ||
                name == "FlyingRouteLanding" || name == "FinalLanding" ||
                name == "Platrform01" || name == "Platrform02";
            if ((!cliff && !hanging) || item.GetComponent<TerrainArtwork>() != null) continue;
            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            BoxCollider2D collider = item.GetComponent<BoxCollider2D>();
            if (renderer == null || collider == null) continue;
            Sprite sprite = Resources.Load<Sprite>("Terrain/" + (cliff ? "MossyGround_Companion_v1" : "Rootbound_HangingPlatform_v1"));
            Shader shader = Resources.Load<Shader>("Terrain/TerrainChromaKey");
            if (sprite == null || shader == null) continue;
            item.gameObject.AddComponent<TerrainArtwork>().Initialize(renderer, collider, sprite, shader, cliff);
            string moss = name == "SecondGround" ? "SecondGround_Moss" : name == "ThirdGround" ? "ThirdGround_Moss" :
                name == "HighLandingPlatform" ? "HighLanding_Moss" : name == "FlyingRouteLanding" ? "FlyingRoute_Moss" :
                name == "FinalLanding" ? "FinalLanding_Moss" : null;
            if (moss == null) continue;
            foreach (GameObject mossRoot in scene.GetRootGameObjects())
                if (mossRoot.name == moss && mossRoot.TryGetComponent(out SpriteRenderer oldMoss)) oldMoss.enabled = false;
        }
    }

    private void Initialize(SpriteRenderer renderer, BoxCollider2D collider, Sprite sprite, Shader shader, bool cliff)
    {
        source = renderer; wasEnabled = source.enabled;
        painting = new GameObject("Painted " + name).transform;
        painting.SetParent(transform, false);
        // Use collider geometry rather than bounds so inactive platforms initialize correctly.
        Vector3 surface = transform.TransformPoint(collider.offset + Vector2.up * collider.size.y * .5f);
        if (cliff)
        {
            float localTop = collider.offset.y + collider.size.y * .5f;
            float depth = 10f / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.y));
            collider.size = new Vector2(collider.size.x, depth);
            collider.offset = new Vector2(collider.offset.x, localTop - depth * .5f);
            collider.isTrigger = false;
            collider.usedByEffector = false;
        }
        float width = Mathf.Abs(transform.lossyScale.x) * collider.size.x;
        painting.position = surface;
        painting.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        float heightScale = cliff ? Mathf.Max(width, 10f / (854f / 1450f)) : width;
        painting.localScale = new Vector3(width / Mathf.Max(.001f, Mathf.Abs(scale.x)),
            heightScale / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1f);
        SpriteRenderer art = painting.gameObject.AddComponent<SpriteRenderer>();
        art.sprite = sprite; material = new Material(shader); art.sharedMaterial = material;
        art.sortingLayerID = source.sortingLayerID; art.sortingOrder = source.sortingOrder;
        art.color = Color.white;
        source.enabled = false;
    }

    private void OnEnable() { if (painting != null) { painting.gameObject.SetActive(true); source.enabled = false; } }
    private void OnDisable() { if (painting != null) { painting.gameObject.SetActive(false); if (source != null) source.enabled = wasEnabled; } }
    private void OnDestroy() { if (painting != null) Destroy(painting.gameObject); if (material != null) Destroy(material); }
}


using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds the enemy prefabs around the rigs from CreatureRigBuilder, and puts the Seed Carrier rig
// on the existing FlyingCreature01. Menu: Qolossal > Creatures > Build Enemy Prefabs
public static class EnemyPrefabBuilder
{
    const string Art = "Assets/Art/Codex/Enemies/";

    [MenuItem("Qolossal/Creatures/Build Enemy Prefabs")]
    public static void BuildAll()
    {
        CreatureRigBuilder.BuildAll();
        InstallCarrier();
        Build("Thornwing01", "Thornwing", root =>
        {
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<CircleCollider2D>().radius = .42f;
            return root.AddComponent<ThornwingEnemy>();
        }, Vector2.zero);
        Build("PodSpitter01", "Spitter", root =>
        {
            root.AddComponent<Rigidbody2D>();
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(.7f, 1.35f); box.offset = new Vector2(0f, .68f);
            var e = root.AddComponent<SpitterEnemy>();
            Set(e, "headClosed", Sprite("Spitter_Head_Closed")); Set(e, "headOpen", Sprite("Spitter_Head_Open")); Set(e, "seed", Sprite("Spitter_Projectile"));
            return e;
        }, Vector2.zero);
        Build("Shellback01", "Shellback", root =>
        {
            root.AddComponent<Rigidbody2D>();
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.35f, .82f); box.offset = new Vector2(0f, .41f);
            var e = root.AddComponent<ShellbackEnemy>();
            Set(e, "shellCracked", Sprite("Shellback_Shell_Cracked"));
            var shards = AssetDatabase.LoadAllAssetsAtPath(Art + "Shellback_Shell_Shards.png").OfType<Sprite>().OrderBy(s => s.name).ToArray();
            var so = new SerializedObject(e); var list = so.FindProperty("shards"); list.arraySize = shards.Length;
            for (int i = 0; i < shards.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = shards[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return e;
        }, Vector2.zero);
        Build("Newt01", "Newt", root =>
        {
            root.AddComponent<Rigidbody2D>();
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.3f, .5f); box.offset = new Vector2(0f, .3f);
            var e = root.AddComponent<NewtEnemy>();
            Set(e, "splash", Sprite("FX_Splash")); Set(e, "ripple", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Codex/Effects/FX_Water_Ripple.png"));
            return e;
        }, Vector2.zero);
        Build("Grub01", "Grub", root =>
        {
            root.AddComponent<Rigidbody2D>();
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.7f, .62f); box.offset = new Vector2(-.15f, .32f);
            var e = root.AddComponent<GrubEnemy>();
            Set(e, "soilBurst", Sprite("FX_Soil_Burst")); Set(e, "mouthOpen", Sprite("Grub_Mouth_Open"));
            return e;
        }, Vector2.zero);
        Build("GustMoth01", "GustMoth", root =>
        {
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<CircleCollider2D>().radius = .45f;
            var e = root.AddComponent<GustMothEnemy>();
            Set(e, "gust", Sprite("FX_Gust"));
            return e;
        }, Vector2.zero);
        Build("Sentinel01", "Sentinel", root =>
        {
            root.AddComponent<Rigidbody2D>();
            var box = root.AddComponent<BoxCollider2D>(); box.size = new Vector2(1.1f, 1.9f); box.offset = new Vector2(.15f, .95f);
            return root.AddComponent<SentinelEnemy>();
        }, Vector2.zero);
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyPrefabBuilder] built enemy prefabs");
    }

    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png") ?? throw new FileNotFoundException(name);

    static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Build(string prefabName, string species, System.Func<GameObject, EnemyBase> setup, Vector2 rigOffset)
    {
        var root = new GameObject(prefabName);
        try
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Characters/{species}/{species}Rig.prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, root.transform);
            rig.transform.localPosition = rigOffset;
            EnemyBase enemy = setup(root);
            Set(enemy, "rig", rig.GetComponent<CreatureRig>());
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/" + prefabName + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    // The Seed Carrier keeps FlyingCreature's figure-8 flight and grapple anchor; its old
    // one-piece visual is switched off and the rig hangs so the seed pod sits on the anchor point.
    static void InstallCarrier()
    {
        const string path = "Assets/Prefabs/FlyingCreature01.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform old = root.transform.Find("CarrierRig");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform legacy = root.transform.Find("AnchorVisual");
            if (legacy != null) legacy.gameObject.SetActive(false);
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Carrier/CarrierRig.prefab"), root.transform);
            rig.name = "CarrierRig";
            Vector3 s = root.transform.localScale;
            rig.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f);
            rig.transform.localPosition = new Vector3(0f, .32f / s.y, 0f);   // seed pod centre on the anchor
            rig.AddComponent<CarrierAnimator>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}

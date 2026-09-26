using System.IO;
using UnityEditor;
using UnityEngine;

// Recolours a placed enemy for an area: every sprite it uses from Assets/Art/Codex/Enemies
// (rig parts, and sprite fields such as shell shards or projectiles) is swapped for the area's
// palette variant, <name>_<AREA>.png, where one exists. Variants share their base part's alpha
// and canvas (Review 05), so the rig needs no other change. Saved as prefab overrides.
public static class AreaPalette
{
    const string Folder = "Assets/Art/Codex/Enemies/";

    public static int Apply(GameObject enemy, string area)
    {
        int swapped = 0;
        foreach (var component in enemy.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            var so = new SerializedObject(component);
            var p = so.GetIterator();
            bool changed = false;
            while (p.Next(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference || !(p.objectReferenceValue is Sprite sprite)) continue;
                string path = AssetDatabase.GetAssetPath(sprite);
                if (!path.StartsWith(Folder)) continue;
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith("_" + area)) continue;
                var variant = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + name + "_" + area + ".png");
                if (variant == null) continue;
                p.objectReferenceValue = variant; changed = true; swapped++;
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }
        return swapped;
    }
}

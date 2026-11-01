using UnityEngine;

// A note left in a scene for whoever opens it in the editor (the underground blockout's README).
public sealed class MraUgSceneNote : MonoBehaviour
{
    [TextArea(3, 12)] public string text;
}

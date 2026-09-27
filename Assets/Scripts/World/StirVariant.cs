using UnityEngine;

// A part of a level that exists only before, or only after, a knot wakes: a pit that a clenched
// finger closes, a vein that grows, a guardian that stops guarding. Its children are shown or
// hidden from the saved game when the scene loads, and again at once when the knot wakes (the
// stir sequence does that behind a flash). It runs before everything else so the rest of the
// scene only ever sees the current state.
[DefaultExecutionOrder(-2000), DisallowMultipleComponent]
public sealed class StirVariant : MonoBehaviour
{
    public enum When { BeforeKnot, AfterKnot }
    [Tooltip("A Knots id, e.g. grip.")] public string knot = Knots.Grip;
    public When when = When.AfterKnot;

    public bool Present => GameSave.IsKnotAwake(knot) == (when == When.AfterKnot);

    void Awake() { Apply(); GameSave.WorldChanged += Apply; }
    void OnDestroy() => GameSave.WorldChanged -= Apply;

    public void Apply()
    {
        if (this == null) return;
        bool present = Present;
        foreach (Transform child in transform)
            if (child.gameObject.activeSelf != present) child.gameObject.SetActive(present);
    }
}

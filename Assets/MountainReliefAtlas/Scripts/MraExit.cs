using UnityEngine;

// The edge of a region, where the climb carries on into the next one: walking into it fades to the
// neighbouring region's scene and its paired landing, carrying hearts and weapon (AreaTransition).
// The builder stands a threshold arch over it; coming within `signRange` shows where it leads.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraExit : MonoBehaviour
{
    public string targetScene, arrivalId;
    public string[] requires = new string[0];
    [Tooltip("Shown on approach, e.g. \"The Long Causeway\".")] public string destinationName = "";
    [Tooltip("1: the way on lies to the right; -1: to the left.")] public float direction = 1f;
    public float signRange = 7f, signHeight = 4.4f;
    float toldAt = -10f; Transform qori;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori == null || AreaTransition.IsTransitioning) return;
        if (!MraState.Holds(requires))
        {
            if (Time.time - toldAt > 3f) { toldAt = Time.time; TownHud.Toast("The way on is closed."); }
            return;
        }
        AreaTransition.Travel(targetScene, arrivalId, qori);
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(destinationName) || MraState.Busy) return;
        if (qori == null) { var q = FindAnyObjectByType<PlayerMovement>(); if (q == null) return; qori = q.transform; }
        float d = Mathf.Abs(qori.position.x - transform.position.x);
        if (d > signRange || Mathf.Abs(qori.position.y - (transform.position.y - 5f)) > 6f) return;
        string text = direction > 0f ? destinationName + "  ›" : "‹  " + destinationName;
        if (!MraState.Holds(requires)) text = destinationName + " (closed)";
        MraState.Prompt(transform.position + new Vector3(0f, signHeight - 5f, 0f), text, d < signRange * .6f);
    }
}

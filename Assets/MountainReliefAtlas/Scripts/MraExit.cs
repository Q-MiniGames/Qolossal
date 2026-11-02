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
    public float groundOffset = 5f;
    public bool retryWhileInside;
    float toldAt = -10f; Transform qori;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori == null || MraState.Busy || MraReveal.IsPlaying) return;
        if (!MraState.Holds(requires))
        {
            if (Time.time - toldAt > 3f) { toldAt = Time.time; TownHud.Toast("The way on is closed."); }
            return;
        }
        AreaTransition.Travel(targetScene, arrivalId, qori, "", GetComponent<CaveThresholdPresentation>());
    }

    // A reveal can unlock a doorway while Qori already overlaps its trigger.
    void OnTriggerStay2D(Collider2D other)
    {
        if (retryWhileInside && MraState.Holds(requires)) OnTriggerEnter2D(other);
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(destinationName) || MraState.Busy) return;
        if (qori == null) { var q = FindAnyObjectByType<PlayerMovement>(); if (q == null) return; qori = q.transform; }
        float d = Mathf.Abs(qori.position.x - transform.position.x);
        if (d > signRange || Mathf.Abs(qori.position.y - (transform.position.y - groundOffset)) > 6f) return;
        string text = direction > 0f ? destinationName + "  ›" : "‹  " + destinationName;
        if (!MraState.Holds(requires)) text = destinationName + " (closed)";
        MraState.Prompt(transform.position + new Vector3(0f, signHeight - groundOffset, 0f), text, d < signRange * .6f);
    }
}

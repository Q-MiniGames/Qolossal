using UnityEngine;

// The way back out of a side chamber: a threshold at its entrance end, always open. It returns Qori
// to the safe spot outside the chamber's mouth in the region (an MraArrival with the chamber's
// return id). His checkpoint there is kept: arriving never moves it.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraChamberReturn : MonoBehaviour
{
    public string parentScene, returnId;
    [Tooltip("Where it leads, shown when Qori is near (e.g. \"Qvale\"); empty for no sign.")] public string label = "";
    Transform qori;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori != null && !AreaTransition.IsTransitioning) AreaTransition.Travel(parentScene, returnId, qori);
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(label) || MraState.Busy) return;
        if (qori == null) { var q = FindAnyObjectByType<PlayerMovement>(); if (q == null) return; qori = q.transform; }
        if (Mathf.Abs(qori.position.x - transform.position.x) > 4f) return;
        MraState.Prompt(new Vector3(transform.position.x + 1.6f, qori.position.y + 2.2f, 0f), "‹  Out to " + label, false);
    }
}

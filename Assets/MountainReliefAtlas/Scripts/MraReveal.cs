using System.Collections;
using UnityEngine;

// The reveal, once, on the Summit's reveal ledge. Qori stops on the ledge; the camera eases out over
// the very same lake, reeds and falls he has just crossed (no terrain is swapped or moved), then
// the accepted reveal still and the completed Chart are shown. The saved flag mra:reveal-complete
// opens the way down to the descent; on a later visit nothing replays.
// Art gaps (see the handoff): the pull-out stills Reveal_Face_02/03 are Review 22 redos and are
// not used; the final assembly is the approved Batch 11 Chart concept, a review-only import.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraReveal : MonoBehaviour
{
    public Vector2 pullCentre;
    public float pullSize = 45f, pullSeconds = 6f;
    public Sprite brow, finalAssembly;

    public static bool IsPlaying { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsPlaying = false;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        var qori = MraState.QoriOf(other);
        if (qori != null) Play(qori);
    }

    // Entering the ledge calls this; so can tests. False when it has already happened.
    public bool Play(PlayerMovement qori)
    {
        if (MraState.Revealed || IsPlaying) return false;
        StartCoroutine(Run(qori));
        return true;
    }

    IEnumerator Run(PlayerMovement qori)
    {
        IsPlaying = true; ModalUi.Open();
        var body = qori.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = new Vector2(0f, Mathf.Min(0f, body.linearVelocity.y));
        Vector2 ledge = qori.transform.position;
        var view = Camera.main; var follow = view != null ? view.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.enabled = false;
        VistaZone.Suspended = true;
        Vector3 from = view.transform.position; float size = view.orthographicSize;
        for (float t = 0f; t < pullSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / pullSeconds);
            view.transform.position = Vector3.Lerp(from, new Vector3(pullCentre.x, pullCentre.y, from.z), k);
            view.orthographicSize = Mathf.Lerp(size, pullSize, k);
            yield return null;
        }
        // Saved before the stills, so a quit during them still counts.
        GameSave.SetFlag(MraState.RevealFlag);
        yield return MraStills.Play(new[] { brow, finalAssembly }, new[] { "", "The Chart is complete." }, 6f);
        for (float t = 0f; t < 1.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.5f);
            view.transform.position = Vector3.Lerp(new Vector3(pullCentre.x, pullCentre.y, from.z), from, k);
            view.orthographicSize = Mathf.Lerp(pullSize, size, k);
            yield return null;
        }
        if (follow != null) follow.enabled = true;
        VistaZone.Suspended = false;
        // Qori never left the ledge.
        if (Vector2.Distance(qori.transform.position, ledge) > .5f) qori.PlaceAt(ledge, 0f, false);
        TownHud.Toast("A way down has opened beyond the ledge.", 5f);
        IsPlaying = false; ModalUi.Close();
    }

    void OnDisable() { if (IsPlaying) { IsPlaying = false; VistaZone.Suspended = false; ModalUi.Close(); } }
}

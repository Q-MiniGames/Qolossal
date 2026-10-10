using System.Collections;
using UnityEngine;

// The reveal, once, on the Summit's reveal ledge (the crown, on the reclining route). Qori stops on
// the ledge; the camera first looks back DOWN the way he came (the old man's "look down before you
// look up"), then eases out over the very same lake, reeds, prow and falls he has just crossed (no
// terrain is swapped or moved), then the accepted reveal still and the completed Chart are shown. The saved flag mra:reveal-complete
// opens the way down to the descent; on a later visit nothing replays.
// Art gaps (see the handoff): the pull-out stills Reveal_Face_02/03 show the old seated pose and are
// not used; the final assembly is still the seated Batch 11 Chart concept, a review-only import,
// until the reclining Chart painting (Batch 18) and the reclining reveal stills arrive.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraReveal : MonoBehaviour
{
    public Vector2 pullCentre;
    public float pullSize = 45f, pullSeconds = 6f;
    [Tooltip("Where the camera first looks back down the route (world), and how wide; zero size skips it.")]
    public Vector2 lookBack; public float lookBackSize, lookBackSeconds = 3.5f;
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
        Sfx.PlayStory("Story_Reveal");
        // Look back down first: the way he came, falling away below the crown.
        Vector3 start = from; float startSize = size;
        if (lookBackSize > 0f)
        {
            var back = new Vector3(lookBack.x, lookBack.y, from.z);
            for (float t = 0f; t < lookBackSeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / lookBackSeconds);
                view.transform.position = Vector3.Lerp(from, back, k);
                view.orthographicSize = Mathf.Lerp(size, lookBackSize, k);
                yield return null;
            }
            yield return new WaitForSeconds(.8f);
            start = back; startSize = lookBackSize;
        }
        for (float t = 0f; t < pullSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / pullSeconds);
            view.transform.position = Vector3.Lerp(start, new Vector3(pullCentre.x, pullCentre.y, from.z), k);
            view.orthographicSize = Mathf.Lerp(startSize, pullSize, k);
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

    // Cut short (a scene change, a quit): its sound stops with it.
    void OnDisable() { if (IsPlaying) { IsPlaying = false; VistaZone.Suspended = false; ModalUi.Close(); Sfx.Stop("Story_Reveal"); } }
}

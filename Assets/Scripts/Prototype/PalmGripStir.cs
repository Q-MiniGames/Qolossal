using System.Collections;
using UnityEngine;

// Palm prototype: the Grip stir as a local moment (world redesign proposal, section 6). The knot
// lies in the cupped palm. Touching it makes the hand grip: the camera pulls back to show the
// hand, the fingers behind flex, and the thumb, standing like a tower over the heel of the hand,
// folds down across the palm until its tip rests in the bowl. It's a bridge over the crease
// ravine, up onto the heel and on to the wrist. Nothing is saved: the scene reloads as it was.
[DisallowMultipleComponent]
public sealed class PalmGripStir : MonoBehaviour
{
    [Header("Knot")]
    public SpriteRenderer core, glow;
    public Sprite coreDim, coreLit;

    [Header("Thumb (two joints: base, then the knuckle)")]
    public Transform thumbBase, thumbKnuckle;
    public Vector2 uprightAngles = new Vector2(88f, 75f), foldedAngles = new Vector2(190f, 217f);   // world angles of the two segments
    public float lengthToKnuckle = 14f;
    public BodyShape[] thumbSegments;

    [Header("The fingers behind (rotated about their bases)")]
    public Transform[] fingers;
    public float fingerCurl = 14f;

    [Header("The view during the stir")]
    public Vector2 frameCentre = new Vector2(84f, 14f);
    public float frameSize = 14f;
    public string caption = "The hand grips.", subCaption = "Its thumb bridges the crease.";

    public bool Done { get; private set; }
    public static bool IsPlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsPlaying = false;

    float textAlpha, shake; Vector3 shaken;
    Camera view; CameraFollow follow;
    static readonly Color Mint = new Color(.72f, 1f, .88f);

    void Start()
    {
        Pose(0f);
        if (core != null) core.sprite = coreDim;
        SetThumbSolid(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Done || IsPlaying || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        StartCoroutine(Run(other.attachedRigidbody));
    }

    // Lays the thumb and fingers at `k` of the way from open (0) to gripped (1).
    public void Pose(float k)
    {
        // Lerp the raw angles (not the shortest way round), so the thumb folds over the palm.
        float a = Mathf.Lerp(uprightAngles.x, foldedAngles.x, k), b = Mathf.Lerp(uprightAngles.y, foldedAngles.y, k);
        if (thumbBase != null) thumbBase.rotation = Quaternion.Euler(0f, 0f, a);
        if (thumbKnuckle != null)
        {
            thumbKnuckle.localPosition = new Vector3(lengthToKnuckle, 0f, 0f);
            thumbKnuckle.localRotation = Quaternion.Euler(0f, 0f, b - a);
        }
        if (fingers != null) foreach (var f in fingers) if (f != null) f.localRotation = Quaternion.Euler(0f, 0f, -fingerCurl * k);
    }

    void SetThumbSolid(bool on)
    {
        if (thumbSegments == null) return;
        foreach (var s in thumbSegments) if (s != null && s.TryGetComponent(out PolygonCollider2D c)) c.enabled = on;
    }

    IEnumerator Run(Rigidbody2D qori)
    {
        IsPlaying = true; VistaZone.Suspended = true;
        view = Camera.main; follow = view != null ? view.GetComponent<CameraFollow>() : null;
        if (follow != null) follow.enabled = false;
        Vector3 fromPos = view.transform.position; float fromSize = view.orthographicSize;
        Vector3 framePos = new Vector3(frameCentre.x, frameCentre.y, fromPos.z);

        // The knot lights, and the view pulls back to show the whole hand.
        if (core != null) core.sprite = coreLit;
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.6f);
            view.transform.position = Vector3.Lerp(fromPos, framePos, k);
            view.orthographicSize = Mathf.Lerp(fromSize, frameSize, k);
            if (glow != null) glow.color = new Color(1f, 1f, 1f, k);
            shake = .03f * k;
            yield return null;
        }

        // The grip: slow to start, heavy at the end.
        const float Fold = 3.8f;
        for (float t = 0f; t < Fold; t += Time.deltaTime)
        {
            float k = t / Fold;
            Pose(k * k * (3f - 2f * k));
            shake = Mathf.Lerp(.05f, .16f, k);
            yield return null;
        }
        Pose(1f);
        SetThumbSolid(true);
        Unstick(qori);
        shake = .45f;   // the thumb lands
        for (float t = 0f; t < .9f; t += Time.deltaTime) { shake = Mathf.Lerp(.45f, 0f, t / .9f); yield return null; }
        shake = 0f;

        // The caption, then the view returns to Qori.
        for (float t = 0f; t < .6f; t += Time.deltaTime) { textAlpha = t / .6f; yield return null; }
        textAlpha = 1f;
        yield return new WaitForSeconds(1.4f);
        Vector3 start = view.transform.position; float startSize = view.orthographicSize;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 1.4f);
            Vector3 home = new Vector3(qori.position.x, qori.position.y + 1f, start.z);
            view.transform.position = Vector3.Lerp(start, home, k);
            view.orthographicSize = Mathf.Lerp(startSize, VistaZone.NormalSize, k);
            yield return null;
        }
        view.orthographicSize = VistaZone.NormalSize;
        if (follow != null) follow.enabled = true;
        IsPlaying = false; VistaZone.Suspended = false; Done = true;
        for (float t = 0f; t < 2.5f; t += Time.deltaTime) yield return null;
        for (float t = 0f; t < .8f; t += Time.deltaTime) { textAlpha = 1f - t / .8f; yield return null; }
        textAlpha = 0f;
    }

    // If the thumb came down where Qori stands, lift him onto it.
    void Unstick(Rigidbody2D qori)
    {
        Physics2D.SyncTransforms();
        var box = qori.GetComponent<BoxCollider2D>();
        if (box == null) return;
        foreach (var s in thumbSegments)
        {
            if (s == null || !s.TryGetComponent(out PolygonCollider2D c) || !box.IsTouching(c) && !c.OverlapPoint(qori.position)) continue;
            RaycastHit2D hit = Physics2D.Raycast(qori.position + Vector2.up * 30f, Vector2.down, 60f, LayerMask.GetMask("Ground"));
            if (hit) qori.position = hit.point + Vector2.up * .1f;
            return;
        }
    }

    void Update() { if (view != null) view.transform.position -= shaken; shaken = Vector3.zero; }
    void LateUpdate()
    {
        if (view == null || shake <= 0f) return;
        float t = Time.time;
        shaken = new Vector3(Mathf.PerlinNoise(t * 17f, 0f) - .5f, Mathf.PerlinNoise(0f, t * 19f) - .5f, 0f) * 2f * shake;
        view.transform.position += shaken;
    }

    void OnGUI()
    {
        if (textAlpha <= 0f) return;
        var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .05f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        var sub = new GUIStyle(title) { fontSize = Mathf.RoundToInt(Screen.height * .03f), fontStyle = FontStyle.Italic };
        title.normal.textColor = new Color(1f, 1f, 1f, textAlpha); sub.normal.textColor = new Color(Mint.r, Mint.g, Mint.b, textAlpha);
        GUI.Label(new Rect(0f, Screen.height * .18f, Screen.width, Screen.height * .08f), caption, title);
        GUI.Label(new Rect(0f, Screen.height * .26f, Screen.width, Screen.height * .05f), subCaption, sub);
    }
}

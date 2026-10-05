using System.Collections;
using UnityEngine;

// A region's knot: a pulsing knot of roots (Codex's Wakeknot art). Touching it wakes it, saved under
// its existing knot id (grip, reach, spring, breath, bloom, sight, heart), and the land shakes: a
// local quake, with only an innocent caption. Nothing here names or shows a body. Saved quakes
// change the land (MraVariant on knot:<id>) and the town (TownState's quake count).
// No guardian guards it in this prototype: the design's knot beat is a checkpointed traversal
// trial until a working guardian exists (see the handoff).
[DefaultExecutionOrder(1001)]   // after CameraFollow: the shake is added after the follow
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MraKnot : MonoBehaviour
{
    public string knot;
    public SpriteRenderer core, glow;
    public Sprite coreDim, coreLit;
    public string caption = "The ground shifts.", subCaption = "Something nearby has moved.";

    public static bool IsPlaying { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsPlaying = false;

    float shake; Vector3 shaken; Camera view; Vector3 coreScale;
    public bool IsAwake => GameSave.IsKnotAwake(knot);

    void Awake() { GetComponent<BoxCollider2D>().isTrigger = true; if (core != null) coreScale = core.transform.localScale; }
    void Start() => Show(IsAwake ? 1f : 0f);

    void OnTriggerEnter2D(Collider2D other) { if (MraState.IsQori(other)) Wake(); }

    // Touching calls this; so can tests. False if already awake (or a quake is under way).
    public bool Wake()
    {
        if (IsAwake || IsPlaying) return false;
        StartCoroutine(Quake());
        return true;
    }

    IEnumerator Quake()
    {
        IsPlaying = true; ModalUi.Open();
        view = Camera.main;
        for (float t = 0f; t < .8f; t += Time.deltaTime) { Show(t / .8f); shake = .05f * t; yield return null; }
        for (float t = 0f; t < 1.8f; t += Time.deltaTime) { shake = Mathf.Lerp(.1f, .38f, t / 1.8f); yield return null; }
        // Saved at the height of the quake: the land changes under the dust.
        GameSave.WakeKnot(knot);
        TownState.Quake();
        TownHud.Caption(caption, subCaption);
        if (Fx.Library != null) Fx.Leaves(transform.position + Vector3.up * 1.5f, 12, 2f);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime) { shake = Mathf.Lerp(.38f, 0f, t / 1.2f); yield return null; }
        shake = 0f; IsPlaying = false; ModalUi.Close();
    }

    void Show(float lit)
    {
        if (core != null) core.sprite = lit > .5f ? coreLit : coreDim;
        if (glow != null) glow.color = new Color(1f, 1f, 1f, lit);
    }

    void Update()
    {
        if (view != null) view.transform.position -= shaken;
        shaken = Vector3.zero;
        if (core != null) core.transform.localScale = coreScale * (1f + (IsAwake ? .03f : .06f) * Mathf.Sin(Time.time * (IsAwake ? 1.6f : 3f)));
    }

    void LateUpdate()
    {
        if (view == null || shake <= 0f) return;
        float t = Time.time;
        shaken = new Vector3(Mathf.PerlinNoise(t * 17f, 0f) - .5f, Mathf.PerlinNoise(0f, t * 19f) - .5f, 0f) * 2f * shake;
        view.transform.position += shaken;
    }

    void OnDisable() { if (IsPlaying) { IsPlaying = false; ModalUi.Close(); } }
}

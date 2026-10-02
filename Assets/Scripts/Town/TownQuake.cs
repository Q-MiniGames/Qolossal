using System.Collections;
using UnityEngine;

// Qvale prototype: a knot of roots outside town. Touching it wakes it, as Qori will do out in the
// world, and the town feels the quake: the ground shakes, a caption says only that the ground has
// shifted, and the town changes (TownState.Quake: a crack in the square, the springs beat faster,
// and everyone has something new to say).
[DefaultExecutionOrder(1001)]   // after CameraFollow, like StirSequence: the shake is added after the follow
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class TownQuake : MonoBehaviour
{
    public SpriteRenderer core, glow;
    public Sprite coreLit;
    public bool Done { get; private set; }
    public static bool IsPlaying { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => IsPlaying = false;

    float shake; Vector3 shaken; Camera view;

    void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Done || IsPlaying || other.attachedRigidbody == null || other.attachedRigidbody.GetComponent<PlayerMovement>() == null) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        IsPlaying = true; ModalUi.Open();
        view = Camera.main;
        if (core != null && coreLit != null) core.sprite = coreLit;
        for (float t = 0f; t < 1f; t += Time.deltaTime) { if (glow != null) glow.color = new Color(1f, 1f, 1f, t); shake = .04f * t; yield return null; }
        for (float t = 0f; t < 2.2f; t += Time.deltaTime) { shake = Mathf.Lerp(.08f, .35f, t / 2.2f); yield return null; }
        TownState.Quake();
        TownHud.Caption("The ground has shifted.", "Somewhere below, the town will have felt it.");
        for (float t = 0f; t < 1.2f; t += Time.deltaTime) { shake = Mathf.Lerp(.35f, 0f, t / 1.2f); yield return null; }
        shake = 0f; Done = true; IsPlaying = false; ModalUi.Close();
    }

    void Update() { if (view != null) view.transform.position -= shaken; shaken = Vector3.zero; }
    void LateUpdate()
    {
        if (view == null || shake <= 0f) return;
        float t = Time.time;
        shaken = new Vector3(Mathf.PerlinNoise(t * 17f, 0f) - .5f, Mathf.PerlinNoise(0f, t * 19f) - .5f, 0f) * 2f * shake;
        view.transform.position += shaken;
    }
}

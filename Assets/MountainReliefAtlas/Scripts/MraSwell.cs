using UnityEngine;

// The Ribwood's swell ledge: the rock art around it rises and settles slowly, like ground heaving
// in a long tremor. Cosmetic only: the colliders stay where the route data puts them.
public sealed class MraSwell : MonoBehaviour
{
    public float amplitude = .35f, period = 8f;
    Vector3 rest;
    void Awake() { rest = transform.localPosition; SfxEmitter.Attach(gameObject, "Swell_Loop"); }
    void Update() => transform.localPosition = rest + Vector3.up * amplitude * Mathf.Sin(Time.time * 2f * Mathf.PI / Mathf.Max(.1f, period));
}

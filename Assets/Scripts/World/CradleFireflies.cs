using UnityEngine;

public sealed class CradleFireflies : MonoBehaviour
{
    public Transform[] lights = new Transform[0];
    Vector3[] rest;
    void Awake()
    {
        rest = new Vector3[lights.Length];
        for (int i = 0; i < lights.Length; i++) if (lights[i]) rest[i] = lights[i].localPosition;
    }
    void Update()
    {
        for (int i = 0; i < lights.Length; i++) if (lights[i])
            lights[i].localPosition = rest[i] + new Vector3(Mathf.Sin(Time.time * .47f + i * 2.4f) * .32f, Mathf.Sin(Time.time * .71f + i) * .22f, 0);
    }
}

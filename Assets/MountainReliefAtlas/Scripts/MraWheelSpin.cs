using UnityEngine;

// The wheel art of a lever-like control (Mill_Wheel standing in as a winch or sluice wheel): it
// turns once when the control is worked.
[RequireComponent(typeof(MraLever))]
public sealed class MraWheelSpin : MonoBehaviour
{
    public Transform wheel;
    MraLever lever; bool wasOn; float turn;

    void Awake() => lever = GetComponent<MraLever>();

    void Update()
    {
        if (lever.IsOn && !wasOn) turn = 360f;
        wasOn = lever.IsOn;
        if (turn <= 0f || wheel == null) return;
        float step = Mathf.Min(turn, 300f * Time.deltaTime);
        turn -= step; wheel.Rotate(0f, 0f, -step);
    }
}

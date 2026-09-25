using System.Collections.Generic;
using UnityEngine;

// Something a switch can open: gates, bridges, doors.
public interface IMechanismTarget { void SetOpen(bool open); }

// Base for switches: forwards its state to every target.
public abstract class MechanismSwitch : MonoBehaviour
{
    public List<MonoBehaviour> targets = new List<MonoBehaviour>();

    protected void Signal(bool on)
    {
        foreach (MonoBehaviour target in targets) if (target is IMechanismTarget t) t.SetOpen(on);
    }
}

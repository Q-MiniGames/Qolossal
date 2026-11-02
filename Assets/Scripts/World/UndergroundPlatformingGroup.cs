using System.Collections.Generic;
using UnityEngine;

// Authoring metadata only: marks a room's greybox platforming group and the old platforms it switched off.
[DisallowMultipleComponent]
public sealed class UndergroundPlatformingGroup : MonoBehaviour
{
    [TextArea] public string note;
    public List<GameObject> switchedOff = new List<GameObject>();
}

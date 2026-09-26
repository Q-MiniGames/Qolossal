using System.Collections.Generic;
using UnityEngine;

// A cutout creature rig built by CreatureRigBuilder from Tools/CreatureRigAuthoring/creature_rig.py:
// named bones under a Facing pivot, each with its sprite, plus named points (e.g. a mouth).
// Enemy animators pose the bones and tint the renderers through this component.
[DisallowMultipleComponent]
public sealed class CreatureRig : MonoBehaviour
{
    public Transform facing;
    public string[] boneNames = new string[0];
    public Transform[] bones = new Transform[0];
    public float[] lengths = new float[0];
    public SpriteRenderer[] renderers = new SpriteRenderer[0];
    public string[] pointNames = new string[0];
    public Transform[] points = new Transform[0];

    Color[] baseColors;
    readonly Dictionary<string, Quaternion> restRotation = new Dictionary<string, Quaternion>();
    readonly Dictionary<string, Vector3> restPosition = new Dictionary<string, Vector3>(), restScale = new Dictionary<string, Vector3>();

    void Awake() => CaptureRest();

    public void CaptureRest()
    {
        if (baseColors != null) return;
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        for (int i = 0; i < bones.Length; i++)
        {
            restRotation[boneNames[i]] = bones[i].localRotation;
            restPosition[boneNames[i]] = bones[i].localPosition;
            restScale[boneNames[i]] = bones[i].localScale;
        }
    }

    public Transform Bone(string name) { int i = System.Array.IndexOf(boneNames, name); return i >= 0 ? bones[i] : null; }
    public float Length(string name) { int i = System.Array.IndexOf(boneNames, name); return i >= 0 ? lengths[i] : 0f; }
    public Transform Point(string name) { int i = System.Array.IndexOf(pointNames, name); return i >= 0 ? points[i] : null; }

    // Sets a bone to its rest pose plus an extra rotation (degrees), offset and scale.
    public void Pose(string name, float degrees, Vector2 offset = default, Vector2? scale = null)
    {
        Transform b = Bone(name); if (b == null) return;
        b.localRotation = restRotation[name] * Quaternion.Euler(0f, 0f, degrees);
        b.localPosition = restPosition[name] + (Vector3)offset;
        Vector3 s = restScale[name]; Vector2 k = scale ?? Vector2.one;
        b.localScale = new Vector3(s.x * k.x, s.y * k.y, s.z);
    }

    public void Face(float direction) { if (facing != null) facing.localScale = new Vector3(direction < 0f ? -1f : 1f, 1f, 1f); }

    public void Tint(Color tint)
    {
        CaptureRest();
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].color = baseColors[i] * tint;
    }

    // Hit flash, death grey, or plain.
    public static Color FeedbackTint(bool alive, bool flashing) =>
        !alive ? new Color(.55f, .55f, .55f, 1f) : flashing ? new Color(1.6f, 1.6f, 1.6f, 1f) : Color.white;
}

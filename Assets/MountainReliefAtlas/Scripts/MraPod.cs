using UnityEngine;

// A springy training pod for Bloomfall: a downward strike bounces Qori off it (the hit supports
// pogo), and it squashes and recovers. It can't be destroyed, so the trial always resets.
[RequireComponent(typeof(Collider2D))]
public sealed class MraPod : MonoBehaviour, ICombatDamageReceiver
{
    public Transform art;
    Vector3 scale = Vector3.one; float hitAt = -10f;
    public int Bounces { get; private set; }

    void Awake() { if (art != null) scale = art.localScale; }

    public CombatDamageResponse ReceiveCombatHit(CombatDamage hit)
    {
        hitAt = Time.time; Bounces++;
        return CombatDamageResponse.Applied(0f, pogo: true);
    }

    void Update()
    {
        if (art == null) return;
        float k = Mathf.Clamp01((Time.time - hitAt) / .35f);
        float squash = (1f - k) * .25f * Mathf.Cos(k * Mathf.PI * 2f);
        art.localScale = new Vector3(scale.x * (1f + squash), scale.y * (1f - squash), scale.z);
    }
}

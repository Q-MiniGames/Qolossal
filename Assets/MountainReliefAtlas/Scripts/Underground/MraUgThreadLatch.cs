using UnityEngine;

// A thread latch (the Bell Arch's pull ring): a ThreadAnchor that saves `flag` the moment Qori's
// Living Thread holds it. The gate it opens listens for the flag (MraGate.requires).
[RequireComponent(typeof(ThreadAnchor))]
public sealed class MraUgThreadLatch : MonoBehaviour
{
    public string flag;
    public string doneText = "Something heavy slides aside.";
    public SpriteRenderer ring;
    PlayerThread thread;

    public bool Done => GameSave.HasFlag(flag);

    void Start() { if (Done && ring != null) ring.color = new Color(1f, .86f, .5f); }

    void Update()
    {
        if (Done) return;
        if (thread == null) { var q = FindAnyObjectByType<PlayerThread>(); if (q == null) return; thread = q; }
        if (!thread.IsAttached || (thread.AnchorPosition - (Vector2)transform.position).sqrMagnitude > .01f) return;
        Pull();
    }

    // The thread calls this through Update; so can tests.
    public void Pull()
    {
        if (!GameSave.SetFlag(flag)) return;
        Sfx.Play("Gate_Open", transform.position);
        if (!string.IsNullOrEmpty(doneText)) TownHud.Toast(doneText);
        if (ring != null) ring.color = new Color(1f, .86f, .5f);
    }
}

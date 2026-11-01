using UnityEngine;

// G-04, the Root Matron (C08): a rooted mother-plant. She lashes a root along the floor (a wave rolling away from her:
// jump it, or swing over it on the arena's anchors). Phase two alternates the lash with a shower of falling seeds.
// Phase three does both at once. Her bloom opens after each attack.
public sealed class RootMatronBoss : UndergroundGuardian
{
    [SerializeField] float lashSpeed = 10f, lashRange = 34f;
    [SerializeField, Min(1)] int seedCount = 6;
    protected override string DesignId => "G-04";
    protected override Vector2 WeakOffset => new Vector2(1.5f, 4f);
    protected override Vector2 BodySize => new Vector2(6.5f, 5.5f);
    protected override int NextAttack(int n) => Phase == 1 ? 0 : Phase == 2 ? n % 2 : 2;
    protected override void OnStrike(int a)
    {
        if (a != 1) ThornShockwave.Launch(Feet + new Vector2(facing * BodySize.x * .5f, 0f), facing, lashSpeed * Speed, lashRange);
        if (a != 0)
            for (int i = 0; i < seedCount; i++)
            {
                float x = Random.Range(Room.min.x + 2f, Feet.x - facing * 0f);
                var p = Player; if (p != null && i == 0) x = p.transform.position.x;   // one always falls on Qori
                UgFallingSeed.Drop(new Vector2(Mathf.Clamp(x, Room.min.x + 1f, Room.max.x - 1f), Room.max.y - 1f), i * .18f);
            }
    }
}

using UnityEngine;

// M-03, the Grain Golem (Mill Undercroft): a millstone body. Its fists slam the floor and send a wave each way (jump
// them); from phase two it also sheds a shower of grain stones from above. Its core opens after each attack.
public sealed class GrainGolemBoss : UndergroundGuardian
{
    protected override string DesignId => "M-03";
    protected override int Hits => 12;
    protected override Vector2 WeakOffset => new Vector2(.2f, 2.4f);
    protected override Vector2 BodySize => new Vector2(2.6f, 3.8f);
    protected override int NextAttack(int n) => Phase == 1 ? 0 : n % 2;
    protected override void OnStrike(int a)
    {
        if (a == 0)
        {
            ThornShockwave.Launch(Feet + new Vector2(1.4f, 0f), 1f, 9f * Speed, 20f);
            ThornShockwave.Launch(Feet - new Vector2(1.4f, 0f), -1f, 9f * Speed, 20f);
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            var p = Player;
            float x = i == 0 && p != null ? p.transform.position.x : Random.Range(Room.min.x + 2f, Room.max.x - 2f);
            UgFallingSeed.Drop(new Vector2(x, Room.max.y - 1f), i * .2f);
        }
    }
}

using UnityEngine;

// Where the staff is drawn in Qori's swing frames, marked by eye on gridded sheets (3 Nov), in frame pixels (y down):
// the near hand to the leaf's tip. PlayerCombat hits along this line, so what the player sees is what hits. A redrawn
// clip needs its rows marked again; frames before the first row come before the hit, frames after the last hold it.
// Moves with a shape of their own (spin, lunge, plunge, charged sweep) don't use this.
public static class QoriFrameHitShapes
{
    public readonly struct Line
    {
        public readonly Vector2 Hand, Tip;
        public Line(float hx, float hy, float tx, float ty) { Hand = new Vector2(hx, hy); Tip = new Vector2(tx, ty); }
    }

    // ArtLudo/SwingTrail, 1216 x 744: rows for frames 4 to 13.
    public const int Combo1First = 4;
    public static readonly Line[] Combo1 =
    {
        new Line(640, 385, 826, 374), new Line(656, 369, 899, 313), new Line(656, 369, 899, 301), new Line(656, 369, 904, 289),
        new Line(668, 372, 911, 274), new Line(656, 379, 879, 282), new Line(656, 384, 879, 291), new Line(607, 396, 838, 335),
        new Line(607, 389, 782, 345), new Line(583, 381, 765, 320),
    };

    // ArtLudo/Combo2, 886 x 784: rows for frames 1 to 5 (the staff is lost in the white arc from frame 2: the arc's line).
    public const int Combo2First = 1;
    public static readonly Line[] Combo2 =
    {
        new Line(482, 427, 675, 415), new Line(485, 491, 675, 422), new Line(416, 509, 656, 451),
        new Line(364, 525, 631, 471), new Line(296, 516, 587, 498),
    };

    // ArtLudo/UpAttackTrail, 942 x 824: rows for frames 3 to 9, from behind over the head to in front.
    public const int UpAttackFirst = 3;
    public static readonly Line[] UpAttack =
    {
        new Line(339, 282, 273, 235), new Line(448, 288, 437, 194), new Line(527, 420, 599, 232), new Line(499, 429, 725, 410),
        new Line(490, 435, 735, 444), new Line(490, 433, 731, 433), new Line(490, 433, 731, 424),
    };
}

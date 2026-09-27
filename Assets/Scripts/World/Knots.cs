// The seven knots where the titan's life gathers, one per region (world design, section 1.3).
// Waking one makes the titan stir, which changes earlier levels (StirVariant).
public static class Knots
{
    public const string Grip = "grip", Reach = "reach", Breath = "breath", Spring = "spring", Bloom = "bloom", Sight = "sight", Heart = "heart";
    public static readonly string[] All = { Grip, Reach, Breath, Spring, Bloom, Sight, Heart };

    public static string DisplayName(string knot) => knot switch
    {
        Grip => "the Grip Knot", Reach => "the Reach Knot", Breath => "the Breath Knot", Spring => "the Spring Knot",
        Bloom => "the Bloom Knot", Sight => "the Sight Knot", Heart => "the Heart", _ => knot,
    };

    // What the titan does when the knot wakes, for the stir caption.
    public static string Stir(string knot) => knot switch
    {
        Grip => "Its hand clenches.", Reach => "Its forearm lifts.", Breath => "It breathes.", Spring => "Its legs flex.",
        Bloom => "Its crown flowers.", Sight => "Its eye opens.", Heart => "It wakes.", _ => "",
    };
}

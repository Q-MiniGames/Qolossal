using System;

// One distance-driven clock; neither foot can independently accelerate or restart.
public sealed class QoriGroundCycle
{
    public double Phase { get; private set; }
    public void Reset() { Phase = 0; }
    public void Advance(double distance, double strideDistance)
    {
        Phase = Wrap(Phase + Math.Abs(distance) / Math.Max(.5, strideDistance));
    }
    public Sample Evaluate(int leg, double contactFraction)
    {
        double p = Wrap(Phase + (leg == 0 ? 0 : .5));
        // Overlapping support phases guarantee weight passes between feet rather
        // than leaving both suspended for most of an otherwise grounded walk.
        double stance = Math.Max(.5, Math.Min(.65, contactFraction));
        if (p < stance) return new Sample(p, true, 1 - 2 * p / stance, 0);
        double t = (p - stance) / (1 - stance);
        double ease = t * t * (3 - 2 * t);
        // Rounded recovery arc; flat endpoints avoid toe snapping at contact.
        double lift = Math.Sin(Math.PI * t);
        return new Sample(p, false, -1 + 2 * ease, lift * lift);
    }
    static double Wrap(double value) => value - Math.Floor(value);
    public readonly struct Sample
    {
        public readonly double phase, horizontal, lift;
        public readonly bool contact;
        public Sample(double phase, bool contact, double horizontal, double lift)
        { this.phase=phase;this.contact=contact;this.horizontal=horizontal;this.lift=lift; }
    }
}

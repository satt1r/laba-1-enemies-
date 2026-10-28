namespace TimedClicker.Core;

public class ClickableCircle
{
    public double X { get; }
    public double Y { get; }
    public double Radius { get; }
    public double Remaining { get; private set; }
    public double Points { get; }

    public ClickableCircle(double x, double y, double radius, double lifetime)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(radius) || radius <= 0 ||
            !double.IsFinite(lifetime) || lifetime <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius));
        X = x;
        Y = y;
        Radius = radius;
        Remaining = lifetime;
        Points = 1000 / (radius * 2 * lifetime);
    }

    public bool Contains(double x, double y) =>
        (x - X) * (x - X) + (y - Y) * (y - Y) <= Radius * Radius;

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining = Math.Max(0, Remaining - seconds);
    }
}

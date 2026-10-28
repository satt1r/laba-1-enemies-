namespace TimedClicker.Core;

public abstract class Collectable
{
    public double X { get; }
    public double Y { get; }
    public double Radius { get; }
    public double Remaining { get; private set; }
    public bool IsCollected { get; private set; }
    public abstract string Label { get; }
    public abstract string Color { get; }

    protected Collectable(double x, double y, double radius, double lifetime)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(radius) || radius <= 0 ||
            !double.IsFinite(lifetime) || lifetime <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius));
        X = x;
        Y = y;
        Radius = radius;
        Remaining = lifetime;
    }

    public bool TryCollect()
    {
        if (IsCollected || Remaining <= 0) return false;
        IsCollected = true;
        OnClick();
        return true;
    }

    // Конкретный эффект обязателен у каждого неабстрактного наследника.
    protected abstract void OnClick();

    public bool Contains(double x, double y) =>
        (x - X) * (x - X) + (y - Y) * (y - Y) <= Radius * Radius;

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining = Math.Max(0, Remaining - seconds);
    }
}

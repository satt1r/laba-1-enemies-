namespace TimedClicker.Core;

// Отсчёт времени не зависит ни от игрока, ни от WPF-таймера.
public sealed class Cooldown
{
    public double Remaining { get; private set; }
    public bool IsReady => Remaining <= 0;

    public void Start(double seconds)
    {
        Validate(seconds);
        Remaining = seconds;
    }

    public void Update(double seconds)
    {
        Validate(seconds);
        Remaining = Math.Max(0, Remaining - seconds);
    }

    private static void Validate(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
    }
}

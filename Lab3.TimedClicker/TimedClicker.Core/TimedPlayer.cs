namespace TimedClicker.Core;

public sealed class TimedPlayer
{
    public Cooldown Cooldown { get; } = new();
    public double ClickInterval { get; private set; } = 0.5;
    public void ReduceCooldown() => ClickInterval = Math.Max(0.1, ClickInterval * 0.8);
}

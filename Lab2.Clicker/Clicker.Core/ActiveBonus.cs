namespace Clicker.Core;

public abstract class ActiveBonus
{
    public double Remaining { get; private set; }
    public abstract string Name { get; }
    // Нейтральная реализация подходит бонусу, который не меняет этот параметр.
    public virtual int DamageFactor => 1;
    public virtual double CooldownFactor => 1;

    protected ActiveBonus(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining = seconds;
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining = Math.Max(0, Remaining - seconds);
    }
}

public sealed class DamageBonus : ActiveBonus
{
    public DamageBonus(double seconds = 8) : base(seconds) { }
    public override string Name => "Урон ×2";
    public override int DamageFactor => 2;
}

public sealed class HasteBonus : ActiveBonus
{
    public HasteBonus(double seconds = 8) : base(seconds) { }
    public override string Name => "Перезарядка ÷2";
    public override double CooldownFactor => 0.5;
}

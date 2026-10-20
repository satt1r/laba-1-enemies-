using System.Globalization;

namespace Clicker.Core;

public sealed class EnemyTemplate
{
    private static readonly BigNumber Zero = new("0");
    private static readonly BigNumber One = new("1");
    private readonly BigNumber lifeNumerator;
    private readonly BigNumber lifeDenominator;
    private readonly BigNumber goldNumerator;
    private readonly BigNumber goldDenominator;

    public string Name { get; }
    public string IconName { get; }
    public BigNumber BaseLife { get; }
    public BigNumber BaseGold { get; }
    public decimal LifeModifier { get; }
    public decimal GoldModifier { get; }
    public double SpawnChance { get; }

    public EnemyTemplate(string name, string iconName, BigNumber baseLife, decimal lifeModifier,
        BigNumber baseGold, decimal goldModifier, double spawnChance)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Нужно указать имя шаблона.", nameof(name));
        if (string.IsNullOrWhiteSpace(iconName))
            throw new ArgumentException("Нужно указать имя иконки.", nameof(iconName));
        ArgumentNullException.ThrowIfNull(baseLife);
        ArgumentNullException.ThrowIfNull(baseGold);
        if (lifeModifier < 0)
            throw new ArgumentOutOfRangeException(nameof(lifeModifier));
        if (goldModifier < 0)
            throw new ArgumentOutOfRangeException(nameof(goldModifier));
        if (!double.IsFinite(spawnChance) || spawnChance < 0)
            throw new ArgumentOutOfRangeException(nameof(spawnChance));

        Name = name;
        IconName = iconName;
        BaseLife = baseLife;
        BaseGold = baseGold;
        LifeModifier = lifeModifier;
        GoldModifier = goldModifier;
        SpawnChance = spawnChance;
        (lifeNumerator, lifeDenominator) = SplitModifier(lifeModifier);
        (goldNumerator, goldDenominator) = SplitModifier(goldModifier);
    }

    internal BigNumber ScaleLife(BigNumber current)
    {
        BigNumber scaled = current * lifeNumerator / lifeDenominator;
        return scaled == Zero ? One : scaled;
    }

    internal BigNumber ScaleGold(BigNumber current) => current * goldNumerator / goldDenominator;

    private static (BigNumber Numerator, BigNumber Denominator) SplitModifier(decimal modifier)
    {
        // decimal используется только для коэффициента из JSON, не для здоровья или золота.
        string text = modifier.ToString(CultureInfo.InvariantCulture);
        int point = text.IndexOf('.');
        if (point < 0)
            return (new BigNumber(text), One);

        string digits = text.Remove(point, 1);
        string denominator = "1" + new string('0', text.Length - point - 1);
        return (new BigNumber(digits), new BigNumber(denominator));
    }
}

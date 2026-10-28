using TimedClicker.Core;

namespace Clicker.Core;

public sealed class Player
{
    private static readonly BigNumber Zero = new("0");
    private static readonly BigNumber One = new("1");

    public BigNumber Gold { get; private set; }
    public BigNumber Damage { get; private set; }
    public BigNumber UpgradeCost { get; private set; }
    public BigNumber Level { get; private set; }
    public Cooldown AttackCooldown { get; } = new();
    public double AttackInterval { get; private set; } = 0.5;
    public BigNumber CooldownUpgradeCost { get; private set; } = new("100");
    public bool CanUpgradeCooldown => AttackInterval > 0.1 && Gold >= CooldownUpgradeCost;

    public bool TryUpgradeCooldown()
    {
        if (!CanUpgradeCooldown) return false;
        Gold -= CooldownUpgradeCost;
        AttackInterval = Math.Max(0.1, AttackInterval * 0.8);
        CooldownUpgradeCost *= 2;
        return true;
    }

    public Player() : this(new BigNumber("0"), new BigNumber("10"), new BigNumber("100"))
    {
    }

    public Player(BigNumber gold, BigNumber damage, BigNumber upgradeCost)
    {
        ArgumentNullException.ThrowIfNull(gold);
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(upgradeCost);

        if (damage == Zero)
            throw new ArgumentOutOfRangeException(nameof(damage), "Урон должен быть больше нуля.");
        if (upgradeCost == Zero)
            throw new ArgumentOutOfRangeException(nameof(upgradeCost), "Цена улучшения должна быть больше нуля.");

        Gold = gold;
        Damage = damage;
        UpgradeCost = upgradeCost;
        Level = One;
    }

    public void AddGold(BigNumber amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        Gold = Gold + amount;
    }

    public bool TryUpgrade()
    {
        if (Gold < UpgradeCost)
            return false;

        // 12 / 10 представляет коэффициент 1.2 без перевода числа в double.
        BigNumber nextDamage = Damage * 12 / 10;
        BigNumber nextCost = UpgradeCost * 12 * Level / 10;

        // При небольших значениях округление вниз не должно останавливать рост.
        if (nextDamage <= Damage)
            nextDamage = Damage + One;
        if (nextCost <= UpgradeCost)
            nextCost = UpgradeCost + One;

        BigNumber remainingGold = Gold - UpgradeCost;
        BigNumber nextLevel = Level + One;

        Gold = remainingGold;
        Damage = nextDamage;
        UpgradeCost = nextCost;
        Level = nextLevel;
        return true;
    }
}

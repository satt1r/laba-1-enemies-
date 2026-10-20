namespace Clicker.Core;

public sealed class Enemy
{
    private static readonly BigNumber Zero = new("0");

    public string Name { get; }
    public string IconName { get; }
    public BigNumber MaxHealth { get; }
    public BigNumber Health { get; private set; }
    public BigNumber GoldReward { get; private set; }
    public bool IsDefeated => Health == Zero;

    public Enemy(string name, string iconName, BigNumber health, BigNumber goldReward)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Нужно указать имя противника.", nameof(name));
        if (string.IsNullOrWhiteSpace(iconName))
            throw new ArgumentException("Нужно указать имя иконки.", nameof(iconName));
        ArgumentNullException.ThrowIfNull(health);
        ArgumentNullException.ThrowIfNull(goldReward);
        if (health == Zero)
            throw new ArgumentOutOfRangeException(nameof(health), "Здоровье должно быть больше нуля.");

        Name = name;
        IconName = iconName;
        MaxHealth = health;
        Health = health;
        GoldReward = goldReward;
    }

    // true означает, что именно эта атака победила противника.
    public bool TakeDamage(BigNumber damage, out BigNumber reward)
    {
        ArgumentNullException.ThrowIfNull(damage);
        reward = Zero;

        if (IsDefeated)
            return false;

        if (damage >= Health)
        {
            Health = Zero;
            reward = GoldReward;
            return true;
        }

        Health = Health - damage;
        return false;
    }
}

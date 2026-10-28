namespace Clicker.Core;

public sealed class GameSession
{
    private static readonly BigNumber One = new("1");
    private readonly EnemyCatalog catalog;
    private readonly List<ActiveBonus> bonuses = new();
    public IReadOnlyList<ActiveBonus> Bonuses { get; }
    public BonusController BonusController { get; }
    public bool CanAttack => Player.AttackCooldown.IsReady;
    public BigNumber EffectiveDamage => bonuses.Aggregate(Player.Damage, (damage, bonus) => damage * bonus.DamageFactor);
    public double EffectiveAttackInterval => Math.Max(0.05,
        bonuses.Aggregate(Player.AttackInterval, (interval, bonus) => interval * bonus.CooldownFactor));

    public void AddBonus(ActiveBonus bonus)
    {
        ArgumentNullException.ThrowIfNull(bonus);
        if (bonus.Remaining > 0 && !bonuses.Contains(bonus)) bonuses.Add(bonus);
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Player.AttackCooldown.Update(seconds);
        foreach (var bonus in bonuses) bonus.Update(seconds);
        bonuses.RemoveAll(bonus => bonus.Remaining <= 0);
        BonusController.Update(seconds);
    }

    public Player Player { get; }
    public Enemy CurrentEnemy { get; private set; }
    public BigNumber Stage => catalog.Stage;
    public BigNumber DefeatedEnemies { get; private set; } = new("0");
    public bool CanUpgrade => Player.Gold >= Player.UpgradeCost;

    public GameSession(EnemyCatalog catalog, Player? player = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        this.catalog = catalog;
        Player = player ?? new Player();
        CurrentEnemy = catalog.CreateEnemy();
        Bonuses = bonuses.AsReadOnly();
        BonusController = new BonusController(this);
    }

    public bool Attack(out BigNumber reward)
    {
        reward = new BigNumber("0");
        if (!CanAttack) return false;
        Player.AttackCooldown.Start(EffectiveAttackInterval);
        if (!CurrentEnemy.TakeDamage(EffectiveDamage, out reward))
            return false;

        Player.AddGold(reward);
        DefeatedEnemies = DefeatedEnemies + One;
        catalog.Advance();
        CurrentEnemy = catalog.CreateEnemy();
        return true;
    }

    public bool TryUpgrade() => Player.TryUpgrade();
}

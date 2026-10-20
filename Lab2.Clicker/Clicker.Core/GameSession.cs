namespace Clicker.Core;

public sealed class GameSession
{
    private static readonly BigNumber One = new("1");
    private readonly EnemyCatalog catalog;

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
    }

    public bool Attack(out BigNumber reward)
    {
        if (!CurrentEnemy.TakeDamage(Player.Damage, out reward))
            return false;

        Player.AddGold(reward);
        DefeatedEnemies = DefeatedEnemies + One;
        catalog.Advance();
        CurrentEnemy = catalog.CreateEnemy();
        return true;
    }

    public bool TryUpgrade() => Player.TryUpgrade();
}

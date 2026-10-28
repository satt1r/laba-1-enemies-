using Clicker.Core;

namespace Clicker.Checks;

internal static class SessionChecks
{
    public static int Run()
    {
        int passed = 0;
        var catalog = EnemyCatalog.FromJson("""
            [{ "Name": "Enemy", "IconName": "car.png", "BaseLife": 25, "LifeModifier": 2,
               "BaseGold": 15, "GoldModifier": 2, "SpawnChance": 1 }]
            """);
        var player = new Player(new BigNumber("0"), new BigNumber("10"), new BigNumber("10"));
        var game = new GameSession(catalog, player);
        Enemy firstEnemy = game.CurrentEnemy;
        Check(!game.CanUpgrade, "A new player must not afford an upgrade.");
        Check(!game.TryUpgrade(), "An unaffordable session upgrade must fail.");
        Check(!Attack(game, out BigNumber reward), "The first attack must be nonlethal.");
        CheckText(reward, "0");
        CheckText(game.CurrentEnemy.Health, "15");
        CheckText(game.Player.Gold, "0");
        CheckText(game.Stage, "1");
        CheckText(game.DefeatedEnemies, "0");
        Check(ReferenceEquals(firstEnemy, game.CurrentEnemy), "A nonlethal attack must keep the same enemy.");
        Check(!Attack(game, out _), "The second attack must be nonlethal.");
        Check(Attack(game, out reward), "The third attack must win.");
        CheckText(reward, "15");
        CheckText(game.Player.Gold, "15");
        CheckText(game.Stage, "2");
        CheckText(game.DefeatedEnemies, "1");
        CheckText(game.CurrentEnemy.Health, "50");
        CheckText(game.CurrentEnemy.GoldReward, "30");
        Check(!ReferenceEquals(firstEnemy, game.CurrentEnemy), "Victory must spawn a new enemy.");
        Check(firstEnemy.IsDefeated, "The previous enemy must remain defeated.");
        Check(!firstEnemy.TakeDamage(player.Damage, out reward), "The previous enemy must not award a second reward.");
        CheckText(reward, "0");
        CheckText(game.Player.Gold, "15");
        Check(game.CanUpgrade, "Victory gold must enable upgrades.");
        Check(game.TryUpgrade(), "Buying an affordable upgrade must succeed.");
        CheckText(game.Player.Gold, "5");
        CheckText(game.Player.Damage, "12");
        CheckText(game.Player.UpgradeCost, "12");
        CheckText(game.Stage, "2");
        for (int i = 0; i < 4; i++)
            Check(!Attack(game, out _), "The second enemy must survive its first four upgraded attacks.");
        Check(Attack(game, out reward), "The fifth upgraded attack must defeat the second enemy.");
        CheckText(reward, "30");
        CheckText(game.Player.Gold, "35");
        CheckText(game.DefeatedEnemies, "2");
        CheckText(game.Stage, "3");
        CheckText(game.CurrentEnemy.Health, "100");
        CheckText(game.CurrentEnemy.GoldReward, "60");

        string samplePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "enemies.sample.json");
        var sampleGame = new GameSession(EnemyCatalog.LoadFromJson(samplePath));
        int victories = 0;
        for (int i = 0; i < 100; i++)
        {
            if (Attack(sampleGame, out reward))
                victories++;
        }
        Check(victories == 10, "The real editor sample must allow ten victories in one hundred clicks.");
        CheckText(sampleGame.Player.Gold, "100");
        CheckText(sampleGame.DefeatedEnemies, "10");
        CheckText(sampleGame.Stage, "11");
        Check(sampleGame.CanUpgrade, "The sample game must enable the first purchase.");
        Check(sampleGame.TryUpgrade(), "The sample game must support the first purchase.");
        CheckText(sampleGame.Player.Gold, "0");
        CheckText(sampleGame.Player.Damage, "12");

        try
        {
            _ = new GameSession(null!);
            throw new InvalidOperationException("A null catalog must be rejected.");
        }
        catch (ArgumentNullException)
        {
            passed++;
        }

        return passed;

        bool Attack(GameSession target, out BigNumber reward)
        {
            target.Update(target.Player.AttackCooldown.Remaining);
            return target.Attack(out reward);
        }

        void CheckText(BigNumber actual, string expected) =>
            Check(actual.ToString() == expected, $"Expected {expected}, got {actual}.");

        void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            passed++;
        }
    }
}

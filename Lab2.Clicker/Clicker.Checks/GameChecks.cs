using Clicker.Core;

namespace Clicker.Checks;

internal static class GameChecks
{
    public static int Run()
    {
        int passed = 0;
        var zero = new BigNumber("0");
        var one = new BigNumber("1");

        var defaults = new Player();
        CheckPlayer(defaults, "0", "10", "100", "1");
        Check(!defaults.TryUpgrade(), "An unaffordable upgrade must fail.");
        CheckPlayer(defaults, "0", "10", "100", "1");
        defaults.AddGold(zero);
        CheckPlayer(defaults, "0", "10", "100", "1");

        var initialGold = new BigNumber("1000");
        var initialDamage = new BigNumber("10");
        var initialCost = new BigNumber("100");
        var player = new Player(initialGold, initialDamage, initialCost);
        Check(player.TryUpgrade(), "The first upgrade must succeed.");
        CheckPlayer(player, "900", "12", "120", "2");
        Check(player.TryUpgrade(), "The second upgrade must succeed.");
        CheckPlayer(player, "780", "14", "288", "3");
        Check(player.TryUpgrade(), "The third upgrade must succeed.");
        CheckPlayer(player, "492", "16", "1036", "4");
        Check(!player.TryUpgrade(), "An unaffordable later upgrade must fail.");
        CheckPlayer(player, "492", "16", "1036", "4");
        player.AddGold(new BigNumber("544"));
        Check(player.TryUpgrade(), "An upgrade with exactly enough gold must succeed.");
        CheckPlayer(player, "0", "19", "4972", "5");
        CheckText(initialGold, "1000");
        CheckText(initialDamage, "10");
        CheckText(initialCost, "100");

        var tiny = new Player(one, one, one);
        Check(tiny.TryUpgrade(), "Small values must not stall after rounding.");
        CheckPlayer(tiny, "0", "2", "2", "2");

        string hugeText = "1" + new string('0', 80);
        var huge = new BigNumber(hugeText);
        var wealthy = new Player(huge, huge, huge);
        Check(wealthy.TryUpgrade(), "An upgrade above the standard numeric range must succeed.");
        CheckPlayer(wealthy, "0", "12" + new string('0', 79), "12" + new string('0', 79), "2");
        wealthy.AddGold(huge);
        CheckText(wealthy.Gold, hugeText);
        CheckText(huge, hugeText);

        var growing = new Player();
        for (int i = 0; i < 30; i++)
        {
            BigNumber oldCost = growing.UpgradeCost;
            BigNumber oldDamage = growing.Damage;
            growing.AddGold(oldCost);
            Check(growing.TryUpgrade(), "An upgrade must succeed after adding its full price.");
            Check(growing.UpgradeCost > oldCost, "Upgrade prices must increase.");
            Check(growing.Damage > oldDamage, "Damage must increase.");
            CheckText(growing.Gold, "0");
        }
        CheckText(growing.Level, "31");
        Check(growing.UpgradeCost > new BigNumber("9223372036854775807"), "Upgrade prices must grow beyond long.");

        var enemy = new Enemy("Enemy", "car.png", new BigNumber("10"), new BigNumber("25"));
        Check(!enemy.IsDefeated, "A new enemy must be alive.");
        Check(!enemy.TakeDamage(new BigNumber("4"), out BigNumber reward), "A nonlethal attack must return false.");
        CheckText(enemy.Health, "6");
        CheckText(reward, "0");
        Check(!enemy.TakeDamage(zero, out reward), "Zero damage must not defeat an enemy.");
        CheckText(enemy.Health, "6");
        CheckText(reward, "0");
        Check(enemy.TakeDamage(new BigNumber("6"), out reward), "Exact lethal damage must defeat an enemy.");
        Check(enemy.IsDefeated, "An enemy at zero health must be defeated.");
        CheckText(enemy.Health, "0");
        CheckText(enemy.MaxHealth, "10");
        CheckText(reward, "25");
        foreach (BigNumber damage in new[] { zero, one, huge })
        {
            Check(!enemy.TakeDamage(damage, out reward), "A defeated enemy must not award gold again.");
            CheckText(reward, "0");
            CheckText(enemy.Health, "0");
        }

        var overkill = new Enemy("Enemy", "car.png", new BigNumber("100"), huge);
        Check(overkill.TakeDamage(huge, out reward), "Overkill must defeat the enemy without negative health.");
        CheckText(overkill.Health, "0");
        CheckText(overkill.MaxHealth, "100");
        CheckText(reward, hugeText);
        CheckText(huge, hugeText);

        var noReward = new Enemy("Enemy", "car.png", one, zero);
        Check(noReward.TakeDamage(one, out reward), "Zero reward must not prevent victory.");
        CheckText(reward, "0");

        var sharedHealth = new BigNumber("100");
        var first = new Enemy("First", "car.png", sharedHealth, one);
        var second = new Enemy("Second", "car.png", sharedHealth, one);
        Check(!first.TakeDamage(one, out _), "A small attack must leave the enemy alive.");
        CheckText(first.Health, "99");
        CheckText(first.MaxHealth, "100");
        CheckText(second.Health, "100");
        CheckText(sharedHealth, "100");

        // Полный бой: три атаки, одна награда, затем покупка улучшения.
        var fighter = new Player(zero, new BigNumber("10"), new BigNumber("10"));
        var target = new Enemy("Enemy", "car.png", new BigNumber("25"), new BigNumber("15"));
        for (int i = 0; i < 3; i++)
        {
            bool won = target.TakeDamage(fighter.Damage, out reward);
            Check(won == (i == 2), "Only the final attack must award victory.");
            if (won)
                fighter.AddGold(reward);
        }
        CheckText(fighter.Gold, "15");
        Check(fighter.TryUpgrade(), "Battle gold must pay for an upgrade.");
        CheckPlayer(fighter, "5", "12", "12", "2");
        if (target.TakeDamage(fighter.Damage, out reward))
            fighter.AddGold(reward);
        CheckText(fighter.Gold, "5");

        ExpectException<ArgumentNullException>(() => new Player(null!, one, one));
        ExpectException<ArgumentNullException>(() => new Player(zero, null!, one));
        ExpectException<ArgumentNullException>(() => new Player(zero, one, null!));
        ExpectException<ArgumentOutOfRangeException>(() => new Player(zero, zero, one));
        ExpectException<ArgumentOutOfRangeException>(() => new Player(zero, one, zero));
        ExpectException<ArgumentNullException>(() => player.AddGold(null!));
        ExpectException<ArgumentException>(() => new Enemy(" ", "car.png", one, zero));
        ExpectException<ArgumentException>(() => new Enemy("Enemy", "", one, zero));
        ExpectException<ArgumentNullException>(() => new Enemy("Enemy", "car.png", null!, zero));
        ExpectException<ArgumentNullException>(() => new Enemy("Enemy", "car.png", one, null!));
        ExpectException<ArgumentOutOfRangeException>(() => new Enemy("Enemy", "car.png", zero, zero));
        ExpectException<ArgumentNullException>(() => second.TakeDamage(null!, out _));
        CheckText(second.Health, "100");

        return passed;

        void CheckPlayer(Player current, string gold, string damage, string cost, string level)
        {
            CheckText(current.Gold, gold);
            CheckText(current.Damage, damage);
            CheckText(current.UpgradeCost, cost);
            CheckText(current.Level, level);
        }

        void CheckText(BigNumber actual, string expected)
        {
            Check(actual.ToString() == expected, $"Expected {expected}, got {actual}.");
        }

        void Check(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
            passed++;
        }

        void ExpectException<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                passed++;
                return;
            }

            throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
        }
    }
}

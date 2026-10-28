using Clicker.Core;
using TimedClicker.Core;

namespace Clicker.Checks;

internal static class BonusChecks
{
    public static int Run()
    {
        int passed = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            passed++;
        }
        GameSession Create() => new(EnemyCatalog.FromJson("""
            [{ "Name": "Target", "IconName": "car.png", "BaseLife": 1000, "LifeModifier": 1,
               "BaseGold": 10, "GoldModifier": 1, "SpawnChance": 1 }]
            """));

        var game = Create();
        game.Attack(out _);
        Check(game.CurrentEnemy.Health.ToString() == "990", "Ready attack deals base damage.");
        Check(!game.Attack(out var reward) && reward.ToString() == "0" &&
            game.CurrentEnemy.Health.ToString() == "990", "Immediate second attack must have no effect.");
        game.Update(0.25);
        Check(!game.CanAttack, "Half of cooldown is insufficient.");
        game.Update(0.25);
        Check(game.CanAttack, "Attack unlocks at exact cooldown boundary.");
        game.Attack(out _);
        Check(game.CurrentEnemy.Health.ToString() == "980", "Next attack deals damage again.");

        var damage = new DamageBonus(2);
        var haste = new HasteBonus(4);
        game.AddBonus(damage);
        game.AddBonus(haste);
        Check(game.EffectiveDamage.ToString() == "20" && game.EffectiveAttackInterval == 0.25,
            "Different effects work simultaneously.");
        game.AddBonus(damage);
        Check(game.Bonuses.Count == 2, "Same effect instance cannot be registered twice.");
        game.Update(0.5);
        game.Attack(out _);
        Check(game.CurrentEnemy.Health.ToString() == "960" && game.Player.AttackCooldown.Remaining == 0.25,
            "Buffed attack applies damage and starts shortened cooldown.");
        game.Update(1.5);
        Check(game.EffectiveDamage.ToString() == "10" && game.EffectiveAttackInterval == 0.25 && game.Bonuses.Count == 1,
            "Damage expiry does not remove still-active haste.");
        game.Update(2);
        Check(game.Bonuses.Count == 0 && game.EffectiveAttackInterval == 0.5, "All effects expire independently.");
        game.AddBonus(new DamageBonus(1));
        game.AddBonus(new DamageBonus(3));
        Check(game.EffectiveDamage.ToString() == "40", "Repeated damage bonuses multiply.");
        game.Update(1);
        Check(game.EffectiveDamage.ToString() == "20", "Earlier repeated bonus expires independently.");
        game.Update(2);

        var player = game.Player;
        Check(!player.TryUpgradeCooldown() && player.Gold.ToString() == "0", "Cannot buy without gold.");
        player.AddGold(new BigNumber("100"));
        Check(player.TryUpgradeCooldown() && player.Gold.ToString() == "0" && player.AttackInterval == 0.4 &&
            player.CooldownUpgradeCost.ToString() == "200", "Exact-price purchase changes interval and next price.");
        game.AddBonus(new HasteBonus(1));
        Check(game.EffectiveAttackInterval == 0.2, "Permanent and temporary upgrades combine.");
        game.Update(1);
        Check(game.EffectiveAttackInterval == 0.4, "Expiry preserves permanent upgrade.");
        player.AddGold(new BigNumber("100000000000000000000000000000000000000000"));
        for (int i = 0; i < 20; i++) player.TryUpgradeCooldown();
        var goldAtCap = player.Gold;
        Check(player.AttackInterval == 0.1 && !player.TryUpgradeCooldown() && player.Gold == goldAtCap,
            "Cooldown has a lower bound and does not charge at maximum level.");

        var sphereGame = Create();
        Collectable[] spheres = { new DamageSphere(sphereGame, 24, 24), new HasteSphere(sphereGame, 24, 24),
            new GoldSphere(sphereGame, 24, 24) };
        foreach (Collectable sphere in spheres)
        {
            Check(sphere.TryCollect(), "Every derived sphere can be collected through base type.");
            Check(!sphere.TryCollect(), "Repeated collection cannot duplicate a bonus.");
        }
        Check(sphereGame.EffectiveDamage.ToString() == "20" && sphereGame.EffectiveAttackInterval == 0.25 &&
            sphereGame.Player.Gold.ToString() == "50", "Three polymorphic effects have distinct outcomes.");
        var expired = new GoldSphere(sphereGame, 24, 24);
        expired.Update(6);
        Check(!expired.TryCollect() && sphereGame.Player.Gold.ToString() == "50", "Expired sphere cannot award gold.");
        var controller = new BonusController(sphereGame, new Random(12));
        controller.Update(3);
        Check(controller.Objects.Count == 1, "Bonus appears after spawn interval.");
        var spawned = controller.Objects[0];
        Check(spawned.X >= 24 && spawned.X <= 376 && spawned.Y >= 24 && spawned.Y <= 276,
            "Bonus stays within enemy scene.");
        Check(controller.Collect(spawned) && !controller.Collect(spawned) && controller.Objects.Count == 0,
            "Controller removes collected sphere exactly once.");
        Check(!controller.Collect(new GoldSphere(sphereGame, 24, 24)), "Foreign sphere is rejected.");
        controller.Update(3);
        spawned = controller.Objects[0];
        controller.Update(6);
        Check(!controller.Objects.Contains(spawned), "Controller removes expired sphere.");
        var fresh = Create();
        Check(fresh.Bonuses.Count == 0 && fresh.Player.Gold.ToString() == "0" && fresh.CanAttack,
            "New session starts with no lingering bonuses, gold or cooldown.");
        return passed;
    }
}

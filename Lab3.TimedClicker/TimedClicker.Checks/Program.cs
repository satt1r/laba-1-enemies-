using TimedClicker.Core;

int passed = 0;
void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException(message);
    passed++;
}
var game = new TimedGame(30, new Random(42));
Check(game.Objects.Count == 0, "No spheres before spawn interval.");
game.Update(1);
Check(game.Objects.Count == 1, "First sphere appears at one second.");
var sphere = game.Objects[0];
Check(sphere.X >= sphere.Radius && sphere.X + sphere.Radius <= TimedGame.Width &&
    sphere.Y >= sphere.Radius && sphere.Y + sphere.Radius <= TimedGame.Height, "Sphere must fit the scene.");
Check(!game.Click(-100, -100) && game.Score == 0, "Miss gives no points.");
Check(game.Click(sphere.X, sphere.Y) && game.Objects.Count == 0, "Hit removes sphere and starts cooldown.");
Check(!game.Click(sphere.X, sphere.Y), "Sphere cannot be collected twice.");
game.Update(1);
var expired = game.Objects[0];
game.Update(6);
Check(!game.Objects.Contains(expired), "Uncollected sphere expires.");
game.Update(100);
Check(!game.IsRunning && game.Objects.Count == 0 && !game.Click(0, 0), "Game ends, clears objects and rejects input.");
game.Update(100);
Check(game.Objects.Count == 0, "Spawning does not resume after game end.");
var largeStep = new TimedGame(30, new Random(7));
var smallSteps = new TimedGame(30, new Random(7));
largeStep.Update(8);
for (int i = 0; i < 32; i++) smallSteps.Update(0.25);
Check(largeStep.Objects.Select(x => (x.X, x.Y, x.Remaining)).SequenceEqual(
    smallSteps.Objects.Select(x => (x.X, x.Y, x.Remaining))), "Timer delay must preserve chronological spawning and expiry.");
var bonusGame = new TimedGame();
Collectable[] bonuses = {
    new PointsSphere(bonusGame, 20, 20, 10, 5, 50),
    new CooldownSphere(bonusGame, 20, 20, 10, 5),
    new LifetimeSphere(bonusGame, 20, 20, 10, 5),
    new SpawnSphere(bonusGame, 20, 20, 10, 5)
};
foreach (Collectable bonus in bonuses)
{
    Check(bonus.TryCollect(), "Polymorphic collection must apply each effect.");
    Check(!bonus.TryCollect(), "Each effect is applied once.");
}
Check(bonusGame.Score == 50, "Points sphere awards its points.");
Check(bonusGame.Player.ClickInterval == 0.4, "Cooldown bonus shortens interval.");
Check(bonusGame.LifetimeMultiplier == 1.25, "Lifetime bonus extends future spheres.");
Check(bonusGame.SpawnInterval == 0.8, "Spawn bonus shortens interval.");
var dead = new PointsSphere(bonusGame, 20, 20, 10, 1, 500);
dead.Update(1);
Check(!dead.TryCollect() && bonusGame.Score == 50, "Expired sphere gives no reward.");
var cooldown = new Cooldown();
cooldown.Start(0.5);
cooldown.Update(0.25);
Check(!cooldown.IsReady && cooldown.Remaining == 0.25, "Cooldown blocks until elapsed.");
cooldown.Update(0.25);
Check(cooldown.IsReady, "Cooldown unlocks at boundary.");
var blocked = new TimedGame(30, new Random(1));
blocked.Update(2);
var top = blocked.Objects[^1];
Check(blocked.Click(top.X, top.Y), "First click accepted.");
Check(!blocked.Click(0, 0) && !blocked.Player.Cooldown.IsReady, "Rapid click is blocked.");
blocked.Update(1);
Check(blocked.Player.Cooldown.IsReady, "Controller advances player cooldown.");
for (int i = 0; i < 100; i++)
{
    bonusGame.Player.ReduceCooldown(); bonusGame.AccelerateSpawning(); bonusGame.ExtendLifetime();
}
Check(bonusGame.Player.ClickInterval == 0.1 && bonusGame.SpawnInterval == 0.25 &&
    bonusGame.LifetimeMultiplier == 3, "Bonuses respect limits.");
try { cooldown.Update(double.NaN); throw new Exception("Invalid time accepted."); }
catch (ArgumentOutOfRangeException) { passed++; }
Console.WriteLine($"Passed: {passed} checks.");

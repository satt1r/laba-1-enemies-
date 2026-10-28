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
Check(game.Click(sphere.X, sphere.Y) && game.Score > 0 && game.Objects.Count == 0, "Hit scores and removes sphere.");
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
Console.WriteLine($"Passed: {passed} checks.");

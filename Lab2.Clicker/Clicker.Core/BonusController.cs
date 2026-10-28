using TimedClicker.Core;

namespace Clicker.Core;

public sealed class BonusController
{
    public const double Width = 400;
    public const double Height = 300;
    private readonly GameSession game;
    private readonly Random random;
    private readonly List<Collectable> objects = new();
    private double untilSpawn = 3;
    public IReadOnlyList<Collectable> Objects { get; }

    public BonusController(GameSession game, Random? random = null)
    {
        this.game = game;
        this.random = random ?? new Random();
        Objects = objects.AsReadOnly();
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        while (seconds > 0)
        {
            double step = Math.Min(seconds, untilSpawn);
            foreach (var item in objects) item.Update(step);
            objects.RemoveAll(item => item.Remaining <= 0);
            seconds -= step;
            untilSpawn -= step;
            if (untilSpawn <= 0)
            {
                double x = 24 + random.NextDouble() * (Width - 48);
                double y = 24 + random.NextDouble() * (Height - 48);
                // Выбор типа нужен только при создании; обработка списка полиморфна.
                objects.Add(random.Next(3) switch
                {
                    0 => new DamageSphere(game, x, y),
                    1 => new HasteSphere(game, x, y),
                    _ => new GoldSphere(game, x, y)
                });
                untilSpawn = 3;
            }
        }
    }

    public bool Collect(Collectable sphere)
    {
        if (!objects.Remove(sphere)) return false;
        return sphere.TryCollect();
    }
}

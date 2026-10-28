namespace TimedClicker.Core;

// Окно только передаёт время и координаты; появление, удаление и счёт принадлежат контроллеру.
public sealed class TimedGame
{
    private readonly List<Collectable> objects = new();
    private readonly Random random;
    private double untilSpawn = 1;
    public const double Width = 760;
    public const double Height = 420;
    public IReadOnlyList<Collectable> Objects { get; }
    public double Remaining { get; private set; }
    public double Score { get; private set; }
    public TimedPlayer Player { get; } = new();
    public double SpawnInterval { get; private set; } = 1;
    public double LifetimeMultiplier { get; private set; } = 1;

    public void AddPoints(double amount) => Score += amount;
    public void ExtendLifetime() => LifetimeMultiplier = Math.Min(3, LifetimeMultiplier + 0.25);
    public void AccelerateSpawning() => SpawnInterval = Math.Max(0.25, SpawnInterval * 0.8);
    public bool IsRunning => Remaining > 0;

    public TimedGame(double duration = 30, Random? random = null)
    {
        if (!double.IsFinite(duration) || duration <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration));
        Remaining = duration;
        this.random = random ?? new Random();
        Objects = objects.AsReadOnly();
    }

    public void Update(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        // Обрабатываем события по порядку даже после задержки UI-потока.
        while (seconds > 0 && IsRunning)
        {
            double step = Math.Min(seconds, Math.Min(Remaining, untilSpawn));
            Player.Cooldown.Update(step);
            foreach (var item in objects)
                item.Update(step);
            objects.RemoveAll(item => item.Remaining <= 0);
            Remaining = Math.Max(0, Remaining - step);
            untilSpawn -= step;
            seconds -= step;
            if (!IsRunning)
            {
                objects.Clear();
                break;
            }
            if (untilSpawn <= 0)
            {
                double radius = random.Next(16, 33);
                double x = radius + random.NextDouble() * (Width - radius * 2);
                double y = radius + random.NextDouble() * (Height - radius * 2);
                double lifetime = random.Next(2, 6) * LifetimeMultiplier;
                objects.Add(CreateSphere(random.Next(5), x, y, radius, lifetime));
                untilSpawn = SpawnInterval;
            }
        }
    }

    private Collectable CreateSphere(int kind, double x, double y, double radius, double lifetime) => kind switch
    {
        1 => new CooldownSphere(this, x, y, radius, lifetime),
        2 => new LifetimeSphere(this, x, y, radius, lifetime),
        3 => new SpawnSphere(this, x, y, radius, lifetime),
        4 => new PointsSphere(this, x, y, radius, lifetime, 50),
        _ => new PointsSphere(this, x, y, radius, lifetime)
    };

    public bool Click(double x, double y)
    {
        if (!IsRunning || !Player.Cooldown.IsReady)
            return false;
        // При пересечении выбираем верхний, последний нарисованный круг.
        int index = objects.FindLastIndex(item => item.Contains(x, y));
        if (index < 0)
            return false;
        if (!objects[index].TryCollect()) return false;
        Player.Cooldown.Start(Player.ClickInterval);
        objects.RemoveAt(index);
        return true;
    }
}

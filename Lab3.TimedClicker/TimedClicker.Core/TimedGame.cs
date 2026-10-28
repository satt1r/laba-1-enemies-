namespace TimedClicker.Core;

// Окно только передаёт время и координаты; появление, удаление и счёт принадлежат контроллеру.
public sealed class TimedGame
{
    private readonly List<ClickableCircle> objects = new();
    private readonly Random random;
    private double untilSpawn = 1;
    public const double Width = 760;
    public const double Height = 420;
    public IReadOnlyList<ClickableCircle> Objects { get; }
    public double Remaining { get; private set; }
    public double Score { get; private set; }
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
                objects.Add(new ClickableCircle(radius + random.NextDouble() * (Width - radius * 2),
                    radius + random.NextDouble() * (Height - radius * 2), radius, random.Next(2, 6)));
                untilSpawn = 1;
            }
        }
    }

    public bool Click(double x, double y)
    {
        if (!IsRunning)
            return false;
        // При пересечении выбираем верхний, последний нарисованный круг.
        int index = objects.FindLastIndex(item => item.Contains(x, y));
        if (index < 0)
            return false;
        Score += objects[index].Points;
        objects.RemoveAt(index);
        return true;
    }
}

namespace TimedClicker.Core;

public sealed class PointsSphere : Collectable
{
    private readonly TimedGame game;
    private readonly double points;
    public override string Label => "+";
    public override string Color => "#7652C7";

    // Перегрузка конструктора — выбор варианта во время компиляции.
    public PointsSphere(TimedGame game, double x, double y, double radius, double lifetime)
        : this(game, x, y, radius, lifetime, 1000 / (radius * 2 * lifetime)) { }

    public PointsSphere(TimedGame game, double x, double y, double radius, double lifetime, double points)
        : base(x, y, radius, lifetime)
    {
        this.game = game;
        if (!double.IsFinite(points) || points < 0) throw new ArgumentOutOfRangeException(nameof(points));
        this.points = points;
    }
    protected override void OnClick() => game.AddPoints(points);
}

public sealed class CooldownSphere : Collectable
{
    private readonly TimedGame game;
    public override string Label => "К";
    public override string Color => "#166CA0";
    public CooldownSphere(TimedGame game, double x, double y, double radius, double lifetime)
        : base(x, y, radius, lifetime) => this.game = game;
    protected override void OnClick() => game.Player.ReduceCooldown();
}

public sealed class LifetimeSphere : Collectable
{
    private readonly TimedGame game;
    public override string Label => "Ж";
    public override string Color => "#237347";
    public LifetimeSphere(TimedGame game, double x, double y, double radius, double lifetime)
        : base(x, y, radius, lifetime) => this.game = game;
    protected override void OnClick() => game.ExtendLifetime();
}

public sealed class SpawnSphere : Collectable
{
    private readonly TimedGame game;
    public override string Label => "С";
    public override string Color => "#A34D16";
    public SpawnSphere(TimedGame game, double x, double y, double radius, double lifetime)
        : base(x, y, radius, lifetime) => this.game = game;
    protected override void OnClick() => game.AccelerateSpawning();
}

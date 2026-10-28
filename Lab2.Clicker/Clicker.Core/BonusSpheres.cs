using TimedClicker.Core;

namespace Clicker.Core;

public sealed class DamageSphere : Collectable
{
    private readonly GameSession game;
    public override string Label => "×2";
    public override string Color => "#A33B4B";
    public DamageSphere(GameSession game, double x, double y) : base(x, y, 24, 6) => this.game = game;
    protected override void OnClick() => game.AddBonus(new DamageBonus());
}

public sealed class HasteSphere : Collectable
{
    private readonly GameSession game;
    public override string Label => "К";
    public override string Color => "#166CA0";
    public HasteSphere(GameSession game, double x, double y) : base(x, y, 24, 6) => this.game = game;
    protected override void OnClick() => game.AddBonus(new HasteBonus());
}

public sealed class GoldSphere : Collectable
{
    private readonly GameSession game;
    public override string Label => "+З";
    public override string Color => "#916410";
    public GoldSphere(GameSession game, double x, double y) : base(x, y, 24, 6) => this.game = game;
    protected override void OnClick() => game.Player.AddGold(game.CurrentEnemy.GoldReward * 5);
}

using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Threading;
using TimedClicker.Core;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Clicker.Core;
using Microsoft.Win32;

namespace Clicker.App;

public partial class MainWindow : Window
{
    private GameSession? session;
    private string iconsFolder = Path.Combine(AppContext.BaseDirectory, "Assets", "icons");
    private string? displayedIconPath;
    private bool initialized;
    private readonly Stopwatch clock = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly Dictionary<Collectable, Button> sphereButtons = new();

    public MainWindow()
    {
        InitializeComponent();
        timer.Tick += (_, _) => { Advance(); RefreshGame(); };
        Closed += (_, _) => { timer.Stop(); clock.Stop(); };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (initialized)
            return;

        initialized = true;
        LoadGame(Path.Combine(AppContext.BaseDirectory, "Assets", "templates", "enemies.sample.json"));
    }

    private void LoadJson_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true)
            LoadGame(dialog.FileName);
    }

    private void LoadGame(string path)
    {
        try
        {
            EnemyCatalog catalog = EnemyCatalog.LoadFromJson(path);
            var nextSession = new GameSession(catalog);
            session = nextSession;
            BonusScene.Children.Clear();
            sphereButtons.Clear();
            clock.Restart();
            timer.Start();

            string? jsonFolder = Path.GetDirectoryName(path);
            if (jsonFolder is not null && IsIconFileName(session.CurrentEnemy.IconName) &&
                File.Exists(Path.Combine(jsonFolder, session.CurrentEnemy.IconName)))
                iconsFolder = jsonFolder;

            TemplatesValue.Text = $"{Path.GetFileName(path)} · {catalog.Count} шт.";
            TemplatesValue.ToolTip = path;
            displayedIconPath = null;
            RefreshGame();
            StatusText.Text = "Новая игра. Нажимай на противника, зарабатывай золото и улучшай урон.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            StatusText.Text = "Не удалось загрузить шаблоны: " + exception.Message;
        }
    }

    private void ChooseIcons_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Выбери папку с иконками противников" };
        if (dialog.ShowDialog(this) != true)
            return;

        iconsFolder = dialog.FolderName;
        displayedIconPath = null;
        RefreshGame();
    }

    private void Attack_Click(object sender, RoutedEventArgs e)
    {
        if (session is null)
            return;

        Advance();
        if (!session.CanAttack) return;
        BigNumber damage = session.EffectiveDamage;
        bool won = session.Attack(out BigNumber reward);
        RefreshGame();
        StatusText.Text = won
            ? $"Победа! Получено {FormatNumber(reward)} золота. Появился следующий противник."
            : $"Нанесено {FormatNumber(damage)} урона.";
    }

    private void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        if (session is null)
            return;

        Advance();
        bool upgraded = session.TryUpgrade();
        RefreshGame();
        StatusText.Text = upgraded ? "Урон улучшен!" : "Недостаточно золота для улучшения.";
    }

    private void RefreshGame()
    {
        if (session is null)
            return;

        Player player = session.Player;
        Enemy enemy = session.CurrentEnemy;
        SetNumber(GoldValue, player.Gold);
        SetNumber(DamageValue, session.EffectiveDamage);
        SetNumber(LevelValue, player.Level);
        SetNumber(VictoriesValue, session.DefeatedEnemies);
        SetNumber(PriceValue, player.UpgradeCost);
        StageValue.Text = $"Этап {FormatNumber(session.Stage)}";
        StageValue.ToolTip = $"Этап {session.Stage}";
        EnemyNameValue.Text = enemy.Name;
        EnemyNameValue.ToolTip = enemy.Name;
        HealthValue.Text = $"Здоровье: {FormatNumber(enemy.Health)} / {FormatNumber(enemy.MaxHealth)}";
        HealthValue.ToolTip = $"Здоровье: {enemy.Health} / {enemy.MaxHealth}";
        RewardValue.Text = $"Награда: {FormatNumber(enemy.GoldReward)} золота";
        RewardValue.ToolTip = $"Награда: {enemy.GoldReward} золота";

        // В int переводится только шкала 0..1000, сами характеристики остаются BigNumber.
        BigNumber progress = enemy.Health * 1000 / enemy.MaxHealth;
        HealthBar.Value = int.Parse(progress.ToString(), CultureInfo.InvariantCulture);
        AttackButton.IsEnabled = session.CanAttack;
        CooldownValue.Text = $"Интервал атак: {session.EffectiveAttackInterval:F2} с\nДо атаки: {player.AttackCooldown.Remaining:F2} с";
        CooldownUpgradeButton.IsEnabled = player.CanUpgradeCooldown;
        CooldownUpgradeButton.Content = player.AttackInterval <= 0.1 ? "Максимальная скорость" : $"Ускорить за {FormatNumber(player.CooldownUpgradeCost)} золота";
        BonusValue.Text = session.Bonuses.Count == 0 ? "Нет активных бонусов" :
            string.Join("\n", session.Bonuses.Select(bonus => $"{bonus.Name}: {bonus.Remaining:F1} с"));
        RefreshSpheres();
        UpgradeButton.IsEnabled = session.CanUpgrade;
        UpgradeHint.Text = session.CanUpgrade ? "Золота достаточно для покупки" : "Зарабатывай золото в боях";
        RefreshIcon(enemy.IconName);
    }

    private void Advance()
    {
        double elapsed = clock.Elapsed.TotalSeconds;
        clock.Restart();
        session?.Update(elapsed);
    }

    private void CooldownUpgrade_Click(object sender, RoutedEventArgs e)
    {
        Advance();
        bool upgraded = session?.Player.TryUpgradeCooldown() == true;
        RefreshGame();
        StatusText.Text = upgraded ? "Интервал следующих атак уменьшен." : "Улучшение недоступно.";
    }

    private void RefreshSpheres()
    {
        if (session is null) return;
        foreach (var old in sphereButtons.Keys.Where(key => !session.BonusController.Objects.Contains(key)).ToArray())
        {
            BonusScene.Children.Remove(sphereButtons[old]);
            sphereButtons.Remove(old);
        }
        foreach (Collectable sphere in session.BonusController.Objects)
        {
            if (sphereButtons.ContainsKey(sphere)) continue;
            var button = new Button { Content = sphere.Label, Width = sphere.Radius * 2, Height = sphere.Radius * 2,
                Padding = new Thickness(0), Background = (Brush)new BrushConverter().ConvertFromString(sphere.Color)!,
                Foreground = Brushes.White, ToolTip = "Собрать бонус " + sphere.Label };
            // Круглая форма для каждой сферы; элемент остаётся доступной с клавиатуры кнопкой.
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(sphere.Radius));
            border.SetValue(Border.BackgroundProperty, button.Background);
            border.SetValue(Border.BorderBrushProperty, Brushes.White);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);
            button.Template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            button.Clip = new EllipseGeometry(new Point(sphere.Radius, sphere.Radius), sphere.Radius, sphere.Radius);
            button.Click += (_, args) =>
            {
                Advance();
                if (session.BonusController.Collect(sphere)) StatusText.Text = "Бонус собран: " + sphere.Label;
                args.Handled = true;
                RefreshGame();
            };
            Canvas.SetLeft(button, sphere.X - sphere.Radius);
            Canvas.SetTop(button, sphere.Y - sphere.Radius);
            sphereButtons.Add(sphere, button);
            BonusScene.Children.Add(button);
        }
    }

    private void RefreshIcon(string iconName)
    {
        string? path = IsIconFileName(iconName) ? Path.Combine(iconsFolder, iconName) : null;
        if (path is not null && string.Equals(displayedIconPath, path, StringComparison.OrdinalIgnoreCase))
            return;

        displayedIconPath = path;
        EnemyImage.Source = null;
        MissingIcon.Visibility = Visibility.Visible;
        IconHint.Text = "Иконка не найдена. Выбери папку иконок; кнопка атаки работает и без изображения.";
        try
        {
            if (path is null || !File.Exists(path))
                return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 512;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            EnemyImage.Source = bitmap;
            MissingIcon.Visibility = Visibility.Collapsed;
            IconHint.Text = "Нажимай на иконку, чтобы атаковать";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or FormatException)
        {
            IconHint.Text = "Иконка не читается. Выбери другую папку иконок; атака доступна без изображения.";
        }
    }

    private static bool IsIconFileName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name == Path.GetFileName(name) &&
        name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    private static void SetNumber(TextBlock target, BigNumber value)
    {
        target.Text = FormatNumber(value);
        target.ToolTip = value.ToString();
    }

    private static string FormatNumber(BigNumber value)
    {
        string text = value.ToString();
        return text.Length <= 9 ? text : $"{text[0]}.{text.Substring(1, 2)}e{text.Length - 1}";
    }
}

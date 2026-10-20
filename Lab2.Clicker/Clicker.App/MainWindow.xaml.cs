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

    public MainWindow()
    {
        InitializeComponent();
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

        bool won = session.Attack(out BigNumber reward);
        RefreshGame();
        StatusText.Text = won
            ? $"Победа! Получено {FormatNumber(reward)} золота. Появился следующий противник."
            : $"Нанесено {FormatNumber(session.Player.Damage)} урона.";
    }

    private void Upgrade_Click(object sender, RoutedEventArgs e)
    {
        if (session is null)
            return;

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
        SetNumber(DamageValue, player.Damage);
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
        AttackButton.IsEnabled = true;
        UpgradeButton.IsEnabled = session.CanUpgrade;
        UpgradeHint.Text = session.CanUpgrade ? "Золота достаточно для покупки" : "Зарабатывай золото в боях";
        RefreshIcon(enemy.IconName);
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

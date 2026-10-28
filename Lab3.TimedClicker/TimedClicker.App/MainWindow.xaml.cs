using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TimedClicker.Core;

namespace TimedClicker.App;

public partial class MainWindow : Window
{
    private TimedGame? game;
    private readonly Stopwatch clock = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };

    public MainWindow()
    {
        InitializeComponent();
        timer.Tick += (_, _) => { Advance(); Render(); };
        Closed += (_, _) => { timer.Stop(); clock.Stop(); };
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        game = new TimedGame();
        clock.Restart();
        timer.Start();
        Render();
    }

    private void Advance()
    {
        if (game is null) return;
        double elapsed = clock.Elapsed.TotalSeconds;
        clock.Restart();
        game.Update(elapsed);
        if (!game.IsRunning) { timer.Stop(); clock.Stop(); }
    }

    private void Scene_Click(object sender, MouseButtonEventArgs e)
    {
        Advance();
        Point point = e.GetPosition(Scene);
        game?.Click(point.X, point.Y);
        Render();
    }

    private void Render()
    {
        if (game is null) return;
        Scene.Children.Clear();
        foreach (var item in game.Objects)
        {
            var shape = new Ellipse { Width = item.Radius * 2, Height = item.Radius * 2,
                Fill = (Brush)new BrushConverter().ConvertFromString(item.Color)!, Stroke = Brushes.White, StrokeThickness = 2,
                IsHitTestVisible = false };
            Canvas.SetLeft(shape, item.X - item.Radius);
            Canvas.SetTop(shape, item.Y - item.Radius);
            Scene.Children.Add(shape);
            var label = new TextBlock { Text = item.Label, Foreground = Brushes.White,
                Width = item.Radius * 2, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold,
                IsHitTestVisible = false };
            Canvas.SetLeft(label, item.X - item.Radius);
            Canvas.SetTop(label, item.Y - 8);
            Scene.Children.Add(label);
        }
        Stats.Text = $"Осталось: {game.Remaining:F1} с   •   Очки: {game.Score:F1}   •   Клик через: {game.Player.Cooldown.Remaining:F1} с";
        Hint.Text = game.IsRunning ? "+ — очки; К — быстрее клик; Ж — дольше живут новые сферы; С — чаще появляются. Бонусы действуют до конца раунда." : $"Время вышло! Результат: {game.Score:F1}. Можно начать заново.";
    }
}

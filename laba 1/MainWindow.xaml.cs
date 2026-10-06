using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace laba_1
{
    public partial class MainWindow : Window
    {
        private List<EnemyIcon> enemyIcons = new List<EnemyIcon>();
        private EnemyIcon? selectedIcon;
        private CEnemyTemplateList enemyList = new CEnemyTemplateList();

        public MainWindow()
        {
            InitializeComponent();
        }

        // ---------- Иконки ----------

        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog();
            if (dlg.ShowDialog() == true)
            {
                LoadIconsFromFolder(dlg.FolderName);
            }
        }

        public void LoadIconsFromFolder(string path)
        {
            enemyIcons.Clear();
            IconsListBox.Items.Clear();
            selectedIcon = null;
            SelectedIconTextBlock.Text = "—";

            string[] extensions = { ".png", ".jpg", ".jpeg" };
            var files = Directory.GetFiles(path)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            foreach (string file in files)
            {
                var icon = new EnemyIcon
                {
                    Name = Path.GetFileName(file),
                    ImagePath = file
                };
                enemyIcons.Add(icon);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(file, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 128;
                bitmap.EndInit();

                var image = new Image
                {
                    Source = bitmap,
                    Width = 64,
                    Height = 64,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(5),
                    Tag = icon
                };
                IconsListBox.Items.Add(image);
            }
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconsListBox.SelectedItem is Image image && image.Tag is EnemyIcon icon)
            {
                selectedIcon = icon;
                SelectedIconTextBlock.Text = icon.Name;
            }
        }

        // ---------- Противники ----------

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            if (name == "")
            {
                MessageBox.Show("Введи имя противника.");
                return;
            }
            if (enemyList.GetEnemyByName(name) != null)
            {
                MessageBox.Show("Противник с таким именем уже есть.");
                return;
            }
            if (selectedIcon == null)
            {
                MessageBox.Show("Выбери иконку в сетке справа.");
                return;
            }

            if (!int.TryParse(BaseLifeTextBox.Text, out int baseLife) || baseLife < 0 ||
                !int.TryParse(BaseGoldTextBox.Text, out int baseGold) || baseGold < 0)
            {
                MessageBox.Show("Здоровье и золото должны быть целыми числами не меньше 0.");
                return;
            }
            if (!TryParseDouble(LifeModifierTextBox.Text, out double lifeModifier) || lifeModifier < 0 ||
                !TryParseDouble(GoldModifierTextBox.Text, out double goldModifier) || goldModifier < 0)
            {
                MessageBox.Show("Модификаторы должны быть числами не меньше 0.");
                return;
            }
            if (!TryParseDouble(SpawnChanceTextBox.Text, out double spawnChance) ||
                spawnChance < 0 || spawnChance > 100)
            {
                MessageBox.Show("Шанс появления должен быть числом от 0 до 100.");
                return;
            }

            enemyList.AddEnemy(name, selectedIcon.Name, baseLife, lifeModifier,
                baseGold, goldModifier, spawnChance);

            RefreshEnemiesList();
            NameTextBox.Clear();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            int index = EnemiesListBox.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("Выбери противника в списке.");
                return;
            }

            enemyList.DeleteEnemyByIndex(index);
            RefreshEnemiesList();
        }

        private void EnemiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = EnemiesListBox.SelectedIndex;
            if (index < 0)
            {
                EnemyInfoTextBlock.Text = "";
                return;
            }

            CEnemyTemplate enemy = enemyList.GetEnemyByIndex(index);
            EnemyInfoTextBlock.Text =
                $"Иконка: {enemy.IconName}\n" +
                $"Здоровье: {enemy.BaseLife} (x{enemy.LifeModifier})\n" +
                $"Золото: {enemy.BaseGold} (x{enemy.GoldModifier})\n" +
                $"Шанс появления: {enemy.SpawnChance}%";
        }

        // ListBox только отображает данные, хранит их CEnemyTemplateList
        private void RefreshEnemiesList()
        {
            EnemiesListBox.ItemsSource = null;
            EnemiesListBox.ItemsSource = enemyList.GetListOfEnemyNames();
            EnemyInfoTextBlock.Text = "";
        }

        // принимает и запятую, и точку
        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text.Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
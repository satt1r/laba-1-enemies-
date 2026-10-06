using System.Collections.Generic;
using System.IO;
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

        public MainWindow()
        {
            InitializeComponent();
        }

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
            // при повторном выборе папки старые иконки не должны остаться
            enemyIcons.Clear();
            IconsListBox.Items.Clear();
            selectedIcon = null;
            SelectedIconTextBlock.Text = "—";

            string[] files = Directory.GetFiles(path, "*.png");

            foreach (string file in files)
            {
                var icon = new EnemyIcon
                {
                    Name = Path.GetFileName(file),
                    ImagePath = file
                };
                enemyIcons.Add(icon);

                // OnLoad читает файл сразу и не держит его заблокированным
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(file, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = 128;
                bitmap.EndInit();

                // Фиксированный размер 64x64, пропорции сохраняются
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
    }
}
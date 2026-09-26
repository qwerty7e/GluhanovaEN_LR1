using ChessModule.Core;
using ChessModule.Models;
using ChessModule.Services;
using System.Windows;

namespace ChessModule.Windows
{
    public partial class LoadGameWindow : Window
    {
        public LoadGameWindow()
        {
            InitializeComponent();
            LoadData();
            ThemeService.ApplyTheme(this, RootGrid);
        }

        private void LoadData()
        {
            SavesGrid.ItemsSource = SavedGameService.GetSaves();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            SavedGameInfo selected = SavesGrid.SelectedItem as SavedGameInfo;
            if (selected == null)
            {
                MessageBox.Show("Выберите сохранение.");
                return;
            }

            GameOptions options = SavedGameService.ToOptions(selected);
            MainWindow window = new MainWindow(options);
            window.ShowDialog();
            Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            SavedGameInfo selected = SavesGrid.SelectedItem as SavedGameInfo;
            if (selected == null)
            {
                MessageBox.Show("Выберите сохранение.");
                return;
            }

            SavedGameService.DeleteSave(selected.SaveId);
            LoadData();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

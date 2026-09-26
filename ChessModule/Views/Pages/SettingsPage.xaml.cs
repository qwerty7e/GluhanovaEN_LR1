using ChessModule.Models;
using ChessModule.Services;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadData();
            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void LoadData()
        {
            PlayerSettings settings = SettingsService.GetSettings();
            MusicCheck.IsChecked = settings.MusicEnabled;
            EffectsCheck.IsChecked = settings.EffectsEnabled;
            CoordinatesCheck.IsChecked = settings.ShowCoordinates;
        }
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            PlayerSettings settings = SettingsService.GetSettings();

            settings.MusicEnabled = MusicCheck.IsChecked == true;
            settings.EffectsEnabled = EffectsCheck.IsChecked == true;
            settings.ShowCoordinates = CoordinatesCheck.IsChecked == true;

            SettingsService.SaveSettings(settings);
            MusicService.ApplySettings();

            MessageBox.Show("Настройки сохранены.");

        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainMenuPage());
        }
    }
}
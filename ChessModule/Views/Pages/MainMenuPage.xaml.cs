using ChessModule.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ChessModule.Windows
{
    public partial class MainMenuPage : Page
    {
        public MainMenuPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            AuthService.RefreshCurrentUser();
            RewardService.CheckRewards();
            AuthService.RefreshCurrentUser();

            if (AuthService.CurrentUser == null)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                Window currentWindow = Window.GetWindow(this);
                if (currentWindow != null)
                    currentWindow.Close();

                return;
            }
            var user = AuthService.CurrentUser;
            var visual = RewardService.GetVisualCustomization();

            int draws = Math.Max(0, user.TotalGames - user.Wins - user.Losses);

            WelcomeText.Text = "Добро пожаловать, " + user.Username + "!";
            ShortStatsText.Text =
                "Победы: " + user.Wins +
                " | Поражения: " + user.Losses +
                " | Ничьи: " + draws +
                " | Всего партий: " + user.TotalGames +
                " | Серия побед: " + user.WinStreak +
                " | Доска: " + visual.BoardColor +
                " | Фигуры: " + visual.PieceColor;

            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            new GameSetupWindow().ShowDialog();
            LoadData();
        }

        private void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            new LoadGameWindow().ShowDialog();
            LoadData();
        }

        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProfilePage());
        }

        private void Stats_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StatisticsPage());
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new HistoryPage());
        }

        private void Achievements_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AchievementsPage());
        }

        private void Customization_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CustomizationPage());
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SettingsPage());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            MusicService.Stop();
            AuthService.Logout();

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            Window currentWindow = Window.GetWindow(this);
            if (currentWindow != null)
                currentWindow.Close();
        }
    }
}
using ChessModule.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            AuthService.RefreshCurrentUser();
            RewardService.CheckRewards();
            AuthService.RefreshCurrentUser();

            var user = AuthService.CurrentUser;
            var visual = RewardService.GetVisualCustomization();

            int draws = Math.Max(0, user.TotalGames - user.Wins - user.Losses);

            UsernameText.Text = "Игрок: " + user.Username;

            StatsText.Text =
                "Победы: " + user.Wins + Environment.NewLine +
                "Поражения: " + user.Losses + Environment.NewLine +
                "Ничьи: " + draws + Environment.NewLine +
                "Всего игр: " + user.TotalGames + Environment.NewLine +
                "Серия побед: " + user.WinStreak;

            CustomizationText.Text =
                "Цвет доски: " + visual.BoardColor + Environment.NewLine +
                "Цвет фигур: " + visual.PieceColor + Environment.NewLine +
                "Форма фигур: " + visual.PieceShape;

            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new MainMenuPage());
        }
    }
}
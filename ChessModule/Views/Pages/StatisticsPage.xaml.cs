using ChessModule.Models;
using ChessModule.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class StatisticsPage : Page
    {
        public StatisticsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            AuthService.RefreshCurrentUser();

            var user = AuthService.CurrentUser;
            var visual = RewardService.GetVisualCustomization();
            var history = GameService.GetHistory();

            int draws = Math.Max(0, user.TotalGames - user.Wins - user.Losses);
            double winRate = user.TotalGames == 0 ? 0 : user.Wins * 100.0 / user.TotalGames;

            GameHistoryItem fastestWin = history
                .Where(g => g.Result == "Победа" && g.DurationSeconds > 0)
                .OrderBy(g => g.DurationSeconds)
                .FirstOrDefault();

            GameHistoryItem shortestWin = history
                .Where(g => g.Result == "Победа" && g.MovesCount > 0)
                .OrderBy(g => g.MovesCount)
                .FirstOrDefault();

            string fastestWinText = fastestWin == null ? "нет данных" : fastestWin.DurationText;
            string shortestWinText = shortestWin == null ? "нет данных" : shortestWin.MovesCount + " ходов";

            StatsBlock.Text =
                "Победы: " + user.Wins + Environment.NewLine +
                "Поражения: " + user.Losses + Environment.NewLine +
                "Ничьи: " + draws + Environment.NewLine +
                "Всего игр: " + user.TotalGames + Environment.NewLine +
                "Процент побед: " + winRate.ToString("0.0") + "%" + Environment.NewLine +
                "Текущая серия побед: " + user.WinStreak + Environment.NewLine +
                "Самая быстрая победа по времени: " + fastestWinText + Environment.NewLine +
                "Самая короткая победа по ходам: " + shortestWinText + Environment.NewLine +
                "Цвет доски: " + visual.BoardColor + Environment.NewLine +
                "Цвет фигур: " + visual.PieceColor + Environment.NewLine +
                "Форма фигур: " + visual.PieceShape;

            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new HistoryPage());
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

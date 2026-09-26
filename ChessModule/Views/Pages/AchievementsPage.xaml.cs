using ChessModule.Services;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ChessModule.Windows
{
    public partial class AchievementsPage : Page
    {
        public AchievementsPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var achievements = AchievementService.GetAchievements();

            ReceivedAchievementsGrid.ItemsSource = achievements
                .Where(a => a.IsReceived)
                .ToList();

            LockedAchievementsGrid.ItemsSource = achievements
                .Where(a => !a.IsReceived)
                .ToList();

            ThemeService.ApplyTheme(null, RootGrid);
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

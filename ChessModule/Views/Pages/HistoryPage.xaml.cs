using ChessModule.Services;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            HistoryGrid.ItemsSource = GameService.GetHistory();
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

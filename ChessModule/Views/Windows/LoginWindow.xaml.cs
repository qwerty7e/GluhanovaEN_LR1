using ChessModule.Services;
using System.Windows;
using System.Windows.Navigation;

namespace ChessModule.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            ThemeService.ApplyTheme(this, RootGrid);
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            bool ok = AuthService.Login(LoginBox.Text, PasswordBox.Password);

            if (ok)
            {
                MusicService.ApplySettings();

                NavigationWindow menuWindow = new NavigationWindow();
                menuWindow.Content = new MainMenuPage();
                menuWindow.WindowState = WindowState.Maximized;
                menuWindow.ShowsNavigationUI = false;
                menuWindow.Show();

                Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            Window registerWindow = new Window();
            registerWindow.Title = "Регистрация";
            registerWindow.Content = new RegisterPage();
            registerWindow.WindowState = WindowState.Maximized;
            registerWindow.ResizeMode = ResizeMode.CanResize;
            registerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            registerWindow.Background = System.Windows.Media.Brushes.White;
            registerWindow.Show();

            Close();
        }
    }
}
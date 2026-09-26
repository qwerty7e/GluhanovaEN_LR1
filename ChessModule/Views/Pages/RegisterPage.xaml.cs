using ChessModule.Services;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            bool ok;

            try
            {
                ok = AuthService.Register(LoginBox.Text, PasswordBox.Password);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message);
                return;
            }
            if (ok)
            {
                MessageBox.Show("Аккаунт успешно создан.");
                OpenLoginWindow();
            }
            else
            {
                MessageBox.Show("Пользователь с таким логином уже существует или данные заполнены некорректно.");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            OpenLoginWindow();
        }

        private void OpenLoginWindow()
        {
            Window currentWindow = Window.GetWindow(this);

            LoginWindow loginWindow = new LoginWindow();
            Application.Current.MainWindow = loginWindow;
            loginWindow.Show();

            if (currentWindow != null)
                currentWindow.Close();
        }
    }
}
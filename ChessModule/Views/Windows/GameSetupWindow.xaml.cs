using ChessModule.Core;
using ChessModule.Services;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class GameSetupWindow : Window
    {
        public GameSetupWindow()
        {
            InitializeComponent();
            FillCombos();
            ThemeService.ApplyTheme(this, RootGrid);
        }

        private void FillCombos()
        {
            ModeCombo.Items.Add("Против бота");
            ModeCombo.Items.Add("Два игрока за одним устройством");
            ModeCombo.SelectedIndex = 0;

            BotCombo.Items.Add("Лёгкий: случайная фигура и случайный ход");
            BotCombo.Items.Add("Средний: выбирает более ценное взятие");
            BotCombo.Items.Add("Сложный: оценивает ответ соперника");
            BotCombo.SelectedIndex = 1;

            TimeCombo.Items.Add("Быстрая 10 минут");
            TimeCombo.Items.Add("Блиц 5 минут");
            TimeCombo.Items.Add("Без ограничения времени");
            TimeCombo.SelectedIndex = 0;
        }

        private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BotCombo.IsEnabled = ModeCombo.SelectedIndex == 0;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            GameOptions options = new GameOptions();
            options.Mode = ModeCombo.SelectedIndex == 1 ? GameMode.HumanVsHuman : GameMode.HumanVsBot;
            options.BotDifficulty = (BotDifficulty)(BotCombo.SelectedIndex + 1);

            if (TimeCombo.SelectedIndex == 1)
                options.TimeMode = GameTimeMode.Blitz5;
            else if (TimeCombo.SelectedIndex == 2)
                options.TimeMode = GameTimeMode.NoLimit;
            else
                options.TimeMode = GameTimeMode.Rapid10;

            int seconds = GameOptions.GetStartSeconds(options.TimeMode);
            options.WhiteTimeSeconds = seconds;
            options.BlackTimeSeconds = seconds;

            MainWindow gameWindow = new MainWindow(options);
            gameWindow.ShowDialog();
            Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using ChessModule.Models;
using ChessModule.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ChessModule.Windows
{
    public partial class CustomizationPage : Page
    {
        private List<RewardStatus> rewards;

        public CustomizationPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            rewards = RewardService.GetRewards();

            RefreshRewardsGrid();

            FillCombo(BoardComboBox, "Цвет доски");
            FillCombo(PieceColorComboBox, "Цвет фигур");
            FillCombo(PieceShapeComboBox, "Форма фигур");

            ThemeService.ApplyTheme(null, RootGrid);
        }

        private void RefreshRewardsGrid()
        {
            RewardsGrid.ItemsSource = null;
            RewardsGrid.ItemsSource = rewards
                .Where(r => r.RewardType != "Тема")
                .ToList();
        }

        private void FillCombo(ComboBox comboBox, string type)
        {
            comboBox.Items.Clear();

            var unlockedRewards = rewards
                .Where(r => r.RewardType == type && r.IsUnlocked)
                .ToList();

            foreach (var reward in unlockedRewards)
                comboBox.Items.Add(reward.Title);

            var selectedReward = unlockedRewards.FirstOrDefault(r => r.IsSelected);

            if (selectedReward != null)
                comboBox.SelectedItem = selectedReward.Title;
            else if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            bool boardOk = ApplySelectedReward(BoardComboBox, "Цвет доски");
            bool pieceColorOk = ApplySelectedReward(PieceColorComboBox, "Цвет фигур");
            bool pieceShapeOk = ApplySelectedReward(PieceShapeComboBox, "Форма фигур");

            if (!boardOk || !pieceColorOk || !pieceShapeOk)
            {
                MessageBox.Show("Не удалось применить выбранную кастомизацию.");
                return;
            }

            rewards = RewardService.GetRewards();

            FillCombo(BoardComboBox, "Цвет доски");
            FillCombo(PieceColorComboBox, "Цвет фигур");
            FillCombo(PieceShapeComboBox, "Форма фигур");

            RefreshRewardsGrid();

            MessageBox.Show("Кастомизация применена.");
        }

        private bool ApplySelectedReward(ComboBox comboBox, string rewardType)
        {
            string selectedTitle = comboBox.SelectedItem == null ? string.Empty : comboBox.SelectedItem.ToString();

            if (string.IsNullOrWhiteSpace(selectedTitle))
                return false;

            RewardStatus reward = rewards.FirstOrDefault(r =>
                r.RewardType == rewardType &&
                r.Title == selectedTitle &&
                r.IsUnlocked);

            if (reward == null)
                return false;

            return RewardService.SelectReward(reward.RewardId);
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
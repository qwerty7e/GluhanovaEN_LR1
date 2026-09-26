using System;

namespace ChessModule.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int TotalGames { get; set; }
        public int WinStreak { get; set; }
    }

    public class GameHistoryItem
    {
        public int Number { get; set; }
        public string Result { get; set; }
        public DateTime GameDate { get; set; }
        public string OpponentType { get; set; }
        public string GameDifficulty { get; set; }
        public int MovesCount { get; set; }
        public int DurationSeconds { get; set; }
        public string FinishReason { get; set; }
        public string Winner { get; set; }

        public string DurationText
        {
            get
            {
                TimeSpan span = TimeSpan.FromSeconds(DurationSeconds);
                return string.Format("{0:D2}:{1:D2}", (int)span.TotalMinutes, span.Seconds);
            }
        }
    }

    public class AchievementStatus
    {
        public int AchievementId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ConditionText { get; set; }
        public bool IsReceived { get; set; }
        public DateTime? DateReceived { get; set; }
        public string StatusText
        {
            get { return IsReceived ? "Получено" : "Не получено"; }
        }
    }

    public class RewardStatus
    {
        public int RewardId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RewardType { get; set; }
        public bool IsUnlocked { get; set; }
        public bool IsSelected { get; set; }
        public string StatusText
        {
            get
            {
                if (!IsUnlocked)
                    return "Не получено";

                return IsSelected ? "Получено (выбрано)" : "Получено";
            }
        }
    }

    public class VisualCustomization
    {
        public string BoardColor { get; set; }
        public string PieceColor { get; set; }
        public string PieceShape { get; set; }

        public VisualCustomization()
        {
            BoardColor = "Классическая доска";
            PieceColor = "Стандартные фигуры";
            PieceShape = "Обычная форма";
        }
    }

    public class SavedGameInfo
    {
        public int SaveId { get; set; }
        public int UserId { get; set; }
        public string SaveName { get; set; }
        public string Mode { get; set; }
        public int BotDifficulty { get; set; }
        public string GameDifficulty { get; set; }
        public string Fen { get; set; }
        public int WhiteTimeSeconds { get; set; }
        public int BlackTimeSeconds { get; set; }
        public int MoveCount { get; set; }
        public string LoadedMoveLog { get; set; }
        public string MoveLog { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string DisplayName
        {
            get { return SaveName + " — " + UpdatedAt.ToString("dd.MM.yyyy HH:mm"); }
        
        }
    }

    public class PlayerSettings
    {
        public bool MusicEnabled { get; set; }
        public bool EffectsEnabled { get; set; }
        public double ScaleFactor { get; set; }
        public bool ShowCoordinates { get; set; }

        public PlayerSettings()
        {
            MusicEnabled = true;
            EffectsEnabled = true;
            ShowCoordinates = false;
        }
    }
}

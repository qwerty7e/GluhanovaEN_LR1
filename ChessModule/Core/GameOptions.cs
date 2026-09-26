namespace ChessModule.Core
{
    public sealed class GameOptions
    {
        public GameMode Mode { get; set; }
        public BotDifficulty BotDifficulty { get; set; }
        public GameTimeMode TimeMode { get; set; }
        public int? LoadedSaveId { get; set; }
        public string LoadedFen { get; set; }
        public string LoadedMoveLog { get; set; }
        public int WhiteTimeSeconds { get; set; }
        public int BlackTimeSeconds { get; set; }
        public int MoveCount { get; set; }

        public GameOptions()
        {
            Mode = GameMode.HumanVsBot;
            BotDifficulty = BotDifficulty.Greedy;
            TimeMode = GameTimeMode.Rapid10;
            LoadedFen = string.Empty;
            LoadedMoveLog = string.Empty;
            WhiteTimeSeconds = 10 * 60;
            BlackTimeSeconds = 10 * 60;
        }
        public static int GetStartSeconds(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Blitz5:
                    return 5 * 60;
                case GameTimeMode.Rapid10:
                    return 10 * 60;
                default:
                    return 0;
            }
        }

        public static string GetTimeModeName(GameTimeMode mode)
        {
            switch (mode)
            {
                case GameTimeMode.Blitz5: return "Блиц 5 минут";
                case GameTimeMode.Rapid10: return "Быстрая 10 минут";
                default: return "Без ограничения времени";
            }
        }

        public static string GetModeName(GameMode mode)
        {
            return mode == GameMode.HumanVsHuman ? "Два игрока" : "Против бота";
        }

        public static string GetBotName(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Random: return "Лёгкий: случайные ходы";
                case BotDifficulty.Greedy: return "Средний: ценность взятия";
                case BotDifficulty.MiniMax: return "Сложный: расчёт на 2 полухода";
                default: return "Не используется";
            }
        }
    }
}

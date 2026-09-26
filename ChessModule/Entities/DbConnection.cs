using System.Data.SqlClient;

namespace ChessModule.Entities
{
    public static class DbConnection
    {
        private static readonly string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ChessProfile;Integrated Security=True;TrustServerCertificate=True";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public static void EnsureSchema(SqlConnection connection)
        {
            ExecuteNonQuery(connection, @"
IF COL_LENGTH('Users', 'WinStreak') IS NULL
    ALTER TABLE Users ADD WinStreak INT NOT NULL CONSTRAINT DF_Users_WinStreak DEFAULT 0;

IF COL_LENGTH('GameHistory', 'OpponentType') IS NULL
    ALTER TABLE GameHistory ADD OpponentType NVARCHAR(100) NOT NULL CONSTRAINT DF_GameHistory_OpponentType DEFAULT N'Не указано';

IF COL_LENGTH('GameHistory', 'BotDifficulty') IS NULL
    ALTER TABLE GameHistory ADD BotDifficulty INT NULL;

IF COL_LENGTH('GameHistory', 'GameDifficulty') IS NULL
    ALTER TABLE GameHistory ADD GameDifficulty NVARCHAR(100) NOT NULL CONSTRAINT DF_GameHistory_GameDifficulty DEFAULT N'-';

IF COL_LENGTH('GameHistory', 'MovesCount') IS NULL
    ALTER TABLE GameHistory ADD MovesCount INT NOT NULL CONSTRAINT DF_GameHistory_MovesCount DEFAULT 0;

IF COL_LENGTH('GameHistory', 'DurationSeconds') IS NULL
    ALTER TABLE GameHistory ADD DurationSeconds INT NOT NULL CONSTRAINT DF_GameHistory_DurationSeconds DEFAULT 0;

IF COL_LENGTH('GameHistory', 'FinishReason') IS NULL
    ALTER TABLE GameHistory ADD FinishReason NVARCHAR(200) NOT NULL CONSTRAINT DF_GameHistory_FinishReason DEFAULT N'';

IF COL_LENGTH('GameHistory', 'Winner') IS NULL
    ALTER TABLE GameHistory ADD Winner NVARCHAR(50) NOT NULL CONSTRAINT DF_GameHistory_Winner DEFAULT N'';

IF COL_LENGTH('Achievements', 'RequiredWinStreak') IS NULL
    ALTER TABLE Achievements ADD RequiredWinStreak INT NULL;

IF COL_LENGTH('Achievements', 'RequiredMaxMoves') IS NULL
    ALTER TABLE Achievements ADD RequiredMaxMoves INT NULL;

IF OBJECT_ID('Rewards', 'U') IS NOT NULL AND COL_LENGTH('Rewards', 'RewardType') IS NULL
    ALTER TABLE Rewards ADD RewardType NVARCHAR(100) NOT NULL CONSTRAINT DF_Rewards_RewardType DEFAULT N'Тема';

IF OBJECT_ID('UserRewards', 'U') IS NOT NULL AND COL_LENGTH('UserRewards', 'IsSelected') IS NULL
    ALTER TABLE UserRewards ADD IsSelected BIT NOT NULL CONSTRAINT DF_UserRewards_IsSelected DEFAULT 0;

IF OBJECT_ID('UserSettings', 'U') IS NOT NULL AND COL_LENGTH('UserSettings', 'MusicEnabled') IS NULL
    ALTER TABLE UserSettings ADD MusicEnabled BIT NOT NULL CONSTRAINT DF_UserSettings_MusicEnabled DEFAULT 1;

IF OBJECT_ID('UserSettings', 'U') IS NOT NULL AND COL_LENGTH('UserSettings', 'EffectsEnabled') IS NULL
    ALTER TABLE UserSettings ADD EffectsEnabled BIT NOT NULL CONSTRAINT DF_UserSettings_EffectsEnabled DEFAULT 1;

IF OBJECT_ID('UserSettings', 'U') IS NOT NULL AND COL_LENGTH('UserSettings', 'ShowCoordinates') IS NULL
    ALTER TABLE UserSettings ADD ShowCoordinates BIT NOT NULL CONSTRAINT DF_UserSettings_ShowCoordinates DEFAULT 1;
");
        }

        private static void ExecuteNonQuery(SqlConnection connection, string query)
        {
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}

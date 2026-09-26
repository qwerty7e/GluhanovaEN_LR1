using ChessModule.Entities;
using ChessModule.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ChessModule.Services
{
    public static class AchievementService
    {
        public static void CheckAchievements()
        {
            if (AuthService.CurrentUser == null)
                return;

            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                EnsureDefaultAchievements(connection);

                string query = @"
                    INSERT INTO UserAchievements (UserId, AchievementId, DateReceived)
                    SELECT @UserId, A.AchievementId, GETDATE()
                    FROM Achievements A
                    INNER JOIN Users U ON U.UserId = @UserId
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM UserAchievements UA
                        WHERE UA.UserId = @UserId
                        AND UA.AchievementId = A.AchievementId
                    )
                    AND
                    (
                        (A.RequiredWins IS NOT NULL AND U.Wins >= A.RequiredWins)
                        OR
                        (A.RequiredGames IS NOT NULL AND U.TotalGames >= A.RequiredGames)
                        OR
                        (A.RequiredWinStreak IS NOT NULL AND U.WinStreak >= A.RequiredWinStreak)
                        OR
                        (
                            A.RequiredMaxMoves IS NOT NULL
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GameHistory GH
                                WHERE GH.UserId = @UserId
                                  AND GH.Result = N'Победа'
                                  AND GH.MovesCount > 0
                                  AND GH.MovesCount <= A.RequiredMaxMoves
                            )
                        )
                    )";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static List<AchievementStatus> GetAchievements()
        {
            CheckAchievements();

            List<AchievementStatus> result = new List<AchievementStatus>();
            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT A.AchievementId,
                           A.Title,
                           A.[Description],
                           A.RequiredWins,
                           A.RequiredGames,
                           A.RequiredWinStreak,
                           A.RequiredMaxMoves,
                           UA.DateReceived
                    FROM Achievements A
                    LEFT JOIN UserAchievements UA
                        ON UA.AchievementId = A.AchievementId
                        AND UA.UserId = @UserId
                    ORDER BY A.AchievementId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new AchievementStatus
                            {
                                AchievementId = (int)reader["AchievementId"],
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                ConditionText = BuildConditionText(reader),
                                IsReceived = reader["DateReceived"] != DBNull.Value,
                                DateReceived = reader["DateReceived"] == DBNull.Value
                                    ? (DateTime?)null
                                    : (DateTime)reader["DateReceived"]
                            });
                        }
                    }
                }
            }

            return result;
        }

        private static void EnsureDefaultAchievements(SqlConnection connection)
        {
            EnsureAchievement(connection, "Первая победа", "Выиграть первую партию.", 1, null, null, null);
            EnsureAchievement(connection, "Пять побед", "Выиграть пять партий.", 5, null, null, null);
            EnsureAchievement(connection, "Опытный игрок", "Сыграть десять партий.", null, 10, null, null);
            EnsureAchievement(connection, "Серия побед", "Победить три раза подряд.", null, null, 3, null);
            EnsureAchievement(connection, "Быстрая победа", "Выиграть партию не более чем за 40 ходов.", null, null, null, 40);
        }

        private static void EnsureAchievement(
        SqlConnection connection,
        string title,
        string description,
        int? requiredWins,
        int? requiredGames,
        int? requiredWinStreak,
        int? requiredMaxMoves)
        {
            string query = @"
IF NOT EXISTS (SELECT 1 FROM Achievements WHERE Title = @Title)
INSERT INTO Achievements
(Title, [Description], RequiredWins, RequiredGames, RequiredWinStreak, RequiredMaxMoves)
VALUES
(@Title, @Description, @RequiredWins, @RequiredGames, @RequiredWinStreak, @RequiredMaxMoves)";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@RequiredWins", requiredWins.HasValue ? (object)requiredWins.Value : DBNull.Value);
                command.Parameters.AddWithValue("@RequiredGames", requiredGames.HasValue ? (object)requiredGames.Value : DBNull.Value);
                command.Parameters.AddWithValue("@RequiredWinStreak", requiredWinStreak.HasValue ? (object)requiredWinStreak.Value : DBNull.Value);
                command.Parameters.AddWithValue("@RequiredMaxMoves", requiredMaxMoves.HasValue ? (object)requiredMaxMoves.Value : DBNull.Value);

                command.ExecuteNonQuery();
            }

        }

        private static string BuildConditionText(SqlDataReader reader)
        {
            if (reader["RequiredWins"] != DBNull.Value)
                return "Побед: " + reader["RequiredWins"];

            if (reader["RequiredGames"] != DBNull.Value)
                return "Игр: " + reader["RequiredGames"];

            if (reader["RequiredWinStreak"] != DBNull.Value)
                return "Серия побед: " + reader["RequiredWinStreak"];

            if (reader["RequiredMaxMoves"] != DBNull.Value)
                return "Победа за ходов: " + reader["RequiredMaxMoves"];

            return "Особое условие";
        }
    }
}

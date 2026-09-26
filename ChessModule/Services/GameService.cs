using ChessModule.Entities;
using ChessModule.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ChessModule.Core;
namespace ChessModule.Services
{
    public static class GameService
    {
        public static void AddWin()
        {
            AddGameResult("Победа");
        }

        public static void AddLoss()
        {
            AddGameResult("Поражение");
        }

        public static void AddFinishedChessGame(
            GameOptions options,
            GameResult result,
            int movesCount,
            int durationSeconds,
            string finishReason)
        {
            if (AuthService.CurrentUser == null)
                return;

            string userResult;
            string winner;

            if (result == GameResult.WhiteWin)
            {
                winner = "Белые";
                userResult = "Победа";
            }
            else if (result == GameResult.BlackWin)
            {
                winner = "Чёрные";
                userResult = "Поражение";
            }
            else
            {
                winner = "Ничья";
                userResult = "Ничья";
            }

            AddGameResult(
                userResult,
                options,
                movesCount,
                durationSeconds,
                finishReason,
                winner);
        }

        private static void AddGameResult(string result)
        {
            AddGameResult(result, null, 0, 0, "Добавлено вручную", string.Empty);
        }

        private static void AddGameResult(
            string result,
            GameOptions options,
            int movesCount,
            int durationSeconds,
            string finishReason,
            string winner)
        {
            if (AuthService.CurrentUser == null)
                return;

            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertGame = @"
                    INSERT INTO GameHistory
                    (UserId, Result, GameDate, OpponentType, BotDifficulty, GameDifficulty, MovesCount, DurationSeconds, FinishReason, Winner)
                    VALUES
                    (@UserId, @Result, GETDATE(), @OpponentType, @BotDifficulty, @GameDifficulty, @MovesCount, @DurationSeconds, @FinishReason, @Winner)";

                        using (SqlCommand gameCommand = new SqlCommand(insertGame, connection, transaction))
                        {
                            gameCommand.Parameters.AddWithValue("@UserId", userId);
                            gameCommand.Parameters.AddWithValue("@Result", result);
                            gameCommand.Parameters.AddWithValue("@OpponentType", options == null ? "Ручное добавление" : GameOptions.GetModeName(options.Mode));
                            gameCommand.Parameters.AddWithValue("@BotDifficulty", options == null ? (object)DBNull.Value : (int)options.BotDifficulty);
                            gameCommand.Parameters.AddWithValue("@GameDifficulty", options == null ? "-" : GameOptions.GetTimeModeName(options.TimeMode));
                            gameCommand.Parameters.AddWithValue("@MovesCount", movesCount);
                            gameCommand.Parameters.AddWithValue("@DurationSeconds", durationSeconds);
                            gameCommand.Parameters.AddWithValue("@FinishReason", finishReason ?? string.Empty);
                            gameCommand.Parameters.AddWithValue("@Winner", winner ?? string.Empty);

                            gameCommand.ExecuteNonQuery();
                        }

                        string updateUser;

                        if (result == "Победа")
                        {
                            updateUser = @"
                        UPDATE Users
                        SET Wins = Wins + 1,
                            TotalGames = TotalGames + 1,
                            WinStreak = WinStreak + 1
                        WHERE UserId = @UserId";
                        }
                        else if (result == "Поражение")
                        {
                            updateUser = @"
                        UPDATE Users
                        SET Losses = Losses + 1,
                            TotalGames = TotalGames + 1,
                            WinStreak = 0
                        WHERE UserId = @UserId";
                        }
                        else
                        {
                            updateUser = @"
                        UPDATE Users
                        SET TotalGames = TotalGames + 1,
                            WinStreak = 0
                        WHERE UserId = @UserId";
                        }

                        using (SqlCommand updateCommand = new SqlCommand(updateUser, connection, transaction))
                        {
                            updateCommand.Parameters.AddWithValue("@UserId", userId);
                            updateCommand.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            RefreshProfileDataSafely();
        }

        private static void RefreshProfileDataSafely()
        {
            try
            {
                AuthService.RefreshCurrentUser();
                AchievementService.CheckAchievements();
                RewardService.CheckRewards();
                AuthService.RefreshCurrentUser();
            }
            catch
            {
                AuthService.RefreshCurrentUser();
            }
        }
        public static List<GameHistoryItem> GetHistory()
        {
            List<GameHistoryItem> result = new List<GameHistoryItem>();

            if (AuthService.CurrentUser == null)
                return result;

            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT GameId, Result, GameDate, OpponentType, BotDifficulty, GameDifficulty,
                           MovesCount, DurationSeconds, FinishReason, Winner
                    FROM GameHistory
                    WHERE UserId = @UserId
                    ORDER BY GameDate DESC, GameId DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        int number = 1;

                        while (reader.Read())
                        {
                            result.Add(new GameHistoryItem
                            {
                                Number = number++,
                                Result = reader["Result"].ToString(),
                                GameDate = (DateTime)reader["GameDate"],
                                OpponentType = reader["OpponentType"].ToString(),
                                GameDifficulty = reader["GameDifficulty"].ToString(),
                                MovesCount = (int)reader["MovesCount"],
                                DurationSeconds = (int)reader["DurationSeconds"],
                                FinishReason = reader["FinishReason"].ToString(),
                                Winner = reader["Winner"].ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}

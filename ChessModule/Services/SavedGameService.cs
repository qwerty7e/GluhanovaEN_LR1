using ChessModule.Core;
using ChessModule.Entities;
using ChessModule.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ChessModule.Services
{
    public static class SavedGameService
    {
        public static void SaveGame(GameOptions options, ChessGame game, int whiteTimeSeconds, int blackTimeSeconds)
        {
            if (AuthService.CurrentUser == null)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                if (options.LoadedSaveId.HasValue)
                {
                    string update = @"
                        UPDATE SavedGames
                        SET Mode = @Mode,
                            BotDifficulty = @BotDifficulty,
                            GameDifficulty = @GameDifficulty,
                            Fen = @Fen,
                            WhiteTimeSeconds = @WhiteTimeSeconds,
                            BlackTimeSeconds = @BlackTimeSeconds,
                            MoveCount = @MoveCount,
                            MoveLog = @MoveLog,
                            UpdatedAt = GETDATE()
                        WHERE SaveId = @SaveId AND UserId = @UserId";

                    using (SqlCommand command = new SqlCommand(update, connection))
                    {
                        FillParameters(command, options, game, whiteTimeSeconds, blackTimeSeconds);
                        command.Parameters.AddWithValue("@SaveId", options.LoadedSaveId.Value);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    string insert = @"
                        INSERT INTO SavedGames
                        (UserId, SaveName, Mode, BotDifficulty, GameDifficulty, Fen, WhiteTimeSeconds, BlackTimeSeconds, MoveCount, MoveLog, CreatedAt, UpdatedAt)                        VALUES
                        (@UserId, @SaveName, @Mode, @BotDifficulty, @GameDifficulty, @Fen, @WhiteTimeSeconds, @BlackTimeSeconds, @MoveCount, @MoveLog, GETDATE(), GETDATE());                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand command = new SqlCommand(insert, connection))
                    {
                        FillParameters(command, options, game, whiteTimeSeconds, blackTimeSeconds);
                        command.Parameters.AddWithValue("@SaveName", "Партия " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                        int id = Convert.ToInt32(command.ExecuteScalar());
                        options.LoadedSaveId = id;
                    }
                }
            }
        }

        public static List<SavedGameInfo> GetSaves()
        {
            List<SavedGameInfo> result = new List<SavedGameInfo>();
            if (AuthService.CurrentUser == null)
                return result;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                string query = @"
                    SELECT SaveId, UserId, SaveName, Mode, BotDifficulty, GameDifficulty, Fen,
WhiteTimeSeconds, BlackTimeSeconds, MoveCount, MoveLog, UpdatedAt
                    FROM SavedGames
                    WHERE UserId = @UserId
                    ORDER BY UpdatedAt DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new SavedGameInfo
                            {
                                SaveId = (int)reader["SaveId"],
                                UserId = (int)reader["UserId"],
                                SaveName = reader["SaveName"].ToString(),
                                Mode = reader["Mode"].ToString(),
                                BotDifficulty = (int)reader["BotDifficulty"],
                                GameDifficulty = reader["GameDifficulty"].ToString(),
                                Fen = reader["Fen"].ToString(),
                                WhiteTimeSeconds = (int)reader["WhiteTimeSeconds"],
                                BlackTimeSeconds = (int)reader["BlackTimeSeconds"],
                                MoveCount = (int)reader["MoveCount"],
                                MoveLog = reader["MoveLog"] == DBNull.Value ? string.Empty : reader["MoveLog"].ToString(),
                                UpdatedAt = (DateTime)reader["UpdatedAt"]
                            });
                        }
                    }
                }
            }

            return result;
        }

        public static void DeleteSave(int saveId)
        {
            if (AuthService.CurrentUser == null)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                string query = "DELETE FROM SavedGames WHERE SaveId = @SaveId AND UserId = @UserId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SaveId", saveId);
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static GameOptions ToOptions(SavedGameInfo save)
        {
            GameOptions options = new GameOptions();
            options.LoadedSaveId = save.SaveId;
            options.LoadedFen = save.Fen;
            options.WhiteTimeSeconds = save.WhiteTimeSeconds;
            options.BlackTimeSeconds = save.BlackTimeSeconds;
            options.MoveCount = save.MoveCount;
            options.LoadedMoveLog = save.MoveLog;
            options.Mode = save.Mode == "Два игрока" ? GameMode.HumanVsHuman : GameMode.HumanVsBot;
            options.BotDifficulty = (BotDifficulty)save.BotDifficulty;

            if (save.GameDifficulty == "Блиц 5 минут")
                options.TimeMode = GameTimeMode.Blitz5;
            else if (save.GameDifficulty == "Быстрая 10 минут")
                options.TimeMode = GameTimeMode.Rapid10;
            else
                options.TimeMode = GameTimeMode.NoLimit;

            return options;
        }

        private static void FillParameters(SqlCommand command, GameOptions options, ChessGame game, int whiteTimeSeconds, int blackTimeSeconds)
        {
            command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
            command.Parameters.AddWithValue("@Mode", GameOptions.GetModeName(options.Mode));
            command.Parameters.AddWithValue("@BotDifficulty", (int)options.BotDifficulty);
            command.Parameters.AddWithValue("@GameDifficulty", GameOptions.GetTimeModeName(options.TimeMode));
            command.Parameters.AddWithValue("@Fen", game.ToFen());
            command.Parameters.AddWithValue("@WhiteTimeSeconds", whiteTimeSeconds);
            command.Parameters.AddWithValue("@BlackTimeSeconds", blackTimeSeconds);
            command.Parameters.AddWithValue("@MoveCount", game.MoveCount);
            command.Parameters.AddWithValue("@MoveLog", string.Join("|", game.MoveLog));
        }
    }
}

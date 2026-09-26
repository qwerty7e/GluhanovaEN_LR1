using ChessModule.Entities;
using ChessModule.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace ChessModule.Services
{
    public static class RewardService
    {
        private const string BoardType = "Цвет доски";
        private const string PieceColorType = "Цвет фигур";
        private const string PieceShapeType = "Форма фигур";

        public static void CheckRewards()
        {
            if (AuthService.CurrentUser == null)
                return;

            AuthService.RefreshCurrentUser();
            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                EnsureDefaultRewards(connection);

                string query = @"
                    INSERT INTO UserRewards (UserId, RewardId, IsSelected)
                    SELECT @UserId, R.RewardId,
                           CASE
                    WHEN R.Title IN (N'Классическая доска', N'Стандартные фигуры', N'Обычная форма')                                    AND NOT EXISTS
                                    (
                                        SELECT 1
                                        FROM UserRewards UR2
                                        INNER JOIN Rewards R2 ON R2.RewardId = UR2.RewardId
                                        WHERE UR2.UserId = @UserId
                                          AND UR2.IsSelected = 1
                                          AND R2.RewardType = R.RewardType
                                    )
                               THEN 1
                               ELSE 0
                           END
                    FROM Rewards R
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM UserRewards UR
                        WHERE UR.UserId = @UserId
                        AND UR.RewardId = R.RewardId
                    )
                    AND
                    (
                        R.Title IN (N'Классическая доска', N'Стандартные фигуры', N'Обычная форма')                        OR (R.Title = N'Зелёная доска' AND @Wins >= 1)
                        OR (R.Title = N'Синие фигуры' AND @Wins >= 1)
                        OR (R.Title = N'Буквенная форма' AND @TotalGames >= 3)
                        OR (R.Title = N'Красные фигуры' AND @Wins >= 3)
                        OR (R.Title = N'Круглая форма' AND @Wins >= 3)
                        OR (R.Title = N'Золотая доска' AND @Wins >= 5)
                        OR (R.Title = N'Золотые фигуры' AND @Wins >= 5)
                        OR (R.Title = N'Турнирная доска' AND @TotalGames >= 10)
                        OR (R.Title = N'Алмазные фигуры' AND @WinStreak >= 3)
                        OR
                        (
                            R.Title = N'Синяя доска'
                            AND EXISTS
                            (
                                SELECT 1
                                FROM GameHistory GH
                                WHERE GH.UserId = @UserId
                                  AND GH.Result = N'Победа'
                                  AND GH.MovesCount > 0
                                  AND GH.MovesCount <= 40
                            )
                        )
                    )";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@Wins", AuthService.CurrentUser.Wins);
                    command.Parameters.AddWithValue("@TotalGames", AuthService.CurrentUser.TotalGames);
                    command.Parameters.AddWithValue("@WinStreak", AuthService.CurrentUser.WinStreak);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static List<RewardStatus> GetRewards()
        {
            CheckRewards();

            List<RewardStatus> result = new List<RewardStatus>();
            int userId = AuthService.CurrentUser.UserId;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT R.RewardId,
                           R.Title,
                           R.[Description],
                           R.RewardType,
                           UR.UserRewardId,
                           UR.IsSelected
                    FROM Rewards R
                    LEFT JOIN UserRewards UR
                        ON UR.RewardId = R.RewardId
                        AND UR.UserId = @UserId
                    ORDER BY
                        CASE R.RewardType
                            WHEN N'Цвет доски' THEN 1
                            WHEN N'Цвет фигур' THEN 2
                            WHEN N'Форма фигур' THEN 3
                            ELSE 5
                        END,
                        R.RewardId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new RewardStatus
                            {
                                RewardId = (int)reader["RewardId"],
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                RewardType = reader["RewardType"].ToString(),
                                IsUnlocked = reader["UserRewardId"] != DBNull.Value,
                                IsSelected = reader["IsSelected"] != DBNull.Value && (bool)reader["IsSelected"]
                            });
                        }
                    }
                }
            }

            EnsureSelectedRewards(result);
            return result;
        }

        public static bool SelectReward(int rewardId)
        {
            if (AuthService.CurrentUser == null)
                return false;

            int userId = AuthService.CurrentUser.UserId;
            RewardStatus selectedReward = GetUnlockedReward(rewardId, userId);

            if (selectedReward == null || !IsSelectableType(selectedReward.RewardType))
                return false;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string resetQuery = @"
                            UPDATE UR
                            SET IsSelected = 0
                            FROM UserRewards UR
                            INNER JOIN Rewards R ON R.RewardId = UR.RewardId
                            WHERE UR.UserId = @UserId AND R.RewardType = @RewardType";

                        using (SqlCommand resetCommand = new SqlCommand(resetQuery, connection, transaction))
                        {
                            resetCommand.Parameters.AddWithValue("@UserId", userId);
                            resetCommand.Parameters.AddWithValue("@RewardType", selectedReward.RewardType);
                            resetCommand.ExecuteNonQuery();
                        }

                        string selectQuery = @"
                            UPDATE UserRewards
                            SET IsSelected = 1
                            WHERE UserId = @UserId AND RewardId = @RewardId";

                        using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection, transaction))
                        {
                            selectCommand.Parameters.AddWithValue("@UserId", userId);
                            selectCommand.Parameters.AddWithValue("@RewardId", rewardId);
                            selectCommand.ExecuteNonQuery();
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

            AuthService.RefreshCurrentUser();
            return true;
        }

        public static VisualCustomization GetVisualCustomization()
        {
            VisualCustomization settings = new VisualCustomization();

            if (AuthService.CurrentUser == null)
                return settings;

            List<RewardStatus> rewards = GetRewards();

            RewardStatus board = rewards.FirstOrDefault(r => r.RewardType == BoardType && r.IsSelected);
            RewardStatus pieceColor = rewards.FirstOrDefault(r => r.RewardType == PieceColorType && r.IsSelected);
            RewardStatus pieceShape = rewards.FirstOrDefault(r => r.RewardType == PieceShapeType && r.IsSelected);

            if (board != null)
                settings.BoardColor = board.Title;

            if (pieceColor != null)
                settings.PieceColor = pieceColor.Title;

            if (pieceShape != null)
                settings.PieceShape = pieceShape.Title;

            return settings;
        }

        private static RewardStatus GetUnlockedReward(int rewardId, int userId)
        {
            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT R.RewardId, R.Title, R.[Description], R.RewardType, UR.IsSelected
                    FROM UserRewards UR
                    INNER JOIN Rewards R ON R.RewardId = UR.RewardId
                    WHERE UR.UserId = @UserId AND UR.RewardId = @RewardId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@RewardId", rewardId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        return new RewardStatus
                        {
                            RewardId = (int)reader["RewardId"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"].ToString(),
                            RewardType = reader["RewardType"].ToString(),
                            IsUnlocked = true,
                            IsSelected = (bool)reader["IsSelected"]
                        };
                    }
                }
            }
        }

        private static void EnsureSelectedRewards(List<RewardStatus> rewards)
        {
            if (AuthService.CurrentUser == null)
                return;

            EnsureSelectedRewardForType(rewards, BoardType, "Классическая доска");
            EnsureSelectedRewardForType(rewards, PieceColorType, "Стандартные фигуры");
            EnsureSelectedRewardForType(rewards, PieceShapeType, "Обычная форма");
        }

        private static void EnsureSelectedRewardForType(List<RewardStatus> rewards, string rewardType, string defaultTitle)
        {
            if (rewards.Any(r => r.RewardType == rewardType && r.IsSelected))
                return;

            RewardStatus defaultReward = rewards.FirstOrDefault(r => r.RewardType == rewardType && r.Title == defaultTitle && r.IsUnlocked)
                ?? rewards.FirstOrDefault(r => r.RewardType == rewardType && r.IsUnlocked);

            if (defaultReward != null)
            {
                MarkRewardAsSelected(defaultReward.RewardId, rewardType);
                defaultReward.IsSelected = true;
            }
        }

        private static void MarkRewardAsSelected(int rewardId, string rewardType)
        {
            if (AuthService.CurrentUser == null)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    UPDATE UR
                    SET IsSelected = CASE WHEN UR.RewardId = @RewardId THEN 1 ELSE 0 END
                    FROM UserRewards UR
                    INNER JOIN Rewards R ON R.RewardId = UR.RewardId
                    WHERE UR.UserId = @UserId AND R.RewardType = @RewardType";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RewardId", rewardId);
                    command.Parameters.AddWithValue("@RewardType", rewardType);
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static bool IsSelectableType(string rewardType)
        {
            return rewardType == BoardType
                || rewardType == PieceColorType
                || rewardType == PieceShapeType;
        }

        private static void EnsureDefaultRewards(SqlConnection connection)
        {
            EnsureReward(connection, "Классическая доска", "Стандартные бежево-коричневые клетки.", BoardType);
            EnsureReward(connection, "Зелёная доска", "Зелёные турнирные клетки. Открывается за первую победу.", BoardType);
            EnsureReward(connection, "Золотая доска", "Золотистые клетки. Открывается за пять побед.", BoardType);
            EnsureReward(connection, "Турнирная доска", "Спокойная профессиональная расцветка. Открывается за десять партий.", BoardType);
            EnsureReward(connection, "Синяя доска", "Синяя доска за быструю победу до 40 ходов.", BoardType);

            EnsureReward(connection, "Стандартные фигуры", "Белые и чёрные фигуры.", PieceColorType);
            EnsureReward(connection, "Синие фигуры", "Сине-голубая расцветка фигур. Открывается за первую победу.", PieceColorType);
            EnsureReward(connection, "Красные фигуры", "Красная расцветка фигур. Открывается за три победы.", PieceColorType);
            EnsureReward(connection, "Золотые фигуры", "Золотая расцветка фигур. Открывается за пять побед.", PieceColorType);
            EnsureReward(connection, "Алмазные фигуры", "Холодная алмазная расцветка за серию из трёх побед.", PieceColorType);

            EnsureReward(connection, "Обычная форма", "Классические векторные шахматные фигуры.", PieceShapeType);
            EnsureReward(connection, "Буквенная форма", "Фигуры отображаются буквами K, Q, R, B, N, P. Открывается за три партии.", PieceShapeType);
            EnsureReward(connection, "Круглая форма", "Фигуры отображаются круглыми фишками с векторными силуэтами. Открывается за три победы.", PieceShapeType);
        }
        private static void EnsureReward(SqlConnection connection, string title, string description, string rewardType)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM Rewards WHERE Title = @Title)
                INSERT INTO Rewards (Title, [Description], RewardType)
                VALUES (@Title, @Description, @RewardType)";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Title", title);
                command.Parameters.AddWithValue("@Description", description);
                command.Parameters.AddWithValue("@RewardType", rewardType);
                command.ExecuteNonQuery();
            }
        }
    }
}

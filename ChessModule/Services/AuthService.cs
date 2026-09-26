using ChessModule.Entities;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace ChessModule.Services
{
    public static class AuthService
    {
        public static User CurrentUser { get; private set; }

        public static bool Register(string username, string password)
        {
            username = (username ?? string.Empty).Trim();
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @Username";

                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Username", username);

                    int count = (int)checkCommand.ExecuteScalar();

                    if (count > 0)
                        return false;
                }

                string insertQuery = @"
                    INSERT INTO Users
                    (Username, Password, Wins, Losses, TotalGames, WinStreak)
                    VALUES
                    (@Username, @Password, 0, 0, 0, 0)";

                using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                {
                    insertCommand.Parameters.AddWithValue("@Username", username);
                    insertCommand.Parameters.AddWithValue("@Password", HashPassword(password));
                    insertCommand.ExecuteNonQuery();
                }

                return true;
            }
        }

        public static bool Login(string username, string password)
        {
            username = (username ?? string.Empty).Trim();
            password = password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT UserId, Username, Password, Wins, Losses, TotalGames, WinStreak
                    FROM Users
                    WHERE Username = @Username";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return false;

                        string storedPassword = reader["Password"].ToString();
                        string passwordHash = HashPassword(password);

                        if (storedPassword != passwordHash && storedPassword != password)
                            return false;

                        CurrentUser = new User
                        {
                            UserId = (int)reader["UserId"],
                            Username = reader["Username"].ToString(),
                            Password = storedPassword,
                            Wins = (int)reader["Wins"],
                            Losses = (int)reader["Losses"],
                            TotalGames = (int)reader["TotalGames"],
                            WinStreak = (int)reader["WinStreak"],
                        };
                    }
                }
            }

            UpdatePasswordHashIfNeeded(password);
            RewardService.CheckRewards();
            RefreshCurrentUser();
            return true;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static void RefreshCurrentUser()
        {
            if (CurrentUser == null)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = @"
                    SELECT UserId, Username, Password, Wins, Losses, TotalGames, WinStreak
                    FROM Users
                    WHERE UserId = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentUser.Username = reader["Username"].ToString();
                            CurrentUser.Password = reader["Password"].ToString();
                            CurrentUser.Wins = (int)reader["Wins"];
                            CurrentUser.Losses = (int)reader["Losses"];
                            CurrentUser.TotalGames = (int)reader["TotalGames"];
                            CurrentUser.WinStreak = (int)reader["WinStreak"];
                        }
                    }
                }
            }
        }

        private static void UpdatePasswordHashIfNeeded(string password)
        {
            if (CurrentUser == null)
                return;

            string passwordHash = HashPassword(password);

            if (CurrentUser.Password == passwordHash)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);

                string query = "UPDATE Users SET Password = @Password WHERE UserId = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Password", passwordHash);
                    command.Parameters.AddWithValue("@UserId", CurrentUser.UserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();

                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }
    }
}

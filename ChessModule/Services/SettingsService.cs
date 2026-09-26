using ChessModule.Entities;
using ChessModule.Models;
using System.Data.SqlClient;

namespace ChessModule.Services
{
    public static class SettingsService
    {
        public static PlayerSettings GetSettings()
        {
            PlayerSettings settings = new PlayerSettings();
            if (AuthService.CurrentUser == null)
                return settings;

            EnsureSettingsRow();

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                string query = @"
                    SELECT MusicEnabled, EffectsEnabled, ShowCoordinates
                    FROM UserSettings
                    WHERE UserId = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            settings.MusicEnabled = (bool)reader["MusicEnabled"];
                            settings.EffectsEnabled = (bool)reader["EffectsEnabled"];
                            settings.ShowCoordinates = (bool)reader["ShowCoordinates"];
                        }
                    }
                }
            }

            return settings;
        }

        public static void SaveSettings(PlayerSettings settings)
        {
            if (AuthService.CurrentUser == null)
                return;

            EnsureSettingsRow();

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                string query = @"
                    UPDATE UserSettings
                    SET MusicEnabled = @MusicEnabled,
                        EffectsEnabled = @EffectsEnabled,
                        ShowCoordinates = @ShowCoordinates
                    WHERE UserId = @UserId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MusicEnabled", settings.MusicEnabled);
                    command.Parameters.AddWithValue("@EffectsEnabled", settings.EffectsEnabled);
                    command.Parameters.AddWithValue("@ShowCoordinates", settings.ShowCoordinates);
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureSettingsRow()
        {
            if (AuthService.CurrentUser == null)
                return;

            using (SqlConnection connection = DbConnection.GetConnection())
            {
                connection.Open();
                DbConnection.EnsureSchema(connection);
                string query = @"
                    IF NOT EXISTS (SELECT 1 FROM UserSettings WHERE UserId = @UserId)
                    INSERT INTO UserSettings (UserId, MusicEnabled, EffectsEnabled, ShowCoordinates)
                    VALUES (@UserId, 1, 1, 1)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", AuthService.CurrentUser.UserId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}

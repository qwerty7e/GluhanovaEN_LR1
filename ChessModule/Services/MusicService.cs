using ChessModule.Models;
using System;
using System.IO;
using System.Windows.Media;

namespace ChessModule.Services
{
    public static class MusicService
    {
        private static MediaPlayer player;

        public static void ApplySettings()
        {
            PlayerSettings settings = SettingsService.GetSettings();

            if (!settings.MusicEnabled)
            {
                Stop();
                return;
            }

            string musicPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", "menu.mp3");

            if (!File.Exists(musicPath))
            {
                Stop();
                return;
            }

            try
            {
                Stop();

                player = new MediaPlayer();
                player.Open(new Uri(musicPath, UriKind.Absolute));
                player.Volume = 0.3;
                player.MediaEnded += Player_MediaEnded;
                player.Play();
            }
            catch
            {
                Stop();
            }
        }

        private static void Player_MediaEnded(object sender, EventArgs e)
        {
            if (player == null)
                return;

            player.Position = TimeSpan.Zero;
            player.Play();
        }

        public static void Stop()
        {
            if (player == null)
                return;

            try
            {
                player.MediaEnded -= Player_MediaEnded;
                player.Stop();
                player.Close();
            }
            finally
            {
                player = null;
            }
        }
    }
}

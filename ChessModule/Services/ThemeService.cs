using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChessModule.Services
{
    public static class ThemeService
    {
        public static void ApplyTheme(Window window, Panel rootPanel)
        {
            Brush background = new SolidColorBrush(Color.FromRgb(238, 244, 250));

            if (window != null)
            {
                window.Background = background;
                window.WindowState = WindowState.Maximized;
                window.ResizeMode = ResizeMode.CanResize;
            }

            if (rootPanel != null)
            {
                rootPanel.Background = background;
            }
        }
    }
}
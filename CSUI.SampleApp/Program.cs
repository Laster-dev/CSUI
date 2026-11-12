using System;
using System.Windows;
using System.Windows.Media;

namespace CSUI.SampleApp
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var app = new Application();

            var window = new Window
            {
                Title = "CSUI",
                Width = 960,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 32))
            };

            return app.Run(window);
        }
    }
}

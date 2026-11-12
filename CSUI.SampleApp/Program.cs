using System;
using System.Windows;

namespace CSUI.SampleApp
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var app = new Application();
            var window = new MainWindow();
            return app.Run(window);
        }
    }
}

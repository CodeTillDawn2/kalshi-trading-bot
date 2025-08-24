using System;
using System.Linq;
using System.Windows;

namespace TradingSimulatorGUI
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Explicit opt-in to show UI: either pass --ui or set env LAUNCH_MAINWINDOW=true
            if (ShouldShowMainWindow(e))
            {
                var win = new MainWindow();
                MainWindow = win;
                win.Show();
            }
            else
            {
                // Do not create any window; exit quietly
                Shutdown(0);
            }
        }

        private static bool ShouldShowMainWindow(StartupEventArgs e)
        {
            // 1) Command-line switch (preferred, most reliable)
            if (e?.Args != null && e.Args.Any(a => string.Equals(a, "--ui", StringComparison.OrdinalIgnoreCase)))
                return true;

            // 2) Env override (useful for ad-hoc runs or CI)
            var env = Environment.GetEnvironmentVariable("LAUNCH_MAINWINDOW");
            if (!string.IsNullOrWhiteSpace(env) && bool.TryParse(env, out var flag))
                return flag;

            // Default: do NOT launch a window (prevents builds/tests/hot-reload from opening it)
            return false;
        }
    }
}

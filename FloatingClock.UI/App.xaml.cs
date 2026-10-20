using System;
using System.Windows;
using System.Windows.Threading;

namespace FloatingClock.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Catch UI thread exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Catch background/non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"UI Error: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}",
                "ChronoTask Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "ChronoTask Fatal Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
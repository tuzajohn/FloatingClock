using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

using FloatingClock.UI.Core.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FloatingClock.UI
{
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }


        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
               .ConfigureServices((hostContext, services) =>
               {
                   string dbPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "floatingclock.db");
                   services.AddDbContextFactory<FloatingClockDbContext>(options =>
                       options.UseSqlite($"Data Source={dbPath}"));

                   services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
                   services.AddTransient<MainWindow>();
               })
               .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            await AppHost!.StartAsync();

            using var scope = AppHost.Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FloatingClockDbContext>>();
            using var db = await factory.CreateDbContextAsync();
            await db.Database.MigrateAsync();

            // Catch UI thread exceptions
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Catch background/non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;


            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Gracefully clean up the host on app close
            if (AppHost != null)
            {
                await AppHost.StopAsync();
                AppHost.Dispose();
            }
            base.OnExit(e);
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
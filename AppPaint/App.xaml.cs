using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.DependencyInjection;
using AppPaint.Services;
using AppPaint.ViewModels;
using Data;
using Microsoft.EntityFrameworkCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AppPaint
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public static IServiceProvider Services { get; private set; } = null!;
        public static Window MainWindow { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Services = ConfigureServices();
            InitializeDatabase();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Database
            var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "apppaint.db");
            services.AddDbContext<AppPaintDbContext>(options =>
  options.UseSqlite($"Data Source={dbPath}"));

            // Services
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IShapeService, ShapeService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IThemeService, ThemeService>();
            services.AddScoped<DrawingService>(); // Add DrawingService

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<DrawingCanvasViewModel>();
            services.AddTransient<TemplateManagerViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<DashboardViewModel>();

            return services.BuildServiceProvider();
        }

        private void InitializeDatabase()
        {
            try
            {
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppPaintDbContext>();
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                // Log error - don't crash app
                System.Diagnostics.Debug.WriteLine($"Database migration error: {ex.Message}");
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            MainWindow = _window;
            _window.Activate();
        }
    }
}

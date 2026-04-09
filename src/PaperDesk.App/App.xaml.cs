using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaperDesk.App.Composition;
using PaperDesk.App.Shell;
using PaperDesk.Infrastructure;
using PaperDesk.Infrastructure.Configuration;
using PaperDesk.Infrastructure.Persistence;

namespace PaperDesk.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = BuildConfiguration();

        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddDebug();
        });

        services.AddSingleton<IConfiguration>(configuration);
        services.AddPaperDeskInfrastructure(configuration);
        services.AddPaperDeskApp();

        _serviceProvider = services.BuildServiceProvider();

        await InitializeDatabaseAsync(_serviceProvider);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = AppContext.BaseDirectory;

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider
            .GetRequiredService<IOptions<AppSettings>>()
            .Value;

        var initializer = serviceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(settings.Database, CancellationToken.None);
    }
}

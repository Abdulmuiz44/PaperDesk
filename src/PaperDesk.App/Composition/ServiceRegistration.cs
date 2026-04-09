using Microsoft.Extensions.DependencyInjection;
using PaperDesk.App.Shell;

namespace PaperDesk.App.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaperDeskApp(this IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}

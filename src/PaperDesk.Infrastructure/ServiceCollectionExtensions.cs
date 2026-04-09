using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperDesk.Application.Abstractions;
using PaperDesk.Infrastructure.Configuration;
using PaperDesk.Infrastructure.Duplicates;
using PaperDesk.Infrastructure.FileWatching;
using PaperDesk.Infrastructure.Indexing;
using PaperDesk.Infrastructure.Logging;
using PaperDesk.Infrastructure.Ocr;
using PaperDesk.Infrastructure.Persistence;
using PaperDesk.Infrastructure.Services;

namespace PaperDesk.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaperDeskInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection(AppSettings.SectionName));

        services.AddSingleton<SqlitePathResolver>();
        services.AddSingleton<DatabaseInitializer>();

        services.AddSingleton<IFolderWatcherService, FolderWatcherService>();
        services.AddSingleton<IOcrService, LocalOcrService>();
        services.AddSingleton<IDocumentClassifier, DocumentClassifier>();
        services.AddSingleton<IRenamingService, RenamingService>();
        services.AddSingleton<IDocumentIndexService, DocumentIndexService>();
        services.AddSingleton<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddSingleton<IActivityLogService, ActivityLogService>();
        services.AddSingleton<IDocumentRepository, SqliteDocumentRepository>();
        services.AddSingleton<IRenameSuggestionRepository, SqliteRenameSuggestionRepository>();
        services.AddSingleton<IUnitOfWork, SqliteUnitOfWork>();

        return services;
    }
}

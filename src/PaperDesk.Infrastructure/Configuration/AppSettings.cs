namespace PaperDesk.Infrastructure.Configuration;

public sealed class AppSettings
{
    public const string SectionName = "PaperDesk";

    public DatabaseSettings Database { get; init; } = new();

    public WatcherSettings Watcher { get; init; } = new();

    public OcrSettings Ocr { get; init; } = new();

    public LoggingSettings Logging { get; init; } = new();
}

public sealed class DatabaseSettings
{
    public string FilePath { get; init; } = "%LocalAppData%/PaperDesk/paperdesk.db";
}

public sealed class WatcherSettings
{
    public string[] DefaultFolders { get; init; } = Array.Empty<string>();
}

public sealed class OcrSettings
{
    public string DefaultLanguage { get; init; } = "en-US";
}

public sealed class LoggingSettings
{
    public string MinimumLevel { get; init; } = "Information";
}

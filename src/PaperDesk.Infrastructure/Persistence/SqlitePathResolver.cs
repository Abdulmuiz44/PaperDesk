using PaperDesk.Infrastructure.Configuration;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class SqlitePathResolver
{
    public string ResolvePath(DatabaseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var expanded = Environment.ExpandEnvironmentVariables(settings.FilePath);
        var normalized = expanded.Replace('/', Path.DirectorySeparatorChar);

        var directory = Path.GetDirectoryName(normalized);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return normalized;
    }
}

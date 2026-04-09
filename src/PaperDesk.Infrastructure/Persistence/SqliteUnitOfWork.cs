using PaperDesk.Application.Abstractions;

namespace PaperDesk.Infrastructure.Persistence;

public sealed class SqliteUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => Task.FromResult(0);
}

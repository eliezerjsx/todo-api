using TodoApi.Repositories;

namespace TodoApi.Data;

public interface IUnitOfWork : IDisposable
{
    ITodoRepository Todos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

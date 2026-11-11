using TodoApi.Repositories;

namespace TodoApi.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private ITodoRepository? _todos;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public ITodoRepository Todos => _todos ??= new TodoRepository(_context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

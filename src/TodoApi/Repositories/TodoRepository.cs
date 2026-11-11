using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Domain;

namespace TodoApi.Repositories;

public class TodoRepository : Repository<TodoItem>, ITodoRepository
{
    public TodoRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<TodoItem>> GetFilteredAsync(
        TodoStatus? status,
        DateTime? dueDateFrom,
        DateTime? dueDateTo,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TodoItem> query = DbSet.AsNoTracking();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (dueDateFrom.HasValue)
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);

        if (dueDateTo.HasValue)
            query = query.Where(t => t.DueDate <= dueDateTo.Value);

        return await query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);
    }
}

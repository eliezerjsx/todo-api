using TodoApi.Domain;

namespace TodoApi.Repositories;

public interface ITodoRepository : IRepository<TodoItem>
{
    Task<IReadOnlyList<TodoItem>> GetFilteredAsync(
        TodoStatus? status,
        DateTime? dueDateFrom,
        DateTime? dueDateTo,
        CancellationToken cancellationToken = default);
}

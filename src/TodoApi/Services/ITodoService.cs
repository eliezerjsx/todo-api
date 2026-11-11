using TodoApi.Dtos;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IReadOnlyList<TodoResponse>> GetAllAsync(TodoFilter filter, CancellationToken cancellationToken = default);

    Task<TodoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default);

    Task<TodoResponse?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default);

    Task<TodoResponse?> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

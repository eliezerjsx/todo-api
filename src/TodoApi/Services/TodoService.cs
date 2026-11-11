using TodoApi.Data;
using TodoApi.Domain;
using TodoApi.Dtos;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TodoService(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TodoResponse>> GetAllAsync(TodoFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.DueDateFrom.HasValue && filter.DueDateTo.HasValue && filter.DueDateFrom > filter.DueDateTo)
            throw new ArgumentException("O parâmetro dueDateFrom não pode ser maior que dueDateTo.");

        // data sem hora -> considera o dia todo
        var dueDateTo = filter.DueDateTo;
        if (dueDateTo.HasValue && dueDateTo.Value.TimeOfDay == TimeSpan.Zero)
            dueDateTo = dueDateTo.Value.Date.AddDays(1).AddTicks(-1);

        var items = await _unitOfWork.Todos.GetFilteredAsync(filter.Status, filter.DueDateFrom, dueDateTo, cancellationToken);
        return items.Select(TodoResponse.FromEntity).ToList();
    }

    public async Task<TodoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Todos.GetByIdAsync(id, cancellationToken);
        return item is null ? null : TodoResponse.FromEntity(item);
    }

    public async Task<TodoResponse> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var item = new TodoItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status,
            DueDate = request.DueDate!.Value,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _unitOfWork.Todos.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TodoResponse.FromEntity(item);
    }

    public async Task<TodoResponse?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Todos.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return null;

        item.Title = request.Title.Trim();
        item.Description = request.Description?.Trim();
        item.Status = request.Status!.Value;
        item.DueDate = request.DueDate!.Value;
        item.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _unitOfWork.Todos.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TodoResponse.FromEntity(item);
    }

    public async Task<TodoResponse?> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Todos.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return null;

        item.Status = request.Status!.Value;
        item.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        _unitOfWork.Todos.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TodoResponse.FromEntity(item);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Todos.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return false;

        _unitOfWork.Todos.Remove(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

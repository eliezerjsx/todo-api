using FluentAssertions;
using Moq;
using TodoApi.Data;
using TodoApi.Domain;
using TodoApi.Dtos;
using TodoApi.Repositories;
using TodoApi.Services;

namespace TodoApi.Tests.Services;

public class TodoServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITodoRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TodoService _service;

    public TodoServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Todos).Returns(_repository.Object);
        _service = new TodoService(_unitOfWork.Object, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task CreateAsync_DevePersistirTarefaEConfirmarUnitOfWork()
    {
        var request = new CreateTodoRequest
        {
            Title = "  Nova tarefa  ",
            Description = "Descrição",
            DueDate = new DateTime(2026, 12, 1)
        };
        TodoItem? added = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<TodoItem>(), It.IsAny<CancellationToken>()))
            .Callback<TodoItem, CancellationToken>((item, _) => { item.Id = 10; added = item; })
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Id.Should().Be(10);
        result.Title.Should().Be("Nova tarefa");
        result.Status.Should().Be(TodoStatus.Pendente);
        result.CreatedAt.Should().Be(Now.UtcDateTime);
        added.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_QuandoNaoExiste_RetornaNull()
    {
        _repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((TodoItem?)null);

        var result = await _service.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_QuandoExiste_RetornaTarefa()
    {
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(NewItem(1));

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_RepassaFiltrosAoRepositorio()
    {
        var from = new DateTime(2026, 1, 1, 8, 30, 0);
        var to = new DateTime(2026, 1, 31, 18, 0, 0);
        _repository.Setup(r => r.GetFilteredAsync(TodoStatus.EmAndamento, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem> { NewItem(1), NewItem(2) });

        var result = await _service.GetAllAsync(new TodoFilter { Status = TodoStatus.EmAndamento, DueDateFrom = from, DueDateTo = to });

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_DueDateToSemHorario_IncluiODiaInteiro()
    {
        _repository.Setup(r => r.GetFilteredAsync(null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoItem>());

        await _service.GetAllAsync(new TodoFilter { DueDateTo = new DateTime(2026, 1, 31) });

        var expectedEnd = new DateTime(2026, 2, 1).AddTicks(-1);
        _repository.Verify(r => r.GetFilteredAsync(null, null, expectedEnd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_IntervaloInvalido_LancaArgumentException()
    {
        var filter = new TodoFilter { DueDateFrom = new DateTime(2026, 2, 1), DueDateTo = new DateTime(2026, 1, 1) };

        var act = () => _service.GetAllAsync(filter);

        await act.Should().ThrowAsync<ArgumentException>();
        _repository.Verify(r => r.GetFilteredAsync(It.IsAny<TodoStatus?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_QuandoExiste_AtualizaCampos()
    {
        var item = NewItem(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _service.UpdateAsync(1, new UpdateTodoRequest
        {
            Title = "Atualizada",
            Description = "Nova descrição",
            Status = TodoStatus.Concluido,
            DueDate = new DateTime(2027, 1, 1)
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("Atualizada");
        result.Status.Should().Be(TodoStatus.Concluido);
        result.DueDate.Should().Be(new DateTime(2027, 1, 1));
        result.UpdatedAt.Should().Be(Now.UtcDateTime);
        _repository.Verify(r => r.Update(item), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_QuandoNaoExiste_RetornaNullSemSalvar()
    {
        _repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((TodoItem?)null);

        var result = await _service.UpdateAsync(5, new UpdateTodoRequest
        {
            Title = "X",
            Status = TodoStatus.Pendente,
            DueDate = DateTime.Today
        });

        result.Should().BeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_AlteraSomenteStatus()
    {
        var item = NewItem(1);
        var originalTitle = item.Title;
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _service.UpdateStatusAsync(1, new UpdateTodoStatusRequest { Status = TodoStatus.EmAndamento });

        result!.Status.Should().Be(TodoStatus.EmAndamento);
        result.Title.Should().Be(originalTitle);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_QuandoNaoExiste_RetornaNull()
    {
        _repository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((TodoItem?)null);

        var result = await _service.UpdateStatusAsync(7, new UpdateTodoStatusRequest { Status = TodoStatus.Concluido });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_QuandoExiste_RemoveERetornaTrue()
    {
        var item = NewItem(1);
        _repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var result = await _service.DeleteAsync(1);

        result.Should().BeTrue();
        _repository.Verify(r => r.Remove(item), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_QuandoNaoExiste_RetornaFalse()
    {
        _repository.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync((TodoItem?)null);

        var result = await _service.DeleteAsync(3);

        result.Should().BeFalse();
        _repository.Verify(r => r.Remove(It.IsAny<TodoItem>()), Times.Never);
    }

    private static TodoItem NewItem(int id) => new()
    {
        Id = id,
        Title = $"Tarefa {id}",
        Description = "Teste",
        Status = TodoStatus.Pendente,
        DueDate = new DateTime(2026, 12, 1),
        CreatedAt = new DateTime(2026, 1, 1)
    };

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}

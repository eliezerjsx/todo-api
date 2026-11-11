using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Domain;

namespace TodoApi.Tests.Repositories;

// sqlite em memoria pra pegar query que o EF nao consegue traduzir
public class TodoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly UnitOfWork _unitOfWork;

    public TodoRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task AddAsync_E_SaveChanges_PersisteTarefa()
    {
        var item = NewItem("A", TodoStatus.Pendente, new DateTime(2026, 5, 1));

        await _unitOfWork.Todos.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        item.Id.Should().BeGreaterThan(0);
        (await _unitOfWork.Todos.GetByIdAsync(item.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilteredAsync_FiltraPorStatus()
    {
        await SeedAsync();

        var result = await _unitOfWork.Todos.GetFilteredAsync(TodoStatus.Concluido, null, null);

        result.Should().ContainSingle().Which.Title.Should().Be("C");
    }

    [Fact]
    public async Task GetFilteredAsync_FiltraPorIntervaloDeVencimento_OrdenadoPorData()
    {
        await SeedAsync();

        var result = await _unitOfWork.Todos.GetFilteredAsync(null, new DateTime(2026, 2, 1), new DateTime(2026, 3, 31));

        result.Select(t => t.Title).Should().Equal("B", "C");
    }

    [Fact]
    public async Task GetFilteredAsync_CombinaStatusEData()
    {
        await SeedAsync();

        var result = await _unitOfWork.Todos.GetFilteredAsync(TodoStatus.Pendente, new DateTime(2026, 2, 1), null);

        result.Select(t => t.Title).Should().Equal("D");
    }

    [Fact]
    public async Task GetFilteredAsync_SemFiltros_RetornaTodas()
    {
        await SeedAsync();

        var result = await _unitOfWork.Todos.GetAllAsync();

        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task Remove_ExcluiTarefa()
    {
        var item = NewItem("X", TodoStatus.Pendente, DateTime.Today);
        await _unitOfWork.Todos.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.Todos.Remove(item);
        await _unitOfWork.SaveChangesAsync();

        (await _unitOfWork.Todos.GetByIdAsync(item.Id)).Should().BeNull();
    }

    private async Task SeedAsync()
    {
        await _unitOfWork.Todos.AddAsync(NewItem("A", TodoStatus.Pendente, new DateTime(2026, 1, 15)));
        await _unitOfWork.Todos.AddAsync(NewItem("B", TodoStatus.EmAndamento, new DateTime(2026, 2, 15)));
        await _unitOfWork.Todos.AddAsync(NewItem("C", TodoStatus.Concluido, new DateTime(2026, 3, 15)));
        await _unitOfWork.Todos.AddAsync(NewItem("D", TodoStatus.Pendente, new DateTime(2026, 4, 15)));
        await _unitOfWork.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private static TodoItem NewItem(string title, TodoStatus status, DateTime dueDate) => new()
    {
        Title = title,
        Status = status,
        DueDate = dueDate,
        CreatedAt = DateTime.UtcNow
    };

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connection.Dispose();
    }
}

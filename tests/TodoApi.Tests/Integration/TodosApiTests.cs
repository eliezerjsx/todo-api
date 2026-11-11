using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Data;
using TodoApi.Domain;
using TodoApi.Dtos;

namespace TodoApi.Tests.Integration;

public class TodosApiTests : IClassFixture<TodosApiTests.ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public TodosApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FluxoCompleto_CriarListarAtualizarRemover()
    {
        // Criar
        var create = await _client.PostAsJsonAsync("/api/todos", new
        {
            title = "Integração",
            description = "Fluxo completo",
            dueDate = "2030-06-15T10:00:00"
        }, Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        create.Headers.Location.Should().NotBeNull();
        var created = await create.Content.ReadFromJsonAsync<TodoResponse>(Json);
        created!.Status.Should().Be(TodoStatus.Pendente);

        // Obter
        var get = await _client.GetAsync($"/api/todos/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        // Atualizar
        var put = await _client.PutAsJsonAsync($"/api/todos/{created.Id}", new
        {
            title = "Integração (editada)",
            status = "EmAndamento",
            dueDate = "2030-06-20T10:00:00"
        }, Json);
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        (await put.Content.ReadFromJsonAsync<TodoResponse>(Json))!.Status.Should().Be(TodoStatus.EmAndamento);

        // Alterar status
        var patch = await _client.PatchAsJsonAsync($"/api/todos/{created.Id}/status", new { status = "Concluido" }, Json);
        patch.StatusCode.Should().Be(HttpStatusCode.OK);

        // Filtrar
        var filtered = await _client.GetFromJsonAsync<List<TodoResponse>>(
            "/api/todos?status=Concluido&dueDateFrom=2030-06-01&dueDateTo=2030-06-30", Json);
        filtered.Should().Contain(t => t.Id == created.Id);

        // Remover
        (await _client.DeleteAsync($"/api/todos/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/todos/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Criar_SemTitulo_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { dueDate = "2030-01-01" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_ComStatusInvalido_Retorna400()
    {
        var response = await _client.PostAsJsonAsync("/api/todos", new { title = "x", status = "Qualquer", dueDate = "2030-01-01" }, Json);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Listar_ComIntervaloInvertido_Retorna400()
    {
        var response = await _client.GetAsync("/api/todos?dueDateFrom=2030-02-01&dueDateTo=2030-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Swagger_EstaDisponivel()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
    }
}

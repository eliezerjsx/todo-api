using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.Domain;
using TodoApi.Dtos;
using TodoApi.Services;

namespace TodoApi.Tests.Controllers;

public class TodosControllerTests
{
    private readonly Mock<ITodoService> _service = new();
    private readonly TodosController _controller;

    public TodosControllerTests()
    {
        _controller = new TodosController(_service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task GetAll_RetornaOkComLista()
    {
        _service.Setup(s => s.GetAllAsync(It.IsAny<TodoFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TodoResponse> { Response(1) });

        var result = await _controller.GetAll(new TodoFilter(), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<TodoResponse>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_QuandoExiste_RetornaOk()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Response(1));

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_QuandoNaoExiste_Retorna404()
    {
        _service.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((TodoResponse?)null);

        var result = await _controller.GetById(1, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Create_RetornaCreatedAtAction()
    {
        _service.Setup(s => s.CreateAsync(It.IsAny<CreateTodoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(5));

        var result = await _controller.Create(new CreateTodoRequest { Title = "x", DueDate = DateTime.Today }, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(TodosController.GetById));
        created.RouteValues!["id"].Should().Be(5);
    }

    [Fact]
    public async Task Update_QuandoNaoExiste_Retorna404()
    {
        _service.Setup(s => s.UpdateAsync(9, It.IsAny<UpdateTodoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoResponse?)null);

        var result = await _controller.Update(9, new UpdateTodoRequest(), CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task UpdateStatus_QuandoExiste_RetornaOk()
    {
        _service.Setup(s => s.UpdateStatusAsync(1, It.IsAny<UpdateTodoStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response(1));

        var result = await _controller.UpdateStatus(1, new UpdateTodoStatusRequest { Status = TodoStatus.Concluido }, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_QuandoExiste_Retorna204()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_QuandoNaoExiste_Retorna404()
    {
        _service.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _controller.Delete(1, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    private static TodoResponse Response(int id) =>
        new(id, $"Tarefa {id}", null, TodoStatus.Pendente, new DateTime(2026, 12, 1), new DateTime(2026, 1, 1), null);
}

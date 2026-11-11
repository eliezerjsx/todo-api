using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos;
using TodoApi.Services;

namespace TodoApi.Controllers;

/// <summary>
/// Endpoints de gestão de tarefas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _service;

    public TodosController(ITodoService service)
    {
        _service = service;
    }

    /// <summary>Lista as tarefas, com filtros opcionais por status e data de vencimento.</summary>
    /// <remarks>
    /// Exemplos:
    ///
    ///     GET /api/todos?status=Pendente
    ///     GET /api/todos?dueDateFrom=2026-01-01&amp;dueDateTo=2026-01-31
    ///     GET /api/todos?status=EmAndamento&amp;dueDateTo=2026-12-31
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TodoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetAll([FromQuery] TodoFilter filter, CancellationToken cancellationToken)
    {
        var items = await _service.GetAllAsync(filter, cancellationToken);
        return Ok(items);
    }

    /// <summary>Obtém uma tarefa pelo ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFoundProblem(id) : Ok(item);
    }

    /// <summary>Cria uma nova tarefa.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoResponse>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Atualiza todos os dados de uma tarefa.</summary>
    [HttpPut("{id:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> Update(int id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFoundProblem(id) : Ok(updated);
    }

    /// <summary>Altera apenas o status de uma tarefa.</summary>
    [HttpPatch("{id:int}/status")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> UpdateStatus(int id, [FromBody] UpdateTodoStatusRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateStatusAsync(id, request, cancellationToken);
        return updated is null ? NotFoundProblem(id) : Ok(updated);
    }

    /// <summary>Remove uma tarefa.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var removed = await _service.DeleteAsync(id, cancellationToken);
        return removed ? NoContent() : NotFoundProblem(id);
    }

    private ObjectResult NotFoundProblem(int id) =>
        Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Tarefa não encontrada",
                detail: $"Nenhuma tarefa encontrada com o ID {id}.");
}

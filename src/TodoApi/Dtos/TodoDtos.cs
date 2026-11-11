using System.ComponentModel.DataAnnotations;
using TodoApi.Domain;

namespace TodoApi.Dtos;

/// <summary>Dados para criação de uma tarefa.</summary>
public class CreateTodoRequest
{
    /// <summary>Título da tarefa.</summary>
    /// <example>Estudar Entity Framework</example>
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição detalhada (opcional).</summary>
    /// <example>Revisar migrations e relacionamentos</example>
    [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    public string? Description { get; set; }

    /// <summary>Status inicial. Padrão: Pendente.</summary>
    /// <example>Pendente</example>
    public TodoStatus Status { get; set; } = TodoStatus.Pendente;

    /// <summary>Data de vencimento.</summary>
    /// <example>2026-12-31T18:00:00</example>
    [Required(ErrorMessage = "A data de vencimento é obrigatória.")]
    public DateTime? DueDate { get; set; }
}

/// <summary>Dados para atualização completa de uma tarefa.</summary>
public class UpdateTodoRequest
{
    /// <summary>Título da tarefa.</summary>
    /// <example>Estudar Entity Framework Core</example>
    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Descrição detalhada (opcional).</summary>
    /// <example>Revisar migrations, relacionamentos e consultas</example>
    [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    public string? Description { get; set; }

    /// <summary>Status da tarefa.</summary>
    /// <example>EmAndamento</example>
    [Required(ErrorMessage = "O status é obrigatório.")]
    public TodoStatus? Status { get; set; }

    /// <summary>Data de vencimento.</summary>
    /// <example>2026-12-31T18:00:00</example>
    [Required(ErrorMessage = "A data de vencimento é obrigatória.")]
    public DateTime? DueDate { get; set; }
}

/// <summary>Dados para alteração apenas do status.</summary>
public class UpdateTodoStatusRequest
{
    /// <summary>Novo status da tarefa.</summary>
    /// <example>Concluido</example>
    [Required(ErrorMessage = "O status é obrigatório.")]
    public TodoStatus? Status { get; set; }
}

/// <summary>Filtros opcionais para listagem de tarefas.</summary>
public class TodoFilter
{
    /// <summary>Filtra pelo status (Pendente, EmAndamento, Concluido).</summary>
    public TodoStatus? Status { get; set; }

    /// <summary>Retorna tarefas com vencimento a partir desta data (inclusive).</summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>Retorna tarefas com vencimento até esta data (inclusive). Se informada sem horário, considera o dia inteiro.</summary>
    public DateTime? DueDateTo { get; set; }
}

/// <summary>Representação de uma tarefa retornada pela API.</summary>
/// <param name="Id">Identificador da tarefa.</param>
/// <param name="Title">Título.</param>
/// <param name="Description">Descrição.</param>
/// <param name="Status">Status atual.</param>
/// <param name="DueDate">Data de vencimento.</param>
/// <param name="CreatedAt">Data de criação (UTC).</param>
/// <param name="UpdatedAt">Data da última atualização (UTC).</param>
public record TodoResponse(
    int Id,
    string Title,
    string? Description,
    TodoStatus Status,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static TodoResponse FromEntity(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.Status,
        item.DueDate,
        item.CreatedAt,
        item.UpdatedAt);
}

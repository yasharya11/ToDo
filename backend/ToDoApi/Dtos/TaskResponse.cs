namespace ToDoApi.Dtos;

/// <summary>
/// Outgoing representation of a task. The API returns this DTO rather than the EF
/// <see cref="Models.TaskItem"/> entity so the persistence model never leaks over the
/// wire. <c>UserId</c> is intentionally omitted — a caller only ever sees their own tasks.
/// </summary>
public class TaskResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateOnly DueDate { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

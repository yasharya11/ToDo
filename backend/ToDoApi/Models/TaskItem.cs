namespace ToDoApi.Models;

/// <summary>
/// A single to-do item owned by one <see cref="User"/>.
/// Named TaskItem (not Task) to avoid colliding with System.Threading.Tasks.Task;
/// the database table is "Tasks".
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Calendar due date (no time-of-day). Modeled as DateOnly so "June 30" stays
    /// June 30 in every timezone. Past dates are allowed.
    /// </summary>
    public DateOnly DueDate { get; set; }

    public bool IsCompleted { get; set; }

    /// <summary>Instant the task was created, in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Instant the task was last modified, in UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Owner of this task. Every query is scoped to this id.</summary>
    public int UserId { get; set; }
}

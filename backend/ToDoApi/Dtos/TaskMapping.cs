using ToDoApi.Models;

namespace ToDoApi.Dtos;

/// <summary>
/// Thin, hand-written DTO &lt;-&gt; entity mapping. No AutoMapper: one entity and three
/// shapes do not justify the dependency or the hidden reflection. The request mappers
/// run only after model validation has passed, so the required fields are non-null here.
/// </summary>
public static class TaskMapping
{
    /// <summary>
    /// Builds a new entity from a create request. The title arrives already trimmed from the
    /// request DTO; the task starts incomplete. Owner id and the Created/Updated timestamps are
    /// the endpoint's responsibility (they are not user input), so they are not set here.
    /// </summary>
    public static TaskItem ToEntity(this CreateTaskRequest request) => new()
    {
        Title = request.Title!,
        Description = request.Description,
        DueDate = request.DueDate!.Value,
        IsCompleted = false,
    };

    /// <summary>
    /// Copies an update request onto an existing tracked entity (the title is already trimmed at
    /// the DTO boundary). The caller refreshes UpdatedAtUtc; UserId and CreatedAtUtc are never
    /// reassigned.
    /// </summary>
    public static void ApplyTo(this UpdateTaskRequest request, TaskItem task)
    {
        task.Title = request.Title!;
        task.Description = request.Description;
        task.DueDate = request.DueDate!.Value;
        task.IsCompleted = request.IsCompleted;
    }

    /// <summary>Projects an entity to the response DTO returned over the wire.</summary>
    public static TaskResponse ToResponse(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        DueDate = task.DueDate,
        IsCompleted = task.IsCompleted,
        CreatedAtUtc = task.CreatedAtUtc,
        UpdatedAtUtc = task.UpdatedAtUtc,
    };
}

using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Dtos;

/// <summary>
/// Incoming payload for updating a task. Same fields as <see cref="CreateTaskRequest"/>
/// plus the completion flag, so a single PUT both edits a task and toggles
/// complete/reopen. See that type for why the fields are nullable.
/// </summary>
public class UpdateTaskRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string? Title { get; set; }

    [StringLength(2000, ErrorMessage = "Description must be 2000 characters or fewer.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Due date is required.")]
    public DateOnly? DueDate { get; set; }

    public bool IsCompleted { get; set; }
}

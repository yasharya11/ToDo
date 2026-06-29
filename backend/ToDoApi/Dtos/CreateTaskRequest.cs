using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Dtos;

/// <summary>
/// Incoming payload for creating a task. The rules below are enforced by the
/// [ApiController] model-validation filter (wired to the endpoints in a later phase),
/// which returns a 400 ProblemDetails listing the failing fields.
/// </summary>
/// <remarks>
/// Properties are nullable rather than using the C# <c>required</c> keyword on purpose:
/// a missing field then surfaces as a controlled DataAnnotations message ("Title is
/// required.") instead of a System.Text.Json "missing required property" deserialization
/// error, so every "bad input" case comes back in the same ProblemDetails shape.
/// </remarks>
public class CreateTaskRequest
{
    // RequiredAttribute trims before checking, so "" and "   " are both rejected.
    // StringLength caps the raw length; the stored value is trimmed during mapping.
    [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string? Title { get; set; }

    [StringLength(2000, ErrorMessage = "Description must be 2000 characters or fewer.")]
    public string? Description { get; set; }

    // DateOnly? (not DateOnly) so an omitted date stays null and Required can catch it;
    // a non-nullable DateOnly would silently default to 0001-01-01 and pass.
    [Required(ErrorMessage = "Due date is required.")]
    public DateOnly? DueDate { get; set; }
}

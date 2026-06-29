using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Dtos;

/// <summary>
/// Incoming payload for <c>POST /api/auth/register</c>. Rules are enforced by the
/// [ApiController] model-validation filter, which returns a 400 ProblemDetails on failure.
/// Fields are nullable so a missing field surfaces as a controlled DataAnnotations message
/// rather than a System.Text.Json "missing required property" error (same shape every time).
/// </summary>
public class RegisterRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "A valid email address is required.")]
    // 254 = RFC 5321 max deliverable address length; matches the Email column cap.
    [StringLength(254, ErrorMessage = "Email must be 254 characters or fewer.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string? Password { get; set; }
}

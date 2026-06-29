using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Dtos;

/// <summary>
/// Incoming payload for <c>POST /api/auth/login</c>. Only presence is validated (a missing
/// field is a 400). Email format and length rules are deliberately omitted so that a value
/// which is present but not a valid address flows through to the same generic 401 as a wrong
/// password — the response never distinguishes "bad email format" from "no such account" or
/// "wrong password" (no account enumeration).
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    public string? Password { get; set; }
}

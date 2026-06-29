namespace ToDoApi.Models;

/// <summary>
/// An account that owns tasks. Email is the unique login identifier;
/// the password is stored only as a PBKDF2 hash (see PasswordHasher in the auth phase).
/// </summary>
public class User
{
    public int Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }
}

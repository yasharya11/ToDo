namespace ToDoApi.Dtos;

/// <summary>
/// Response from a successful login: the bearer access token the SPA sends on every
/// authenticated request. Short-lived and stateless — there is no refresh token (see README).
/// </summary>
public class AuthResponse
{
    public required string AccessToken { get; set; }
}

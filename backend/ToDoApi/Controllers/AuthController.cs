using Microsoft.AspNetCore.Mvc;
using ToDoApi.Auth;
using ToDoApi.Dtos;

namespace ToDoApi.Controllers;

/// <summary>
/// Account endpoints: register an account and log in for an access token. The controller stays
/// thin — it maps HTTP to <see cref="AuthService"/> calls and chooses status codes. Model
/// validation (400) is handled automatically by [ApiController]; failures are ProblemDetails.
/// These endpoints are anonymous by design (you cannot hold a token before logging in).
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Registers a new account. Duplicate email returns 409 Conflict.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        RegisterResult result = await _auth.RegisterAsync(request.Email!, request.Password!, cancellationToken);

        if (result == RegisterResult.EmailAlreadyExists)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Email already registered.",
                detail: "An account with this email already exists.");
        }

        // 201 with no body/Location: registration creates an account but exposes no resource to
        // GET. The client's next step is to log in for a token.
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>Logs in and returns an access token. Bad credentials return a generic 401.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        string? accessToken = await _auth.LoginAsync(request.Email!, request.Password!, cancellationToken);

        if (accessToken is null)
        {
            // Deliberately generic: same response whether the email is unknown or the password
            // is wrong, so the API never reveals which accounts exist.
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials.",
                detail: "The email or password is incorrect.");
        }

        return Ok(new AuthResponse { AccessToken = accessToken });
    }
}

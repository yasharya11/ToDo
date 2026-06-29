using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using ToDoApi.Models;

namespace ToDoApi.Auth;

/// <summary>Outcome of a registration attempt, mapped to an HTTP status by the controller.</summary>
public enum RegisterResult
{
    Success,
    EmailAlreadyExists,
}

/// <summary>
/// Registration and login over <see cref="AppDbContext"/>, using the framework's
/// <see cref="IPasswordHasher{TUser}"/> (PBKDF2) to hash and verify passwords and
/// <see cref="TokenService"/> to mint access tokens. No ASP.NET Core Identity scaffolding
/// (<c>UserManager</c>/<c>SignInManager</c>) — just the two primitives this exercise needs.
/// </summary>
public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly TokenService _tokenService;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, TokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Creates an account with a hashed password. Email is normalized (trimmed + lowercased)
    /// so case/whitespace variants of the same address collide. Returns
    /// <see cref="RegisterResult.EmailAlreadyExists"/> if the address is already taken.
    /// </summary>
    public async Task<RegisterResult> RegisterAsync(string email, string password, CancellationToken cancellationToken)
    {
        string normalizedEmail = NormalizeEmail(email);

        if (await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return RegisterResult.EmailAlreadyExists;
        }

        User user = new() { Email = normalizedEmail, PasswordHash = string.Empty };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The unique email index caught a race between the check above and this insert.
            return RegisterResult.EmailAlreadyExists;
        }

        return RegisterResult.Success;
    }

    /// <summary>
    /// Verifies credentials and returns a signed access token, or <c>null</c> if the email is
    /// unknown or the password is wrong. The caller maps null to a single generic 401 so the
    /// response never reveals which of the two failed (no account enumeration).
    /// </summary>
    public async Task<string?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        string normalizedEmail = NormalizeEmail(email);

        User? user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null)
        {
            return null;
        }

        // VerifyHashedPassword returns Success, SuccessRehashNeeded, or Failed; only Failed is a
        // wrong password. A stored value that isn't a real PBKDF2 hash (the seed dev user's
        // "SEED-NO-LOGIN" placeholder, or any corrupt row) makes it throw FormatException while
        // base64-decoding — treat that as a failed login, not a 500, so such accounts simply
        // can't be logged into.
        PasswordVerificationResult verification;
        try
        {
            verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        }
        catch (FormatException)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return _tokenService.CreateAccessToken(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

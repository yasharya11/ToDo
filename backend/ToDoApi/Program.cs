using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using ToDoApi.Auth;
using ToDoApi.Data;
using ToDoApi.Models;
using ToDoApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Return failures (validation, 404, unhandled) in the standard ProblemDetails shape.
builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Thin task data-access service over AppDbContext (scoped, matching the DbContext lifetime).
builder.Services.AddScoped<TaskService>();

// EF Core + SQLite (file-based, so data survives an API restart). The connection
// string is configurable; it defaults to a todo.db file in the app's working directory.
string connectionString =
    builder.Configuration.GetConnectionString("Default") ?? "Data Source=todo.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// --- Authentication: minimal JWT (PasswordHasher + JwtBearer), see README. ---
// Bind JWT settings up front so they configure both token issuance (TokenService) and
// validation (JwtBearer) from one source. The dev signing key lives in appsettings.json so a
// fresh clone runs with no setup; production overrides it via env/secret.
JwtOptions jwtOptions =
    builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    // HS256 requires a key of at least 256 bits; fail fast with a clear message rather than
    // emitting tokens signed with a weak key.
    throw new InvalidOperationException(
        "Jwt:SigningKey must be at least 32 bytes (256 bits) for HS256. " +
        "Set a strong key in configuration (override the dev key in production).");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<TokenService>();
// PasswordHasher<User> is stateless, so a singleton is fine. AuthService uses the scoped
// DbContext, so it is scoped.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the 'sub' claim as 'sub' (no legacy mapping to ClaimTypes.NameIdentifier) so the
        // user id reads back cleanly when ownership is enforced in issue #18.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Defense-in-depth: every token this app issues carries a numeric 'sub'
        // (TokenService); reject any validated token that lacks one so the ownership
        // code that reads the user id can never fault on a malformed principal.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                string? sub = context.Principal?.FindFirst("sub")?.Value;
                if (!int.TryParse(sub, out _))
                {
                    context.Fail("Token is missing a valid numeric subject.");
                }
                return Task.CompletedTask;
            },
        };
    });
builder.Services.AddAuthorization();

// CORS for the SPA. Allowed origins are configurable (Cors:AllowedOrigins) and
// default to the Vite dev server, so a fresh clone works without extra config.
const string SpaCorsPolicy = "SpaCors";
string[] allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Apply migrations on startup so a fresh clone runs with no manual database steps.
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    // Enforce HTTPS in production only. In dev the SPA calls the API over plain http
    // (http://localhost:5270); redirecting to https would break those cross-origin
    // requests (dev-cert trust + CORS preflight).
    app.UseHttpsRedirection();
}

app.UseCors(SpaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed so the test project can spin up the API via WebApplicationFactory<Program>.
public partial class Program { }

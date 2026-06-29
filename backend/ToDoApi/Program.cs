using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ToDoApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// EF Core + SQLite (file-based, so data survives an API restart). The connection
// string is configurable; it defaults to a todo.db file in the app's working directory.
string connectionString =
    builder.Configuration.GetConnectionString("Default") ?? "Data Source=todo.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

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

app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Exposed so the test project can spin up the API via WebApplicationFactory<Program>.
public partial class Program { }

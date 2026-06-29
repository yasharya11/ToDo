using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ToDoApi.Dtos;

namespace ToDoApi.Tests;

/// <summary>
/// Boots the real API in-process for integration tests, but points EF Core at a throwaway
/// SQLite database unique to this factory instead of the dev <c>todo.db</c> named in
/// appsettings.json.
/// </summary>
/// <remarks>
/// This isolation is load-bearing, not incidental. The default
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/> reads the
/// app's own configuration, so every test run would bind to (and persist into) the same dev
/// database. Tests would then share state across runs and become order-dependent and flaky —
/// e.g. a "register" test passing the first time (201) and failing the next (409, email already
/// taken). Each factory instance gets its own temp file, so every test class starts from a
/// clean, migrated database that is deleted on dispose.
/// </remarks>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"todo-tests-{Guid.NewGuid():N}.db");

    /// <summary>The default password used by the auth helpers; satisfies the 8–128 char rule.</summary>
    private const string DefaultPassword = "Password123!";

    /// <summary>A fresh, globally-unique email so tests never collide on the unique-email index.</summary>
    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.local";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override only the connection string the app already reads (ConnectionStrings:Default),
        // before the host builds. Program's existing EF Core registration and migrate-on-startup
        // path then target this isolated file with no other changes to the application.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_databasePath}",
            });
        });
    }

    /// <summary>
    /// Registers a brand-new account, logs in, and returns an <see cref="HttpClient"/> that
    /// carries that user's bearer token on every request. Two calls yield two independent users,
    /// which is exactly what the ownership tests need.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string? email = null)
    {
        email ??= UniqueEmail();
        HttpClient client = CreateClient();

        HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = DefaultPassword });
        register.EnsureSuccessStatusCode();

        HttpResponseMessage login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = DefaultPassword });
        login.EnsureSuccessStatusCode();

        AuthResponse auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        // Dispose the host first so EF closes its SQLite connections and releases the file.
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        // Best-effort cleanup of the temp database and any SQLite side files (-wal/-shm).
        foreach (string path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
                // A throwaway temp file briefly locked by the OS is harmless; let the OS reclaim it.
            }
        }
    }
}

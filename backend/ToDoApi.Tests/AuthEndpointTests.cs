using System.Net;
using System.Net.Http.Json;
using ToDoApi.Dtos;

namespace ToDoApi.Tests;

/// <summary>
/// Register/login behavior over real HTTP. Covers the status codes the brief calls out
/// (duplicate email, wrong password) plus the surrounding happy and validation paths, and
/// confirms a freshly-issued token actually authorizes the task endpoints — closing the auth
/// loop end to end.
/// </summary>
public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string ValidPassword = "Password123!";

    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_with_valid_input_returns_201()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = CustomWebApplicationFactory.UniqueEmail(), password = ValidPassword });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_duplicate_email_returns_409()
    {
        HttpClient client = _factory.CreateClient();
        string email = CustomWebApplicationFactory.UniqueEmail();

        HttpResponseMessage first = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = ValidPassword });
        first.EnsureSuccessStatusCode();

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = ValidPassword });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_normalizes_email_so_case_variants_collide()
    {
        // Email is normalized (trimmed + lower-cased), so "Foo@x" and "foo@x" are the same account.
        HttpClient client = _factory.CreateClient();
        string local = $"Mixed-{Guid.NewGuid():N}";

        HttpResponseMessage first = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = $"{local}@Test.Local", password = ValidPassword });
        first.EnsureSuccessStatusCode();

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = $"{local.ToLowerInvariant()}@test.local", password = ValidPassword });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", ValidPassword)] // malformed address
    [InlineData("valid@test.local", "short")]   // password under 8 chars
    [InlineData("", ValidPassword)]             // missing email
    public async Task Register_with_invalid_input_returns_400(string email, string password)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_valid_credentials_returns_200_and_a_token()
    {
        HttpClient client = _factory.CreateClient();
        string email = CustomWebApplicationFactory.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = ValidPassword });

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = ValidPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AuthResponse auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        HttpClient client = _factory.CreateClient();
        string email = CustomWebApplicationFactory.UniqueEmail();
        await client.PostAsJsonAsync("/api/auth/register", new { email, password = ValidPassword });

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "WrongPassword123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_401()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = CustomWebApplicationFactory.UniqueEmail(), password = ValidPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Issued_token_authorizes_the_task_endpoints()
    {
        // The full auth loop: register → login → use the token to call a protected endpoint.
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

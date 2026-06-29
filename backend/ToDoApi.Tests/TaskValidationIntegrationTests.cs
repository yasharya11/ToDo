using System.Net;
using System.Net.Http.Json;
using ToDoApi.Dtos;

namespace ToDoApi.Tests;

/// <summary>
/// Validation enforced over real HTTP: bad task input must come back as <c>400</c> with a
/// ProblemDetails body, not be silently accepted. The DTO-level rules are unit-tested in
/// <see cref="TaskDtoValidationTests"/>; these confirm the same rules are actually wired into
/// the request pipeline and reach the client through the authenticated endpoints.
/// </summary>
public class TaskValidationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TaskValidationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Valid_create_returns_201_and_the_created_task()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Buy milk", description = "2% organic", dueDate = new DateOnly(2026, 7, 1) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        TaskResponse created = (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
        Assert.True(created.Id > 0);
        Assert.Equal("Buy milk", created.Title);
        Assert.False(created.IsCompleted);
    }

    [Fact]
    public async Task Empty_title_is_rejected_with_400_problem_details()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "", description = (string?)null, dueDate = new DateOnly(2026, 7, 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // [ApiController] returns a validation ProblemDetails body listing the failing field.
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", body);
    }

    [Fact]
    public async Task Whitespace_only_title_is_rejected_with_400()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "   ", description = (string?)null, dueDate = new DateOnly(2026, 7, 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_due_date_is_rejected_with_400()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "No due date", description = (string?)null, dueDate = (DateOnly?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("DueDate", body);
    }

    [Fact]
    public async Task Title_over_200_chars_is_rejected_with_400()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = new string('a', 201), description = (string?)null, dueDate = new DateOnly(2026, 7, 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Description_over_2000_chars_is_rejected_with_400()
    {
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Long description", description = new string('a', 2001), dueDate = new DateOnly(2026, 7, 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_with_invalid_body_is_rejected_with_400()
    {
        // Validation applies to PUT as well as POST: a task can't be edited into an invalid state.
        HttpClient client = await _factory.CreateAuthenticatedClientAsync();
        HttpResponseMessage create = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title = "Valid", description = (string?)null, dueDate = new DateOnly(2026, 7, 1) });
        TaskResponse created = (await create.Content.ReadFromJsonAsync<TaskResponse>())!;

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/tasks/{created.Id}",
            new { title = "", description = (string?)null, dueDate = (DateOnly?)null, isCompleted = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

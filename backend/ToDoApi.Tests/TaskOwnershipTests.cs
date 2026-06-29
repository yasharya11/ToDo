using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ToDoApi.Dtos;

namespace ToDoApi.Tests;

/// <summary>
/// The single most-tested behavior in the brief: a user can never reach another user's task.
/// These run end to end over real HTTP (register → login → call the task endpoints) so they
/// exercise the JWT auth, the <c>[Authorize]</c> filter, and the per-user query scoping exactly
/// as a client would hit them. Cross-user access must return <c>404</c> (not <c>403</c>) so the
/// API never reveals that the row exists.
/// </summary>
public class TaskOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TaskOwnershipTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<int> CreateTaskAsync(HttpClient client, string title = "Owned task")
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tasks",
            new { title, description = (string?)null, dueDate = new DateOnly(2026, 7, 1) });
        response.EnsureSuccessStatusCode();

        TaskResponse created = (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
        return created.Id;
    }

    [Fact]
    public async Task Get_by_id_of_another_users_task_returns_404()
    {
        HttpClient alice = await _factory.CreateAuthenticatedClientAsync();
        HttpClient bob = await _factory.CreateAuthenticatedClientAsync();
        int aliceTaskId = await CreateTaskAsync(alice);

        HttpResponseMessage response = await bob.GetAsync($"/api/tasks/{aliceTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // 404 is returned as a ProblemDetails body with the deliberately generic wording — it
        // must not hint that the task actually exists under another owner.
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Task not found", body);
    }

    [Fact]
    public async Task Update_of_another_users_task_returns_404()
    {
        HttpClient alice = await _factory.CreateAuthenticatedClientAsync();
        HttpClient bob = await _factory.CreateAuthenticatedClientAsync();
        int aliceTaskId = await CreateTaskAsync(alice);

        HttpResponseMessage response = await bob.PutAsJsonAsync(
            $"/api/tasks/{aliceTaskId}",
            new { title = "Hijacked", description = "by bob", dueDate = new DateOnly(2026, 8, 1), isCompleted = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_of_another_users_task_returns_404()
    {
        HttpClient alice = await _factory.CreateAuthenticatedClientAsync();
        HttpClient bob = await _factory.CreateAuthenticatedClientAsync();
        int aliceTaskId = await CreateTaskAsync(alice);

        HttpResponseMessage response = await bob.DeleteAsync($"/api/tasks/{aliceTaskId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_user_sees_only_their_own_tasks_in_the_list()
    {
        // Positive and negative scoping in one shot: with a task from each user in the same
        // database, Bob's list is exactly his own task — Alice's never leaks into it.
        HttpClient alice = await _factory.CreateAuthenticatedClientAsync();
        HttpClient bob = await _factory.CreateAuthenticatedClientAsync();
        await CreateTaskAsync(alice, "Alice's task");
        int bobTaskId = await CreateTaskAsync(bob, "Bob's task");

        IReadOnlyList<TaskResponse> bobTasks =
            (await bob.GetFromJsonAsync<List<TaskResponse>>("/api/tasks"))!;

        TaskResponse only = Assert.Single(bobTasks);
        Assert.Equal(bobTaskId, only.Id);
        Assert.Equal("Bob's task", only.Title);
    }

    [Fact]
    public async Task Owner_still_has_the_task_after_another_user_fails_to_reach_it()
    {
        // Proves the cross-user 404s above are real isolation, not a silent mutation: after Bob's
        // failed update + delete, Alice's task is untouched and still readable by Alice.
        HttpClient alice = await _factory.CreateAuthenticatedClientAsync();
        HttpClient bob = await _factory.CreateAuthenticatedClientAsync();
        int aliceTaskId = await CreateTaskAsync(alice, "Original title");

        await bob.PutAsJsonAsync(
            $"/api/tasks/{aliceTaskId}",
            new { title = "Hijacked", description = (string?)null, dueDate = new DateOnly(2026, 8, 1), isCompleted = true });
        await bob.DeleteAsync($"/api/tasks/{aliceTaskId}");

        HttpResponseMessage ownerGet = await alice.GetAsync($"/api/tasks/{aliceTaskId}");
        Assert.Equal(HttpStatusCode.OK, ownerGet.StatusCode);
        TaskResponse task = (await ownerGet.Content.ReadFromJsonAsync<TaskResponse>())!;
        Assert.Equal("Original title", task.Title);
        Assert.False(task.IsCompleted);
    }

    [Fact]
    public async Task Task_endpoints_require_a_token()
    {
        // No Authorization header → 401 before any ownership logic runs. A lock on the lobby is
        // worthless without locks on the apartments; this is the lock on the apartments' front door.
        HttpClient anonymous = _factory.CreateClient();

        HttpResponseMessage response = await anonymous.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_forged_or_garbage_token_is_rejected_with_401()
    {
        // A syntactically-present but invalid bearer token must not authenticate: this pins the
        // JWT validation (signature/issuer/audience), not just the "no header at all" path above.
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real.jwt");

        HttpResponseMessage response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

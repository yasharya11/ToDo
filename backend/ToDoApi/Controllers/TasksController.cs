using Microsoft.AspNetCore.Mvc;
using ToDoApi.Dtos;
using ToDoApi.Services;

namespace ToDoApi.Controllers;

/// <summary>
/// CRUD endpoints for the signed-in user's tasks. The controller stays thin: it maps HTTP
/// to <see cref="TaskService"/> calls and chooses status codes. Model validation (400) is
/// handled automatically by [ApiController]; "not found" is returned as ProblemDetails.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly TaskService _tasks;

    public TasksController(TaskService tasks)
    {
        _tasks = tasks;
    }

    /// <summary>
    /// The owner every request is scoped to. Phase 3 has no auth yet, so this is the fixed
    /// seeded dev user (see AppDbContext). This is the single seam Phase 4 replaces with the
    /// authenticated user's id from the JWT — every action below already routes ownership
    /// through it, so nothing else in this controller changes when real auth lands.
    /// </summary>
    private const int CurrentUserId = 1;

    /// <summary>Lists the current user's tasks.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<TaskResponse> tasks = await _tasks.GetAllAsync(CurrentUserId, cancellationToken);
        return Ok(tasks);
    }

    /// <summary>Gets a single task by id.</summary>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        TaskResponse? task = await _tasks.GetByIdAsync(CurrentUserId, id, cancellationToken);
        return task is null ? TaskNotFound(id) : Ok(task);
    }

    /// <summary>Creates a task and returns it with a Location header pointing at GET /{id}.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        TaskResponse created = await _tasks.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtRoute(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a task (this also toggles completion). Returns the updated task.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        TaskResponse? updated = await _tasks.UpdateAsync(CurrentUserId, id, request, cancellationToken);
        return updated is null ? TaskNotFound(id) : Ok(updated);
    }

    /// <summary>Deletes a task.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        bool deleted = await _tasks.DeleteAsync(CurrentUserId, id, cancellationToken);
        return deleted ? NoContent() : TaskNotFound(id);
    }

    /// <summary>
    /// Consistent 404 ProblemDetails for a task that isn't found. The wording is deliberately
    /// generic: once ownership is enforced (Phase 4) this same response covers "exists but
    /// owned by someone else", so it must not reveal whether the row exists.
    /// </summary>
    private ObjectResult TaskNotFound(int id) => Problem(
        statusCode: StatusCodes.Status404NotFound,
        title: "Task not found.",
        detail: $"No task with id {id} was found.");
}

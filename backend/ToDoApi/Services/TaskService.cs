using Microsoft.EntityFrameworkCore;
using ToDoApi.Data;
using ToDoApi.Dtos;
using ToDoApi.Models;

namespace ToDoApi.Services;

/// <summary>
/// Thin task data-access service over <see cref="AppDbContext"/>. There is no repository
/// layer (see README): DbContext is the unit of work and DbSet is the repository, so this
/// service just holds the EF queries the controller needs, keeping the controller down to
/// HTTP concerns.
/// </summary>
/// <remarks>
/// Every method takes the owner's <c>userId</c> and scopes its query to it, so a task that
/// belongs to another user is invisible here (read/update/delete all return "not found").
/// In Phase 3 the controller passes a single seeded dev user; Phase 4 passes the
/// authenticated user's id and the same scoping becomes real ownership enforcement.
/// </remarks>
public class TaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns all of the user's tasks, soonest due first.</summary>
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(int userId, CancellationToken cancellationToken)
    {
        List<TaskItem> tasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return tasks.Select(t => t.ToResponse()).ToList();
    }

    /// <summary>Returns the user's task with this id, or null if there is none.</summary>
    public async Task<TaskResponse?> GetByIdAsync(int userId, int id, CancellationToken cancellationToken)
    {
        TaskItem? task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

        return task?.ToResponse();
    }

    /// <summary>Creates a task owned by the user and returns it (with its generated id).</summary>
    public async Task<TaskResponse> CreateAsync(int userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        TaskItem task = request.ToEntity();
        task.UserId = userId;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        task.CreatedAtUtc = now;
        task.UpdatedAtUtc = now;

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    /// <summary>
    /// Applies the update to the user's task (this also toggles completion) and returns the
    /// updated task, or null if the user has no task with this id.
    /// </summary>
    public async Task<TaskResponse?> UpdateAsync(int userId, int id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        TaskItem? task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        request.ApplyTo(task);
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }

    /// <summary>
    /// Deletes the user's task with this id. Returns false if the user has no such task.
    /// </summary>
    public async Task<bool> DeleteAsync(int userId, int id, CancellationToken cancellationToken)
    {
        TaskItem? task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

        if (task is null)
        {
            return false;
        }

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(cancellationToken);

        return true;
    }
}

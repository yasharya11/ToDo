using Microsoft.EntityFrameworkCore;
using ToDoApi.Models;

namespace ToDoApi.Data;

/// <summary>
/// The single EF Core context for the app. DbContext is the unit of work and each
/// DbSet is the repository, so there is no separate repository layer (see README).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();

            // Phase 3 placeholder owner. Tasks carry a required UserId FK, but real
            // accounts don't exist until JWT auth lands in Phase 4. This seeds one
            // fixed dev user (Id 1) so CRUD has a valid owner to attach tasks to; the
            // controller's CurrentUserId constant points here. The hash is deliberately
            // not a real PBKDF2 hash, so this seed account cannot be logged into once
            // auth exists. Phase 4 replaces the constant with the authenticated user's
            // id and can drop this seed in a follow-up migration.
            entity.HasData(new User
            {
                Id = 1,
                Email = "dev@todo.local",
                PasswordHash = "SEED-NO-LOGIN",
            });
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);
            entity.Property(t => t.DueDate).IsRequired();

            // Ownership relationship. No navigation properties are exposed (the app
            // always queries by UserId); the FK + index back the ownership scoping,
            // and deleting a user removes their tasks.
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => t.UserId);
        });
    }
}

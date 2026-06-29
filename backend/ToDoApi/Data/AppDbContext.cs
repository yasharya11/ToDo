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
            // 254 is the RFC 5321 maximum length of a deliverable email address. Email is the
            // unique index key, so it must be bounded for a server RDBMS (a unique index can't
            // cover an unbounded column); on SQLite the cap is documentation only.
            entity.Property(u => u.Email).IsRequired().HasMaxLength(254);
            // PasswordHasher's PBKDF2 (v3) output is a fixed 84 chars regardless of password
            // length; 256 leaves headroom for a future hash format while staying bounded.
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
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

using CTFGame.Models;
using Microsoft.EntityFrameworkCore;

namespace CTFGame.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Challenge> Challenges => Set<Challenge>();
    public DbSet<Solve> Solves => Set<Solve>();
    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.UserName).UseCollation("NOCASE");
            e.HasIndex(u => u.UserName).IsUnique();
        });

        modelBuilder.Entity<Solve>()
            .HasIndex(s => new { s.UserId, s.ChallengeId })
            .IsUnique();

        modelBuilder.Entity<Submission>()
            .HasIndex(s => new { s.UserId, s.SubmittedAt });
    }
}

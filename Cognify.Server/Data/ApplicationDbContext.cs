using Cognify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<QuestionSet> QuestionSets => Set<QuestionSet>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User - Module (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Modules)
            .WithOne(m => m.OwnerUser)
            .HasForeignKey(m => m.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of modules if user is deleted (safety)

        // Module - Document (One-to-Many)
        modelBuilder.Entity<Module>()
            .HasMany(m => m.Documents)
            .WithOne(d => d.Module)
            .HasForeignKey(d => d.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Module - Note (One-to-Many)
        modelBuilder.Entity<Module>()
            .HasMany(m => m.Notes)
            .WithOne(n => n.Module)
            .HasForeignKey(n => n.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note - QuestionSet (One-to-Many)
        modelBuilder.Entity<Note>()
            .HasMany(n => n.QuestionSets)
            .WithOne(qs => qs.Note)
            .HasForeignKey(qs => qs.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // QuestionSet - Question (One-to-Many)
        modelBuilder.Entity<QuestionSet>()
            .HasMany(qs => qs.Questions)
            .WithOne(q => q.QuestionSet)
            .HasForeignKey(q => q.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Attempt Relationships
        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.QuestionSet)
            .WithMany()
            .HasForeignKey(a => a.QuestionSetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Cognify.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<Attempt> Attempts => Set<Attempt>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<UserKnowledgeState> UserKnowledgeStates => Set<UserKnowledgeState>();
    public DbSet<ConceptCluster> ConceptClusters => Set<ConceptCluster>();
    public DbSet<ConceptTopic> ConceptTopics => Set<ConceptTopic>();
    public DbSet<LearningInteraction> LearningInteractions => Set<LearningInteraction>();
    public DbSet<AnswerEvaluation> AnswerEvaluations => Set<AnswerEvaluation>();
    public DbSet<UserMistakePattern> UserMistakePatterns => Set<UserMistakePattern>();
    public DbSet<ExtractedContent> ExtractedContents => Set<ExtractedContent>();
    public DbSet<PendingQuiz> PendingQuizzes => Set<PendingQuiz>();
    public DbSet<AgentRun> AgentRuns => Set<AgentRun>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialExtraction> MaterialExtractions => Set<MaterialExtraction>();
    public DbSet<CategorySuggestionBatch> CategorySuggestionBatches => Set<CategorySuggestionBatch>();
    public DbSet<CategorySuggestionItem> CategorySuggestionItems => Set<CategorySuggestionItem>();

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
            .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths (Modules -> Notes and Modules -> Materials -> Notes)

        modelBuilder.Entity<Module>()
            .HasOne(m => m.CurrentFinalExamQuiz)
            .WithMany()
            .HasForeignKey(m => m.CurrentFinalExamQuizId)
            .OnDelete(DeleteBehavior.SetNull);

        // Module - Material (One-to-Many)
        modelBuilder.Entity<Module>()
            .HasMany(m => m.Materials)
            .WithOne(m => m.Module)
            .HasForeignKey(m => m.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note - Quiz (One-to-Many)
        modelBuilder.Entity<Note>()
            .HasMany(n => n.Quizzes)
            .WithOne(q => q.Note)
            .HasForeignKey(q => q.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Note>()
            .HasOne(n => n.SourceMaterial)
            .WithMany()
            .HasForeignKey(n => n.SourceMaterialId)
            .OnDelete(DeleteBehavior.SetNull);

        // Quiz - QuizQuestion (One-to-Many)
        modelBuilder.Entity<Quiz>()
            .HasMany(q => q.Questions)
            .WithOne(qq => qq.Quiz)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Quiz>()
            .Property(q => q.Difficulty)
            .HasConversion<string>();

        // Attempt Relationships
        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.Quiz)
            .WithMany()
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamAttempt>()
            .HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExamAttempt>()
            .HasOne(e => e.Quiz)
            .WithMany()
            .HasForeignKey(e => e.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExamAttempt>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Knowledge Model Relationships
        modelBuilder.Entity<UserKnowledgeState>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserKnowledgeState>()
            .HasOne(s => s.ConceptCluster)
            .WithMany()
            .HasForeignKey(s => s.ConceptClusterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<UserKnowledgeState>()
            .HasIndex(s => new { s.UserId, s.Topic })
            .IsUnique();

        modelBuilder.Entity<UserKnowledgeState>()
            .HasIndex(s => s.ConceptClusterId);

        modelBuilder.Entity<ConceptCluster>()
            .HasOne(c => c.Module)
            .WithMany()
            .HasForeignKey(c => c.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConceptTopic>()
            .HasOne(t => t.ConceptCluster)
            .WithMany(c => c.Topics)
            .HasForeignKey(t => t.ConceptClusterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LearningInteraction>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths (e.g., Users -> Attempts -> LearningInteractions)

        modelBuilder.Entity<LearningInteraction>()
            .HasOne(i => i.Attempt)
            .WithMany()
            .HasForeignKey(i => i.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LearningInteraction>()
            .HasOne(i => i.ExamAttempt)
            .WithMany()
            .HasForeignKey(i => i.ExamAttemptId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LearningInteraction>()
            .Property(i => i.Type)
            .HasConversion<string>();

        modelBuilder.Entity<AnswerEvaluation>()
            .HasOne(a => a.LearningInteraction)
            .WithMany()
            .HasForeignKey(a => a.LearningInteractionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserMistakePattern>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserMistakePattern>()
            .HasIndex(p => new { p.UserId, p.Topic, p.Category })
            .IsUnique();

        // Category suggestion history
        modelBuilder.Entity<CategorySuggestionBatch>()
            .HasMany(b => b.Items)
            .WithOne(i => i.Batch)
            .HasForeignKey(i => i.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CategorySuggestionBatch>()
            .HasOne<Module>()
            .WithMany()
            .HasForeignKey(b => b.ModuleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CategorySuggestionBatch>()
            .HasOne<Quiz>()
            .WithMany()
            .HasForeignKey(b => b.QuizId)
            .OnDelete(DeleteBehavior.SetNull);

        // ExtractedContent Relationships
        modelBuilder.Entity<ExtractedContent>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths (e.g., Users -> AgentRuns -> ExtractedContents)

        modelBuilder.Entity<ExtractedContent>()
            .HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExtractedContent>()
            .HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths

        modelBuilder.Entity<ExtractedContent>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // PendingQuiz Relationships
        modelBuilder.Entity<PendingQuiz>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths (e.g., Users -> AgentRuns -> PendingQuizzes)

        // AgentRun Relationships
        modelBuilder.Entity<AgentRun>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PendingQuiz>()
            .HasOne(p => p.AgentRun)
            .WithMany()
            .HasForeignKey(p => p.AgentRunId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PendingQuiz>()
            .HasOne(p => p.Note)
            .WithMany()
            .HasForeignKey(p => p.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PendingQuiz>()
            .HasOne(p => p.Module)
            .WithMany()
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.NoAction); // Prevent multiple cascade paths

        modelBuilder.Entity<ExtractedContent>()
            .HasOne(e => e.AgentRun)
            .WithMany()
            .HasForeignKey(e => e.AgentRunId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AgentRun>()
            .Property(a => a.Type)
            .HasConversion<string>();

        modelBuilder.Entity<AgentRun>()
            .Property(a => a.Status)
            .HasConversion<string>();

        // Material relationships
        modelBuilder.Entity<Material>()
            .HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Material>()
            .HasOne(m => m.SourceDocument)
            .WithMany()
            .HasForeignKey(m => m.SourceDocumentId)
            .OnDelete(DeleteBehavior.NoAction); // Avoid multiple cascade paths (Modules -> Documents -> Materials)

        modelBuilder.Entity<Material>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MaterialExtraction>()
            .HasOne(e => e.Material)
            .WithMany(m => m.Extractions)
            .HasForeignKey(e => e.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

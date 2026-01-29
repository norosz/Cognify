using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IPendingQuizService
{
    Task<PendingQuiz> CreateAsync(Guid userId, Guid noteId, Guid moduleId, string title, QuizDifficulty difficulty, int questionType, int questionCount);
    Task<List<PendingQuiz>> GetByUserAsync(Guid userId);
    Task<PendingQuiz?> GetByIdAsync(Guid id);
    Task<Quiz> SaveAsQuizAsync(Guid pendingQuizId, Guid userId);
    Task DeleteAsync(Guid id);
    Task UpdateStatusAsync(Guid id, PendingQuizStatus status, string? questionsJson = null, string? errorMessage = null, int? actualQuestionCount = null);
    Task<int> GetCountByUserAsync(Guid userId);
}

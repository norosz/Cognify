using Cognify.Server.Dtos.Exam;

namespace Cognify.Server.Services.Interfaces;

public interface IExamAttemptService
{
    Task<ExamAttemptDto> SubmitExamAttemptAsync(SubmitExamAttemptDto dto);
    Task<List<ExamAttemptDto>> GetExamAttemptsAsync(Guid moduleId);
    Task<ExamAttemptDto?> GetExamAttemptByIdAsync(Guid moduleId, Guid examAttemptId);
}

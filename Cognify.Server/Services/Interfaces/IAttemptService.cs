using Cognify.Server.DTOs;

namespace Cognify.Server.Services.Interfaces;

public interface IAttemptService
{
    Task<AttemptDto> SubmitAttemptAsync(SubmitAttemptDto dto);
    Task<List<AttemptDto>> GetAttemptsAsync(Guid quizId); // For current user
    Task<AttemptDto?> GetAttemptByIdAsync(Guid id);
}

using Cognify.Server.DTOs;

namespace Cognify.Server.Services.Interfaces;

public interface IQuizService
{
    Task<QuizDto> CreateAsync(CreateQuizDto dto);
    Task<QuizDto?> GetByIdAsync(Guid id);
    Task<List<QuizDto>> GetByNoteIdAsync(Guid noteId);
    Task<bool> DeleteAsync(Guid id);
}

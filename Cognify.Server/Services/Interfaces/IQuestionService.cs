using Cognify.Server.DTOs;

namespace Cognify.Server.Services.Interfaces;

public interface IQuestionService
{
    Task<QuestionSetDto> CreateAsync(CreateQuestionSetDto dto);
    Task<QuestionSetDto?> GetByIdAsync(Guid id);
    Task<List<QuestionSetDto>> GetByNoteIdAsync(Guid noteId);
    Task<bool> DeleteAsync(Guid id);
}

using Cognify.Server.DTOs;

namespace Cognify.Server.Services.Interfaces;

public interface IAiService
{
    Task<List<QuestionDto>> GenerateQuestionsFromNoteAsync(string noteContent, int count = 5);
}

using Cognify.Server.Models.Ai;
using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IAiService
{
    Task<List<GeneratedQuestion>> GenerateQuestionsAsync(string content, QuestionType type, int difficulty, int count);
    Task<string> ParseHandwritingAsync(Stream imageStream, string contentType);
    Task<string> GradeAnswerAsync(string question, string answer, string context);
}

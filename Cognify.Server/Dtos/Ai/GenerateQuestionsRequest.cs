using Cognify.Server.Models;
using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Ai;

public class GenerateQuestionsRequest
{
    [Required]
    public string NoteId { get; set; } = string.Empty;
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Difficulty { get; set; } = 2; // 1-3
    public int Count { get; set; } = 5;
}

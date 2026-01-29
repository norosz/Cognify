using System.ComponentModel.DataAnnotations;
using Cognify.Server.Dtos;

namespace Cognify.Server.Dtos.Adaptive;

public enum AdaptiveQuizMode
{
    Review = 0,
    Weakness = 1,
    Note = 2
}

public record AdaptiveQuizRequest
{
    [Required]
    public AdaptiveQuizMode Mode { get; init; } = AdaptiveQuizMode.Review;

    [Range(1, 20)]
    public int QuestionCount { get; init; } = 5;

    [Range(1, 20)]
    public int MaxTopics { get; init; } = 5;

    public string? QuestionType { get; init; }

    public Guid? NoteId { get; init; }

    public string? Title { get; init; }
}

public record AdaptiveQuizResponse(
    PendingQuizDto PendingQuiz,
    string? SelectedTopic,
    double? MasteryScore,
    double? ForgettingRisk
);

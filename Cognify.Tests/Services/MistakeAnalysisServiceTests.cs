using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Services;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Services;

public class MistakeAnalysisServiceTests
{
    private readonly MistakeAnalysisService _service = new();

    [Fact]
    public void DetectMistakes_ShouldReturnUnanswered_WhenNoAnswer()
    {
        var result = _service.DetectMistakes(new KnowledgeInteractionInput
        {
            QuestionId = Guid.NewGuid(),
            UserAnswer = null,
            IsCorrect = false
        });

        result.Should().Contain("Unanswered");
    }

    [Fact]
    public void DetectMistakes_ShouldReturnEmpty_WhenCorrect()
    {
        var result = _service.DetectMistakes(new KnowledgeInteractionInput
        {
            QuestionId = Guid.NewGuid(),
            UserAnswer = "True",
            IsCorrect = true
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public void DetectMistakes_ShouldReturnIncorrectAnswer_WhenIncorrect()
    {
        var result = _service.DetectMistakes(new KnowledgeInteractionInput
        {
            QuestionId = Guid.NewGuid(),
            UserAnswer = "A",
            IsCorrect = false
        });

        result.Should().Contain("IncorrectAnswer");
    }

    [Fact]
    public void UpdateMistakePatterns_ShouldIncrementCounts_FromDetectedMistakes()
    {
        var patterns = _service.UpdateMistakePatterns(null,
            new[]
            {
                new KnowledgeInteractionInput
                {
                    QuestionId = Guid.NewGuid(),
                    UserAnswer = "A",
                    IsCorrect = false,
                    DetectedMistakes = new[] { "Foo", "Bar" }
                },
                new KnowledgeInteractionInput
                {
                    QuestionId = Guid.NewGuid(),
                    UserAnswer = "B",
                    IsCorrect = false,
                    DetectedMistakes = new[] { "Foo" }
                }
            });

        patterns["incorrectCount"].Should().Be(2);
        patterns["Foo"].Should().Be(2);
        patterns["Bar"].Should().Be(1);
    }
}

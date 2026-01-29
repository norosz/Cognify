using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Services;

public class MistakeAnalysisServiceTests
{
    private readonly MistakeAnalysisService _service = new();

    [Fact]
    public void Analyze_ShouldReturnUnanswered_WhenNoAnswer()
    {
        var result = _service.Analyze(null, new KnowledgeInteractionInput
        {
            QuestionId = Guid.NewGuid(),
            UserAnswer = null,
            IsCorrect = false
        });

        result.Should().Contain("Unanswered");
    }

    [Fact]
    public void Analyze_ShouldReturnEmpty_WhenCorrect()
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            Prompt = "True/False",
            Type = QuestionType.TrueFalse,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"True\""
        };

        var result = _service.Analyze(question, new KnowledgeInteractionInput
        {
            QuestionId = question.Id,
            UserAnswer = "True",
            IsCorrect = true
        });

        result.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ShouldReturnIncorrectAnswer_WhenQuestionMissing()
    {
        var result = _service.Analyze(null, new KnowledgeInteractionInput
        {
            QuestionId = Guid.NewGuid(),
            UserAnswer = "A",
            IsCorrect = false
        });

        result.Should().Contain("IncorrectAnswer");
    }

    [Fact]
    public void Analyze_ShouldReturnOpenTextIncorrect()
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            Prompt = "Explain",
            Type = QuestionType.OpenText,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"Answer\""
        };

        var result = _service.Analyze(question, new KnowledgeInteractionInput
        {
            QuestionId = question.Id,
            UserAnswer = "Wrong",
            IsCorrect = false
        });

        result.Should().Contain("OpenTextIncorrect");
    }

    [Fact]
    public void Analyze_ShouldDetectMultipleSelectMistakes()
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            Prompt = "Select",
            Type = QuestionType.MultipleSelect,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A|B\""
        };

        var result = _service.Analyze(question, new KnowledgeInteractionInput
        {
            QuestionId = question.Id,
            UserAnswer = "A|C",
            IsCorrect = false
        });

        result.Should().Contain(new[] { "MissingSelection", "ExtraSelection" });
    }

    [Fact]
    public void Analyze_ShouldDetectOrderingMismatch()
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            Prompt = "Order",
            Type = QuestionType.Ordering,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A|B|C\""
        };

        var result = _service.Analyze(question, new KnowledgeInteractionInput
        {
            QuestionId = question.Id,
            UserAnswer = "B|A|C",
            IsCorrect = false
        });

        result.Should().Contain("OrderMismatch");
    }

    [Fact]
    public void Analyze_ShouldReturnPairingMismatch_ForMatching()
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            QuestionSetId = Guid.NewGuid(),
            Prompt = "Match",
            Type = QuestionType.Matching,
            OptionsJson = "[]",
            CorrectAnswerJson = "\"A:1|B:2\""
        };

        var result = _service.Analyze(question, new KnowledgeInteractionInput
        {
            QuestionId = question.Id,
            UserAnswer = "A:2|B:1",
            IsCorrect = false
        });

        result.Should().Contain("PairingMismatch");
    }
}

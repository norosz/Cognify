using Cognify.Server.Models;
using Cognify.Server.Services;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Services;

public class AiPromptsTests
{
    [Theory]
    [InlineData(1, "Beginner")]
    [InlineData(2, "Intermediate")]
    [InlineData(3, "Advanced")]
    [InlineData(0, "Standard")]
    public void GetDifficultySystemPrompt_ShouldReturnExpectedText(int difficulty, string expected)
    {
        var result = AiPrompts.GetDifficultySystemPrompt(difficulty);

        result.Should().Contain(expected);
    }

    [Fact]
    public void GetTypeSystemPrompt_ShouldReturnSchemaForType()
    {
        var result = AiPrompts.GetTypeSystemPrompt(QuestionType.Matching);

        result.Should().Contain("Matching");
        result.Should().Contain("pairs");
    }

    [Fact]
    public void BuildGenerationPrompt_ShouldIncludeContentAndRules()
    {
        var result = AiPrompts.BuildGenerationPrompt(3, 2, "Intermediate", "MultipleChoice", "content");

        result.Should().Contain("3");
        result.Should().Contain("Intermediate");
        result.Should().Contain("content");
        result.Should().Contain("Mandatory Output Format");
    }
}

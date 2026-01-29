using Cognify.Server.Models;
using Cognify.Server.Models.Ai;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Models;

public class AssessmentBundleTests
{
    [Fact]
    public void AssessmentBundle_ShouldAllowQuestions()
    {
        var bundle = new AssessmentBundle
        {
            Questions = [new GeneratedQuestion { Text = "Q1", Type = QuestionType.MultipleChoice }]
        };

        bundle.Questions.Should().HaveCount(1);
        bundle.Questions[0].Text.Should().Be("Q1");
    }
}

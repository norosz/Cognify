using Cognify.Server.Dtos;
using Cognify.Server.Dtos.Notes;
using Cognify.Server.Models;
using FluentAssertions;
using Xunit;

namespace Cognify.Tests.Dtos;

public class DtoCoverageTests
{
    [Fact]
    public void ExtractedContentDto_ShouldHoldValues()
    {
        var dto = new ExtractedContentDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "doc.pdf",
            "Module",
            "Text",
            DateTime.UtcNow,
            ExtractedContentStatus.Ready,
            null,
            [new ExtractedImageMetadataDto("id", "blob", "file", 1, null, null, "url")]);

        dto.DocumentName.Should().Be("doc.pdf");
        dto.Images.Should().HaveCount(1);
    }

    [Fact]
    public void SaveAsNoteRequest_ShouldHoldTitle()
    {
        var dto = new SaveAsNoteRequest("Title");

        dto.Title.Should().Be("Title");
    }

    [Fact]
    public void PendingQuizDto_ShouldHoldValues()
    {
        var dto = new PendingQuizDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Quiz", "Note", "Module", "Beginner", "MultipleChoice", 3, "Ready", null, DateTime.UtcNow);

        dto.Title.Should().Be("Quiz");
        dto.QuestionCount.Should().Be(3);
    }

    [Fact]
    public void CreatePendingQuizRequest_ShouldHoldValues()
    {
        var dto = new CreatePendingQuizRequest(Guid.NewGuid(), "Quiz", "Beginner", "MultipleChoice", 5);

        dto.QuestionCount.Should().Be(5);
    }

    [Fact]
    public void NoteEmbeddedImageDto_ShouldHoldValues()
    {
        var dto = new NoteEmbeddedImageDto("id", "blob", "file", 1, "url");

        dto.FileName.Should().Be("file");
    }
}

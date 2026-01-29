using Cognify.Server.Controllers;
using Cognify.Server.Dtos;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class PendingControllerTests
{
    private readonly Mock<IExtractedContentService> _extractedContentServiceMock = new();
    private readonly Mock<IPendingQuizService> _pendingQuizServiceMock = new();
    private readonly Mock<IUserContextService> _userContextMock = new();
    private readonly Mock<IBlobStorageService> _blobStorageMock = new();
    private readonly Guid _userId = Guid.NewGuid();

    private PendingController CreateController()
    {
        _userContextMock.Setup(x => x.GetCurrentUserId()).Returns(_userId);
        return new PendingController(
            _extractedContentServiceMock.Object,
            _pendingQuizServiceMock.Object,
            _userContextMock.Object,
            _blobStorageMock.Object);
    }

    [Fact]
    public async Task GetExtractedContents_ShouldMapDtos_WithImages()
    {
        var agentRun = new AgentRun
        {
            Id = Guid.NewGuid(),
            OutputJson = "{\"images\":[{\"id\":\"img\",\"blobPath\":\"blob\",\"fileName\":\"img.png\",\"pageNumber\":1}]}"
        };

        var content = new ExtractedContent
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            DocumentId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Document = new Document { FileName = "doc.pdf", BlobPath = "blob", ModuleId = Guid.NewGuid() },
            Module = new Module { Title = "Module", OwnerUserId = _userId },
            Text = "Text",
            AgentRun = agentRun
        };

        _blobStorageMock
            .Setup(b => b.GenerateDownloadSasToken("blob", It.IsAny<DateTimeOffset>(), It.IsAny<string?>()))
            .Returns("https://download");

        _extractedContentServiceMock.Setup(s => s.GetByUserAsync(_userId)).ReturnsAsync([content]);

        var controller = CreateController();
        var result = await controller.GetExtractedContents();

        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Images.Should().NotBeNull();
        result.Value![0].Images![0].DownloadUrl.Should().Be("https://download");
    }

    [Fact]
    public async Task GetExtractedContent_ShouldReturnNotFound_WhenMissing()
    {
        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ExtractedContent?)null);
        var controller = CreateController();

        var result = await controller.GetExtractedContent(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetExtractedContent_ShouldReturnForbid_WhenNotOwner()
    {
        var content = new ExtractedContent { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), ModuleId = Guid.NewGuid() };
        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(content.Id)).ReturnsAsync(content);
        var controller = CreateController();

        var result = await controller.GetExtractedContent(content.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetExtractedContent_ShouldReturnDto_WhenOwned()
    {
        var content = new ExtractedContent
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            DocumentId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Document = new Document { FileName = "doc.pdf", BlobPath = "blob", ModuleId = Guid.NewGuid() },
            Module = new Module { Title = "Module", OwnerUserId = _userId }
        };

        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(content.Id)).ReturnsAsync(content);
        var controller = CreateController();

        var result = await controller.GetExtractedContent(content.Id);

        result.Value.Should().NotBeNull();
        result.Value!.DocumentName.Should().Be("doc.pdf");
    }

    [Fact]
    public async Task SaveExtractedContentAsNote_ShouldReturnOk_WhenSaved()
    {
        var note = new Note { Id = Guid.NewGuid(), Title = "Note" };
        _extractedContentServiceMock
            .Setup(s => s.SaveAsNoteAsync(It.IsAny<Guid>(), _userId, "Title"))
            .ReturnsAsync(note);

        var controller = CreateController();
        var result = await controller.SaveExtractedContentAsNote(Guid.NewGuid(), new SaveAsNoteRequest("Title"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveExtractedContentAsNote_ShouldReturnNotFound_WhenMissing()
    {
        _extractedContentServiceMock
            .Setup(s => s.SaveAsNoteAsync(It.IsAny<Guid>(), _userId, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Missing"));

        var controller = CreateController();
        var result = await controller.SaveExtractedContentAsNote(Guid.NewGuid(), new SaveAsNoteRequest("Title"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteExtractedContent_ShouldReturnNoContent_WhenOwned()
    {
        var content = new ExtractedContent { Id = Guid.NewGuid(), UserId = _userId, DocumentId = Guid.NewGuid(), ModuleId = Guid.NewGuid() };
        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(content.Id)).ReturnsAsync(content);
        var controller = CreateController();

        var result = await controller.DeleteExtractedContent(content.Id);

        result.Should().BeOfType<NoContentResult>();
        _extractedContentServiceMock.Verify(s => s.DeleteAsync(content.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteExtractedContent_ShouldReturnNotFound_WhenMissing()
    {
        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ExtractedContent?)null);
        var controller = CreateController();

        var result = await controller.DeleteExtractedContent(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteExtractedContent_ShouldReturnForbid_WhenNotOwner()
    {
        var content = new ExtractedContent { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), DocumentId = Guid.NewGuid(), ModuleId = Guid.NewGuid() };
        _extractedContentServiceMock.Setup(s => s.GetByIdAsync(content.Id)).ReturnsAsync(content);
        var controller = CreateController();

        var result = await controller.DeleteExtractedContent(content.Id);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPendingQuizzes_ShouldReturnDtos()
    {
        var quiz = new PendingQuiz
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Quiz",
            Difficulty = QuizDifficulty.Beginner,
            QuestionType = 0,
            QuestionCount = 3,
            Status = PendingQuizStatus.Generating,
            Note = new Note { Title = "Note" },
            Module = new Module { Title = "Module", OwnerUserId = _userId }
        };

        _pendingQuizServiceMock.Setup(s => s.GetByUserAsync(_userId)).ReturnsAsync([quiz]);
        var controller = CreateController();

        var result = await controller.GetPendingQuizzes();

        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(1);
        result.Value![0].Title.Should().Be("Quiz");
    }

    [Fact]
    public async Task CreatePendingQuiz_ShouldReturnCreated()
    {
        var request = new CreatePendingQuizRequest(Guid.NewGuid(), "Quiz", "Advanced", "OpenText", 5);
        var quiz = new PendingQuiz
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            NoteId = request.NoteId,
            ModuleId = Guid.NewGuid(),
            Title = request.Title,
            Difficulty = QuizDifficulty.Advanced,
            QuestionType = 2,
            QuestionCount = request.QuestionCount,
            Status = PendingQuizStatus.Generating,
            Note = new Note { Title = "Note" },
            Module = new Module { Title = "Module", OwnerUserId = _userId }
        };

        _pendingQuizServiceMock
            .Setup(s => s.CreateAsync(_userId, request.NoteId, Guid.Empty, request.Title, QuizDifficulty.Advanced, 2, request.QuestionCount))
            .ReturnsAsync(quiz);

        var controller = CreateController();
        var result = await controller.CreatePendingQuiz(request);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetPendingQuiz_ShouldReturnForbid_WhenNotOwner()
    {
        var quiz = new PendingQuiz
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            NoteId = Guid.NewGuid(),
            ModuleId = Guid.NewGuid(),
            Title = "Quiz",
            Difficulty = QuizDifficulty.Beginner,
            QuestionType = 0,
            QuestionCount = 3,
            Status = PendingQuizStatus.Generating
        };

        _pendingQuizServiceMock.Setup(s => s.GetByIdAsync(quiz.Id)).ReturnsAsync(quiz);
        var controller = CreateController();

        var result = await controller.GetPendingQuiz(quiz.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPendingQuiz_ShouldReturnNotFound_WhenMissing()
    {
        _pendingQuizServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PendingQuiz?)null);
        var controller = CreateController();

        var result = await controller.GetPendingQuiz(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task SavePendingQuiz_ShouldReturnOk_WhenSaved()
    {
        var quiz = new Quiz { Id = Guid.NewGuid(), NoteId = Guid.NewGuid(), Title = "Quiz" };
        _pendingQuizServiceMock.Setup(s => s.SaveAsQuizAsync(It.IsAny<Guid>(), _userId)).ReturnsAsync(quiz);

        var controller = CreateController();
        var result = await controller.SavePendingQuiz(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SavePendingQuiz_ShouldReturnBadRequest_WhenInvalid()
    {
        _pendingQuizServiceMock
            .Setup(s => s.SaveAsQuizAsync(It.IsAny<Guid>(), _userId))
            .ThrowsAsync(new InvalidOperationException("Not ready"));

        var controller = CreateController();
        var result = await controller.SavePendingQuiz(Guid.NewGuid());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SavePendingQuiz_ShouldReturnForbid_WhenUnauthorized()
    {
        _pendingQuizServiceMock
            .Setup(s => s.SaveAsQuizAsync(It.IsAny<Guid>(), _userId))
            .ThrowsAsync(new UnauthorizedAccessException());

        var controller = CreateController();
        var result = await controller.SavePendingQuiz(Guid.NewGuid());

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeletePendingQuiz_ShouldReturnNoContent_WhenOwned()
    {
        var quiz = new PendingQuiz { Id = Guid.NewGuid(), UserId = _userId, NoteId = Guid.NewGuid(), ModuleId = Guid.NewGuid(), Title = "Quiz" };
        _pendingQuizServiceMock.Setup(s => s.GetByIdAsync(quiz.Id)).ReturnsAsync(quiz);

        var controller = CreateController();
        var result = await controller.DeletePendingQuiz(quiz.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeletePendingQuiz_ShouldReturnNotFound_WhenMissing()
    {
        _pendingQuizServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((PendingQuiz?)null);
        var controller = CreateController();

        var result = await controller.DeletePendingQuiz(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPendingCount_ShouldReturnSum()
    {
        _extractedContentServiceMock.Setup(s => s.GetCountByUserAsync(_userId)).ReturnsAsync(2);
        _pendingQuizServiceMock.Setup(s => s.GetCountByUserAsync(_userId)).ReturnsAsync(3);

        var controller = CreateController();
        var result = await controller.GetPendingCount();

        result.Value.Should().Be(5);
    }
}

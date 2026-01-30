using Cognify.Server.Controllers;
using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cognify.Tests.Controllers;

public class QuizzesControllerTests
{
    [Fact]
    public async Task Create_ShouldReturnCreated_WhenSuccess()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        var dto = new CreateQuizDto { NoteId = Guid.NewGuid(), Title = "Quiz" };
        var resultDto = new QuizDto { Id = Guid.NewGuid(), NoteId = dto.NoteId, Title = "Quiz" };

        service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(resultDto);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.Create(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnForbid_WhenUnauthorized()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        var dto = new CreateQuizDto { NoteId = Guid.NewGuid(), Title = "Quiz" };

        service.Setup(s => s.CreateAsync(dto)).ThrowsAsync(new UnauthorizedAccessException());

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.Create(dto);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenMissing()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        service.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((QuizDto?)null);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenFound()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        var dto = new QuizDto { Id = Guid.NewGuid(), NoteId = Guid.NewGuid(), Title = "Quiz" };
        service.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.GetById(dto.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetByNote_ShouldReturnOk()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        service.Setup(s => s.GetByNoteIdAsync(It.IsAny<Guid>())).ReturnsAsync([]);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.GetByNote(Guid.NewGuid());

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenDeleted()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenMissing()
    {
        var service = new Mock<IQuizService>();
        var statsService = new Mock<IStatsService>();
        var categoryService = new Mock<ICategoryService>();
        service.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var controller = new QuizzesController(service.Object, statsService.Object, categoryService.Object);
        var result = await controller.Delete(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }
}

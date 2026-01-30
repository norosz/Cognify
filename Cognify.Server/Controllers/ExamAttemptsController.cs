using Cognify.Server.Dtos.Exam;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/modules/{moduleId}/exam-attempts")]
[Authorize]
public class ExamAttemptsController(IExamAttemptService examAttemptService, IAttemptReviewService reviewService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitExamAttempt(Guid moduleId, [FromBody] SubmitExamAttemptDto dto)
    {
        if (moduleId != dto.ModuleId)
        {
            return Problem("ID mismatch", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await examAttemptService.SubmitExamAttemptAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Problem("Exam quiz not found", statusCode: StatusCodes.Status404NotFound);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyExamAttempts(Guid moduleId)
    {
        var attempts = await examAttemptService.GetExamAttemptsAsync(moduleId);
        return Ok(attempts);
    }

    [HttpGet("{examAttemptId}")]
    public async Task<IActionResult> GetExamAttempt(Guid moduleId, Guid examAttemptId)
    {
        var attempt = await examAttemptService.GetExamAttemptByIdAsync(moduleId, examAttemptId);
        if (attempt == null) return NotFound();
        return Ok(attempt);
    }

    [HttpGet("{examAttemptId}/review")]
    public async Task<IActionResult> GetExamAttemptReview(Guid moduleId, Guid examAttemptId)
    {
        var review = await reviewService.GetExamAttemptReviewAsync(moduleId, examAttemptId);
        if (review == null) return NotFound();
        return Ok(review);
    }
}

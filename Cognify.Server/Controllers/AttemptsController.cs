using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/quizzes/{quizId}/attempts")]
public class AttemptsController(IAttemptService attemptService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitAttempt(Guid quizId, [FromBody] SubmitAttemptDto dto)
    {
        if (quizId != dto.QuizId)
            return Problem("ID mismatch", statusCode: StatusCodes.Status400BadRequest);

        try
        {
            var result = await attemptService.SubmitAttemptAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return Problem("Quiz not found", statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyAttempts(Guid quizId)
    {
        var attempts = await attemptService.GetAttemptsAsync(quizId);
        return Ok(attempts);
    }
}

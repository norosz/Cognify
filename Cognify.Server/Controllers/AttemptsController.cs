using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/question-sets/{questionSetId}/attempts")]
public class AttemptsController(IAttemptService attemptService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SubmitAttempt(Guid questionSetId, [FromBody] SubmitAttemptDto dto)
    {
        if (questionSetId != dto.QuestionSetId)
            return BadRequest("ID mismatch");

        try
        {
            var result = await attemptService.SubmitAttemptAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Question set not found");
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyAttempts(Guid questionSetId)
    {
        var attempts = await attemptService.GetAttemptsAsync(questionSetId);
        return Ok(attempts);
    }
}

using Cognify.Server.Dtos.Adaptive;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/adaptive-quizzes")]
public class AdaptiveQuizzesController(IAdaptiveQuizService adaptiveQuizService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AdaptiveQuizResponse>> CreateAdaptiveQuiz([FromBody] AdaptiveQuizRequest request)
    {
        if (request.QuestionCount is < 1 or > 20)
        {
            return BadRequest("QuestionCount must be between 1 and 20.");
        }

        if (request.MaxTopics is < 1 or > 20)
        {
            return BadRequest("MaxTopics must be between 1 and 20.");
        }

        if (!IsValidQuestionType(request.QuestionType))
        {
            return BadRequest("QuestionType is invalid. Allowed values: MultipleChoice, TrueFalse, OpenText, Matching, Ordering, MultipleSelect.");
        }

        try
        {
            var result = await adaptiveQuizService.CreateAdaptiveQuizAsync(request);
            return CreatedAtAction(nameof(CreateAdaptiveQuiz), new { id = result.PendingQuiz.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static bool IsValidQuestionType(string? questionType)
    {
        if (string.IsNullOrWhiteSpace(questionType))
        {
            return true;
        }

        return Enum.TryParse<QuestionType>(questionType, true, out _);
    }
}

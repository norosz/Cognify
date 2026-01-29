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
            return Problem("QuestionCount must be between 1 and 20.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.MaxTopics is < 1 or > 20)
        {
            return Problem("MaxTopics must be between 1 and 20.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (!IsValidQuestionType(request.QuestionType))
        {
            return Problem("QuestionType is invalid. Allowed values: MultipleChoice, TrueFalse, OpenText, Matching, Ordering, MultipleSelect.", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var result = await adaptiveQuizService.CreateAdaptiveQuizAsync(request);
            return CreatedAtAction(nameof(CreateAdaptiveQuiz), new { id = result.PendingQuiz.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (UnauthorizedAccessException)
        {
            return Problem("Forbidden", statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
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

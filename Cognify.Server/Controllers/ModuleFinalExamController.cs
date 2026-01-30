using Cognify.Server.Dtos.Modules;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/modules/{moduleId}/final-exam")]
[Authorize]
public class ModuleFinalExamController(IFinalExamService finalExamService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FinalExamDto>> GetFinalExam(Guid moduleId)
    {
        var result = await finalExamService.GetFinalExamAsync(moduleId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("regenerate")]
    public async Task<ActionResult> RegenerateFinalExam(Guid moduleId, [FromBody] RegenerateFinalExamRequest request)
    {
        try
        {
            var pending = await finalExamService.RegenerateFinalExamAsync(moduleId, request);
            return Ok(new { pendingQuizId = pending.Id });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("pending/{pendingQuizId}/save")]
    public async Task<ActionResult<FinalExamSaveResultDto>> SaveFinalExam(Guid moduleId, Guid pendingQuizId)
    {
        try
        {
            var result = await finalExamService.SaveFinalExamAsync(moduleId, pendingQuizId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}

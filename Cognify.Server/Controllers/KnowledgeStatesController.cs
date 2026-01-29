using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/knowledge-states")]
public class KnowledgeStatesController(IKnowledgeStateService knowledgeStateService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserKnowledgeStateDto>>> GetMyStates()
    {
        var states = await knowledgeStateService.GetMyStatesAsync();
        return Ok(states);
    }

    [HttpGet("review-queue")]
    public async Task<ActionResult<List<ReviewQueueItemDto>>> GetReviewQueue([FromQuery] int maxItems = 10)
    {
        var items = await knowledgeStateService.GetReviewQueueAsync(maxItems);
        return Ok(items);
    }
}

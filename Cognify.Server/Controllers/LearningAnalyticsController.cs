using Cognify.Server.Dtos.Analytics;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/learning-analytics")]
public class LearningAnalyticsController(ILearningAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<LearningAnalyticsSummaryDto>> GetSummary([FromQuery] int days = 30, [FromQuery] int trendDays = 14, [FromQuery] int maxTopics = 10)
    {
        if (days is < 1 or > 365)
        {
            return BadRequest("days must be between 1 and 365.");
        }

        if (trendDays is < 1 or > 365)
        {
            return BadRequest("trendDays must be between 1 and 365.");
        }

        if (maxTopics is < 1 or > 50)
        {
            return BadRequest("maxTopics must be between 1 and 50.");
        }

        var summary = await analyticsService.GetSummaryAsync(days, trendDays, maxTopics);
        return Ok(summary);
    }
}

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
    public async Task<ActionResult<LearningAnalyticsSummaryDto>> GetSummary()
    {
        var summary = await analyticsService.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("trends")]
    public async Task<ActionResult<PerformanceTrendsDto>> GetTrends(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int bucketDays = 7)
    {
        if (bucketDays is < 1 or > 90)
        {
            return BadRequest("bucketDays must be between 1 and 90.");
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            return BadRequest("from must be <= to.");
        }

        var trends = await analyticsService.GetTrendsAsync(from, to, bucketDays);
        return Ok(trends);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<TopicDistributionDto>> GetTopics([FromQuery] int maxTopics = 20, [FromQuery] int maxWeakTopics = 5)
    {
        if (maxTopics is < 1 or > 100)
        {
            return BadRequest("maxTopics must be between 1 and 100.");
        }

        if (maxWeakTopics is < 0 or > 50)
        {
            return BadRequest("maxWeakTopics must be between 0 and 50.");
        }

        var topics = await analyticsService.GetTopicDistributionAsync(maxTopics, maxWeakTopics);
        return Ok(topics);
    }

    [HttpGet("retention-heatmap")]
    public async Task<ActionResult<List<RetentionHeatmapPointDto>>> GetRetentionHeatmap([FromQuery] int maxTopics = 30)
    {
        if (maxTopics is < 1 or > 200)
        {
            return BadRequest("maxTopics must be between 1 and 200.");
        }

        var points = await analyticsService.GetRetentionHeatmapAsync(maxTopics);
        return Ok(points);
    }

    [HttpGet("decay-forecast")]
    public async Task<ActionResult<DecayForecastDto>> GetDecayForecast([FromQuery] int maxTopics = 6, [FromQuery] int days = 14, [FromQuery] int stepDays = 2)
    {
        if (maxTopics is < 1 or > 50)
        {
            return BadRequest("maxTopics must be between 1 and 50.");
        }

        if (days is < 1 or > 365)
        {
            return BadRequest("days must be between 1 and 365.");
        }

        if (stepDays is < 1 or > 30)
        {
            return BadRequest("stepDays must be between 1 and 30.");
        }

        var forecast = await analyticsService.GetDecayForecastAsync(maxTopics, days, stepDays);
        return Ok(forecast);
    }
}

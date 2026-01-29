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
    public async Task<ActionResult<PerformanceTrendsDto>> GetTrends([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int bucketDays = 7)
    {
        var trends = await analyticsService.GetTrendsAsync(from, to, bucketDays);
        return Ok(trends);
    }

    [HttpGet("topics")]
    public async Task<ActionResult<TopicDistributionDto>> GetTopics([FromQuery] int maxTopics = 20, [FromQuery] int maxWeakTopics = 5)
    {
        var topics = await analyticsService.GetTopicDistributionAsync(maxTopics, maxWeakTopics);
        return Ok(topics);
    }

    [HttpGet("retention-heatmap")]
    public async Task<ActionResult<List<RetentionHeatmapPointDto>>> GetRetentionHeatmap([FromQuery] int maxTopics = 30)
    {
        var points = await analyticsService.GetRetentionHeatmapAsync(maxTopics);
        return Ok(points);
    }

    [HttpGet("decay-forecast")]
    public async Task<ActionResult<DecayForecastDto>> GetDecayForecast([FromQuery] int maxTopics = 6, [FromQuery] int days = 14, [FromQuery] int stepDays = 2)
    {
        var forecast = await analyticsService.GetDecayForecastAsync(maxTopics, days, stepDays);
        return Ok(forecast);
    }
}

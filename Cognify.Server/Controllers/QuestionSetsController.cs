using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/quizzes")]
public class QuizzesController(IQuizService quizService, IStatsService statsService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuizDto dto)
    {
        try
        {
            var result = await quizService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await quizService.GetByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(Guid id)
    {
        var stats = await statsService.GetQuizStatsAsync(id);
        if (stats == null) return NotFound();
        return Ok(stats);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetByNote([FromQuery] Guid noteId)
    {
        // Require noteId?
        var results = await quizService.GetByNoteIdAsync(noteId);
        return Ok(results);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await quizService.DeleteAsync(id);
        if (!success)
            return NotFound(); // Or Forbidden, but service returns boolean
            
        return NoContent();
    }
}

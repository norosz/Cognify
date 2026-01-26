using Cognify.Server.DTOs;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/question-sets")]
public class QuestionSetsController(IQuestionService questionService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQuestionSetDto dto)
    {
        try
        {
            var result = await questionService.CreateAsync(dto);
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
        var result = await questionService.GetByIdAsync(id);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetByNote([FromQuery] Guid noteId)
    {
        // Require noteId?
        var results = await questionService.GetByNoteIdAsync(noteId);
        return Ok(results);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await questionService.DeleteAsync(id);
        if (!success)
            return NotFound(); // Or Forbidden, but service returns boolean
            
        return NoContent();
    }
}

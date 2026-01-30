using Cognify.Server.DTOs;
using Cognify.Server.Dtos.Categories;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/quizzes")]
public class QuizzesController(IQuizService quizService, IStatsService statsService, ICategoryService categoryService) : ControllerBase
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

    [HttpPost("{id}/categories/suggest")]
    public async Task<IActionResult> SuggestCategories(Guid id, [FromBody] CategorySuggestionRequest request)
    {
        try
        {
            var suggestions = await categoryService.SuggestQuizCategoriesAsync(id, request.MaxSuggestions);
            if (suggestions == null) return NotFound();
            return Ok(suggestions);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Category suggestion unavailable",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpGet("{id}/categories/history")]
    public async Task<IActionResult> GetCategoryHistory(Guid id, [FromQuery] int take = 10, [FromQuery] Guid? cursor = null)
    {
        if (take < 1 || take > 50)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid take",
                Detail = "Take must be between 1 and 50.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var history = await categoryService.GetQuizCategoryHistoryAsync(id, take, cursor);
            if (history == null) return NotFound();
            return Ok(history);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid cursor",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPut("{id}/category")]
    public async Task<IActionResult> SetCategory(Guid id, [FromBody] SetCategoryRequest request)
    {
        var success = await categoryService.SetQuizCategoryAsync(id, request.CategoryLabel);
        if (!success) return NotFound();
        return Ok();
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

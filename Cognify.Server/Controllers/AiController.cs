using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly INoteService _noteService;

    public AiController(IAiService aiService, INoteService noteService)
    {
        _aiService = aiService;
        _noteService = noteService;
    }

    [HttpPost("questions/from-note/{noteId}")]
    public async Task<IActionResult> GenerateQuestions(Guid noteId)
    {
        var note = await _noteService.GetByIdAsync(noteId);
        if (note == null)
            return NotFound("Note not found or you do not have permission to access it.");

        if (string.IsNullOrWhiteSpace(note.Content))
            return BadRequest("Note content is empty.");

        try 
        {
            var questions = await _aiService.GenerateQuestionsFromNoteAsync(note.Content);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to generate questions. Please try again later.", details = ex.Message });
        }
    }
}

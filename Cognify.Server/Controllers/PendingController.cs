using Cognify.Server.Dtos;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/pending")]
[Authorize]
public class PendingController(
    IExtractedContentService extractedContentService,
    IPendingQuizService pendingQuizService,
    IUserContextService userContext) : ControllerBase
{
    private Guid GetUserId() => userContext.GetCurrentUserId();

    #region Extracted Contents

    /// <summary>
    /// Get all pending extracted contents for the current user.
    /// </summary>
    [HttpGet("extracted-contents")]
    public async Task<ActionResult<List<ExtractedContentDto>>> GetExtractedContents()
    {
        var userId = GetUserId();
        var contents = await extractedContentService.GetByUserAsync(userId);

        return contents.Select(c => new ExtractedContentDto(
            c.Id,
            c.DocumentId,
            c.ModuleId,
            c.Document?.FileName ?? "Unknown",
            c.Module?.Title ?? "Unknown",
            c.Text,
            c.ExtractedAt,
            c.Status,
            c.ErrorMessage
        )).ToList();
    }

    /// <summary>
    /// Get a specific extracted content by ID.
    /// </summary>
    [HttpGet("extracted-contents/{id}")]
    public async Task<ActionResult<ExtractedContentDto>> GetExtractedContent(Guid id)
    {
        var content = await extractedContentService.GetByIdAsync(id);
        if (content == null) return NotFound();
        if (content.UserId != GetUserId()) return Forbid();

        return new ExtractedContentDto(
            content.Id,
            content.DocumentId,
            content.ModuleId,
            content.Document?.FileName ?? "Unknown",
            content.Module?.Title ?? "Unknown",
            content.Text,
            content.ExtractedAt,
            content.Status,
            content.ErrorMessage
        );
    }

    /// <summary>
    /// Save extracted content as a new Note.
    /// </summary>
    [HttpPost("extracted-contents/{id}/save-as-note")]
    public async Task<ActionResult> SaveExtractedContentAsNote(Guid id, [FromBody] SaveAsNoteRequest request)
    {
        try
        {
            var note = await extractedContentService.SaveAsNoteAsync(id, GetUserId(), request.Title);
            return Ok(new { noteId = note.Id });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Delete an extracted content (discard without saving).
    /// </summary>
    [HttpDelete("extracted-contents/{id}")]
    public async Task<ActionResult> DeleteExtractedContent(Guid id)
    {
        var content = await extractedContentService.GetByIdAsync(id);
        if (content == null) return NotFound();
        if (content.UserId != GetUserId()) return Forbid();

        await extractedContentService.DeleteAsync(id);
        return NoContent();
    }

    #endregion

    #region Pending Quizzes

    /// <summary>
    /// Get all pending quizzes for the current user.
    /// </summary>
    [HttpGet("quizzes")]
    public async Task<ActionResult<List<PendingQuizDto>>> GetPendingQuizzes()
    {
        var userId = GetUserId();
        var quizzes = await pendingQuizService.GetByUserAsync(userId);

        return quizzes.Select(q => new PendingQuizDto(
            q.Id,
            q.NoteId,
            q.ModuleId,
            q.Title,
            q.Note?.Title ?? "Unknown",
            q.Module?.Title ?? "Unknown",
            q.Difficulty.ToString(),
            GetQuestionTypeName(q.QuestionType),
            q.QuestionCount,
            q.Status.ToString(),
            q.ErrorMessage,
            q.CreatedAt
        )).ToList();
    }

    /// <summary>
    /// Initiate a new quiz generation. Creates a pending quiz and starts AI task.
    /// </summary>
    [HttpPost("quizzes")]
    public async Task<ActionResult<PendingQuizDto>> CreatePendingQuiz([FromBody] CreatePendingQuizRequest request)
    {
        var userId = GetUserId();
        
        // This would ideally be backgrounded, but for now we'll initiate it.
        // In a real app, we'd fire an event or use a background worker.
        
        // Map string difficulty to enum
        if (!Enum.TryParse<QuizDifficulty>(request.Difficulty, true, out var difficulty))
            difficulty = QuizDifficulty.Intermediate;

        // Map type string
        int typeValue = request.QuestionType.ToLower() switch
        {
            "mixed" => -1,
            "multiplechoice" => 0,
            "truefalse" => 1,
            "opentext" => 2,
            "matching" => 3,
            "ordering" => 4,
            _ => 0
        };

        var quiz = await pendingQuizService.CreateAsync(
            userId, 
            request.NoteId, 
            Guid.Empty, // We'll look up module from note in service or here
            request.Title,
            difficulty,
            typeValue,
            request.QuestionCount
        );

        return CreatedAtAction(nameof(GetPendingQuiz), new { id = quiz.Id }, new PendingQuizDto(
            quiz.Id,
            quiz.NoteId,
            quiz.ModuleId,
            quiz.Title,
            quiz.Note?.Title ?? "Unknown",
            quiz.Module?.Title ?? "Unknown",
            quiz.Difficulty.ToString(),
            GetQuestionTypeName(quiz.QuestionType),
            quiz.QuestionCount,
            quiz.Status.ToString(),
            quiz.ErrorMessage,
            quiz.CreatedAt
        ));
    }

    /// <summary>
    /// Get a specific pending quiz by ID.
    /// </summary>
    [HttpGet("quizzes/{id}")]
    public async Task<ActionResult<PendingQuizDto>> GetPendingQuiz(Guid id)
    {
        var quiz = await pendingQuizService.GetByIdAsync(id);
        if (quiz == null) return NotFound();
        if (quiz.UserId != GetUserId()) return Forbid();

        return new PendingQuizDto(
            quiz.Id,
            quiz.NoteId,
            quiz.ModuleId,
            quiz.Title,
            quiz.Note?.Title ?? "Unknown",
            quiz.Module?.Title ?? "Unknown",
            quiz.Difficulty.ToString(),
            GetQuestionTypeName(quiz.QuestionType),
            quiz.QuestionCount,
            quiz.Status.ToString(),
            quiz.ErrorMessage,
            quiz.CreatedAt
        );
    }

    /// <summary>
    /// Save pending quiz as a real QuestionSet.
    /// </summary>
    [HttpPost("quizzes/{id}/save")]
    public async Task<ActionResult> SavePendingQuiz(Guid id)
    {
        try
        {
            var questionSet = await pendingQuizService.SaveAsQuizAsync(id, GetUserId());
            return Ok(new { questionSetId = questionSet.Id });
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

    /// <summary>
    /// Delete a pending quiz (discard without saving).
    /// </summary>
    [HttpDelete("quizzes/{id}")]
    public async Task<ActionResult> DeletePendingQuiz(Guid id)
    {
        var quiz = await pendingQuizService.GetByIdAsync(id);
        if (quiz == null) return NotFound();
        if (quiz.UserId != GetUserId()) return Forbid();

        await pendingQuizService.DeleteAsync(id);
        return NoContent();
    }

    #endregion

    #region Counts

    /// <summary>
    /// Get total pending items count for badge display.
    /// </summary>
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetPendingCount()
    {
        var userId = GetUserId();
        var extractedCount = await extractedContentService.GetCountByUserAsync(userId);
        var quizCount = await pendingQuizService.GetCountByUserAsync(userId);
        return extractedCount + quizCount;
    }

    #endregion

    private static string GetQuestionTypeName(int type) => type switch
    {
        -1 => "Mixed",
        0 => "MultipleChoice",
        1 => "TrueFalse",
        2 => "OpenText",
        3 => "Matching",
        4 => "Ordering",
        _ => "Unknown"
    };
}

using Cognify.Server.Dtos.Ai;
using Cognify.Server.Models;
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
    private readonly IDocumentService _documentService;
    private readonly IExtractedContentService _extractedContentService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiService aiService, 
        INoteService noteService, 
        IDocumentService documentService,
        IExtractedContentService extractedContentService,
        IUserContextService userContext,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _noteService = noteService;
        _documentService = documentService;
        _extractedContentService = extractedContentService;
        _userContext = userContext;
        _logger = logger;
    }

    private Guid GetUserId() => _userContext.GetCurrentUserId();

    [HttpPost("extract-text/{documentId}")]
    public async Task<IActionResult> ExtractText(Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);

        if (document == null)
            return NotFound("Document not found.");

        if (string.IsNullOrEmpty(document.BlobPath))
            return BadRequest("Invalid document path.");

        // Basic extension check
        var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf", 
            _ => "application/octet-stream"
        };
        
        if (!contentType.StartsWith("image/"))
        {
            return BadRequest("Only image files are currently supported for handwriting extraction.");
        }

        try
        {
            var userId = GetUserId();
            var extractedContent = await _extractedContentService.CreatePendingAsync(userId, documentId, document.ModuleId);
            return Accepted(new { extractedContentId = extractedContent.Id, status = ExtractedContentStatus.Processing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate extraction for document {DocumentId}", documentId);
            return Problem("Failed to initiate extraction.");
        }
    }

    [HttpPost("questions/generate")]
    public async Task<IActionResult> GenerateQuestions([FromBody] GenerateQuestionsRequest request)
    {
        if (request.Difficulty < 1 || request.Difficulty > 3) return BadRequest("Difficulty must be between 1 and 3.");
        if (request.Count < 1 || request.Count > 20) return BadRequest("Count must be between 1 and 20.");

        var note = await _noteService.GetByIdAsync(Guid.Parse(request.NoteId));
        if (note == null)
            return NotFound("Note not found.");

        if (string.IsNullOrWhiteSpace(note.Content))
            return BadRequest("Note content is empty.");

        try 
        {
            var questions = await _aiService.GenerateQuestionsAsync(note.Content, request.Type, request.Difficulty, request.Count);
            return Ok(questions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate questions for note {NoteId}", request.NoteId);
            return Problem("Failed to generate questions.");
        }
    }

    [HttpPost("grade")]
    public async Task<IActionResult> GradeAnswer([FromBody] GradeAnswerRequest request)
    {
        try
        {
            var result = await _aiService.GradeAnswerAsync(request.Question, request.Answer, request.Context);
            return Ok(new { analysis = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to grade answer");
            return Problem("Grading failed.");
        }
    }
}

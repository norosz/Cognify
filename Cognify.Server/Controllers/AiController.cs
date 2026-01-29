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
    private readonly IMaterialService _materialService;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiService aiService, 
        INoteService noteService, 
        IDocumentService documentService,
        IExtractedContentService extractedContentService,
        IMaterialService materialService,
        IUserContextService userContext,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _noteService = noteService;
        _documentService = documentService;
        _extractedContentService = extractedContentService;
        _materialService = materialService;
        _userContext = userContext;
        _logger = logger;
    }

    private Guid GetUserId() => _userContext.GetCurrentUserId();

    [HttpPost("extract-text/{documentId}")]
    public async Task<IActionResult> ExtractText(Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);

        if (document == null)
            return Problem("Document not found.", statusCode: StatusCodes.Status404NotFound);

        if (string.IsNullOrEmpty(document.BlobPath))
            return Problem("Invalid document path.", statusCode: StatusCodes.Status400BadRequest);

        if (document.Status != Cognify.Server.Dtos.Documents.DocumentStatus.Uploaded)
            return Problem("Document must be uploaded before extraction.", statusCode: StatusCodes.Status400BadRequest);

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
            ".txt" => "text/plain",
            ".json" => "text/plain",
            ".yaml" => "text/plain",
            ".yml" => "text/plain",
            ".md" => "text/markdown",
            ".html" => "text/html",
            ".htm" => "text/html",
            ".mhtml" => "text/html",
            ".epub" => "application/epub+zip",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
        
        var isImage = contentType.StartsWith("image/");
        var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        var isText = contentType.StartsWith("text/") || contentType == "application/xhtml+xml";
        var isOffice = contentType is "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            or "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            or "application/epub+zip";
        if (!isImage && !isPdf && !isText && !isOffice)
        {
            return Problem("Only supported document formats can be extracted.", statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var userId = GetUserId();
            await _materialService.EnsureForDocumentAsync(documentId, userId);
            var extractedContent = await _extractedContentService.CreatePendingAsync(userId, documentId, document.ModuleId);
            return Accepted(new ExtractTextResponse(extractedContent.Id, ExtractedContentStatus.Processing));
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
        if (request.Difficulty < 1 || request.Difficulty > 3)
            return Problem("Difficulty must be between 1 and 3.", statusCode: StatusCodes.Status400BadRequest);
        if (request.Count < 1 || request.Count > 20)
            return Problem("Count must be between 1 and 20.", statusCode: StatusCodes.Status400BadRequest);
        if (!Guid.TryParse(request.NoteId, out var noteId))
            return Problem("Invalid note id.", statusCode: StatusCodes.Status400BadRequest);

        var note = await _noteService.GetByIdAsync(noteId);
        if (note == null)
            return Problem("Note not found.", statusCode: StatusCodes.Status404NotFound);

        if (string.IsNullOrWhiteSpace(note.Content))
            return Problem("Note content is empty.", statusCode: StatusCodes.Status400BadRequest);

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

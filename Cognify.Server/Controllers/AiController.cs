using Cognify.Server.Dtos.Ai;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    private readonly INoteService _noteService;
    private readonly IDocumentService _documentService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IExtractedContentService _extractedContentService;
    private readonly IUserContextService _userContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AiController(
        IAiService aiService, 
        INoteService noteService, 
        IDocumentService documentService, 
        IBlobStorageService blobStorageService,
        IExtractedContentService extractedContentService,
        IUserContextService userContext,
        IServiceScopeFactory serviceScopeFactory)
    {
        _aiService = aiService;
        _noteService = noteService;
        _documentService = documentService;
        _blobStorageService = blobStorageService;
        _extractedContentService = extractedContentService;
        _userContext = userContext;
        _serviceScopeFactory = serviceScopeFactory;
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
            // 1. Create Pending Record Immediately (Synchronous to current request)
            var userId = GetUserId();
            var extractedContent = await _extractedContentService.CreatePendingAsync(userId, documentId, document.ModuleId);
            
            // 2. Update with Text (Fire-and-Forget)
            _ = Task.Run(async () => 
            {
                try 
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var aiService = scope.ServiceProvider.GetRequiredService<IAiService>();
                    var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
                    var extractionService = scope.ServiceProvider.GetRequiredService<IExtractedContentService>();

                    // Re-download stream in background
                    // We cannot re-use the 'document' variable safely if context is disposed? 
                    // 'document' is a DTO/POCO from Service so it should be fine, but let's be safe.
                    // The path string is safe.
                    var stream = await blobService.DownloadStreamAsync(document.BlobPath);
                    
                    var text = await aiService.ParseHandwritingAsync(stream, contentType);
                    
                    await extractionService.UpdateAsync(extractedContent.Id, text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Background Extraction Failed: {ex.Message}");
                    try 
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var extractionService = scope.ServiceProvider.GetRequiredService<IExtractedContentService>();
                        await extractionService.MarkAsErrorAsync(extractedContent.Id, "AI Extraction failed. Please try again.");
                    } 
                    catch { /* Swallow error logging failure */ }
                }
            });

            return Accepted(new { extractedContentId = extractedContent.Id, status = "Processing" });
        }
        catch (Exception ex)
        {
             return StatusCode(500, new { error = "Failed to initiate extraction.", details = ex.Message });
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
            return StatusCode(500, new { error = "Failed to generate questions.", details = ex.Message });
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
            return StatusCode(500, new { error = "Grading failed.", details = ex.Message });
        }
    }
}

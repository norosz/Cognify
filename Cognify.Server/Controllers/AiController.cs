using Cognify.Server.Dtos.Ai;
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
    private readonly IBlobStorageService _blobStorageService;

    public AiController(IAiService aiService, INoteService noteService, IDocumentService documentService, IBlobStorageService blobStorageService)
    {
        _aiService = aiService;
        _noteService = noteService;
        _documentService = documentService;
        _blobStorageService = blobStorageService;
    }

    [HttpPost("extract-text/{documentId}")]
    public async Task<IActionResult> ExtractText(Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);

        if (document == null)
            return NotFound("Document not found.");

        if (string.IsNullOrEmpty(document.BlobPath))
            return BadRequest("Invalid document path.");

        try
        {
            var stream = await _blobStorageService.DownloadStreamAsync(document.BlobPath);
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf", // GPT-4o Vision might support PDF pages? No, mostly images.
                _ => "application/octet-stream"
            };

            // Basic check to ensure it's an image for vision model
            if (!contentType.StartsWith("image/"))
            {
                return BadRequest("Only image files are currently supported for handwriting extraction.");
            }

            var text = await _aiService.ParseHandwritingAsync(stream, contentType);
            return Ok(new { text });
        }
        catch (Exception ex)
        {
             return StatusCode(500, new { error = "AI Processing failed.", details = ex.Message });
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

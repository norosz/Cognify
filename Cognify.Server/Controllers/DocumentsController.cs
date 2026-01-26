using Cognify.Server.Dtos.Documents;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".txt", ".md", ".png", ".jpg", ".jpeg" };

    public DocumentsController(IDocumentService documentService, ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    [HttpPost("modules/{moduleId}/documents/initiate")]
    public async Task<ActionResult<UploadInitiateResponse>> InitiateUpload(Guid moduleId, [FromBody] UploadInitiateRequest request)
    {
        try
        {
            var response = await _documentService.InitiateUploadAsync(moduleId, request.FileName, request.ContentType);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Module not found for upload initiation. ModuleId: {ModuleId}", moduleId);
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized upload attempt. ModuleId: {ModuleId}", moduleId);
            return StatusCode(403, ex.Message);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error initiating upload for ModuleId: {ModuleId}", moduleId);
             return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("documents/{documentId}/complete")]
    public async Task<ActionResult<DocumentDto>> CompleteUpload(Guid documentId)
    {
        try
        {
            var document = await _documentService.CompleteUploadAsync(documentId);
            return Ok(document);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpGet("modules/{moduleId}/documents")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetModuleDocuments(Guid moduleId)
    {
        try
        {
            var documents = await _documentService.GetModuleDocumentsAsync(moduleId);
            return Ok(documents);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpDelete("documents/{id}")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            await _documentService.DeleteDocumentAsync(id);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }
}

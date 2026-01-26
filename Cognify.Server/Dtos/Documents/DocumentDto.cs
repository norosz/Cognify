using Cognify.Server.Models;

namespace Cognify.Server.Dtos.Documents;

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string BlobPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty; // Extracted from BlobPath or stored separately? 
    // Spec says "BlobPath". I'll format BlobPath to be just the name or URL? 
    // Usually BlobPath is internal. But Spec says "BlobPath, Status...".
    // I'll assume BlobPath is the file name in the container for now.
    public DocumentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Documents;

public record UploadInitiateRequest(
    [Required] string FileName,
    [Required] string ContentType,
    [Required] long FileSize
);

public record UploadInitiateResponse(
    Guid DocumentId,
    string SasUrl,
    string BlobName
);

public record DocumentDto(
    Guid Id,
    Guid ModuleId,
    string FileName,
    string BlobPath,
    DocumentStatus Status,
    DateTime CreatedAt,
    long FileSize,
    string? DownloadUrl = null // SAS URL for download
);

public enum DocumentStatus
{
    Pending = 0,
    Uploaded = 1,
    Error = 2
}

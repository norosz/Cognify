namespace Cognify.Server.Dtos.Notes;

public record NoteSourceDocumentDto(
    Guid DocumentId,
    string FileName,
    string Status,
    DateTime CreatedAt,
    long? FileSizeBytes,
    string? DownloadUrl
);

public record NoteSourceExtractionDto(
    Guid ExtractedContentId,
    Guid DocumentId,
    string FileName,
    DateTime ExtractedAt,
    string Status,
    bool IsSaved,
    string? DownloadUrl
);

public record NoteSourcesDto
{
    public Guid NoteId { get; init; }
    public Guid ModuleId { get; init; }
    public List<NoteSourceDocumentDto> UploadedDocuments { get; init; } = [];
    public List<NoteSourceExtractionDto> ExtractedDocuments { get; init; } = [];
}

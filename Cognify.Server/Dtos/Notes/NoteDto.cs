namespace Cognify.Server.Dtos.Notes;

public class NoteDto
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public Guid? SourceMaterialId { get; set; }
    public required string Title { get; set; }
    public string? Content { get; set; }
    public string? UserContent { get; set; }
    public string? AiContent { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<NoteEmbeddedImageDto>? EmbeddedImages { get; set; }
}

public record NoteEmbeddedImageDto(
    string Id,
    string BlobPath,
    string FileName,
    int PageNumber,
    string? DownloadUrl
);

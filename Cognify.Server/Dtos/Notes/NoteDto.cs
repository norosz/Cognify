namespace Cognify.Server.Dtos.Notes;

public class NoteDto
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public required string Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
}

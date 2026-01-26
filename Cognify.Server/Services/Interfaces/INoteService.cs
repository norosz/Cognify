using Cognify.Server.Dtos.Notes;

namespace Cognify.Server.Services.Interfaces;

public interface INoteService
{
    Task<List<NoteDto>> GetByModuleIdAsync(Guid moduleId);
    Task<NoteDto?> GetByIdAsync(Guid id);
    Task<NoteDto> CreateAsync(CreateNoteDto dto);
    Task<NoteDto?> UpdateAsync(Guid id, UpdateNoteDto dto);
    Task<bool> DeleteAsync(Guid id);
}

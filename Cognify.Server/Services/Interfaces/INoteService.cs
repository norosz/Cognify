using Cognify.Server.Dtos.Notes;

namespace Cognify.Server.Services.Interfaces;

public interface INoteService
{
    Task<List<NoteDto>> GetByModuleIdAsync(Guid moduleId);
    Task<NoteDto?> GetByIdAsync(Guid id);
    Task<NoteSourcesDto?> GetSourcesAsync(Guid id);
    Task<NoteDto> CreateAsync(CreateNoteDto dto);
    Task<NoteDto?> UpdateAsync(Guid id, UpdateNoteDto dto);
    Task<NoteDto?> UpdateExamInclusionAsync(Guid id, NoteExamInclusionDto dto);
    Task<bool> DeleteAsync(Guid id);
}

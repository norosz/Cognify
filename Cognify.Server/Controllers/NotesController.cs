using Cognify.Server.Dtos.Notes;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController(INoteService noteService) : ControllerBase
{
    [HttpGet("module/{moduleId}")]
    public async Task<ActionResult<List<NoteDto>>> GetByModuleId(Guid moduleId)
    {
        var notes = await noteService.GetByModuleIdAsync(moduleId);
        return Ok(notes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NoteDto>> GetById(Guid id)
    {
        var note = await noteService.GetByIdAsync(id);
        if (note == null)
        {
            return NotFound();
        }
        return Ok(note);
    }

    [HttpGet("{id}/sources")]
    public async Task<ActionResult<NoteSourcesDto>> GetSources(Guid id)
    {
        var sources = await noteService.GetSourcesAsync(id);
        if (sources == null)
        {
            return NotFound();
        }

        return Ok(sources);
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create(CreateNoteDto dto)
    {
        try
        {
            var createdNote = await noteService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdNote.Id }, createdNote);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NoteDto>> Update(Guid id, UpdateNoteDto dto)
    {
        var updatedNote = await noteService.UpdateAsync(id, dto);
        if (updatedNote == null)
        {
            return NotFound();
        }
        return Ok(updatedNote);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await noteService.DeleteAsync(id);
        if (!result)
        {
            return NotFound();
        }
        return NoContent();
    }
}

using Cognify.Server.Dtos.Modules;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModulesController(IModuleService moduleService, IStatsService statsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ModuleDto>>> GetModules()
    {
        var modules = await moduleService.GetModulesAsync();
        return Ok(modules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ModuleDto>> GetModule(Guid id)
    {
        var module = await moduleService.GetModuleAsync(id);
        if (module == null) return NotFound();
        return Ok(module);
    }

    [HttpGet("{id}/stats")]
    public async Task<ActionResult> GetModuleStats(Guid id)
    {
        var stats = await statsService.GetModuleStatsAsync(id);
        if (stats == null) return NotFound();
        return Ok(stats);
    }

    [HttpPost]
    public async Task<ActionResult<ModuleDto>> CreateModule(CreateModuleDto dto)
    {
        var module = await moduleService.CreateModuleAsync(dto);
        return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ModuleDto>> UpdateModule(Guid id, UpdateModuleDto dto)
    {
        var module = await moduleService.UpdateModuleAsync(id, dto);
        if (module == null) return NotFound();
        return Ok(module);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteModule(Guid id)
    {
        var success = await moduleService.DeleteModuleAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}

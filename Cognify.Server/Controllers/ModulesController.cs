using System.Security.Claims;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModulesController(IModuleService moduleService) : ControllerBase
{
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<List<ModuleDto>>> GetModules()
    {
        var modules = await moduleService.GetModulesAsync(GetUserId());
        return Ok(modules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ModuleDto>> GetModule(Guid id)
    {
        var module = await moduleService.GetModuleAsync(id, GetUserId());
        if (module == null) return NotFound();
        return Ok(module);
    }

    [HttpPost]
    public async Task<ActionResult<ModuleDto>> CreateModule(CreateModuleDto dto)
    {
        var module = await moduleService.CreateModuleAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetModule), new { id = module.Id }, module);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ModuleDto>> UpdateModule(Guid id, UpdateModuleDto dto)
    {
        var module = await moduleService.UpdateModuleAsync(id, GetUserId(), dto);
        if (module == null) return NotFound();
        return Ok(module);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteModule(Guid id)
    {
        var success = await moduleService.DeleteModuleAsync(id, GetUserId());
        if (!success) return NotFound();
        return NoContent();
    }
}

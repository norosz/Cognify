using Cognify.Server.Dtos.Categories;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Dtos.Concepts;
using Cognify.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cognify.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ModulesController(IModuleService moduleService, IStatsService statsService, ICategoryService categoryService, IConceptClusteringService conceptService) : ControllerBase
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

    [HttpGet("{id}/concepts")]
    public async Task<ActionResult> GetModuleConcepts(Guid id)
    {
        var concepts = await conceptService.GetConceptClustersAsync(id);
        return Ok(concepts);
    }

    [HttpPost("{id}/concepts/refresh")]
    public async Task<ActionResult> RefreshModuleConcepts(Guid id, [FromBody] RefreshConceptsRequest? request)
    {
        var concepts = await conceptService.RefreshConceptClustersAsync(id, request?.ForceRegenerate ?? true);
        return Ok(concepts);
    }

    [HttpPost("{id}/categories/suggest")]
    public async Task<ActionResult> SuggestCategories(Guid id, [FromBody] CategorySuggestionRequest request)
    {
        var suggestions = await categoryService.SuggestModuleCategoriesAsync(id, request.MaxSuggestions);
        if (suggestions == null) return NotFound();
        return Ok(suggestions);
    }

    [HttpPut("{id}/category")]
    public async Task<ActionResult> SetCategory(Guid id, [FromBody] SetCategoryRequest request)
    {
        var success = await categoryService.SetModuleCategoryAsync(id, request.CategoryLabel);
        if (!success) return NotFound();
        return Ok();
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

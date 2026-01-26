using Cognify.Server.Data;
using Cognify.Server.Dtos.Modules;
using Cognify.Server.Models;
using Cognify.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cognify.Server.Services;

public class ModuleService(ApplicationDbContext context, IUserContextService userContext) : IModuleService
{
    public async Task<List<ModuleDto>> GetModulesAsync()
    {
        var userId = userContext.GetCurrentUserId();
        return await context.Modules
            .Where(m => m.OwnerUserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new ModuleDto
            {
                Id = m.Id,
                Title = m.Title,
                Description = m.Description,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ModuleDto?> GetModuleAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return null;

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt
        };
    }

    public async Task<ModuleDto> CreateModuleAsync(CreateModuleDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        var module = new Module
        {
            Title = dto.Title,
            Description = dto.Description,
            OwnerUserId = userId
        };

        context.Modules.Add(module);
        await context.SaveChangesAsync();

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt
        };
    }

    public async Task<ModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleDto dto)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return null;

        module.Title = dto.Title;
        module.Description = dto.Description;

        await context.SaveChangesAsync();

        return new ModuleDto
        {
            Id = module.Id,
            Title = module.Title,
            Description = module.Description,
            CreatedAt = module.CreatedAt
        };
    }

    public async Task<bool> DeleteModuleAsync(Guid id)
    {
        var userId = userContext.GetCurrentUserId();
        var module = await context.Modules
            .FirstOrDefaultAsync(m => m.Id == id && m.OwnerUserId == userId);

        if (module == null) return false;

        context.Modules.Remove(module);
        await context.SaveChangesAsync();
        return true;
    }
}

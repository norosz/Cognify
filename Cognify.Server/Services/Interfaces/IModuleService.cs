using Cognify.Server.Dtos.Modules;

namespace Cognify.Server.Services.Interfaces;

public interface IModuleService
{
    Task<List<ModuleDto>> GetModulesAsync(Guid userId);
    Task<ModuleDto?> GetModuleAsync(Guid id, Guid userId);
    Task<ModuleDto> CreateModuleAsync(Guid userId, CreateModuleDto dto);
    Task<ModuleDto?> UpdateModuleAsync(Guid id, Guid userId, UpdateModuleDto dto);
    Task<bool> DeleteModuleAsync(Guid id, Guid userId);
}

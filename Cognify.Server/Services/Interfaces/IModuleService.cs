using Cognify.Server.Dtos.Modules;

namespace Cognify.Server.Services.Interfaces;

public interface IModuleService
{
    Task<List<ModuleDto>> GetModulesAsync();
    Task<ModuleDto?> GetModuleAsync(Guid id);
    Task<ModuleDto> CreateModuleAsync(CreateModuleDto dto);
    Task<ModuleDto?> UpdateModuleAsync(Guid id, UpdateModuleDto dto);
    Task<bool> DeleteModuleAsync(Guid id);
}

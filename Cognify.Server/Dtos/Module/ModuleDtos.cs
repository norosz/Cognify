using System.ComponentModel.DataAnnotations;

namespace Cognify.Server.Dtos.Modules;

public class ModuleDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateModuleDto
{
    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateModuleDto
{
    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

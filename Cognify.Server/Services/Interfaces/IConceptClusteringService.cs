using Cognify.Server.Dtos.Concepts;

namespace Cognify.Server.Services.Interfaces;

public interface IConceptClusteringService
{
    Task<List<ConceptClusterDto>> GetConceptClustersAsync(Guid moduleId);
    Task<List<ConceptClusterDto>> RefreshConceptClustersAsync(Guid moduleId, bool forceRegenerate = true);
}

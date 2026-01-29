using Cognify.Server.Dtos.Knowledge;

namespace Cognify.Server.Services.Interfaces;

public interface IMistakeAnalysisService
{
    Dictionary<string, int> UpdateMistakePatterns(string? existingJson, IReadOnlyCollection<KnowledgeInteractionInput> interactions);
    string SerializeMistakePatterns(Dictionary<string, int> patterns);
    List<string> DetectMistakes(KnowledgeInteractionInput interaction);
}

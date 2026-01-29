using Cognify.Server.Dtos.Knowledge;
using Cognify.Server.Models;

namespace Cognify.Server.Services.Interfaces;

public interface IMistakeAnalysisService
{
    IReadOnlyList<string> Analyze(Question? question, KnowledgeInteractionInput interaction);
}

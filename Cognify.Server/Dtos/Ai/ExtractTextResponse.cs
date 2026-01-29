using Cognify.Server.Models;

namespace Cognify.Server.Dtos.Ai;

public record ExtractTextResponse(Guid ExtractedContentId, ExtractedContentStatus Status);
using System.Security.Claims;
using Cognify.Server.Services.Interfaces;

namespace Cognify.Server.Services;

public class UserContextService(IHttpContextAccessor httpContextAccessor) : IUserContextService
{
    public Guid GetCurrentUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier);
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID");
        }
        
        return userId;
    }
}

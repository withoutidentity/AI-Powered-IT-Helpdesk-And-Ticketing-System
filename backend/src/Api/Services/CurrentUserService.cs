using System.Security.Claims;
using Application.Common.Interfaces;

namespace Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(value, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }
    }
}
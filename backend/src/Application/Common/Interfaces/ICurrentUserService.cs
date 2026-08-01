using Domain.Enums;

namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    UserRole Role { get; }
}

using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users.Models;
using Domain.Enums;
using MediatR;

namespace Application.Users.Queries.ListAgents;

public sealed class ListAgentsQueryHandler : IRequestHandler<ListAgentsQuery, Result<IReadOnlyList<AgentDto>>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserRepository _users;

    public ListAgentsQueryHandler(ICurrentUserService currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<AgentDto>>> Handle(ListAgentsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.ITAdmin)
        {
            return Result<IReadOnlyList<AgentDto>>.Failure("Forbidden", "Only IT admins can list assignable agents.");
        }

        var agents = await _users.ListByRoleAsync(UserRole.ITAgent, cancellationToken);
        var result = agents
            .OrderBy(user => user.Username)
            .Select(user => new AgentDto(user.Id, user.Username))
            .ToList();

        return Result<IReadOnlyList<AgentDto>>.Success(result);
    }
}

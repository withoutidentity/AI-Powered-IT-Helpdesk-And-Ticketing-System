using Application.Common.Models;
using Application.Users.Models;
using MediatR;

namespace Application.Users.Queries.ListAgents;

public sealed record ListAgentsQuery : IRequest<Result<IReadOnlyList<AgentDto>>>;

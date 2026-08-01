using Domain.Enums;
using FluentValidation;

namespace Application.Tickets.Queries.GetTickets;

public sealed class GetTicketsQueryValidator : AbstractValidator<GetTicketsQuery>
{
    public GetTicketsQueryValidator()
    {
        RuleFor(query => query.Status)
            .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<TicketStatus>(value, ignoreCase: true, out _))
            .WithMessage("Status must be one of: Open, InProgress, Resolved, Closed.");

        RuleFor(query => query.Priority)
            .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<TicketPriority>(value, ignoreCase: true, out _))
            .WithMessage("Priority must be one of: Low, Medium, High.");

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
    }
}

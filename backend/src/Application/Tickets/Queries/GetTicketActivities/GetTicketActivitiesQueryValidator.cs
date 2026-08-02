using FluentValidation;

namespace Application.Tickets.Queries.GetTicketActivities;

public sealed class GetTicketActivitiesQueryValidator : AbstractValidator<GetTicketActivitiesQuery>
{
    public GetTicketActivitiesQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

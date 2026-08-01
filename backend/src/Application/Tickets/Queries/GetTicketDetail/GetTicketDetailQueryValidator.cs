using FluentValidation;

namespace Application.Tickets.Queries.GetTicketDetail;

public sealed class GetTicketDetailQueryValidator : AbstractValidator<GetTicketDetailQuery>
{
    public GetTicketDetailQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

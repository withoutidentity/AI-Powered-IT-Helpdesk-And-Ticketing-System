using FluentValidation;

namespace Application.Tickets.Queries.GetTicketComments;

public sealed class GetTicketCommentsQueryValidator : AbstractValidator<GetTicketCommentsQuery>
{
    public GetTicketCommentsQueryValidator()
    {
        RuleFor(query => query.TicketId).NotEmpty();
    }
}

using Domain.Enums;

namespace Application.Common.Models;

public sealed record TicketListCriteria(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? CreatedBy,
    Guid? AssignedTo,
    bool IncludeUnassigned,
    bool IncludeAll,
    int Page,
    int PageSize);

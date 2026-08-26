using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IIntentClassifierService
{
    Task<MessageIntent> ClassifyAsync(string message, CancellationToken cancellationToken);
}

using Application.Chat.Models;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Chat.Commands.StartConversation;

public sealed class StartConversationCommandHandler : IRequestHandler<StartConversationCommand, Result<ConversationDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConversationRepository _conversations;
    private readonly IUnitOfWork _unitOfWork;

    public StartConversationCommandHandler(ICurrentUserService currentUser, IConversationRepository conversations, IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _conversations = conversations;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConversationDto>> Handle(StartConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = Conversation.Start(_currentUser.UserId, request.Title, DateTimeOffset.UtcNow);

        await _conversations.AddAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConversationDto>.Success(new ConversationDto(
            conversation.Id,
            conversation.UserId,
            conversation.Title,
            conversation.CreatedAt,
            conversation.LastMessageAt));
    }
}
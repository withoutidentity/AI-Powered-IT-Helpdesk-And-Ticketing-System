import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { getApiErrorMessage } from '../../../core/http/api-error-message';
import { finalize } from 'rxjs';
import { ChatService } from '../data-access/chat.service';
import { Conversation, Message } from '../data-access/chat.models';

@Component({
  selector: 'app-chat-page',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './chat-page.component.html',
  styleUrl: './chat-page.component.scss',
})
export class ChatPageComponent implements OnInit {
  @ViewChild('messageList') private messageList?: ElementRef<HTMLElement>;

  private readonly chatService = inject(ChatService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  readonly conversations = signal<Conversation[]>([]);
  readonly activeConversation = signal<Conversation | null>(null);
  readonly messages = signal<Message[]>([]);
  readonly isLoadingConversations = signal(false);
  readonly isLoadingMessages = signal(false);
  readonly isSending = signal(false);
  readonly isCreatingTicket = signal(false);
  readonly isCreatingConversation = signal(false);
  readonly isCreatingNewConversation = signal(false);
  readonly conversationHasTicket = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly statusMessage = signal<string | null>(null);

  readonly hasMessages = computed(() => this.messages().length > 0);
  readonly latestUserMessage = computed(() => [...this.messages()].reverse().find((message) => message.sender === 'User') ?? null);
  readonly canCreateTicket = computed(() => Boolean(this.activeConversation() && this.latestUserMessage() && !this.conversationHasTicket()));

  readonly newConversationForm = this.formBuilder.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(120)]],
  });

  readonly messageForm = this.formBuilder.nonNullable.group({
    content: ['', [Validators.required, Validators.maxLength(4000)]],
  });

  ngOnInit(): void {
    this.loadConversations();
  }

  loadConversations(): void {
    this.errorMessage.set(null);
    this.isLoadingConversations.set(true);

    this.chatService.listConversations().pipe(
      finalize(() => this.isLoadingConversations.set(false)),
    ).subscribe({
      next: (conversations) => {
        this.conversations.set(conversations);
        const requestedConversationId = this.route.snapshot.queryParamMap.get('conversationId');
        const requestedConversation = requestedConversationId
          ? conversations.find((conversation) => conversation.id === requestedConversationId)
          : null;

        if (requestedConversation && this.activeConversation()?.id !== requestedConversation.id) {
          this.selectConversation(requestedConversation);
          return;
        }

        this.syncActiveConversationTicketState(conversations);
        if (!this.activeConversation() && conversations.length > 0) {
          this.selectConversation(conversations[0]);
        }
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Could not load conversations.')),
    });
  }

  showNewConversationForm(): void {
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isCreatingNewConversation.set(true);
    this.newConversationForm.reset({ title: '' });
  }

  cancelNewConversation(): void {
    this.isCreatingNewConversation.set(false);
    this.newConversationForm.reset({ title: '' });
  }

  startConversation(): void {
    this.errorMessage.set(null);
    this.statusMessage.set(null);

    if (this.newConversationForm.invalid || this.isCreatingConversation()) {
      this.newConversationForm.markAllAsTouched();
      return;
    }

    this.isCreatingConversation.set(true);
    this.chatService.startConversation({ title: this.newConversationForm.getRawValue().title }).pipe(
      finalize(() => this.isCreatingConversation.set(false)),
    ).subscribe({
      next: (conversation) => {
        this.conversations.update((current) => [conversation, ...current]);
        this.activeConversation.set(conversation);
        this.messages.set([]);
        this.conversationHasTicket.set(conversation.hasTicket);
        this.isCreatingNewConversation.set(false);
        this.newConversationForm.reset({ title: '' });
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Could not start a conversation.')),
    });
  }

  selectConversation(conversation: Conversation): void {
    this.activeConversation.set(conversation);
    this.messages.set([]);
    this.conversationHasTicket.set(conversation.hasTicket);
    this.statusMessage.set(null);
    this.loadMessages(conversation.id);
  }

  sendMessage(): void {
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    if (this.messageForm.invalid) {
      this.messageForm.markAllAsTouched();
      return;
    }

    const activeConversation = this.activeConversation();
    if (!activeConversation) {
      this.startAndSendMessage();
      return;
    }

    this.sendMessageToConversation(activeConversation.id, this.messageForm.getRawValue().content);
  }

  createTicketForConversation(): void {
    const message = this.latestUserMessage();
    const conversation = this.activeConversation();
    if (!message || !conversation || this.isCreatingTicket() || this.conversationHasTicket()) {
      return;
    }

    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isCreatingTicket.set(true);

    this.chatService.createTicketFromMessage(message.conversationId, message.id, {
      title: conversation.title,
      description: message.content,
      priority: 'Medium',
    }).pipe(
      finalize(() => this.isCreatingTicket.set(false)),
    ).subscribe({
      next: (ticket) => {
        this.markActiveConversationHasTicket();
        this.statusMessage.set(`Ticket created: ${ticket.title}`);
      },
      error: (error) => {
        const status = typeof error === 'object' && error !== null && 'status' in error ? (error as { status?: unknown }).status : null;
        if (status === 409) {
          this.markActiveConversationHasTicket();
          this.errorMessage.set('A ticket already exists for this conversation.');
          return;
        }

        this.errorMessage.set(getApiErrorMessage(error, {
          fallback: 'Could not create a ticket.',
          conflict: 'A ticket already exists for this conversation.',
        }));
      },
    });
  }

  isActive(conversation: Conversation): boolean {
    return this.activeConversation()?.id === conversation.id;
  }

  private syncActiveConversationTicketState(conversations: Conversation[]): void {
    const activeConversation = this.activeConversation();
    if (!activeConversation) {
      return;
    }

    const refreshed = conversations.find((conversation) => conversation.id === activeConversation.id);
    if (refreshed) {
      this.activeConversation.set(refreshed);
      this.conversationHasTicket.set(refreshed.hasTicket);
    }
  }

  private markActiveConversationHasTicket(): void {
    const activeConversation = this.activeConversation();
    if (!activeConversation) {
      return;
    }

    const updatedConversation = { ...activeConversation, hasTicket: true };
    this.activeConversation.set(updatedConversation);
    this.conversationHasTicket.set(true);
    this.conversations.update((current) =>
      current.map((conversation) => conversation.id === updatedConversation.id ? updatedConversation : conversation),
    );
  }

  private startAndSendMessage(): void {
    const content = this.messageForm.getRawValue().content;
    this.isSending.set(true);

    this.chatService.startConversation({ title: this.buildConversationTitle(content) }).subscribe({
      next: (conversation) => {
        this.conversations.update((current) => [conversation, ...current]);
        this.activeConversation.set(conversation);
        this.messages.set([]);
        this.conversationHasTicket.set(conversation.hasTicket);
        this.sendMessageToConversation(conversation.id, content);
      },
      error: (error) => {
        this.isSending.set(false);
        this.errorMessage.set(getApiErrorMessage(error, 'Could not start a conversation.'));
      },
    });
  }

  private loadMessages(conversationId: string): void {
    this.errorMessage.set(null);
    this.isLoadingMessages.set(true);

    this.chatService.listMessages(conversationId).pipe(
      finalize(() => this.isLoadingMessages.set(false)),
    ).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        this.scrollMessagesToBottom();
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Could not load messages.')),
    });
  }

  private sendMessageToConversation(conversationId: string, content: string): void {
    this.isSending.set(true);

    this.chatService.sendMessage(conversationId, { content }).pipe(
      finalize(() => this.isSending.set(false)),
    ).subscribe({
      next: (response) => {
        this.messages.update((current) => [...current, response.userMessage, response.assistantMessage]);
        this.messageForm.reset();
        this.scrollMessagesToBottom();
        this.loadConversations();
      },
      error: (error) => this.errorMessage.set(getApiErrorMessage(error, 'Could not send the message.')),
    });
  }

  private buildConversationTitle(content: string): string {
    const trimmed = content.trim();
    return trimmed.length > 60 ? `${trimmed.slice(0, 57)}...` : trimmed;
  }

  private scrollMessagesToBottom(): void {
    requestAnimationFrame(() => {
      const element = this.messageList?.nativeElement;
      if (element) {
        element.scrollTop = element.scrollHeight;
      }
    });
  }
}





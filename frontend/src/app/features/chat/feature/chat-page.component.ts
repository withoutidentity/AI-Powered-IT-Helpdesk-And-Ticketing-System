import { DatePipe } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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

  readonly conversations = signal<Conversation[]>([]);
  readonly activeConversation = signal<Conversation | null>(null);
  readonly messages = signal<Message[]>([]);
  readonly isLoadingConversations = signal(false);
  readonly isLoadingMessages = signal(false);
  readonly isSending = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly hasMessages = computed(() => this.messages().length > 0);

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
        if (!this.activeConversation() && conversations.length > 0) {
          this.selectConversation(conversations[0]);
        }
      },
      error: () => this.errorMessage.set('Could not load conversations.'),
    });
  }

  startConversation(): void {
    this.errorMessage.set(null);
    this.chatService.startConversation({ title: null }).subscribe({
      next: (conversation) => {
        this.conversations.update((current) => [conversation, ...current]);
        this.activeConversation.set(conversation);
        this.messages.set([]);
      },
      error: () => this.errorMessage.set('Could not start a conversation.'),
    });
  }

  selectConversation(conversation: Conversation): void {
    this.activeConversation.set(conversation);
    this.messages.set([]);
    this.loadMessages(conversation.id);
  }

  sendMessage(): void {
    this.errorMessage.set(null);
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

  isActive(conversation: Conversation): boolean {
    return this.activeConversation()?.id === conversation.id;
  }

  private startAndSendMessage(): void {
    const content = this.messageForm.getRawValue().content;
    this.isSending.set(true);

    this.chatService.startConversation({ title: this.buildConversationTitle(content) }).subscribe({
      next: (conversation) => {
        this.conversations.update((current) => [conversation, ...current]);
        this.activeConversation.set(conversation);
        this.messages.set([]);
        this.sendMessageToConversation(conversation.id, content);
      },
      error: () => {
        this.isSending.set(false);
        this.errorMessage.set('Could not start a conversation.');
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
      error: () => this.errorMessage.set('Could not load messages.'),
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
      error: () => this.errorMessage.set('Could not send the message.'),
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
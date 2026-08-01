import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Conversation,
  CreateTicketFromMessageRequest,
  Message,
  SendMessageRequest,
  SendMessageResponse,
  StartConversationRequest,
  Ticket,
} from './chat.models';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/chat`;

  listConversations(): Observable<Conversation[]> {
    return this.http.get<Conversation[]>(`${this.baseUrl}/conversations`);
  }

  startConversation(request: StartConversationRequest): Observable<Conversation> {
    return this.http.post<Conversation>(`${this.baseUrl}/conversations`, request);
  }

  listMessages(conversationId: string): Observable<Message[]> {
    return this.http.get<Message[]>(`${this.baseUrl}/conversations/${conversationId}/messages`);
  }

  sendMessage(conversationId: string, request: SendMessageRequest): Observable<SendMessageResponse> {
    return this.http.post<SendMessageResponse>(`${this.baseUrl}/conversations/${conversationId}/messages`, request);
  }

  createTicketFromMessage(
    conversationId: string,
    messageId: string,
    request: CreateTicketFromMessageRequest,
  ): Observable<Ticket> {
    return this.http.post<Ticket>(`${this.baseUrl}/conversations/${conversationId}/messages/${messageId}/ticket`, request);
  }
}
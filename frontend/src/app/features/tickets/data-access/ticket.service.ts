import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateTicketCommentRequest,
  PaginatedList,
  TicketActivity,
  TicketComment,
  TicketDetail,
  TicketListFilters,
  TicketSummary,
  UpdateTicketAssignmentRequest,
  UpdateTicketStatusRequest,
  UserRef,
} from './ticket.models';

@Injectable({ providedIn: 'root' })
export class TicketService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/tickets`;

  listTickets(filters: TicketListFilters = {}): Observable<PaginatedList<TicketSummary>> {
    let params = new HttpParams()
      .set('page', String(filters.page ?? 1))
      .set('pageSize', String(filters.pageSize ?? 20));

    if (filters.status) {
      params = params.set('status', filters.status);
    }

    if (filters.priority) {
      params = params.set('priority', filters.priority);
    }

    return this.http.get<PaginatedList<TicketSummary>>(this.baseUrl, { params });
  }

  getTicket(ticketId: string): Observable<TicketDetail> {
    return this.http.get<TicketDetail>(`${this.baseUrl}/${ticketId}`);
  }

  listAgents(): Observable<UserRef[]> {
    return this.http.get<UserRef[]>(`${environment.apiBaseUrl}/users/agents`);
  }

  listComments(ticketId: string): Observable<TicketComment[]> {
    return this.http.get<TicketComment[]>(`${this.baseUrl}/${ticketId}/comments`);
  }

  createComment(ticketId: string, request: CreateTicketCommentRequest): Observable<TicketComment> {
    return this.http.post<TicketComment>(`${this.baseUrl}/${ticketId}/comments`, request);
  }

  listActivities(ticketId: string): Observable<TicketActivity[]> {
    return this.http.get<TicketActivity[]>(`${this.baseUrl}/${ticketId}/activities`);
  }

  updateStatus(ticketId: string, request: UpdateTicketStatusRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${ticketId}/status`, request);
  }

  updateAssignment(ticketId: string, request: UpdateTicketAssignmentRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${ticketId}/assignment`, request);
  }
}

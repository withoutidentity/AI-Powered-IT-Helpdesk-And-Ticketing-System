import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PaginatedList, TicketDetail, TicketListFilters, TicketSummary, UpdateTicketStatusRequest } from './ticket.models';

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
  updateStatus(ticketId: string, request: UpdateTicketStatusRequest): Observable<TicketDetail> {
    return this.http.patch<TicketDetail>(`${this.baseUrl}/${ticketId}/status`, request);
  }
}

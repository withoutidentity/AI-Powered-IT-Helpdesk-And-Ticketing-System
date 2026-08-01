import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { TicketDetail, TicketPriority, TicketStatus, TicketSummary } from '../data-access/ticket.models';
import { TicketService } from '../data-access/ticket.service';

@Component({
  selector: 'app-ticket-list-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './ticket-list-page.component.html',
  styleUrl: './ticket-list-page.component.scss',
})
export class TicketListPageComponent implements OnInit {
  private readonly ticketService = inject(TicketService);
  private readonly formBuilder = inject(FormBuilder);

  readonly tickets = signal<TicketSummary[]>([]);
  readonly selectedTicket = signal<TicketDetail | null>(null);
  readonly selectedTicketId = signal<string | null>(null);
  readonly isLoadingTickets = signal(false);
  readonly isLoadingDetail = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly hasTickets = computed(() => this.tickets().length > 0);

  readonly filterForm = this.formBuilder.nonNullable.group({
    status: ['' as TicketStatus | ''],
    priority: ['' as TicketPriority | ''],
  });

  ngOnInit(): void {
    this.loadTickets();
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadTickets();
  }

  clearFilters(): void {
    this.filterForm.reset({ status: '', priority: '' });
    this.applyFilters();
  }

  selectTicket(ticket: TicketSummary): void {
    this.selectedTicketId.set(ticket.id);
    this.selectedTicket.set(null);
    this.errorMessage.set(null);
    this.isLoadingDetail.set(true);

    this.ticketService.getTicket(ticket.id).pipe(
      finalize(() => this.isLoadingDetail.set(false)),
    ).subscribe({
      next: (detail) => this.selectedTicket.set(detail),
      error: () => this.errorMessage.set('Could not load ticket detail.'),
    });
  }

  isSelected(ticket: TicketSummary): boolean {
    return this.selectedTicketId() === ticket.id;
  }

  private loadTickets(): void {
    this.errorMessage.set(null);
    this.isLoadingTickets.set(true);

    const filters = this.filterForm.getRawValue();
    this.ticketService.listTickets({
      status: filters.status,
      priority: filters.priority,
      page: this.page(),
      pageSize: this.pageSize,
    }).pipe(
      finalize(() => this.isLoadingTickets.set(false)),
    ).subscribe({
      next: (response) => {
        this.tickets.set(response.items);
        this.totalCount.set(response.totalCount);

        const selectedId = this.selectedTicketId();
        const selectedStillVisible = selectedId && response.items.some((ticket) => ticket.id === selectedId);
        if (!selectedStillVisible) {
          this.selectedTicketId.set(null);
          this.selectedTicket.set(null);
        }

        if (!this.selectedTicketId() && response.items.length > 0) {
          this.selectTicket(response.items[0]);
        }
      },
      error: () => this.errorMessage.set('Could not load tickets.'),
    });
  }
}


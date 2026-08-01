import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
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
  private readonly auth = inject(AuthService);

  readonly tickets = signal<TicketSummary[]>([]);
  readonly selectedTicket = signal<TicketDetail | null>(null);
  readonly selectedTicketId = signal<string | null>(null);
  readonly isLoadingTickets = signal(false);
  readonly isLoadingDetail = signal(false);
  readonly isUpdatingStatus = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly statusMessage = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly hasTickets = computed(() => this.tickets().length > 0);
  readonly canUpdateStatus = computed(() => {
    const role = this.auth.currentUser()?.role;
    return role === 'ITAgent' || role === 'ITAdmin';
  });
  readonly nextStatus = computed(() => this.getNextStatus(this.selectedTicket()?.status ?? null));

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
    this.statusMessage.set(null);
    this.isLoadingDetail.set(true);

    this.ticketService.getTicket(ticket.id).pipe(
      finalize(() => this.isLoadingDetail.set(false)),
    ).subscribe({
      next: (detail) => this.selectedTicket.set(detail),
      error: () => this.errorMessage.set('Could not load ticket detail.'),
    });
  }

  updateStatus(): void {
    const ticket = this.selectedTicket();
    const nextStatus = this.nextStatus();
    if (!ticket || !nextStatus || this.isUpdatingStatus()) {
      return;
    }

    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isUpdatingStatus.set(true);

    this.ticketService.updateStatus(ticket.id, { status: nextStatus }).pipe(
      finalize(() => this.isUpdatingStatus.set(false)),
    ).subscribe({
      next: (updatedTicket) => {
        this.selectedTicket.set(updatedTicket);
        this.statusMessage.set(`Status updated to ${updatedTicket.status}.`);
        this.tickets.update((current) => current.map((item) =>
          item.id === updatedTicket.id ? this.toSummary(updatedTicket) : item,
        ));
      },
      error: (error) => {
        if (error?.status === 409) {
          this.errorMessage.set('This status transition is not allowed.');
          return;
        }

        if (error?.status === 403) {
          this.errorMessage.set('You do not have permission to update this ticket.');
          return;
        }

        this.errorMessage.set('Could not update ticket status.');
      },
    });
  }

  isSelected(ticket: TicketSummary): boolean {
    return this.selectedTicketId() === ticket.id;
  }

  statusLabel(status: TicketStatus): string {
    return status === 'InProgress' ? 'In progress' : status;
  }

  canOpenConversation(ticket: TicketDetail): boolean {
    return this.auth.currentUser()?.id === ticket.createdBy.id;
  }

  private loadTickets(): void {
    this.errorMessage.set(null);
    this.statusMessage.set(null);
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

  private getNextStatus(status: TicketStatus | null): TicketStatus | null {
    switch (status) {
      case 'Open':
        return 'InProgress';
      case 'InProgress':
        return 'Resolved';
      case 'Resolved':
        return 'Closed';
      default:
        return null;
    }
  }

  private toSummary(ticket: TicketDetail): TicketSummary {
    return {
      id: ticket.id,
      conversationId: ticket.conversationId,
      messageId: ticket.messageId,
      title: ticket.title,
      status: ticket.status,
      priority: ticket.priority,
      createdBy: ticket.createdBy,
      assignedTo: ticket.assignedTo,
      createdAt: ticket.createdAt,
      updatedAt: ticket.updatedAt,
    };
  }
}

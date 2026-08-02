import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { TicketActivity, TicketComment, TicketDetail, TicketPriority, TicketStatus, TicketSummary, UserRef } from '../data-access/ticket.models';
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
  readonly assignableAgents = signal<UserRef[]>([]);
  readonly comments = signal<TicketComment[]>([]);
  readonly activities = signal<TicketActivity[]>([]);
  readonly isLoadingTickets = signal(false);
  readonly isLoadingDetail = signal(false);
  readonly isLoadingAgents = signal(false);
  readonly isLoadingThreads = signal(false);
  readonly isUpdatingStatus = signal(false);
  readonly isUpdatingAssignment = signal(false);
  readonly isPostingComment = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly statusMessage = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly pageSize = 20;

  readonly hasTickets = computed(() => this.tickets().length > 0);
  readonly isITAdmin = computed(() => this.auth.currentUser()?.role === 'ITAdmin');
  readonly isITAgent = computed(() => this.auth.currentUser()?.role === 'ITAgent');
  readonly nextStatus = computed(() => this.getNextStatus(this.selectedTicket()?.status ?? null));

  readonly filterForm = this.formBuilder.nonNullable.group({
    status: ['' as TicketStatus | ''],
    priority: ['' as TicketPriority | ''],
  });

  readonly assignmentForm = this.formBuilder.nonNullable.group({
    assignedToUserId: [''],
  });

  readonly commentForm = this.formBuilder.nonNullable.group({
    content: [''],
  });

  ngOnInit(): void {
    if (this.isITAdmin()) {
      this.loadAssignableAgents();
    }

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
    this.comments.set([]);
    this.activities.set([]);
    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isLoadingDetail.set(true);

    this.ticketService.getTicket(ticket.id).pipe(
      finalize(() => this.isLoadingDetail.set(false)),
    ).subscribe({
      next: (detail) => {
        this.selectedTicket.set(detail);
        this.syncAssignmentForm(detail);
        this.loadTicketThreads(detail.id);
      },
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
        this.applyUpdatedTicket(updatedTicket);
        this.loadActivities(updatedTicket.id);
        this.statusMessage.set(`Status updated to ${updatedTicket.status}.`);
      },
      error: (error) => this.setTicketMutationError(error, 'Could not update ticket status.'),
    });
  }

  assignToSelf(ticket: TicketDetail): void {
    const user = this.auth.currentUser();
    if (!user || !this.canAssignToSelf(ticket)) {
      return;
    }

    this.updateAssignment(ticket, user.id);
  }

  assignSelectedAgent(ticket: TicketDetail): void {
    const assignedToUserId = this.assignmentForm.getRawValue().assignedToUserId;
    if (!assignedToUserId || assignedToUserId === ticket.assignedTo?.id) {
      return;
    }

    this.updateAssignment(ticket, assignedToUserId);
  }

  postComment(ticket: TicketDetail): void {
    const content = this.commentForm.getRawValue().content.trim();
    if (!content || this.isPostingComment()) {
      return;
    }

    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isPostingComment.set(true);

    this.ticketService.createComment(ticket.id, { content }).pipe(
      finalize(() => this.isPostingComment.set(false)),
    ).subscribe({
      next: (comment) => {
        this.comments.update((current) => [...current, comment]);
        this.commentForm.reset({ content: '' });
      },
      error: (error) => this.setTicketMutationError(error, 'Could not post comment.'),
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

  canAssignToSelf(ticket: TicketDetail): boolean {
    return this.isITAgent() && ticket.assignedTo === null;
  }

  canUpdateTicketStatus(ticket: TicketDetail): boolean {
    const user = this.auth.currentUser();
    return this.isITAdmin() || (this.isITAgent() && ticket.assignedTo?.id === user?.id);
  }

  activityText(activity: TicketActivity): string {
    if (activity.action === 'TicketCreated') {
      return 'created this ticket';
    }

    if (activity.action === 'Assigned') {
      return `assigned ticket from ${activity.oldValue ?? 'Unassigned'} to ${activity.newValue ?? 'Unassigned'}`;
    }

    if (activity.action === 'StatusChanged') {
      return `changed status from ${activity.oldValue ?? '-'} to ${activity.newValue ?? '-'}`;
    }

    return activity.action;
  }

  private updateAssignment(ticket: TicketDetail, assignedToUserId: string): void {
    if (this.isUpdatingAssignment()) {
      return;
    }

    this.errorMessage.set(null);
    this.statusMessage.set(null);
    this.isUpdatingAssignment.set(true);
    this.syncAssignmentControlState();

    this.ticketService.updateAssignment(ticket.id, { assignedToUserId }).pipe(
      finalize(() => {
        this.isUpdatingAssignment.set(false);
        this.syncAssignmentControlState();
      }),
    ).subscribe({
      next: (updatedTicket) => {
        this.applyUpdatedTicket(updatedTicket);
        this.loadActivities(updatedTicket.id);
        this.statusMessage.set(`Assigned to ${updatedTicket.assignedTo?.username ?? 'agent'}.`);
      },
      error: (error) => this.setTicketMutationError(error, 'Could not update ticket assignment.'),
    });
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
          this.comments.set([]);
          this.activities.set([]);
        }

        if (!this.selectedTicketId() && response.items.length > 0) {
          this.selectTicket(response.items[0]);
        }
      },
      error: () => this.errorMessage.set('Could not load tickets.'),
    });
  }

  private loadAssignableAgents(): void {
    this.isLoadingAgents.set(true);
    this.syncAssignmentControlState();

    this.ticketService.listAgents().pipe(
      finalize(() => {
        this.isLoadingAgents.set(false);
        this.syncAssignmentControlState();
      }),
    ).subscribe({
      next: (agents) => this.assignableAgents.set(agents),
      error: () => this.errorMessage.set('Could not load assignable agents.'),
    });
  }

  private loadTicketThreads(ticketId: string): void {
    this.isLoadingThreads.set(true);

    forkJoin({
      comments: this.ticketService.listComments(ticketId),
      activities: this.ticketService.listActivities(ticketId),
    }).pipe(
      finalize(() => this.isLoadingThreads.set(false)),
    ).subscribe({
      next: ({ comments, activities }) => {
        this.comments.set(comments);
        this.activities.set(activities);
      },
      error: () => this.errorMessage.set('Could not load ticket comments or activity.'),
    });
  }

  private loadActivities(ticketId: string): void {
    this.ticketService.listActivities(ticketId).subscribe({
      next: (activities) => this.activities.set(activities),
      error: () => this.errorMessage.set('Could not load ticket activity.'),
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

  private applyUpdatedTicket(ticket: TicketDetail): void {
    this.selectedTicket.set(ticket);
    this.syncAssignmentForm(ticket);
    this.tickets.update((current) => current.map((item) =>
      item.id === ticket.id ? this.toSummary(ticket) : item,
    ));
  }

  private syncAssignmentForm(ticket: TicketDetail): void {
    this.assignmentForm.patchValue({ assignedToUserId: ticket.assignedTo?.id ?? '' });
    this.syncAssignmentControlState();
  }

  private syncAssignmentControlState(): void {
    const control = this.assignmentForm.controls.assignedToUserId;
    if (this.isLoadingAgents() || this.isUpdatingAssignment()) {
      control.disable({ emitEvent: false });
      return;
    }

    control.enable({ emitEvent: false });
  }

  private setTicketMutationError(error: { status?: number } | null | undefined, fallback: string): void {
    if (error?.status === 400) {
      this.errorMessage.set('The requested ticket update is invalid.');
      return;
    }

    if (error?.status === 403) {
      this.errorMessage.set('You do not have permission to update this ticket.');
      return;
    }

    if (error?.status === 404) {
      this.errorMessage.set('Ticket or assignee was not found.');
      return;
    }

    if (error?.status === 409) {
      this.errorMessage.set('This status transition is not allowed.');
      return;
    }

    this.errorMessage.set(fallback);
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

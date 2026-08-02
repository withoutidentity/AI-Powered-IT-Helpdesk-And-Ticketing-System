export type TicketStatus = 'Open' | 'InProgress' | 'Resolved' | 'Closed';
export type TicketPriority = 'Low' | 'Medium' | 'High';

export interface UserRef {
  id: string;
  username: string;
}

export interface TicketSummary {
  id: string;
  conversationId: string;
  messageId: string | null;
  title: string;
  status: TicketStatus;
  priority: TicketPriority;
  createdBy: UserRef;
  assignedTo: UserRef | null;
  createdAt: string;
  updatedAt: string;
}

export interface TicketDetail extends TicketSummary {
  description: string;
  attachmentsJson: string;
}

export interface PaginatedList<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface TicketListFilters {
  status?: TicketStatus | '';
  priority?: TicketPriority | '';
  page?: number;
  pageSize?: number;
}

export interface UpdateTicketStatusRequest {
  status: TicketStatus;
}
export interface UpdateTicketAssignmentRequest {
  assignedToUserId: string;
}
export interface CreateTicketCommentRequest {
  content: string;
}

export interface TicketComment {
  id: string;
  ticketId: string;
  author: UserRef;
  content: string;
  createdAt: string;
}

export interface TicketActivity {
  id: string;
  ticketId: string;
  actor: UserRef;
  action: string;
  field: string | null;
  oldValue: string | null;
  newValue: string | null;
  createdAt: string;
}
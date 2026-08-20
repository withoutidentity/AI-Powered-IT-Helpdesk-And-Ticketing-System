export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: string;
  lastMessageAt: string;
  hasTicket: boolean;
}

export interface Message {
  id: string;
  conversationId: string;
  sender: 'User' | 'Assistant';
  content: string;
  intent: 'Greeting' | 'Question' | 'Action' | null;
  createdAt: string;
  sourceDocuments: string[];
}

export interface SendMessageResponse {
  userMessage: Message;
  assistantMessage: Message;
}

export interface Ticket {
  id: string;
  conversationId: string;
  messageId: string | null;
  createdBy: string;
  assignedTo: string | null;
  title: string;
  description: string;
  status: 'Open' | 'InProgress' | 'Resolved' | 'Closed';
  priority: 'Low' | 'Medium' | 'High';
  createdAt: string;
  updatedAt: string;
  attachmentsJson: string;
}

export interface StartConversationRequest {
  title: string | null;
}

export interface SendMessageRequest {
  content: string;
}

export interface CreateTicketFromMessageRequest {
  title: string | null;
  description: string | null;
  priority: 'Low' | 'Medium' | 'High' | null;
}
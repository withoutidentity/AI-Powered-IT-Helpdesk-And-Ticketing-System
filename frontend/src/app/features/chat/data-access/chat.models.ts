export interface Conversation {
  id: string;
  userId: string;
  title: string;
  createdAt: string;
  lastMessageAt: string;
}

export interface Message {
  id: string;
  conversationId: string;
  sender: 'User' | 'Assistant';
  content: string;
  intent: 'Greeting' | 'Question' | 'Action' | null;
  createdAt: string;
}

export interface SendMessageResponse {
  userMessage: Message;
  assistantMessage: Message;
}

export interface StartConversationRequest {
  title: string | null;
}

export interface SendMessageRequest {
  content: string;
}
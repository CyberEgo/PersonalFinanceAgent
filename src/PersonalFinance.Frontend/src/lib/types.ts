export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  agentName?: string;
  timestamp: Date;
  isStreaming?: boolean;
  toolName?: string;
  attachment?: {
    name: string;
    previewUrl: string;
  };
}

export interface ChatStreamEvent {
  type: 'thread_id' | 'agent' | 'delta' | 'done' | 'error' | 'tool' | 'tool_call' | 'clear' | 'widget';
  content?: string;
  agentName?: string;
  toolName?: string;
  widgetData?: string;
  error?: string;
}

export interface Account {
  id: string;
  userName: string;
  accountHolderFullName: string;
  currency: string;
  balance: number;
  paymentMethods: PaymentMethod[];
}

export interface PaymentMethod {
  id: string;
  type: string;
  availableBalance: number;
  cardNumber?: string;
}

export interface Transaction {
  id: string;
  description: string;
  type: string;
  flowType: 'income' | 'outcome';
  recipientName: string;
  amount: number;
  timestamp: string;
  paymentType: string;
  cardId?: string;
  category: string;
  status: string;
}

export interface Card {
  id: string;
  type: string;
  name: string;
  balance: number;
  number: string;
  limit: number;
  status: string;
}

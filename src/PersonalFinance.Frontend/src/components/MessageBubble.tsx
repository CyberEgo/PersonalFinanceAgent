import { motion } from 'framer-motion';
import { Bot, User, FileText, Loader2, Search } from 'lucide-react';
import { cn } from '../lib/utils';
import Markdown from './Markdown';
import type { ChatMessage } from '../lib/types';

const agentColors: Record<string, string> = {
  AccountAgent: 'from-blue-500 to-cyan-500',
  TransactionHistoryAgent: 'from-amber-500 to-orange-500',
  PaymentAgent: 'from-emerald-500 to-green-500',
  TriageAgent: 'from-[var(--color-primary)] to-purple-500',
};

interface Props {
  message: ChatMessage;
}

export default function MessageBubble({ message }: Props) {
  const isUser = message.role === 'user';
  const gradientClass = message.agentName
    ? agentColors[message.agentName] || agentColors.TriageAgent
    : agentColors.TriageAgent;

  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2 }}
      className={cn('flex gap-3 max-w-[85%]', isUser ? 'ml-auto flex-row-reverse' : '')}
    >
      {/* Avatar */}
      <div
        className={cn(
          'w-8 h-8 rounded-lg flex items-center justify-center shrink-0',
          isUser
            ? 'bg-[var(--color-surface-hover)]'
            : `bg-gradient-to-br ${gradientClass}`,
        )}
      >
        {isUser ? (
          <User className="w-4 h-4 text-[var(--color-text-muted)]" />
        ) : (
          <Bot className="w-4 h-4 text-white" />
        )}
      </div>

      {/* Message */}
      <div
        className={cn(
          'rounded-2xl px-4 py-3 text-sm leading-relaxed',
          isUser
            ? 'bg-[var(--color-primary)] text-white rounded-tr-md'
            : 'glass rounded-tl-md',
        )}
      >
        {message.agentName && !isUser && (
          <span className="text-xs font-medium text-[var(--color-primary-light)] block mb-1">
            {message.agentName.replace('Agent', ' Agent')}
          </span>
        )}
        <div className="break-words">
          {isUser && message.attachment && (
            <div className="mb-2">
              {message.attachment.previewUrl ? (
                <img src={message.attachment.previewUrl} alt={message.attachment.name} className="max-w-[200px] rounded-lg" />
              ) : (
                <div className="flex items-center gap-2 px-3 py-2 rounded-lg bg-white/20 text-xs">
                  <FileText className="w-4 h-4" />
                  <span className="truncate">{message.attachment.name}</span>
                </div>
              )}
            </div>
          )}
          {isUser ? (
            <span className="whitespace-pre-wrap">{message.content}</span>
          ) : (
            <Markdown content={message.content} />
          )}
          {message.isStreaming && !message.content && (
            <div className="flex items-center gap-2 py-1 min-w-[140px]">
              {message.toolName ? (
                <>
                  <Search className="w-3.5 h-3.5 text-[var(--color-primary-light)] animate-pulse" />
                  <span className="text-xs text-[var(--color-text-muted)] animate-pulse">
                    {toolDisplayName(message.toolName)}
                  </span>
                </>
              ) : (
                <>
                  <Loader2 className="w-3.5 h-3.5 text-[var(--color-primary-light)] animate-spin" />
                  <span className="text-xs text-[var(--color-text-muted)]">Thinking...</span>
                </>
              )}
            </div>
          )}
          {message.isStreaming && message.content && (
            <span className="inline-block w-2 h-4 ml-1 bg-[var(--color-primary-light)] animate-pulse rounded-sm" />
          )}
        </div>
      </div>
    </motion.div>
  );
}

function toolDisplayName(name: string): string {
  const map: Record<string, string> = {
    ScanInvoiceAsync: 'Scanning invoice...',
    GetAccountsByUserNameAsync: 'Looking up accounts...',
    GetAccountDetailsAsync: 'Fetching account details...',
    GetRegisteredBeneficiariesAsync: 'Loading beneficiaries...',
    GetCreditCardsAsync: 'Loading cards...',
    GetCardDetailsAsync: 'Fetching card details...',
    GetLastTransactionsAsync: 'Fetching transactions...',
    GetTransactionsByRecipientNameAsync: 'Searching transactions...',
    GetCardTransactionsAsync: 'Loading card transactions...',
    ProcessPaymentAsync: 'Processing payment...',
  };
  return map[name] || `Running ${name}...`;
}

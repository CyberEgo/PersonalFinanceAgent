import { useState } from 'react';
import { MessageSquare, X, Minus, Maximize2, Minimize2 } from 'lucide-react';
import ChatWidget from './ChatWidget';
import { cn } from '../lib/utils';

export default function FloatingChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [isMinimized, setIsMinimized] = useState(false);
  const [isExpanded, setIsExpanded] = useState(false);

  return (
    <>
      {/* Chat Panel — always mounted to preserve state, hidden via CSS */}
      <div
        className={cn(
          'fixed z-50 glass rounded-2xl shadow-2xl border border-[var(--color-border)] flex flex-col transition-all duration-300',
          isMinimized
            ? 'bottom-24 right-6 w-80 h-14'
            : isExpanded
              ? 'inset-4 w-auto h-auto'
              : 'bottom-24 right-6 w-[420px] h-[600px]',
          !isOpen && 'hidden',
        )}
      >
        {/* Panel Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border)] shrink-0">
          <span className="text-sm font-semibold gradient-text">AI Assistant</span>
          <div className="flex items-center gap-1">
            <button
              onClick={() => setIsMinimized(prev => !prev)}
              className="p-1.5 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-surface-hover)] transition-colors"
              title={isMinimized ? 'Restore' : 'Minimize'}
            >
              <Minus className="w-4 h-4" />
            </button>
            <button
              onClick={() => { setIsExpanded(prev => !prev); setIsMinimized(false); }}
              className="p-1.5 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-text)] hover:bg-[var(--color-surface-hover)] transition-colors"
              title={isExpanded ? 'Collapse' : 'Expand'}
            >
              {isExpanded ? <Minimize2 className="w-4 h-4" /> : <Maximize2 className="w-4 h-4" />}
            </button>
            <button
              onClick={() => { setIsOpen(false); setIsMinimized(false); setIsExpanded(false); }}
              className="p-1.5 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-danger)] hover:bg-[var(--color-surface-hover)] transition-colors"
              title="Close"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>

        {/* Chat Body */}
        {!isMinimized && (
          <div className="flex-1 overflow-hidden px-4 pb-4">
            <ChatWidget />
          </div>
        )}
      </div>

      {/* Floating Button — hidden when chat is expanded to avoid overlapping input */}
      {!(isOpen && isExpanded) && (
        <button
          onClick={() => { setIsOpen(prev => !prev); setIsMinimized(false); setIsExpanded(false); }}
          className={cn(
            'fixed bottom-6 right-6 z-50 w-14 h-14 rounded-full shadow-lg flex items-center justify-center transition-all duration-300',
            'bg-gradient-to-br from-[var(--color-primary)] to-purple-500 text-white hover:scale-110 glow',
          )}
          title="Chat with AI Assistant"
        >
          {isOpen ? <X className="w-6 h-6" /> : <MessageSquare className="w-6 h-6" />}
        </button>
      )}
    </>
  );
}

import { useState, useRef, useEffect } from 'react';
import { Send, Trash2, Sparkles, Paperclip, X, FileText } from 'lucide-react';
import { useChat } from '../hooks/useChat';
import MessageBubble from './MessageBubble';
import { cn } from '../lib/utils';

export default function ChatWidget() {
  const { messages, isLoading, activeAgent, sendMessage, clearChat } = useChat();
  const [input, setInput] = useState('');
  const [attachment, setAttachment] = useState<{ file: File; previewUrl: string } | null>(null);
  const [retainFile, setRetainFile] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (input.trim() || attachment) {
      const att = attachment ? { name: attachment.file.name, previewUrl: attachment.previewUrl, file: attachment.file, retain: retainFile } : undefined;
      sendMessage(input, att);
      setInput('');
      setAttachment(null);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const previewUrl = file.type.startsWith('image/') ? URL.createObjectURL(file) : '';
    setAttachment({ file, previewUrl });
    e.target.value = '';
  };

  const removeAttachment = () => {
    if (attachment?.previewUrl) URL.revokeObjectURL(attachment.previewUrl);
    setAttachment(null);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-[var(--color-border)]">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-[var(--color-primary)] to-purple-500 flex items-center justify-center">
            <Sparkles className="w-5 h-5 text-white" />
          </div>
          <div>
            <h2 className="text-lg font-semibold">AI Personal Finance Assistant</h2>
            {activeAgent && (
              <p className="text-xs text-[var(--color-primary-light)]">
                Connected to {activeAgent.replace('Agent', ' Agent')}
              </p>
            )}
          </div>
        </div>
        {messages.length > 0 && (
          <button
            onClick={clearChat}
            className="p-2 rounded-lg text-[var(--color-text-muted)] hover:text-[var(--color-danger)] hover:bg-[var(--color-surface-hover)] transition-colors"
            title="Clear conversation"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        )}
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-auto py-6 space-y-4">
        {messages.length === 0 && (
          <div className="flex flex-col items-center justify-center h-full text-center space-y-4 opacity-60">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-[var(--color-primary)]/20 to-purple-500/20 flex items-center justify-center">
              <Sparkles className="w-8 h-8 text-[var(--color-primary-light)]" />
            </div>
            <div>
              <p className="text-lg font-medium">How can I help you today?</p>
              <p className="text-sm text-[var(--color-text-muted)] mt-1">
                Ask about your accounts, transactions, or make a payment
              </p>
            </div>
            <div className="flex flex-wrap gap-2 justify-center max-w-md">
              {[
                "What's my account balance?",
                'Show my recent transactions',
                'Upload an invoice',
                'Show my credit cards',
              ].map(suggestion => (
                <button
                  key={suggestion}
                  onClick={() => sendMessage(suggestion)}
                  className="px-3 py-1.5 text-xs rounded-full glass hover:bg-[var(--color-surface-hover)] transition-colors"
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>
        )}

        {messages.map(msg => (
          <MessageBubble key={msg.id} message={msg} />
        ))}
        <div ref={messagesEndRef} />
      </div>

      {/* Input */}
      <form onSubmit={handleSubmit} className="pt-4 border-t border-[var(--color-border)]">
        {/* Attachment preview */}
        {attachment && (
          <div className="flex items-center gap-2 mb-2 px-1">
            <div className="flex items-center gap-2 px-3 py-2 rounded-xl bg-[var(--color-surface-hover)] text-sm">
              {attachment.previewUrl ? (
                <img src={attachment.previewUrl} alt="" className="w-8 h-8 rounded object-cover" />
              ) : (
                <FileText className="w-5 h-5 text-[var(--color-primary)]" />
              )}
              <span className="truncate max-w-[200px]">{attachment.file.name}</span>
              <button type="button" onClick={removeAttachment} className="p-0.5 rounded hover:bg-[var(--color-border)] transition-colors">
                <X className="w-3.5 h-3.5 text-[var(--color-text-muted)]" />
              </button>
            </div>
            <label className="flex items-center gap-1.5 text-xs text-[var(--color-text-muted)] cursor-pointer select-none">
              <input
                type="checkbox"
                checked={retainFile}
                onChange={e => setRetainFile(e.target.checked)}
                className="rounded accent-[var(--color-primary)]"
              />
              Keep file
            </label>
          </div>
        )}
        <div className="flex items-end gap-3">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/*,.pdf"
            onChange={handleFileSelect}
            className="hidden"
          />
          <button
            type="button"
            onClick={() => fileInputRef.current?.click()}
            className="p-3 rounded-xl text-[var(--color-text-muted)] hover:text-[var(--color-primary)] hover:bg-[var(--color-surface-hover)] transition-colors"
            title="Attach invoice or receipt"
          >
            <Paperclip className="w-5 h-5" />
          </button>
          <div className="flex-1 glass rounded-2xl p-1">
            <textarea
              ref={inputRef}
              value={input}
              onChange={e => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="Type your message..."
              rows={1}
              className="w-full bg-transparent px-4 py-3 text-sm resize-none outline-none placeholder:text-[var(--color-text-muted)]"
              style={{ maxHeight: '120px' }}
            />
          </div>
          <button
            type="submit"
            disabled={isLoading || (!input.trim() && !attachment)}
            className={cn(
              'p-3 rounded-xl transition-all duration-200',
              (input.trim() || attachment) && !isLoading
                ? 'bg-[var(--color-primary)] hover:bg-[var(--color-primary-dark)] text-white glow'
                : 'bg-[var(--color-surface-hover)] text-[var(--color-text-muted)] cursor-not-allowed',
            )}
          >
            <Send className="w-5 h-5" />
          </button>
        </div>
      </form>
    </div>
  );
}

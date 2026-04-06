import { useState, useCallback, useRef } from 'react';
import { streamChat, uploadAttachment, deleteAttachment } from '../lib/api';
import type { ChatMessage } from '../lib/types';

export function useChat() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [threadId, setThreadId] = useState<string | undefined>();
  const [activeAgent, setActiveAgent] = useState<string | undefined>();
  const isLoadingRef = useRef(false);

  const sendMessage = useCallback(async (content: string, attachment?: { name: string; previewUrl: string; file?: File; retain?: boolean }) => {
    if ((!content.trim() && !attachment) || isLoadingRef.current) return;

    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: content.trim() || (attachment ? `Please scan this invoice and show me the details` : ''),
      timestamp: new Date(),
      attachment: attachment ? { name: attachment.name, previewUrl: attachment.previewUrl } : undefined,
    };

    const assistantMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: '',
      timestamp: new Date(),
      isStreaming: true,
    };

    setMessages(prev => [...prev, userMsg, assistantMsg]);
    setIsLoading(true);
    isLoadingRef.current = true;

    let attachmentId: string | undefined;
    try {
      // Upload file to backend and get real attachment ID
      if (attachment?.file) {
        attachmentId = await uploadAttachment(attachment.file);
      }
      for await (const event of streamChat(userMsg.content, threadId, attachmentId)) {
        switch (event.type) {
          case 'thread_id':
            if (event.content) setThreadId(event.content);
            break;
          case 'agent':
            if (event.agentName) {
              setActiveAgent(event.agentName);
              setMessages(prev =>
                prev.map(m =>
                  m.id === assistantMsg.id
                    ? { ...m, agentName: event.agentName }
                    : m,
                ),
              );
            }
            break;
          case 'delta':
            if (event.content) {
              setMessages(prev =>
                prev.map(m =>
                  m.id === assistantMsg.id
                    ? { ...m, content: m.content + event.content, toolName: undefined }
                    : m,
                ),
              );
            }
            break;
          case 'tool_call':
            if (event.toolName) {
              setMessages(prev =>
                prev.map(m =>
                  m.id === assistantMsg.id
                    ? { ...m, toolName: event.toolName }
                    : m,
                ),
              );
            }
            break;
          case 'clear':
            // Server signals that the previously streamed text was from an
            // intermediate round (before tool execution) and should be discarded.
            setMessages(prev =>
              prev.map(m =>
                m.id === assistantMsg.id
                  ? { ...m, content: '' }
                  : m,
              ),
            );
            break;
          case 'done':
            setMessages(prev =>
              prev.map(m =>
                m.id === assistantMsg.id
                  ? { ...m, isStreaming: false }
                  : m,
              ),
            );
            break;
          case 'error':
            setMessages(prev =>
              prev.map(m =>
                m.id === assistantMsg.id
                  ? { ...m, content: `Error: ${event.error}`, isStreaming: false }
                  : m,
              ),
            );
            break;
        }
      }
      // Generator returned normally (stream closed without done event)
      // The finally block below will handle cleanup
    } catch (err) {
      setMessages(prev =>
        prev.map(m =>
          m.id === assistantMsg.id
            ? { ...m, content: `Connection error. Please try again.`, isStreaming: false }
            : m,
        ),
      );
    } finally {
      // Ensure the assistant message is marked as done even if no 'done' event was received
      setMessages(prev =>
        prev.map(m =>
          m.id === assistantMsg.id && m.isStreaming
            ? { ...m, isStreaming: false }
            : m,
        ),
      );
      // Delete attachment if user opted out of retention
      if (attachmentId && attachment?.retain === false) {
        deleteAttachment(attachmentId).catch(() => {});
      }
      setIsLoading(false);
      isLoadingRef.current = false;
    }
  }, [threadId]);

  const clearChat = useCallback(() => {
    setMessages([]);
    setThreadId(undefined);
    setActiveAgent(undefined);
  }, []);

  return { messages, isLoading, threadId, activeAgent, sendMessage, clearChat };
}

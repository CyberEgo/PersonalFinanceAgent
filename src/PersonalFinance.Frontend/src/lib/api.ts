import type { ChatStreamEvent } from './types';

const API_BASE = '/api';

export async function* streamChat(
  message: string,
  threadId?: string,
  attachmentId?: string,
): AsyncGenerator<ChatStreamEvent> {
  const controller = new AbortController();
  const IDLE_TIMEOUT = 90_000; // 90 s — generous for Document Intelligence calls
  let idleTimer: ReturnType<typeof setTimeout> | undefined;

  const resetIdle = () => {
    clearTimeout(idleTimer);
    idleTimer = setTimeout(() => controller.abort(), IDLE_TIMEOUT);
  };

  try {
    resetIdle();
    const response = await fetch(`${API_BASE}/chat`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message, threadId, attachmentId }),
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new Error(`Chat request failed: ${response.status}`);
    }

    const reader = response.body?.getReader();
    if (!reader) throw new Error('No response body');

    const decoder = new TextDecoder();
    let buffer = '';

    try {
      while (true) {
        resetIdle();
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          const trimmed = line.trim();
          if (trimmed.startsWith('data: ')) {
            try {
              const event: ChatStreamEvent = JSON.parse(trimmed.slice(6));
              yield event;
              // Stop reading once the server signals completion
              if (event.type === 'done' || event.type === 'error') return;
            } catch {
              // Skip malformed events
            }
          }
          // SSE comments (e.g. ": keepalive") are silently ignored
        }
      }
    } finally {
      reader.cancel().catch(() => {});
    }
  } finally {
    clearTimeout(idleTimer);
  }
}

export async function fetchAccounts(userName: string) {
  const res = await fetch(`${API_BASE}/accounts/user/${encodeURIComponent(userName)}`);
  return res.json();
}

export async function fetchAccountDetails(accountId: string) {
  const res = await fetch(`${API_BASE}/accounts/${encodeURIComponent(accountId)}`);
  return res.json();
}

export async function fetchCards(accountId: string) {
  const res = await fetch(`${API_BASE}/accounts/${encodeURIComponent(accountId)}/cards`);
  return res.json();
}

export async function fetchTransactions(accountId: string, count = 10) {
  const res = await fetch(`${API_BASE}/transactions/${encodeURIComponent(accountId)}?count=${count}`);
  return res.json();
}

export async function uploadAttachment(file: File): Promise<string> {
  const form = new FormData();
  form.append('file', file);
  const res = await fetch(`${API_BASE}/attachments/upload`, { method: 'POST', body: form });
  if (!res.ok) throw new Error(`Upload failed: ${res.status}`);
  const data = await res.json();
  return data.attachmentId;
}

export async function deleteAttachment(attachmentId: string): Promise<void> {
  await fetch(`${API_BASE}/attachments/${encodeURIComponent(attachmentId)}`, { method: 'DELETE' });
}

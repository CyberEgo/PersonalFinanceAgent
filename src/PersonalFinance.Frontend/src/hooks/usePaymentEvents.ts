import { useEffect, useRef, useState } from 'react';

const API_BASE = '/api';

/**
 * Subscribes to the SSE payment events endpoint.
 * Each time a payment is processed, `refreshKey` increments,
 * which consumers can use as a dependency to re-fetch data.
 */
export function usePaymentEvents() {
  const [refreshKey, setRefreshKey] = useState(0);
  const retryDelay = useRef(1000);

  useEffect(() => {
    let cancelled = false;
    let controller: AbortController;

    function connect() {
      if (cancelled) return;
      controller = new AbortController();

      fetch(`${API_BASE}/events/payments`, { signal: controller.signal })
        .then(async (res) => {
          if (!res.ok || !res.body) return;
          retryDelay.current = 1000; // reset on successful connect

          const reader = res.body.getReader();
          const decoder = new TextDecoder();
          let buffer = '';

          while (!cancelled) {
            const { done, value } = await reader.read();
            if (done) break;
            buffer += decoder.decode(value, { stream: true });

            // Process complete SSE messages
            const lines = buffer.split('\n');
            buffer = lines.pop() ?? '';
            for (const line of lines) {
              if (line.startsWith('data:')) {
                setRefreshKey((k) => k + 1);
              }
            }
          }
        })
        .catch(() => {
          /* connection lost — will reconnect */
        })
        .finally(() => {
          // Auto-reconnect with exponential backoff (max 30s)
          if (!cancelled) {
            setTimeout(connect, retryDelay.current);
            retryDelay.current = Math.min(retryDelay.current * 2, 30000);
          }
        });
    }

    connect();

    return () => {
      cancelled = true;
      controller?.abort();
    };
  }, []);

  return refreshKey;
}

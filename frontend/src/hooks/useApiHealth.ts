import { useCallback, useEffect, useState } from 'react';
import { isApiHealthy } from '../api/health';

export type ApiHealth = 'checking' | 'online' | 'offline';

const POLL_INTERVAL_MS = 30_000;

/** Verifica /health periodicamente para o indicador de status da interface. */
export function useApiHealth(): { health: ApiHealth; recheck: () => void } {
  const [health, setHealth] = useState<ApiHealth>('checking');

  const check = useCallback(async () => {
    setHealth((current) => (current === 'offline' ? 'checking' : current));
    setHealth((await isApiHealthy()) ? 'online' : 'offline');
  }, []);

  useEffect(() => {
    void check();
    const timer = window.setInterval(() => void check(), POLL_INTERVAL_MS);
    return () => window.clearInterval(timer);
  }, [check]);

  return { health, recheck: () => void check() };
}

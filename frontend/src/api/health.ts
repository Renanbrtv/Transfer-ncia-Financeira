import { apiUrl } from './http';

/** GET /health da API. Qualquer falha (rede, 502 do proxy, timeout) conta como indisponível. */
export async function isApiHealthy(): Promise<boolean> {
  try {
    const response = await fetch(apiUrl('/health'), { signal: AbortSignal.timeout(5_000) });
    return response.ok;
  } catch {
    return false;
  }
}

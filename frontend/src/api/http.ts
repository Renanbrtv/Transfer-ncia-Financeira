import { ApiError } from './errors';
import type { ProblemDetails } from './types';

/**
 * URL base da API. Vazio = mesma origem: em desenvolvimento o Vite encaminha /api para a API
 * local e, no Docker, o Nginx faz o mesmo. Assim o navegador nunca precisa de CORS.
 */
const API_BASE_URL = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

const TIMEOUT_MS = 15_000;

interface RequestOptions {
  method?: 'GET' | 'POST';
  body?: unknown;
  headers?: Record<string, string>;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const response = await send(path, options);
  const payload = await readJson(response);

  if (!response.ok) {
    throw ApiError.fromResponse(response.status, (payload as ProblemDetails | null) ?? {});
  }

  return payload as T;
}

async function send(path: string, { method = 'GET', body, headers }: RequestOptions): Promise<Response> {
  try {
    return await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: { Accept: 'application/json', ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), ...headers },
      body: body === undefined ? undefined : JSON.stringify(body),
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });
  } catch (error) {
    const timedOut = error instanceof DOMException && error.name === 'TimeoutError';
    throw ApiError.network(timedOut ? 'timeout' : 'offline');
  }
}

/** Respostas de proxy (ex.: 502 do Nginx) vêm em HTML; só interpretamos JSON. */
async function readJson(response: Response): Promise<unknown> {
  const contentType = response.headers.get('Content-Type') ?? '';
  if (response.status === 204 || !contentType.includes('json')) {
    return null;
  }
  try {
    return await response.json();
  } catch {
    return null;
  }
}

export function apiUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}

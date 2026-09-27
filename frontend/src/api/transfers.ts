import { request } from './http';
import type { CreateTransferRequest, ScheduleTransferRequest, Transfer } from './types';

const IDEMPOTENCY_HEADER = 'Idempotency-Key';

export const transfersApi = {
  get: (id: string) => request<Transfer>(`/api/transfers/${encodeURIComponent(id)}`),

  /** A chave de idempotência garante que um reenvio (duplo clique, retry de rede) não duplique a operação. */
  create: (payload: CreateTransferRequest, idempotencyKey: string) =>
    request<Transfer>('/api/transfers', {
      method: 'POST',
      body: payload,
      headers: { [IDEMPOTENCY_HEADER]: idempotencyKey },
    }),

  schedule: (payload: ScheduleTransferRequest, idempotencyKey: string) =>
    request<Transfer>('/api/transfers/scheduled', {
      method: 'POST',
      body: payload,
      headers: { [IDEMPOTENCY_HEADER]: idempotencyKey },
    }),

  cancel: (id: string) => request<Transfer>(`/api/transfers/${encodeURIComponent(id)}/cancel`, { method: 'POST' }),
};

export function newIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

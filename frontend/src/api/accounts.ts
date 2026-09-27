import { request } from './http';
import type { Account, Transfer } from './types';

/** Limite máximo aceito pela API em GET /api/accounts/{id}/transfers. */
export const MAX_TRANSFERS_PER_ACCOUNT = 100;

export const accountsApi = {
  list: () => request<Account[]>('/api/accounts'),

  get: (id: number) => request<Account>(`/api/accounts/${id}`),

  listTransfers: (id: number, take = 20) =>
    request<Transfer[]>(`/api/accounts/${id}/transfers?take=${Math.min(take, MAX_TRANSFERS_PER_ACCOUNT)}`),
};

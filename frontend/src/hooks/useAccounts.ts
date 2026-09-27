import { accountsApi } from '../api/accounts';
import { useAsync } from './useAsync';

/** Lista de contas usada pelos formulários e pelo dashboard. */
export function useAccounts() {
  return useAsync(accountsApi.list, []);
}

import { MAX_TRANSFERS_PER_ACCOUNT, accountsApi } from '../api/accounts';
import type { Account, Transfer } from '../api/types';
import { mergeTransfers } from '../lib/dashboard';
import { useAsync } from './useAsync';

export interface DashboardData {
  accounts: Account[];
  transfers: Transfer[];
  loadedAt: Date;
}

/**
 * A API não expõe estatísticas globais; o dashboard é montado a partir dos endpoints existentes:
 * a lista de contas e o extrato de cada uma (até 100 transferências mais recentes por conta).
 */
async function loadDashboard(): Promise<DashboardData> {
  const accounts = await accountsApi.list();
  const statements = await Promise.all(
    accounts.map((account) => accountsApi.listTransfers(account.id, MAX_TRANSFERS_PER_ACCOUNT)),
  );
  return { accounts, transfers: mergeTransfers(statements), loadedAt: new Date() };
}

export function useDashboardData() {
  return useAsync(loadDashboard, []);
}

import type { Account, Transfer } from '../api/types';

export interface PortfolioPosition {
  totalBalance: number;
  totalAvailable: number;
  /** Soma dos saldos negativos (quanto do cheque especial está em uso). */
  overdraftInUse: number;
  activeAccounts: number;
  totalAccounts: number;
}

export interface TransferMetrics {
  completed: number;
  scheduled: number;
  failed: number;
  /** Execuções (concluídas + recusadas) nos últimos 60 minutos: o mesmo critério dos limites da API. */
  attemptsLastHour: number;
}

export function computePosition(accounts: Account[]): PortfolioPosition {
  return accounts.reduce<PortfolioPosition>(
    (position, account) => ({
      totalBalance: position.totalBalance + account.balance,
      totalAvailable: position.totalAvailable + account.availableBalance,
      overdraftInUse: position.overdraftInUse + Math.max(0, -account.balance),
      activeAccounts: position.activeAccounts + (account.status === 'Active' ? 1 : 0),
      totalAccounts: position.totalAccounts + 1,
    }),
    { totalBalance: 0, totalAvailable: 0, overdraftInUse: 0, activeAccounts: 0, totalAccounts: 0 },
  );
}

const ONE_HOUR_MS = 60 * 60 * 1000;

export function computeMetrics(transfers: Transfer[], now: Date): TransferMetrics {
  const windowStart = now.getTime() - ONE_HOUR_MS;

  return transfers.reduce<TransferMetrics>(
    (metrics, transfer) => {
      const executedAt = transfer.processedAt ? new Date(transfer.processedAt).getTime() : null;
      const executedInWindow =
        executedAt !== null &&
        executedAt > windowStart &&
        (transfer.status === 'Completed' || transfer.status === 'Failed');

      return {
        completed: metrics.completed + (transfer.status === 'Completed' ? 1 : 0),
        scheduled: metrics.scheduled + (transfer.status === 'Scheduled' ? 1 : 0),
        failed: metrics.failed + (transfer.status === 'Failed' ? 1 : 0),
        attemptsLastHour: metrics.attemptsLastHour + (executedInWindow ? 1 : 0),
      };
    },
    { completed: 0, scheduled: 0, failed: 0, attemptsLastHour: 0 },
  );
}

/** Junta os extratos das contas (cada transferência aparece na origem e no destino) sem duplicar. */
export function mergeTransfers(perAccount: Transfer[][]): Transfer[] {
  const byId = new Map<string, Transfer>();
  for (const transfer of perAccount.flat()) {
    byId.set(transfer.id, transfer);
  }
  return [...byId.values()].sort((a, b) => b.createdAt.localeCompare(a.createdAt));
}

/** Quanto do cheque especial está sendo usado e quanto ainda resta. */
export function overdraftUsage(account: Account): { used: number; remaining: number; ratio: number } {
  const used = Math.min(Math.max(0, -account.balance), account.overdraftLimit);
  const remaining = Math.max(0, account.overdraftLimit - used);
  const ratio = account.overdraftLimit > 0 ? used / account.overdraftLimit : 0;
  return { used, remaining, ratio };
}

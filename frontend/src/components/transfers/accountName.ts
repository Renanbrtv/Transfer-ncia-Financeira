import type { Account } from '../../api/types';

/** Nome do titular para exibição; cai para "Conta N" se a conta não estiver na lista carregada. */
export function accountName(accounts: Account[], id: number): string {
  return accounts.find((account) => account.id === id)?.holderName ?? `Conta ${id}`;
}

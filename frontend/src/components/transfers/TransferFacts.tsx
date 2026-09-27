import type { ReactNode } from 'react';
import type { Account, Transfer } from '../../api/types';
import { formatDateTime, transferTypeLabel } from '../../lib/format';
import { CopyButton } from '../ui/CopyButton';
import { TransferStatusBadge } from '../ui/StatusBadge';
import { accountName } from './accountName';
import styles from './TransferFacts.module.css';

interface Fact {
  label: string;
  value: ReactNode;
}

/** Monta apenas os campos que existem na transferência (nada de "—" para datas que não se aplicam). */
function factsOf(transfer: Transfer, accounts: Account[]): Fact[] {
  const party = (id: number) => (
    <>
      {accountName(accounts, id)} <span className={styles.muted}>· conta {id}</span>
    </>
  );

  const facts: (Fact | null)[] = [
    {
      label: 'Id da transferência',
      value: (
        <span className={styles.id}>
          <span className="mono">{transfer.id}</span>
          <CopyButton value={transfer.id} />
        </span>
      ),
    },
    { label: 'Status', value: <TransferStatusBadge status={transfer.status} /> },
    { label: 'Tipo', value: transferTypeLabel[transfer.type] },
    { label: 'Conta de origem', value: party(transfer.sourceAccountId) },
    { label: 'Conta de destino', value: party(transfer.destinationAccountId) },
    { label: 'Criada em', value: formatDateTime(transfer.createdAt) },
    transfer.scheduledFor ? { label: 'Agendada para', value: formatDateTime(transfer.scheduledFor) } : null,
    transfer.processedAt
      ? {
          label: transfer.status === 'Completed' ? 'Concluída em' : 'Processada em',
          value: formatDateTime(transfer.processedAt),
        }
      : null,
    transfer.cancelledAt ? { label: 'Cancelada em', value: formatDateTime(transfer.cancelledAt) } : null,
  ];

  return facts.filter((fact): fact is Fact => fact !== null);
}

export function TransferFacts({ transfer, accounts }: { transfer: Transfer; accounts: Account[] }) {
  return (
    <dl className={styles.facts}>
      {factsOf(transfer, accounts).map((fact) => (
        <div key={fact.label} className={styles.fact}>
          <dt>{fact.label}</dt>
          <dd>{fact.value}</dd>
        </div>
      ))}
    </dl>
  );
}

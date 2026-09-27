import type { AccountStatus, TransferStatus } from '../../api/types';
import { accountStatusLabel, transferStatusLabel } from '../../lib/format';
import styles from './StatusBadge.module.css';

type Tone = 'positive' | 'info' | 'warning' | 'negative' | 'neutral';

const TRANSFER_TONE: Record<TransferStatus, Tone> = {
  Completed: 'positive',
  Scheduled: 'info',
  Processing: 'warning',
  Failed: 'negative',
  Cancelled: 'neutral',
};

const ACCOUNT_TONE: Record<AccountStatus, Tone> = {
  Active: 'positive',
  Blocked: 'negative',
  Inactive: 'neutral',
};

function Badge({ tone, children }: { tone: Tone; children: string }) {
  return (
    <span className={`${styles.badge} ${styles[tone]}`}>
      <span className={styles.dot} aria-hidden="true" />
      {children}
    </span>
  );
}

export function TransferStatusBadge({ status }: { status: TransferStatus }) {
  return <Badge tone={TRANSFER_TONE[status]}>{transferStatusLabel[status]}</Badge>;
}

export function AccountStatusBadge({ status }: { status: AccountStatus }) {
  return <Badge tone={ACCOUNT_TONE[status]}>{accountStatusLabel[status]}</Badge>;
}

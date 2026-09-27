import type { Account } from '../../api/types';
import { formatLongDateTime } from '../../lib/format';
import { availableAfter, previewWarnings } from '../../lib/transferPreview';
import { Alert } from '../ui/Alert';
import { Money } from '../ui/Money';
import { AccountStatusBadge } from '../ui/StatusBadge';
import styles from './TransferSummary.module.css';

interface TransferSummaryProps {
  source?: Account;
  destination?: Account;
  amount: number;
  mode: 'immediate' | 'scheduled';
  scheduledFor?: Date | null;
}

function Party({ label, account }: { label: string; account?: Account }) {
  return (
    <div className={styles.party}>
      <span className={styles.partyLabel}>{label}</span>
      {account ? (
        <>
          <span className={styles.partyName}>{account.holderName}</span>
          <span className={styles.partyMeta}>
            Conta {account.id}
            {account.status !== 'Active' && <AccountStatusBadge status={account.status} />}
          </span>
        </>
      ) : (
        <span className={styles.placeholder}>Não selecionada</span>
      )}
    </div>
  );
}

/** Resumo da operação exibido ao lado do formulário, antes da confirmação. */
export function TransferSummary({ source, destination, amount, mode, scheduledFor }: TransferSummaryProps) {
  const after = availableAfter(source, amount);
  const warnings = previewWarnings(source, destination, amount, mode);

  return (
    <aside className={styles.summary} aria-label="Resumo da operação">
      <h2 className={styles.heading}>Resumo da operação</h2>

      <div className={styles.amount}>
        <span className={styles.rowLabel}>Valor</span>
        <Money value={amount} size="xl" />
      </div>

      <div className={styles.parties}>
        <Party label="De" account={source} />
        <Party label="Para" account={destination} />
      </div>

      <dl className={styles.rows}>
        {mode === 'scheduled' && (
          <div className={styles.row}>
            <dt>Data programada</dt>
            <dd>{scheduledFor ? formatLongDateTime(scheduledFor) : '—'}</dd>
          </div>
        )}
        <div className={styles.row}>
          <dt>Saldo disponível hoje</dt>
          <dd>{source ? <Money value={source.availableBalance} /> : '—'}</dd>
        </div>
        {mode === 'immediate' && (
          <div className={styles.row}>
            <dt>Disponível após a operação</dt>
            <dd>{after !== null && amount > 0 ? <Money value={after} highlightNegative /> : '—'}</dd>
          </div>
        )}
      </dl>

      {warnings.length > 0 && (
        <div className={styles.warnings}>
          {warnings.map((warning) => (
            <Alert key={warning.message} tone={warning.tone}>
              {warning.message}
            </Alert>
          ))}
        </div>
      )}

      <p className={styles.footnote}>
        {mode === 'immediate'
          ? 'Débito e crédito acontecem na mesma operação. Limites por hora e status das contas são validados no envio.'
          : 'Saldo, status das contas e limites por hora são verificados novamente no momento da execução.'}
      </p>
    </aside>
  );
}

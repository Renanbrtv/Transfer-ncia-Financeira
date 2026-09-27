import type { Account, Transfer } from '../../api/types';
import { routeHref } from '../../hooks/useRoute';
import { formatLongDateTime } from '../../lib/format';
import { Button } from '../ui/Button';
import { Icon, type IconName } from '../ui/Icon';
import { Money } from '../ui/Money';
import { TransferFacts } from './TransferFacts';
import styles from './TransferReceipt.module.css';

interface Outcome {
  tone: 'positive' | 'info' | 'negative';
  icon: IconName;
  title: string;
  subtitle: string;
}

function outcomeOf(transfer: Transfer): Outcome {
  switch (transfer.status) {
    case 'Completed':
      return {
        tone: 'positive',
        icon: 'check',
        title: 'Transferência realizada com sucesso',
        subtitle: 'O valor foi debitado da origem e creditado no destino.',
      };
    case 'Scheduled':
      return {
        tone: 'info',
        icon: 'calendar',
        title: 'Transferência agendada com sucesso',
        subtitle: transfer.scheduledFor
          ? `Execução programada para ${formatLongDateTime(transfer.scheduledFor)}.`
          : 'A transferência será executada na data programada.',
      };
    default:
      return {
        tone: 'negative',
        icon: 'ban',
        title: 'Transferência recusada',
        subtitle: transfer.failureMessage ?? 'A operação não atendeu às regras de transferência.',
      };
  }
}

interface TransferReceiptProps {
  transfer: Transfer;
  accounts: Account[];
  onNew: () => void;
  newLabel: string;
}

/** Comprovante exibido após enviar uma transferência (concluída, agendada ou recusada). */
export function TransferReceipt({ transfer, accounts, onNew, newLabel }: TransferReceiptProps) {
  const outcome = outcomeOf(transfer);

  return (
    <section className={styles.receipt} aria-live="polite" aria-labelledby="receipt-title">
      <header className={`${styles.header} ${styles[outcome.tone]}`}>
        <span className={styles.icon}>
          <Icon name={outcome.icon} size={20} />
        </span>
        <div>
          <h2 id="receipt-title" className={styles.title}>
            {outcome.title}
          </h2>
          <p className={styles.subtitle}>{outcome.subtitle}</p>
        </div>
      </header>

      <div className={styles.body}>
        <div className={styles.amount}>
          <span>Valor</span>
          <Money value={transfer.amount} size="xl" />
        </div>

        <TransferFacts transfer={transfer} accounts={accounts} />

        {transfer.status === 'Failed' && (
          <p className={styles.note}>
            A tentativa ficou registrada com status <strong>Recusada</strong> e conta para o limite de tentativas por hora da
            conta de origem.
          </p>
        )}
      </div>

      <footer className={styles.footer}>
        <a className={styles.detailsLink} href={routeHref({ name: 'lookup', param: transfer.id })}>
          Ver detalhes
          <Icon name="arrowRight" size={14} />
        </a>
        <Button variant="secondary" onClick={onNew}>
          {newLabel}
        </Button>
      </footer>
    </section>
  );
}

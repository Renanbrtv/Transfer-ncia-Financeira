import type { Transfer } from '../../api/types';
import { formatDateTime } from '../../lib/format';
import styles from './TransferTimeline.module.css';

interface TimelineEvent {
  label: string;
  at: string | null;
  tone: 'done' | 'pending' | 'negative' | 'neutral' | 'skipped';
}

/** Eventos derivados apenas dos campos de data que a API devolve. */
function eventsOf(transfer: Transfer): TimelineEvent[] {
  const events: TimelineEvent[] = [{ label: 'Transferência registrada', at: transfer.createdAt, tone: 'done' }];

  if (transfer.status === 'Cancelled') {
    events.push({ label: 'Cancelada antes da execução', at: transfer.cancelledAt, tone: 'neutral' });
    if (transfer.scheduledFor) {
      events.push({ label: 'Execução programada (não realizada)', at: transfer.scheduledFor, tone: 'skipped' });
    }
    return events;
  }

  if (transfer.type === 'Scheduled') {
    events.push({
      label: transfer.status === 'Scheduled' ? 'Aguardando execução programada' : 'Execução programada',
      at: transfer.scheduledFor,
      tone: transfer.status === 'Scheduled' ? 'pending' : 'done',
    });
  }

  if (transfer.status === 'Completed') {
    events.push({ label: 'Débito e crédito concluídos', at: transfer.processedAt, tone: 'done' });
  } else if (transfer.status === 'Failed') {
    events.push({
      label: transfer.failureMessage ?? 'Recusada pelas regras de transferência',
      at: transfer.processedAt,
      tone: 'negative',
    });
  } else if (transfer.status === 'Processing') {
    events.push({ label: 'Em processamento', at: null, tone: 'pending' });
  }

  return events;
}

export function TransferTimeline({ transfer }: { transfer: Transfer }) {
  return (
    <ol className={styles.timeline} aria-label="Histórico da transferência">
      {eventsOf(transfer).map((event) => (
        <li key={event.label} className={`${styles.event} ${styles[event.tone]}`}>
          <span className={styles.marker} aria-hidden="true" />
          <div>
            <p className={styles.label}>{event.label}</p>
            {event.at && <p className={`${styles.time} num`}>{formatDateTime(event.at)}</p>}
          </div>
        </li>
      ))}
    </ol>
  );
}

import { AccountsTable } from '../components/accounts/AccountsTable';
import { PageHeader } from '../components/layout/PageHeader';
import { TransferTable } from '../components/transfers/TransferTable';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { EmptyState } from '../components/ui/EmptyState';
import { ErrorState } from '../components/ui/ErrorState';
import { Money } from '../components/ui/Money';
import { Skeleton, SkeletonRows } from '../components/ui/Skeleton';
import { useDashboardData, type DashboardData } from '../hooks/useDashboardData';
import { computeMetrics, computePosition } from '../lib/dashboard';
import { formatMoney, formatTime } from '../lib/format';
import styles from './DashboardPage.module.css';

const RECENT_LIMIT = 8;

interface DashboardPageProps {
  onNewTransfer: () => void;
}

export function DashboardPage({ onNewTransfer }: DashboardPageProps) {
  const dashboard = useDashboardData();

  return (
    <>
      <PageHeader
        title="Dashboard"
        description="Posição consolidada das contas e movimentação recente."
        actions={
          <>
            <Button variant="secondary" icon="refresh" onClick={dashboard.reload} loading={dashboard.loading && !!dashboard.data}>
              Atualizar
            </Button>
            <Button icon="transfer" onClick={onNewTransfer}>
              Nova transferência
            </Button>
          </>
        }
      />

      {dashboard.error && !dashboard.data ? (
        <ErrorState error={dashboard.error} onRetry={dashboard.reload} retrying={dashboard.loading} variant="page" />
      ) : !dashboard.data ? (
        <DashboardSkeleton />
      ) : (
        <DashboardContent data={dashboard.data} onNewTransfer={onNewTransfer} />
      )}
    </>
  );
}

function DashboardContent({ data, onNewTransfer }: { data: DashboardData; onNewTransfer: () => void }) {
  const position = computePosition(data.accounts);
  const metrics = computeMetrics(data.transfers, data.loadedAt);

  return (
    <div className={styles.stack}>
      <section className={styles.position} aria-label="Posição consolidada">
        <div className={styles.primaryFigure}>
          <span className={styles.label}>Saldo total disponível</span>
          <Money value={position.totalAvailable} size="xl" />
          <span className={styles.caption}>Saldo em conta + limites de cheque especial, todas as contas</span>
        </div>
        <dl className={styles.secondaryFigures}>
          <div>
            <dt>Saldo em conta</dt>
            <dd>
              <Money value={position.totalBalance} size="inherit" highlightNegative />
            </dd>
          </div>
          <div>
            <dt>Cheque especial em uso</dt>
            <dd className={position.overdraftInUse > 0 ? styles.warningText : undefined}>
              <span className="num">{formatMoney(position.overdraftInUse)}</span>
            </dd>
          </div>
          <div>
            <dt>Contas ativas</dt>
            <dd className="num">
              {position.activeAccounts} de {position.totalAccounts}
            </dd>
          </div>
        </dl>
      </section>

      <section aria-label="Indicadores de transferências">
        <ul className={styles.metrics}>
          <Metric label="Concluídas" value={metrics.completed} tone="positive" />
          <Metric label="Agendadas" value={metrics.scheduled} tone="info" />
          <Metric label="Recusadas" value={metrics.failed} tone="negative" />
          <Metric label="Tentativas na última hora" value={metrics.attemptsLastHour} tone="neutral" hint="Concluídas + recusadas, todas as contas" />
        </ul>
        <p className={styles.source}>
          Calculado a partir do extrato das contas (até 100 transferências mais recentes de cada uma). Atualizado às{' '}
          <span className="num">{formatTime(data.loadedAt)}</span>.
        </p>
      </section>

      <Card
        title="Transferências recentes"
        description={`Últimas ${RECENT_LIMIT} operações entre todas as contas.`}
        padding="flush"
      >
        {data.transfers.length === 0 ? (
          <EmptyState
            icon="transfer"
            title="Nenhuma transferência ainda"
            description="As operações realizadas e agendadas aparecem aqui."
            action={
              <Button icon="transfer" onClick={onNewTransfer}>
                Realizar a primeira transferência
              </Button>
            }
          />
        ) : (
          <TransferTable transfers={data.transfers.slice(0, RECENT_LIMIT)} accounts={data.accounts} />
        )}
      </Card>

      <Card title="Contas" description="Saldos atuais e limites de cheque especial." padding="flush">
        <AccountsTable accounts={data.accounts} />
      </Card>
    </div>
  );
}

function Metric({
  label,
  value,
  tone,
  hint,
}: {
  label: string;
  value: number;
  tone: 'positive' | 'info' | 'negative' | 'neutral';
  hint?: string;
}) {
  return (
    <li className={styles.metric}>
      <span className={styles.metricLabel}>
        <span className={`${styles.metricDot} ${styles[tone]}`} aria-hidden="true" />
        {label}
      </span>
      <span className={`${styles.metricValue} num`}>{value}</span>
      {hint && <span className={styles.metricHint}>{hint}</span>}
    </li>
  );
}

function DashboardSkeleton() {
  return (
    <div className={styles.stack} role="status" aria-label="Carregando dashboard">
      <div className={styles.position}>
        <div className={styles.primaryFigure}>
          <Skeleton width={140} height={12} />
          <Skeleton width={220} height={32} />
        </div>
        <div className={styles.secondaryFigures}>
          {[0, 1, 2].map((index) => (
            <div key={index}>
              <Skeleton width={110} height={12} />
              <Skeleton width={90} height={16} style={{ marginTop: 8 }} />
            </div>
          ))}
        </div>
      </div>
      <Card padding="flush">
        <SkeletonRows rows={6} columns={5} />
      </Card>
    </div>
  );
}

import { accountsApi } from '../api/accounts';
import type { Account, Transfer } from '../api/types';
import { AccountSelect } from '../components/accounts/AccountSelect';
import { OverdraftMeter } from '../components/accounts/OverdraftMeter';
import { PageHeader } from '../components/layout/PageHeader';
import { TransferTable } from '../components/transfers/TransferTable';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { EmptyState } from '../components/ui/EmptyState';
import { ErrorState } from '../components/ui/ErrorState';
import { Money } from '../components/ui/Money';
import { SkeletonRows } from '../components/ui/Skeleton';
import { AccountStatusBadge } from '../components/ui/StatusBadge';
import { useAccounts } from '../hooks/useAccounts';
import { useAsync } from '../hooks/useAsync';
import { overdraftUsage } from '../lib/dashboard';
import { accountStatusLabel, formatMoney } from '../lib/format';
import styles from './AccountPage.module.css';

interface AccountPageProps {
  accountId?: string;
  onSelect: (accountId: string) => void;
  onNewTransfer: () => void;
}

interface AccountDetails {
  account: Account;
  statement: Transfer[];
}

async function loadAccount(id: number): Promise<AccountDetails> {
  const [account, statement] = await Promise.all([accountsApi.get(id), accountsApi.listTransfers(id, 50)]);
  return { account, statement };
}

export function AccountPage({ accountId, onSelect, onNewTransfer }: AccountPageProps) {
  const accounts = useAccounts();
  const numericId = accountId && /^\d+$/.test(accountId) ? Number(accountId) : null;
  const details = useAsync(numericId ? () => loadAccount(numericId) : null, [numericId]);

  return (
    <>
      <PageHeader title="Consultar conta" description="Saldo, cheque especial e extrato de transferências da conta." />

      <Card className={styles.selector}>
        <AccountSelect
          id="account-lookup"
          label="Conta"
          accounts={accounts.data ?? []}
          value={numericId ? String(numericId) : ''}
          onChange={(value) => value && onSelect(value)}
          disabled={!accounts.data}
          hint={accounts.error ? 'Não foi possível carregar a lista de contas.' : undefined}
        />
      </Card>

      {!numericId ? (
        <Card>
          <EmptyState icon="wallet" title="Selecione uma conta" description="Escolha uma conta acima para ver o saldo e o extrato." />
        </Card>
      ) : details.error ? (
        details.error.kind === 'notFound' ? (
          <Card>
            <EmptyState icon="wallet" title="Conta não encontrada" description={`Não existe conta com o Id ${numericId}.`} />
          </Card>
        ) : (
          <ErrorState error={details.error} onRetry={details.reload} retrying={details.loading} variant="page" />
        )
      ) : !details.data ? (
        <Card padding="flush">
          <SkeletonRows rows={4} columns={3} />
        </Card>
      ) : (
        <AccountPanel details={details.data} accounts={accounts.data ?? []} onNewTransfer={onNewTransfer} />
      )}
    </>
  );
}

function AccountPanel({
  details: { account, statement },
  accounts,
  onNewTransfer,
}: {
  details: AccountDetails;
  accounts: Account[];
  onNewTransfer: () => void;
}) {
  const { used } = overdraftUsage(account);

  return (
    <div className={styles.panel}>
      <section className={styles.overview} aria-labelledby="account-holder">
        <header className={styles.overviewHeader}>
          <div>
            <h2 id="account-holder" className={styles.holder}>
              {account.holderName}
            </h2>
            <p className={styles.accountMeta}>
              Conta <span className="num">{account.id}</span>
            </p>
          </div>
          <div className={styles.headerActions}>
            <AccountStatusBadge status={account.status} />
            {account.status === 'Active' && (
              <Button variant="secondary" icon="transfer" onClick={onNewTransfer}>
                Transferir
              </Button>
            )}
          </div>
        </header>

        {account.status !== 'Active' && (
          <div className={styles.statusAlert}>
            <Alert tone="warning" title={`Conta ${accountStatusLabel[account.status].toLowerCase()}`}>
              Esta conta não pode enviar nem receber transferências enquanto não estiver ativa.
            </Alert>
          </div>
        )}

        <div className={styles.figures}>
          <div className={styles.figure}>
            <span className={styles.figureLabel}>Saldo atual</span>
            <Money value={account.balance} size="lg" highlightNegative />
            <span className={styles.figureNote}>
              {used > 0 ? `Usando ${formatMoney(used)} do cheque especial` : 'Sem uso do cheque especial'}
            </span>
          </div>

          <div className={styles.figure}>
            <span className={styles.figureLabel}>Limite do cheque especial</span>
            <Money value={account.overdraftLimit} size="lg" />
            <OverdraftMeter account={account} />
          </div>

          <div className={`${styles.figure} ${styles.figureHighlight}`}>
            <span className={styles.figureLabel}>Disponível para transferência</span>
            <Money value={account.availableBalance} size="lg" />
            <span className={styles.figureNote}>Saldo + cheque especial</span>
          </div>
        </div>
      </section>

      <Card
        title="Extrato"
        description="Transferências enviadas e recebidas, das mais recentes para as mais antigas."
        padding="flush"
      >
        {statement.length === 0 ? (
          <EmptyState icon="transfer" title="Nenhuma transferência" description="Esta conta ainda não enviou nem recebeu transferências." />
        ) : (
          <TransferTable transfers={statement} accounts={accounts} perspectiveAccountId={account.id} />
        )}
      </Card>
    </div>
  );
}

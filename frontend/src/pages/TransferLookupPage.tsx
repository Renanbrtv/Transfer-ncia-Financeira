import { useEffect, useState, type FormEvent } from 'react';
import { toApiError, type ApiError } from '../api/errors';
import { transfersApi } from '../api/transfers';
import { PageHeader } from '../components/layout/PageHeader';
import { TransferFacts } from '../components/transfers/TransferFacts';
import { TransferTimeline } from '../components/transfers/TransferTimeline';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { ConfirmDialog } from '../components/ui/ConfirmDialog';
import { EmptyState } from '../components/ui/EmptyState';
import { ErrorState } from '../components/ui/ErrorState';
import { Money } from '../components/ui/Money';
import { Skeleton } from '../components/ui/Skeleton';
import { TransferStatusBadge } from '../components/ui/StatusBadge';
import { TextInput } from '../components/ui/TextInput';
import { useAccounts } from '../hooks/useAccounts';
import { useAsync } from '../hooks/useAsync';
import { formatMoney } from '../lib/format';
import { isTransferId } from '../lib/validation';
import styles from './TransferLookupPage.module.css';

interface TransferLookupPageProps {
  transferId?: string;
  onSearch: (transferId: string) => void;
}

export function TransferLookupPage({ transferId, onSearch }: TransferLookupPageProps) {
  const [query, setQuery] = useState(transferId ?? '');
  const [queryError, setQueryError] = useState<string>();
  const [confirmingCancel, setConfirmingCancel] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [cancelError, setCancelError] = useState<ApiError | null>(null);
  const [cancelled, setCancelled] = useState(false);

  const validId = transferId && isTransferId(transferId) ? transferId : null;
  const transfer = useAsync(validId ? () => transfersApi.get(validId) : null, [validId]);
  const accounts = useAccounts();

  useEffect(() => {
    setQuery(transferId ?? '');
    setCancelError(null);
    setCancelled(false);
  }, [transferId]);

  const handleSearch = (event: FormEvent) => {
    event.preventDefault();
    const id = query.trim();
    if (!isTransferId(id)) {
      setQueryError('Informe um Id válido, no formato 00000000-0000-0000-0000-000000000000.');
      return;
    }
    setQueryError(undefined);
    if (id === transferId) transfer.reload();
    else onSearch(id);
  };

  const confirmCancel = async () => {
    if (!validId) return;
    setCancelling(true);
    setCancelError(null);
    try {
      await transfersApi.cancel(validId);
      setCancelled(true);
    } catch (reason) {
      setCancelError(toApiError(reason));
    } finally {
      setCancelling(false);
      setConfirmingCancel(false);
      // Recarrega em qualquer caso: se o worker executou a transferência, mostra o estado real.
      transfer.reload();
    }
  };

  const current = transfer.data;
  const invalidRouteId = Boolean(transferId) && !validId;

  return (
    <>
      <PageHeader
        title="Consultar transferência"
        description="Acompanhe o status de uma transferência e cancele agendamentos que ainda não foram executados."
      />

      <Card className={styles.searchCard}>
        <form className={styles.search} onSubmit={handleSearch} role="search" noValidate>
          <TextInput
            id="transfer-id"
            label="Id da transferência"
            className="mono"
            placeholder="00000000-0000-0000-0000-000000000000"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            error={queryError ?? (invalidRouteId ? 'O Id informado na URL não é válido.' : undefined)}
            autoComplete="off"
            spellCheck={false}
          />
          <Button type="submit" icon="search" loading={transfer.loading} className={styles.searchButton}>
            Consultar
          </Button>
        </form>
      </Card>

      {!validId ? (
        <Card>
          <EmptyState
            icon="search"
            title="Nenhuma transferência selecionada"
            description="O Id aparece no comprovante após cada operação e no extrato das contas."
          />
        </Card>
      ) : transfer.error ? (
        transfer.error.kind === 'notFound' ? (
          <Card>
            <EmptyState
              icon="search"
              title="Transferência não encontrada"
              description="Confira o Id informado. Ele deve ser exatamente igual ao exibido no comprovante."
            />
          </Card>
        ) : (
          <ErrorState error={transfer.error} onRetry={transfer.reload} retrying={transfer.loading} variant="page" />
        )
      ) : !current ? (
        <Card>
          <div className={styles.loading} role="status" aria-label="Carregando transferência">
            <Skeleton width={180} height={28} />
            <Skeleton height={14} width="60%" />
            <Skeleton height={14} width="80%" />
            <Skeleton height={14} width="70%" />
          </div>
        </Card>
      ) : (
        <div className={styles.result}>
          {cancelled && current.status === 'Cancelled' && (
            <Alert tone="success" title="Agendamento cancelado">
              A transferência não será executada e nenhum valor foi movimentado.
            </Alert>
          )}
          {cancelError && (
            <Alert tone="error" title="Não foi possível cancelar">
              {cancelError.isConnectivity ? 'Não foi possível conectar ao servidor. Tente novamente.' : cancelError.message}
            </Alert>
          )}

          <div className={styles.detailGrid}>
            <Card
              title="Detalhes"
              actions={
                current.status === 'Scheduled' && (
                  <Button variant="danger" icon="close" onClick={() => setConfirmingCancel(true)}>
                    Cancelar agendamento
                  </Button>
                )
              }
            >
              <div className={styles.headline}>
                <Money value={current.amount} size="xl" />
                <TransferStatusBadge status={current.status} />
              </div>
              {current.failureMessage && (
                <div className={styles.failure}>
                  <Alert tone="error" title="Motivo da recusa">
                    {current.failureMessage} <span className="mono">({current.failureCode})</span>
                  </Alert>
                </div>
              )}
              <TransferFacts transfer={current} accounts={accounts.data ?? []} />
            </Card>

            <Card title="Histórico">
              <TransferTimeline transfer={current} />
            </Card>
          </div>
        </div>
      )}

      {current && (
        <ConfirmDialog
          open={confirmingCancel}
          title="Cancelar agendamento?"
          confirmLabel="Cancelar agendamento"
          cancelLabel="Manter agendamento"
          tone="danger"
          busy={cancelling}
          onConfirm={() => void confirmCancel()}
          onCancel={() => setConfirmingCancel(false)}
        >
          A transferência de <strong className="num">{formatMoney(current.amount)}</strong> não será executada. Esta ação
          não pode ser desfeita.
        </ConfirmDialog>
      )}
    </>
  );
}

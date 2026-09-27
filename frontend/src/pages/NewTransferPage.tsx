import { useState, type FormEvent } from 'react';
import { transfersApi } from '../api/transfers';
import { PageHeader } from '../components/layout/PageHeader';
import { TransferFields } from '../components/transfers/TransferFields';
import { TransferReceipt } from '../components/transfers/TransferReceipt';
import { TransferSummary } from '../components/transfers/TransferSummary';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { ErrorState } from '../components/ui/ErrorState';
import { FormSkeleton } from '../components/ui/FormSkeleton';
import { useAccounts } from '../hooks/useAccounts';
import { useTransferSubmission } from '../hooks/useTransferSubmission';
import { centsToAmount } from '../lib/money';
import { hasErrors, validateTransfer, type FormErrors, type TransferFormValues } from '../lib/validation';
import styles from './OperationPage.module.css';

const EMPTY: TransferFormValues = { sourceId: '', destinationId: '', amountCents: 0 };

export function NewTransferPage() {
  const accounts = useAccounts();
  const { state, submit, reset } = useTransferSubmission(transfersApi.create);
  const [values, setValues] = useState<TransferFormValues>(EMPTY);
  const [showErrors, setShowErrors] = useState(false);

  const errors: FormErrors<TransferFormValues> = showErrors ? validateTransfer(values) : {};
  const submitting = state.status === 'submitting';
  const accountList = accounts.data ?? [];
  const source = accountList.find((account) => String(account.id) === values.sourceId);
  const destination = accountList.find((account) => String(account.id) === values.destinationId);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setShowErrors(true);
    if (hasErrors(validateTransfer(values))) return;

    await submit({
      sourceAccountId: Number(values.sourceId),
      destinationAccountId: Number(values.destinationId),
      amount: centsToAmount(values.amountCents),
    });
    accounts.reload();
  };

  const startNew = () => {
    reset();
    setValues(EMPTY);
    setShowErrors(false);
  };

  return (
    <>
      <PageHeader
        title="Nova transferência"
        description="Transferência imediata entre contas. O débito e o crédito acontecem na mesma operação."
      />

      {state.status === 'done' ? (
        <TransferReceipt transfer={state.transfer} accounts={accountList} onNew={startNew} newLabel="Nova transferência" />
      ) : accounts.error && !accounts.data ? (
        <ErrorState error={accounts.error} onRetry={accounts.reload} retrying={accounts.loading} variant="page" />
      ) : (
        <div className={styles.layout}>
          <Card title="Dados da transferência">
            {!accounts.data ? (
              <FormSkeleton />
            ) : (
              <form className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
                <TransferFields
                  idPrefix="transfer"
                  accounts={accountList}
                  values={values}
                  errors={errors}
                  disabled={submitting}
                  onChange={setValues}
                />

                {state.status === 'error' && (
                  <Alert
                    tone="error"
                    title={state.error.isConnectivity ? 'Não foi possível conectar ao servidor' : 'Não foi possível concluir'}
                  >
                    {state.error.isConnectivity
                      ? 'Nenhum valor foi movimentado. Tente novamente; a operação não será duplicada.'
                      : state.error.message}
                  </Alert>
                )}

                <div className={styles.actions}>
                  <Button type="submit" size="lg" block loading={submitting} icon="transfer">
                    {submitting ? 'Processando transferência…' : 'Realizar transferência'}
                  </Button>
                  <p className={styles.formFooter}>Ao confirmar, o valor é debitado imediatamente da conta de origem.</p>
                </div>
              </form>
            )}
          </Card>

          <div className={styles.summaryColumn}>
            <TransferSummary
              source={source}
              destination={destination}
              amount={centsToAmount(values.amountCents)}
              mode="immediate"
            />
          </div>
        </div>
      )}
    </>
  );
}

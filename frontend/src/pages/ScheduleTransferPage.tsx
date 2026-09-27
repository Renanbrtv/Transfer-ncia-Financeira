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
import { TextInput } from '../components/ui/TextInput';
import { useAccounts } from '../hooks/useAccounts';
import { useTransferSubmission } from '../hooks/useTransferSubmission';
import { suggestedScheduleDate, toDateInputValue, toTimeInputValue } from '../lib/dates';
import { centsToAmount } from '../lib/money';
import {
  combineLocalDateTime,
  hasErrors,
  validateSchedule,
  type FormErrors,
  type ScheduleFormValues,
} from '../lib/validation';
import styles from './OperationPage.module.css';

function initialValues(): ScheduleFormValues {
  const suggestion = suggestedScheduleDate(new Date());
  return {
    sourceId: '',
    destinationId: '',
    amountCents: 0,
    date: toDateInputValue(suggestion),
    time: toTimeInputValue(suggestion),
  };
}

export function ScheduleTransferPage() {
  const accounts = useAccounts();
  const { state, submit, reset } = useTransferSubmission(transfersApi.schedule);
  const [values, setValues] = useState<ScheduleFormValues>(initialValues);
  const [showErrors, setShowErrors] = useState(false);

  const errors: FormErrors<ScheduleFormValues> = showErrors ? validateSchedule(values, new Date()) : {};
  const submitting = state.status === 'submitting';
  const accountList = accounts.data ?? [];
  const source = accountList.find((account) => String(account.id) === values.sourceId);
  const destination = accountList.find((account) => String(account.id) === values.destinationId);
  const scheduledFor = combineLocalDateTime(values.date, values.time);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setShowErrors(true);
    if (hasErrors(validateSchedule(values, new Date())) || !scheduledFor) return;

    await submit({
      sourceAccountId: Number(values.sourceId),
      destinationAccountId: Number(values.destinationId),
      amount: centsToAmount(values.amountCents),
      scheduledFor: scheduledFor.toISOString(),
    });
  };

  const startNew = () => {
    reset();
    setValues(initialValues());
    setShowErrors(false);
  };

  return (
    <>
      <PageHeader
        title="Agendar transferência"
        description="Programe uma transferência para uma data futura. Ela pode ser cancelada enquanto estiver agendada."
      />

      {state.status === 'done' ? (
        <TransferReceipt transfer={state.transfer} accounts={accountList} onNew={startNew} newLabel="Novo agendamento" />
      ) : accounts.error && !accounts.data ? (
        <ErrorState error={accounts.error} onRetry={accounts.reload} retrying={accounts.loading} variant="page" />
      ) : (
        <div className={styles.layout}>
          <Card title="Dados do agendamento">
            {!accounts.data ? (
              <FormSkeleton fields={4} />
            ) : (
              <form className={styles.form} onSubmit={(event) => void handleSubmit(event)} noValidate>
                <TransferFields
                  idPrefix="schedule"
                  accounts={accountList}
                  values={values}
                  errors={errors}
                  disabled={submitting}
                  onChange={(transferValues) => setValues({ ...values, ...transferValues })}
                />

                <div className={styles.dateRow}>
                  <TextInput
                    id="schedule-date"
                    label="Data"
                    type="date"
                    value={values.date}
                    min={toDateInputValue(new Date())}
                    onChange={(event) => setValues({ ...values, date: event.target.value })}
                    error={errors.date}
                    disabled={submitting}
                  />
                  <TextInput
                    id="schedule-time"
                    label="Hora"
                    type="time"
                    value={values.time}
                    onChange={(event) => setValues({ ...values, time: event.target.value })}
                    error={errors.time}
                    hint="Horário do seu navegador."
                    disabled={submitting}
                  />
                </div>

                {state.status === 'error' && (
                  <Alert
                    tone="error"
                    title={state.error.isConnectivity ? 'Não foi possível conectar ao servidor' : 'Não foi possível agendar'}
                  >
                    {state.error.isConnectivity
                      ? 'O agendamento não foi registrado. Tente novamente; ele não será duplicado.'
                      : state.error.message}
                  </Alert>
                )}

                <div className={styles.actions}>
                  <Button type="submit" size="lg" block loading={submitting} icon="calendar">
                    {submitting ? 'Agendando…' : 'Agendar transferência'}
                  </Button>
                  <p className={styles.formFooter}>Nenhum valor é movimentado agora; a execução ocorre na data programada.</p>
                </div>
              </form>
            )}
          </Card>

          <div className={styles.summaryColumn}>
            <TransferSummary
              source={source}
              destination={destination}
              amount={centsToAmount(values.amountCents)}
              mode="scheduled"
              scheduledFor={scheduledFor}
            />
          </div>
        </div>
      )}
    </>
  );
}

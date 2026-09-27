/**
 * Validação de formulário no cliente: evita requisições que certamente seriam recusadas com 400.
 * As regras de negócio (saldo, limites, status das contas) continuam sendo decididas pela API.
 */

export interface TransferFormValues {
  sourceId: string;
  destinationId: string;
  amountCents: number;
}

export interface ScheduleFormValues extends TransferFormValues {
  date: string;
  time: string;
}

export type FormErrors<T> = Partial<Record<keyof T, string>>;

export function validateTransfer(values: TransferFormValues): FormErrors<TransferFormValues> {
  const errors: FormErrors<TransferFormValues> = {};

  if (!values.sourceId) errors.sourceId = 'Selecione a conta de origem.';
  if (!values.destinationId) errors.destinationId = 'Selecione a conta de destino.';
  if (values.sourceId && values.sourceId === values.destinationId) {
    errors.destinationId = 'A conta de destino deve ser diferente da conta de origem.';
  }
  if (values.amountCents <= 0) errors.amountCents = 'Informe um valor maior que zero.';

  return errors;
}

export function validateSchedule(values: ScheduleFormValues, now: Date): FormErrors<ScheduleFormValues> {
  const errors: FormErrors<ScheduleFormValues> = validateTransfer(values);

  if (!values.date) {
    errors.date = 'Informe a data.';
  }
  if (!values.time) {
    errors.time = 'Informe a hora.';
  }
  if (values.date && values.time) {
    const scheduledFor = combineLocalDateTime(values.date, values.time);
    if (!scheduledFor || scheduledFor.getTime() <= now.getTime()) {
      errors.time = 'A data e a hora devem estar no futuro.';
    }
  }

  return errors;
}

export function hasErrors<T>(errors: FormErrors<T>): boolean {
  return Object.values(errors).some(Boolean);
}

/** "2030-01-15" + "14:30" no fuso do navegador. */
export function combineLocalDateTime(date: string, time: string): Date | null {
  const result = new Date(`${date}T${time}`);
  return Number.isNaN(result.getTime()) ? null : result;
}

const GUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function isTransferId(value: string): boolean {
  return GUID_PATTERN.test(value.trim());
}

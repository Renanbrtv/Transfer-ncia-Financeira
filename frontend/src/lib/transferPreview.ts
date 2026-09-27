import type { Account } from '../api/types';

export interface PreviewWarning {
  tone: 'warning' | 'error';
  message: string;
}

/**
 * Avisos mostrados no resumo antes de confirmar. São apenas orientação: a decisão final é da API,
 * que reavalia saldo, status e limites dentro de uma transação.
 */
export function previewWarnings(
  source: Account | undefined,
  destination: Account | undefined,
  amount: number,
  mode: 'immediate' | 'scheduled',
): PreviewWarning[] {
  const warnings: PreviewWarning[] = [];

  if (source && source.status !== 'Active') {
    warnings.push({ tone: 'error', message: 'A conta de origem não está ativa e não pode enviar transferências.' });
  }
  if (destination && destination.status !== 'Active') {
    warnings.push({ tone: 'error', message: 'A conta de destino não está ativa e não pode receber transferências.' });
  }
  if (source && amount > 0 && amount > source.availableBalance) {
    warnings.push(
      mode === 'immediate'
        ? { tone: 'error', message: 'O valor excede o saldo disponível (saldo + cheque especial) da conta de origem.' }
        : { tone: 'warning', message: 'Hoje o valor excede o saldo disponível. O saldo será verificado novamente na execução.' },
    );
  }

  return warnings;
}

/** Saldo disponível estimado depois da operação (antes da confirmação da API). */
export function availableAfter(source: Account | undefined, amount: number): number | null {
  return source ? source.availableBalance - amount : null;
}

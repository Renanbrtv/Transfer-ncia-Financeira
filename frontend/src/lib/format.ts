import type { AccountStatus, TransferStatus, TransferType } from '../api/types';

const LOCALE = 'pt-BR';

const currencyFormatter = new Intl.NumberFormat(LOCALE, { style: 'currency', currency: 'BRL' });
const dateTimeFormatter = new Intl.DateTimeFormat(LOCALE, { dateStyle: 'short', timeStyle: 'short' });
const fullDateTimeFormatter = new Intl.DateTimeFormat(LOCALE, {
  weekday: 'long',
  day: '2-digit',
  month: 'long',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});
const timeWithSecondsFormatter = new Intl.DateTimeFormat(LOCALE, { timeStyle: 'medium' });

/** R$ 1.500,00 — e -R$ 500,00 para negativos (o Intl usa o sinal antes do símbolo). */
export function formatMoney(value: number): string {
  return currencyFormatter.format(value);
}

export function formatDateTime(value: string | Date): string {
  return dateTimeFormatter.format(toDate(value));
}

/** "segunda-feira, 15 de janeiro de 2030 às 14:30" */
export function formatLongDateTime(value: string | Date): string {
  return fullDateTimeFormatter.format(toDate(value));
}

export function formatTime(value: string | Date): string {
  return timeWithSecondsFormatter.format(toDate(value));
}

/** Primeiros 8 caracteres do GUID, como exibido em extratos. */
export function shortId(id: string): string {
  return id.slice(0, 8).toUpperCase();
}

function toDate(value: string | Date): Date {
  return value instanceof Date ? value : new Date(value);
}

export const transferStatusLabel: Record<TransferStatus, string> = {
  Scheduled: 'Agendada',
  Processing: 'Processando',
  Completed: 'Concluída',
  Failed: 'Recusada',
  Cancelled: 'Cancelada',
};

export const transferTypeLabel: Record<TransferType, string> = {
  Immediate: 'Imediata',
  Scheduled: 'Agendada',
};

export const accountStatusLabel: Record<AccountStatus, string> = {
  Active: 'Ativa',
  Blocked: 'Bloqueada',
  Inactive: 'Inativa',
};

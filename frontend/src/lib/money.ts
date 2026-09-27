/**
 * Entrada de valores no padrão dos apps bancários: o usuário digita apenas números e eles
 * preenchem a partir dos centavos (1 → 0,01; 150 → 1,50; 150000 → 1.500,00).
 * Trabalhar em centavos inteiros evita erros de ponto flutuante.
 */

const MAX_CENTS = 99_999_999_999; // R$ 999.999.999,99: bem acima de qualquer limite, evita overflow visual

export function centsFromInput(raw: string): number {
  const digits = raw.replace(/\D/g, '').replace(/^0+/, '');
  if (digits === '') return 0;
  return Math.min(Number(digits), MAX_CENTS);
}

const inputFormatter = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** "1.500,00" (sem o símbolo; o "R$" é um prefixo visual do campo). */
export function formatCentsForInput(cents: number): string {
  return inputFormatter.format(cents / 100);
}

/** Valor enviado à API: decimal com no máximo duas casas. */
export function centsToAmount(cents: number): number {
  return Math.round(cents) / 100;
}

export function amountToCents(amount: number): number {
  return Math.round(amount * 100);
}

import { formatMoney } from '../../lib/format';
import styles from './Money.module.css';

interface MoneyProps {
  value: number;
  /** "signed": prefixa + para entradas; "direction" colore entrada/saída. */
  display?: 'plain' | 'signed';
  direction?: 'in' | 'out';
  /** Destaca valores negativos (saldo usando cheque especial). */
  highlightNegative?: boolean;
  /** "inherit" herda tamanho e peso do elemento pai. */
  size?: 'inherit' | 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
}

export function Money({
  value,
  display = 'plain',
  direction,
  highlightNegative = false,
  size = 'md',
  className,
}: MoneyProps) {
  const text =
    display === 'signed' && direction ? `${direction === 'in' ? '+' : '−'} ${formatMoney(Math.abs(value))}` : formatMoney(value);

  const tone =
    direction === 'in' ? styles.in : direction === 'out' ? styles.out : highlightNegative && value < 0 ? styles.negative : '';

  return <span className={['num', styles.money, styles[size], tone, className ?? ''].join(' ')}>{text}</span>;
}

import type { Account } from '../../api/types';
import { overdraftUsage } from '../../lib/dashboard';
import { formatMoney } from '../../lib/format';
import styles from './OverdraftMeter.module.css';

/** Barra de uso do cheque especial: quanto do limite está sendo usado. */
export function OverdraftMeter({ account }: { account: Account }) {
  const { used, remaining, ratio } = overdraftUsage(account);
  const percent = Math.round(ratio * 100);

  if (account.overdraftLimit <= 0) {
    return <p className={styles.caption}>Conta sem limite de cheque especial.</p>;
  }

  return (
    <div className={styles.meter}>
      <div
        className={styles.track}
        role="meter"
        aria-label="Uso do cheque especial"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        aria-valuetext={`${percent}% utilizado`}
      >
        <span
          className={`${styles.fill} ${ratio >= 0.8 ? styles.high : ''}`}
          style={{ width: `${Math.max(percent, used > 0 ? 2 : 0)}%` }}
        />
      </div>
      <div className={styles.legend}>
        <span>
          Utilizado <strong className="num">{formatMoney(used)}</strong>
        </span>
        <span>
          Disponível <strong className="num">{formatMoney(remaining)}</strong>
        </span>
      </div>
    </div>
  );
}

import styles from './Logo.module.css';

/** Marca do TransferFlow: duas setas opostas, representando o fluxo entre contas. */
export function Logo({ compact = false }: { compact?: boolean }) {
  return (
    <span className={styles.logo}>
      <svg className={styles.mark} viewBox="0 0 32 32" aria-hidden="true" focusable="false">
        <rect width="32" height="32" rx="7" fill="currentColor" />
        <path
          d="M8 12h13l-3.5-3.5M24 20H11l3.5 3.5"
          fill="none"
          stroke="#fff"
          strokeWidth="2.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
      </svg>
      <span className={styles.text}>
        <span className={styles.name}>TransferFlow</span>
        {!compact && <span className={styles.tagline}>Gestão de Transferências Financeiras</span>}
      </span>
    </span>
  );
}

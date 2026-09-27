import type { CSSProperties } from 'react';
import styles from './Skeleton.module.css';

export function Skeleton({ width = '100%', height = 14, style }: { width?: number | string; height?: number; style?: CSSProperties }) {
  return <span className={styles.skeleton} style={{ width, height, ...style }} aria-hidden="true" />;
}

/** Linhas de tabela em carregamento, com a mesma altura das linhas reais. */
export function SkeletonRows({ rows = 5, columns }: { rows?: number; columns: number }) {
  return (
    <div className={styles.rows} role="status" aria-label="Carregando">
      {Array.from({ length: rows }, (_, row) => (
        <div key={row} className={styles.row} style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
          {Array.from({ length: columns }, (_, column) => (
            <Skeleton key={column} width={column === 0 ? '70%' : '55%'} />
          ))}
        </div>
      ))}
    </div>
  );
}

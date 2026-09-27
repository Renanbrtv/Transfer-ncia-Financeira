import { useId, type ReactNode } from 'react';
import styles from './Card.module.css';

interface CardProps {
  title?: string;
  description?: ReactNode;
  actions?: ReactNode;
  /** "flush": o conteúdo encosta nas bordas (tabelas). */
  padding?: 'default' | 'flush';
  className?: string;
  children: ReactNode;
}

export function Card({ title, description, actions, padding = 'default', className, children }: CardProps) {
  const hasHeader = Boolean(title || actions);
  const titleId = useId();

  return (
    <section className={[styles.card, className ?? ''].join(' ')} aria-labelledby={title ? titleId : undefined}>
      {hasHeader && (
        <header className={styles.header}>
          <div>
            {title && <h2 id={titleId} className={styles.title}>{title}</h2>}
            {description && <p className={styles.description}>{description}</p>}
          </div>
          {actions && <div className={styles.actions}>{actions}</div>}
        </header>
      )}
      <div className={padding === 'flush' ? styles.flush : styles.body}>{children}</div>
    </section>
  );
}

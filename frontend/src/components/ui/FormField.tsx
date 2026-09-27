import type { ReactNode } from 'react';
import { Icon } from './Icon';
import styles from './FormField.module.css';

export interface FieldProps {
  id: string;
  label: string;
  hint?: ReactNode;
  error?: string;
}

/** Ids usados em aria-describedby para ligar o campo à dica e ao erro. */
export function describedBy({ id, hint, error }: FieldProps): string | undefined {
  const ids = [hint ? `${id}-hint` : null, error ? `${id}-error` : null].filter(Boolean);
  return ids.length > 0 ? ids.join(' ') : undefined;
}

/** Estrutura comum a todos os campos: label, controle, dica e mensagem de validação. */
export function FormField({ id, label, hint, error, children }: FieldProps & { children: ReactNode }) {
  return (
    <div className={styles.field}>
      <label htmlFor={id} className={styles.label}>
        {label}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className={styles.error} role="alert">
          <Icon name="alert" size={14} />
          {error}
        </p>
      ) : (
        hint && (
          <p id={`${id}-hint`} className={styles.hint}>
            {hint}
          </p>
        )
      )}
    </div>
  );
}

export const controlClassName = styles.control;
export const invalidClassName = styles.invalid;

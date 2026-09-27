import type { ChangeEvent } from 'react';
import { centsFromInput, formatCentsForInput } from '../../lib/money';
import { FormField, controlClassName, describedBy, invalidClassName, type FieldProps } from './FormField';
import styles from './MoneyInput.module.css';

interface MoneyInputProps extends FieldProps {
  cents: number;
  onChange: (cents: number) => void;
  disabled?: boolean;
}

/** Campo monetário no padrão bancário: digita-se apenas números, preenchidos a partir dos centavos. */
export function MoneyInput({ cents, onChange, disabled, ...field }: MoneyInputProps) {
  const handleChange = (event: ChangeEvent<HTMLInputElement>) => onChange(centsFromInput(event.target.value));

  return (
    <FormField {...field}>
      <div className={styles.wrapper}>
        <span className={styles.prefix} aria-hidden="true">
          R$
        </span>
        <input
          id={field.id}
          className={[controlClassName, styles.input, 'num', field.error ? invalidClassName : ''].join(' ')}
          inputMode="numeric"
          autoComplete="off"
          value={formatCentsForInput(cents)}
          onChange={handleChange}
          onFocus={(event) => event.target.select()}
          disabled={disabled}
          aria-invalid={field.error ? true : undefined}
          aria-describedby={describedBy(field)}
        />
      </div>
    </FormField>
  );
}

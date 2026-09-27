import type { InputHTMLAttributes } from 'react';
import { FormField, controlClassName, describedBy, invalidClassName, type FieldProps } from './FormField';

type TextInputProps = FieldProps & Omit<InputHTMLAttributes<HTMLInputElement>, 'id'>;

export function TextInput({ id, label, hint, error, className, ...input }: TextInputProps) {
  const field = { id, label, hint, error };

  return (
    <FormField {...field}>
      <input
        id={id}
        className={[controlClassName, error ? invalidClassName : '', className ?? ''].join(' ')}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy(field)}
        {...input}
      />
    </FormField>
  );
}

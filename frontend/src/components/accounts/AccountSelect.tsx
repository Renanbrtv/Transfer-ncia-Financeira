import type { Account } from '../../api/types';
import { accountStatusLabel, formatMoney } from '../../lib/format';
import { FormField, controlClassName, describedBy, invalidClassName, type FieldProps } from '../ui/FormField';
import styles from './AccountSelect.module.css';

interface AccountSelectProps extends FieldProps {
  accounts: Account[];
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  /** Conta que não pode ser escolhida (ex.: a origem, no campo de destino). */
  excludeId?: string;
}

/**
 * Seleção de conta. Contas bloqueadas e inativas continuam listadas (identificadas) porque a regra
 * de status é da API: a interface mostra o motivo da recusa em vez de esconder a opção.
 */
export function AccountSelect({ accounts, value, onChange, disabled, excludeId, ...field }: AccountSelectProps) {
  return (
    <FormField {...field}>
      <select
        id={field.id}
        className={[controlClassName, styles.select, field.error ? invalidClassName : ''].join(' ')}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        aria-invalid={field.error ? true : undefined}
        aria-describedby={describedBy(field)}
      >
        <option value="">Selecione uma conta</option>
        {accounts.map((account) => (
          <option key={account.id} value={account.id} disabled={String(account.id) === excludeId}>
            {account.holderName} · conta {account.id}
            {account.status === 'Active'
              ? ` · disponível ${formatMoney(account.availableBalance)}`
              : ` · ${accountStatusLabel[account.status].toLowerCase()}`}
          </option>
        ))}
      </select>
    </FormField>
  );
}

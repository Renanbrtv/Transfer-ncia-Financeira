import type { Account } from '../../api/types';
import type { FormErrors, TransferFormValues } from '../../lib/validation';
import { AccountSelect } from '../accounts/AccountSelect';
import { MoneyInput } from '../ui/MoneyInput';
import styles from './TransferFields.module.css';

interface TransferFieldsProps {
  idPrefix: string;
  accounts: Account[];
  values: TransferFormValues;
  errors: FormErrors<TransferFormValues>;
  disabled: boolean;
  onChange: (values: TransferFormValues) => void;
}

/** Campos comuns à transferência imediata e à agendada. */
export function TransferFields({ idPrefix, accounts, values, errors, disabled, onChange }: TransferFieldsProps) {
  return (
    <div className={styles.fields}>
      <div className={styles.accounts}>
        <AccountSelect
          id={`${idPrefix}-source`}
          label="Conta de origem"
          accounts={accounts}
          value={values.sourceId}
          onChange={(sourceId) => onChange({ ...values, sourceId })}
          error={errors.sourceId}
          disabled={disabled}
        />
        <AccountSelect
          id={`${idPrefix}-destination`}
          label="Conta de destino"
          accounts={accounts}
          value={values.destinationId}
          onChange={(destinationId) => onChange({ ...values, destinationId })}
          excludeId={values.sourceId}
          error={errors.destinationId}
          disabled={disabled}
        />
      </div>
      <MoneyInput
        id={`${idPrefix}-amount`}
        label="Valor"
        cents={values.amountCents}
        onChange={(amountCents) => onChange({ ...values, amountCents })}
        error={errors.amountCents}
        hint="Digite apenas números; os dois últimos dígitos são os centavos."
        disabled={disabled}
      />
    </div>
  );
}

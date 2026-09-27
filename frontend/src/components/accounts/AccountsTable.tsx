import type { Account } from '../../api/types';
import { routeHref } from '../../hooks/useRoute';
import { Money } from '../ui/Money';
import { AccountStatusBadge } from '../ui/StatusBadge';
import table from '../ui/DataTable.module.css';

export function AccountsTable({ accounts }: { accounts: Account[] }) {
  return (
    <>
      <AccountRows accounts={accounts} />
      <AccountList accounts={accounts} />
    </>
  );
}

function AccountRows({ accounts }: { accounts: Account[] }) {
  return (
    <div className={`${table.wrapper} ${table.desktopOnly}`}>
      <table className={table.table}>
        <thead>
          <tr>
            <th scope="col">Titular</th>
            <th scope="col">Status</th>
            <th scope="col" className={table.numeric}>
              Saldo
            </th>
            <th scope="col" className={table.numeric}>
              Cheque especial
            </th>
            <th scope="col" className={table.numeric}>
              Disponível
            </th>
          </tr>
        </thead>
        <tbody>
          {accounts.map((account) => (
            <tr key={account.id}>
              <td>
                <a href={routeHref({ name: 'account', param: String(account.id) })} className={table.primaryLink}>
                  {account.holderName}
                </a>
                <span className={table.secondary}>Conta {account.id}</span>
              </td>
              <td>
                <AccountStatusBadge status={account.status} />
              </td>
              <td className={table.numeric}>
                <Money value={account.balance} highlightNegative />
              </td>
              <td className={table.numeric}>
                <Money value={account.overdraftLimit} className={table.muted} />
              </td>
              <td className={table.numeric}>
                <Money value={account.availableBalance} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function AccountList({ accounts }: { accounts: Account[] }) {
  return (
    <ul className={`${table.list} ${table.mobileOnly}`}>
      {accounts.map((account) => (
        <li key={account.id}>
          <a href={routeHref({ name: 'account', param: String(account.id) })} className={table.listItem}>
            <span className={table.listMain}>
              <span className={table.listTitle}>{account.holderName}</span>
              <span className={table.listAmount}>
                <span className={table.listAmountLabel}>Disponível</span>
                <Money value={account.availableBalance} />
              </span>
            </span>
            <span className={table.listMeta}>
              <AccountStatusBadge status={account.status} />
              <span>
                Saldo <Money value={account.balance} size="sm" highlightNegative />
              </span>
            </span>
          </a>
        </li>
      ))}
    </ul>
  );
}
